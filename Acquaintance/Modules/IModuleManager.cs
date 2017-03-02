using System;
using System.Collections.Generic;

namespace Acquaintance.Modules
{
    public interface IModuleManager
    {
        IDisposable Add(IMessageBusModule module);

        IEnumerable<TModule> Get<TModule>()
            where TModule : IMessageBusModule;

    }
}