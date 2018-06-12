using System;

namespace Acquaintance.Utility
{
    // A null-object adaptor for the IDisposable pattern
    public class DoNothingDisposable : IDisposable
    {
        public void Dispose()
        {
        }

        public override string ToString()
        {
            return "Nothing";
        }
    }
}
