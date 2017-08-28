using System;
using System.Collections.Generic;

namespace Acquaintance.Modules
{
    /// <summary>
    /// Pluggable modules for the message bus system
    /// </summary>
    public interface IModuleManager
    {
        /// <summary>
        /// Add a new module to the message bus.
        /// </summary>
        /// <param name="module"></param>
        /// <returns>A token which can be disposed to remove the module</returns>
        IDisposable Add(IMessageBusModule module);

        /// <summary>
        /// Get all existing instances of this module type from the message bus.
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <returns></returns>
        IEnumerable<TModule> Get<TModule>()
            where TModule : IMessageBusModule;

    }
}