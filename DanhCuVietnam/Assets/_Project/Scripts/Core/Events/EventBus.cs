using System;
using System.Collections.Generic;
using DanhCu.Core.Diagnostics;

namespace DanhCu.Core.Events
{
    /// <summary>
    /// Typed publish/subscribe bus for decoupled, low-frequency game events
    /// (state changes, match start/end, settings changes).
    ///
    /// Not for per-frame or per-physics-step data: read that directly from the owning system.
    /// Main-thread only. Publishing does not allocate; subscribing copies the handler array.
    /// A throwing handler is logged and does not stop delivery to other handlers.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate[]> _handlers = new Dictionary<Type, Delegate[]>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type key = typeof(T);
            _handlers.TryGetValue(key, out Delegate[] existing);
            existing = existing ?? Array.Empty<Delegate>();

            var updated = new Delegate[existing.Length + 1];
            Array.Copy(existing, updated, existing.Length);
            updated[existing.Length] = handler;
            _handlers[key] = updated;

            return new Subscription(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            Type key = typeof(T);
            if (handler == null || !_handlers.TryGetValue(key, out Delegate[] existing))
            {
                return;
            }

            int index = Array.IndexOf(existing, handler);
            if (index < 0)
            {
                return;
            }

            if (existing.Length == 1)
            {
                _handlers.Remove(key);
                return;
            }

            var updated = new Delegate[existing.Length - 1];
            Array.Copy(existing, 0, updated, 0, index);
            Array.Copy(existing, index + 1, updated, index, existing.Length - index - 1);
            _handlers[key] = updated;
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out Delegate[] handlers))
            {
                return;
            }

            // The array is never mutated in place, so (un)subscribing during delivery is safe.
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action<T>)handlers[i])(evt);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        public int SubscriberCount<T>()
        {
            return _handlers.TryGetValue(typeof(T), out Delegate[] handlers) ? handlers.Length : 0;
        }

        public void Clear() => _handlers.Clear();

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
