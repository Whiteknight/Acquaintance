using System;
using System.Collections.Generic;

namespace Acquaintance.Modules
{
    public interface IModuleManager
    {
        IDisposable Add(IMessageBusModule module);
        IDisposable Add(Guid id, IMessageBusModule module);

        IEnumerable<TModule> Get<TModule>()
            where TModule : IMessageBusModule;
        TModule Get<TModule>(Guid id)
            where TModule : IMessageBusModule;
    }
}