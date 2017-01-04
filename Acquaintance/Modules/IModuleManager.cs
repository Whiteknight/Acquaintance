using System;
using System.Collections.Generic;

namespace Acquaintance.Modules
{
    public interface IModuleManager
    {
        IDisposable Add(IMessageBusModule module);

        IEnumerable<TModule> GetByType<TModule>()
            where TModule : IMessageBusModule;

    }
}