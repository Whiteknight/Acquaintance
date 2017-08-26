﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Utility
{
    public sealed class DisposableCollection : IDisposable
    {
        private readonly ConcurrentBag<IDisposable> _disposables;
        private volatile int _isDisposed;

        public DisposableCollection()
        {
            _disposables = new ConcurrentBag<IDisposable>();
            _isDisposed = 0;
        }

        public void Add(IDisposable disposable)
        {
            if (_isDisposed > 0)
                throw new ObjectDisposedException("This DisposableCollection has already been disposed");
            if (disposable != null)
                _disposables.Add(disposable);
        }

        public void AddRange(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
                return;
            if (_isDisposed > 0)
                throw new ObjectDisposedException("This DisposableCollection has already been disposed");
            foreach (var disposable in disposables)
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
