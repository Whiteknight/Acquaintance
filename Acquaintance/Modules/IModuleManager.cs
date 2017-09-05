using System;

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
        IDisposable Add<TModule>(TModule module)
            where TModule : class, IMessageBusModule;

        /// <summary>
        /// Get existing instance of this module type from the message bus or null
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <returns></returns>
        TModule Get<TModule>()
            where TModule : class, IMessageBusModule;

    }
}