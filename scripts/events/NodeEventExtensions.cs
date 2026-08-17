using Godot;
using System;

namespace Circumlink.Events;

public static class NodeEventExtensions
{
    extension(Node node)
    {
        /// <summary>
        /// Subscribes to an event of type T, automatically unsubscribing when the node exits the tree.
        /// </summary>
        public void SubscribeEvent<T>(Action<T> handler)
            where T: IEvent
        {
            EventHub.Instance.Subscribe(handler);
            node.TreeExiting += () => EventHub.Instance.Unsubscribe(handler);
        }
    }
}
