using System;

namespace SLXI
{
    public class DelegateDisposable<T> : IDisposable
    {
        private readonly Action<T> _disposeAction;
        private readonly T _state;
        private bool _disposed;

        public DelegateDisposable(Action<T> disposeAction, T state)
        {
            _disposeAction = disposeAction;
            _state = state;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposeAction(_state);
            _disposed = true;
        }
    }

    public class DelegateDisposable : IDisposable
    {
        private readonly Action _disposeAction;
        private bool _disposed;

        public DelegateDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposeAction();
            _disposed = true;
        }
    }

    public static class DelegateDisposableExtensions
    {
        public static DelegateDisposable<T> CreateDelegateDisposable<T>(this T state, Action<T> action)
        {
            return new DelegateDisposable<T>(action, state);
        }
    }
}
