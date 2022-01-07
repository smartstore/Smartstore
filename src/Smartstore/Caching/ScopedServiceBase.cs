using Smartstore.Utilities;

namespace Smartstore.Caching
{
    /// <inheritdoc/>
    public abstract class ScopedServiceBase : IScopedService
    {
        /// <inheritdoc/>
        public IDisposable BeginScope(bool clearCache = true)
        {
            if (IsInScope)
            {
                // nested batches are not supported
                return ActionDisposable.Empty;
            }

            OnBeginScope();
            IsInScope = true;

            return new ActionDisposable(() =>
            {
                IsInScope = false;
                OnEndScope();
                if (clearCache && HasChanges)
                {
                    ClearCache();
                }
            });
        }

        /// <inheritdoc/>
        public bool HasChanges
        {
            get;
            protected set;
        }

        protected bool IsInScope
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        public void ClearCache()
        {
            if (!IsInScope)
            {
                OnClearCache();
                HasChanges = false;
            }
        }

        protected abstract void OnClearCache();

        protected virtual void OnBeginScope() { }
        protected virtual void OnEndScope() { }
    }
}
