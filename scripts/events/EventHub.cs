
using System;
using System.Collections.Generic;
using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Events;

// I don't know whether to add this as a singleton or a child node
public partial class EventHub : Node
{
    public static EventHub Instance { get; private set; }

    private readonly Dictionary<Type, List<Delegate>> _subscriptions = [];
    private readonly ILogger<EventHub> _logger = Debug.Log.GetLogger<EventHub>();

    public override void _Ready()
    {
        Instance = this;
        _logger.LogInformation("EventHub ready.");
    }

    public void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var eventType = typeof(T);
        if (!_subscriptions.ContainsKey(eventType))
        {
            _subscriptions[eventType] = new List<Delegate>();
        }
        _subscriptions[eventType].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        Type eventType = typeof(T);
        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _subscriptions.Remove(eventType);
            }
        }
    }

    public void Publish<T>(T eventData) where T : IEvent
    {
        Type eventType = typeof(T);
        if (!_subscriptions.TryGetValue(eventType, out var handlers))
            return;

        // IMPORTANT: Copy the list to prevent modification exceptions
        // (in case a handler tries to unsubscribe during the publish)
        var handlersCopy = new List<Delegate>(handlers);

        foreach (var handler in handlersCopy)
        {
            // Safe cast – we know it's Action<T> because of how we store it
            if (handler is Action<T> action)
            {
                action.Invoke(eventData);
            }
        }
    }
}
