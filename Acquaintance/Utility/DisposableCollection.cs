using System;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public sealed class DisposableCollection : IDisposable
    {
        private readonly List<IDisposable> _disposables;
        private int _isDisposed;

        public DisposableCollection()
        {
            _disposables = new List<IDisposable>();
            _isDisposed = 0;
        }

        public void Add(IDisposable disposable)
        {
            if (disposable != null)
                _disposables.Add(disposable);
        }

        public void Dispose()
        {
            var isDisposed = Interlocked.CompareExchange(ref _isDisposed, 1, 0);
            if (isDisposed != 0)
                return;
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}
