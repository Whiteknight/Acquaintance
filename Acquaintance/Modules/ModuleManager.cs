﻿using Acquaintance.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Modules
{
    public class ModuleManager : IDisposable, IModuleManager
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Guid, IMessageBusModule> _modules;

        public ModuleManager(IMessageBus messageBus, ILogger logger)
        {
            _messageBus = messageBus;
            _logger = logger;
            _modules = new ConcurrentDictionary<Guid, IMessageBusModule>();
        }

        public IDisposable Add(IMessageBusModule module)
        {
            var id = Guid.NewGuid();
            return Add(id, module);
        }

        public IDisposable Add(Guid id, IMessageBusModule module)
        {
            _logger.Debug("Adding module Id={0} Type={1}", id, module.GetType().Name);
            module.Attach(_messageBus);
            if (!_modules.TryAdd(id, module))
            {
                _logger.Error($"Could not add module of type {module.GetType().Name} for unknown reasons");
                return null;
            }

            _logger.Debug("Starting module Id={0} Type={1}", id, module.GetType().Name);
            module.Start();
            return new ModuleToken(this, id);
        }

        public IEnumerable<TModule> Get<TModule>()
            where TModule : IMessageBusModule
        {
            return _modules.Values.OfType<TModule>().ToList();
        }

        public TModule Get<TModule>(Guid id)
            where TModule : IMessageBusModule
        {
            return (TModule)_modules[id];
        }

        public void Dispose()
        {
            foreach (var module in _modules.Values)
                module.Dispose();
            _modules.Clear();
        }

        private void RemoveModule(Guid id)
        {
            _modules.TryRemove(id, out IMessageBusModule module);

            _logger.Debug("Stopping module Id={0} Type={1}", id, module.GetType().Name);
            module.Stop();

            _logger.Debug("Removing module Id={0} Type={1}", id, module.GetType().Name);
            module.Unattach();
        }

        private class ModuleToken : IDisposable
        {
            private readonly ModuleManager _manager;
            private readonly Guid _moduleId;

            public ModuleToken(ModuleManager manager, Guid moduleId)
            {
                _manager = manager;
                _moduleId = moduleId;
            }

            public void Dispose()
            {
                _manager.RemoveModule(_moduleId);
            }
        }
    }
}
