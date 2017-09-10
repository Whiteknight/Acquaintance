using System;
using System.Threading;

namespace Acquaintance.Utility
{
    public abstract class DisposeOnceToken : IDisposable
    {
        private readonly bool _throwException;
        private int _isDisposed;

        protected DisposeOnceToken()
            : this(false)
        {
        }

        protected DisposeOnceToken(bool throwException)
        {
            _throwException = throwException;
            _isDisposed = 0;
        }

        public void Dispose()
        {
            var isDisposed = Interlocked.Increment(ref _isDisposed);
            if (isDisposed == 1)
                Dispose();
            else if (isDisposed > 1 && _throwException)
                throw new ObjectDisposedException("Token:" + ToString());
        }

        protected abstract void Dispose(bool disposing);
    }
}