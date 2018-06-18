using System;

namespace Acquaintance.Utility
{
    public static class ObjectManagement
    {
        public static void TryDispose(object obj)
        {
            (obj as IDisposable)?.Dispose();
        }

        public static void Ignore(object obj)
        {
        }
    }
}