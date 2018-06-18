using Acquaintance.Logging;
using System;
using System.Collections.Concurrent;
using Acquaintance.Utility;

namespace Acquaintance.Modules
{
    public class ModuleManager : IDisposable, IModuleManager
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IMessageBusModule> _modules;

        public ModuleManager(ILogger logger)
        {
            _logger = logger;
            _modules = new ConcurrentDictionary<string, IMessageBusModule>();
        }

        public IDisposable Add<TModule>(TModule module)
            where TModule : class, IMessageBusModule
        {
            var key = GetKey<TModule>();
            if (_modules.ContainsKey(key))
                throw new Exception($"A module of type {typeof(TModule).FullName} has already been added.");

            _logger.Debug($"Adding module Type={module.GetType().Name}");
            if (!_modules.TryAdd(key, module))
            {
                _logger.Error($"Could not add module of type {module.GetType().Name} for unknown reasons");
                throw new Exception($"Could not add module of type {typeof(TModule).FullName}. You may be trying to add another copy of it on a separate thread");
            }

            _logger.Debug($"Starting module Type={module.GetType().Name}");
            module.Start();
            return new ModuleToken(this, key, module.GetType().Name);
        }

        public TModule Get<TModule>()
            where TModule : class, IMessageBusModule
        {
            var key = GetKey<TModule>();
            bool ok = _modules.TryGetValue(key, out IMessageBusModule module);
            if (!ok || module == null)
                return null;
            if (!(module is TModule typedModule))
                throw new Exception($"Module type mismatch. Expected {typeof(TModule).FullName} but found {module.GetType().FullName}");
            return typedModule;
        }

        public void Dispose()
        {
            foreach (var module in _modules.Values)
            {
                module.Stop();
                ObjectManagement.TryDispose(module);
            }
            _modules.Clear();
        }

        private void RemoveModule(string key)
        {
            _modules.TryRemove(key, out IMessageBusModule module);
            _logger.Debug($"Stopping module Type={module.GetType().Name}");
            module.Stop();
        }

        private static string GetKey<TModule>()
        {
            return typeof(TModule).FullName;
        }

        private class ModuleToken : DisposeOnceToken
        {
            private readonly ModuleManager _manager;
            private readonly string _key;
            private readonly string _moduleName;

            public ModuleToken(ModuleManager manager, string key, string moduleName)
            {
                _manager = manager;
                _key = key;
                _moduleName = moduleName;
            }

            protected override void Dispose(bool disposing)
            {
                _manager.RemoveModule(_key);
            }

            public override string ToString()
            {
                return $"Module Name={_moduleName}";
            }
        }
    }
}
