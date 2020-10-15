using System;
using System.Threading.Tasks;

namespace Smartstore.Utilities
{
    /// <summary>
    /// Allows action to be executed when it is disposed
    /// </summary>
    public struct AsyncActionDisposable : IAsyncDisposable
    {
        readonly Func<ValueTask> _action;

        public static readonly AsyncActionDisposable Empty = new AsyncActionDisposable(() => new ValueTask(Task.CompletedTask));

        public AsyncActionDisposable(Func<ValueTask> action)
        {
			Guard.NotNull(action, nameof(action));
            
            _action = action;
        }

        public async ValueTask DisposeAsync()
        {
            await _action().ConfigureAwait(false);
        }

    }
}