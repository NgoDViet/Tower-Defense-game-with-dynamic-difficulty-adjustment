using System;
using System.Collections.Generic;

namespace TowerDefense.Core
{
    /// <summary>
    /// A lightweight, static, type-safe Event Bus.
    /// Uses static generics so subscription lists are segregated by event type,
    /// avoiding runtime dictionary lookups and boxing.
    /// </summary>
    /// <typeparam name="T">The event type to listen to.</typeparam>
    public static class EventBus<T>
    {
        private static readonly List<Action<T>> _listeners = new List<Action<T>>();

        /// <summary>
        /// Registers a listener for this event type.
        /// </summary>
        public static void Subscribe(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener for this event type.
        /// </summary>
        public static void Unsubscribe(Action<T> listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Dispatches the event to all registered listeners.
        /// Iterates backwards to allow safe unsubscription during invocation.
        /// </summary>
        public static void Raise(T eventData)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count)
                {
                    try
                    {
                        _listeners[i]?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[EventBus] Error executing event listener for {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }
    }
}
