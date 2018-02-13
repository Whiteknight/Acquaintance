using Acquaintance.Logging;
using System;
using System.Collections.Concurrent;

namespace Acquaintance.Modules
{
    public class ModuleManager : IDisposable, IModuleManager
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IMessageBusModule> _modules;

        public ModuleManager(IMessageBus messageBus, ILogger logger)
        {
            _messageBus = messageBus;
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
            module.Attach(_messageBus);
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
            var typedModule = module as TModule;
            if (typedModule == null)
                throw new Exception($"Module type mismatch. Expected {typeof(TModule).FullName} but found {module.GetType().FullName}");
            return typedModule;
        }

        public void Dispose()
        {
            foreach (var module in _modules.Values)
                module.Dispose();
            _modules.Clear();
        }

        private void RemoveModule(string key)
        {
            _modules.TryRemove(key, out IMessageBusModule module);

            _logger.Debug($"Stopping module Type={module.GetType().Name}");
            module.Stop();

            _logger.Debug($"Removing module Type={module.GetType().Name}");
            module.Unattach();
        }

        private static string GetKey<TModule>()
        {
            return typeof(TModule).FullName;
        }

        private class ModuleToken : Utility.DisposeOnceToken
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
