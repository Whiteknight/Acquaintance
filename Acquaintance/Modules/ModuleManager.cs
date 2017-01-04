using Acquaintance.Logging;
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
            Guid id = Guid.NewGuid();
            _logger.Debug("Adding module Id={0} Type={1}", id, module.GetType().Name);
            module.Attach(_messageBus);
            bool added = _modules.TryAdd(id, module);
            if (!added)
                return null;

            _logger.Debug("Starting module Id={0} Type={1}", id, module.GetType().Name);
            module.Start();
            return new ModuleToken(this, id);
        }

        public IEnumerable<TModule> GetByType<TModule>()
            where TModule : IMessageBusModule
        {
            return _modules.Values.OfType<TModule>().ToList();
        }

        public void Dispose()
        {
            foreach (var module in _modules.Values)
            {
                module.Dispose();
            }
            _modules.Clear();
        }

        private void RemoveModule(Guid id)
        {
            IMessageBusModule module;
            _modules.TryRemove(id, out module);

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
