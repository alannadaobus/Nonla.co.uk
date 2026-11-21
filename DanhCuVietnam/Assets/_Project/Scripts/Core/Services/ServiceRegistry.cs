using System;
using System.Collections.Generic;

namespace DanhCu.Core.Services
{
    /// <summary>
    /// Explicit registry for long-lived services, owned by the GameManager.
    /// Services are registered once at boot by interface type. Prefer constructor
    /// injection; use the registry only at composition boundaries (scene entry points).
    /// </summary>
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (_services.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Service '{typeof(T).Name}' is already registered.");
            }

            _services.Add(typeof(T), service);
        }

        public bool Unregister<T>() where T : class => _services.Remove(typeof(T));

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new KeyNotFoundException($"Service '{typeof(T).Name}' is not registered.");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));

        public void Clear() => _services.Clear();
    }
}
