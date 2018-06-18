using System;
using Acquaintance.Logging;

namespace Acquaintance.Utility
{
    public static class ErrorHandling
    {
        public static void IgnoreExceptions(Action act, ILogger logger = null)
        {
            Assert.ArgumentNotNull(act, nameof(act));
            try
            {
                act();
            }
            catch (Exception e)
            {
                logger?.Warn(e, "Explicitly ignored exception");
            }
        }

        public static T TryGetOrDefault<T>(Func<T> func, ILogger logger = null)
        {
            Assert.ArgumentNotNull(func, nameof(func));

            try
            {
                return func();
            }
            catch (Exception e)
            {
                logger?.Warn(e, "Ignoring exception and returning default value");
                return default(T);
            }
        }

        public static bool TryAndReturnStatus(Action act, ILogger logger = null)
        {
            Assert.ArgumentNotNull(act, nameof(act));
            try
            {
                act();
                return true;
            }
            catch (Exception e)
            {
                logger?.Warn(e, "Ignoring exception and returning false");
                return false;
            }
        }
    }
}