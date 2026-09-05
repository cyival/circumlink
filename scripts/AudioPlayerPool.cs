using System.Collections.Generic;
using Godot;

namespace Circumlink;

/// <summary>
/// A least-recently-used pool of AudioStreamPlayer nodes, keyed by AudioStream
/// resource. It supports overlapping one-shots of the same stream and prefers
/// evicting idle players when the pool is full.
/// </summary>
public sealed class AudioPlayerPool
{
    private readonly Node _owner;
    private readonly StringName _busName;

    private readonly Dictionary<AudioStream, List<AudioStreamPlayer>> _playersByStream = [];
    private readonly LinkedList<AudioStreamPlayer> _lru = [];
    private readonly Dictionary<AudioStreamPlayer, LinkedListNode<AudioStreamPlayer>> _lruNodes = [];

    public ushort MaxPlayers { get; set; }

    public AudioPlayerPool(Node owner, StringName busName, ushort maxPlayers)
    {
        _owner = owner;
        _busName = busName;
        MaxPlayers = maxPlayers;
    }

    public void Play(AudioStream stream)
    {
        if (stream is null)
            return;

        // Pooling disabled: create a transient player that frees itself.
        if (MaxPlayers == 0)
        {
            PlayOneShot(stream);
            return;
        }

        // Reuse an idle player already assigned to this stream.
        if (_playersByStream.TryGetValue(stream, out var players))
        {
            foreach (var player in players)
            {
                if (!player.Playing)
                {
                    TouchLru(player);
                    player.Play();
                    return;
                }
            }
        }

        AudioStreamPlayer playerToUse;
        if (_lru.Count < MaxPlayers)
        {
            // Pool still has room: create and cache a new player.
            playerToUse = CreatePlayer(stream);
        }
        else
        {
            // Pool is full: prefer evicting an idle player so active one-shots
            // are not cut short. If every player is active, evict the LRU one.
            playerToUse = FindBestEvictionCandidate();
            Evict(playerToUse);
            playerToUse.Stop();
            playerToUse.Stream = stream;
        }

        AddToCache(playerToUse, stream);
        playerToUse.Play();
    }

    public void StopAll()
    {
        foreach (var player in _lru)
            player.Stop();
    }

    private AudioStreamPlayer CreatePlayer(AudioStream stream)
    {
        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = _busName
        };
        _owner.AddChild(player);
        return player;
    }

    private void PlayOneShot(AudioStream stream)
    {
        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = _busName
        };
        _owner.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }

    private AudioStreamPlayer FindBestEvictionCandidate()
    {
        for (var node = _lru.First; node is not null; node = node.Next)
        {
            if (!node.Value.Playing)
                return node.Value;
        }

        return _lru.First!.Value;
    }

    private void AddToCache(AudioStreamPlayer player, AudioStream stream)
    {
        if (!_playersByStream.TryGetValue(stream, out var players))
        {
            players = [];
            _playersByStream[stream] = players;
        }

        players.Add(player);
        _lruNodes[player] = _lru.AddLast(player);
    }

    private void Evict(AudioStreamPlayer player)
    {
        var oldStream = player.Stream;
        if (oldStream is not null && _playersByStream.TryGetValue(oldStream, out var players))
        {
            players.Remove(player);
            if (players.Count == 0)
                _playersByStream.Remove(oldStream);
        }

        if (_lruNodes.Remove(player, out var node))
            _lru.Remove(node);
    }

    private void TouchLru(AudioStreamPlayer player)
    {
        if (_lruNodes.Remove(player, out var node))
        {
            _lru.Remove(node);
            _lruNodes[player] = _lru.AddLast(player);
        }
    }
}
