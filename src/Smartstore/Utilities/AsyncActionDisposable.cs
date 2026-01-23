namespace Smartstore.Utilities;

/// <summary>
/// Allows action to be executed when it is disposed
/// </summary>
public readonly struct AsyncActionDisposable : IAsyncDisposable
{
    readonly Func<ValueTask> _action;

    public static readonly AsyncActionDisposable Empty = new(() => new ValueTask(Task.CompletedTask));

    public AsyncActionDisposable(Func<ValueTask> action)
    {
        _action = Guard.NotNull(action);
    }

    public async ValueTask DisposeAsync()
    {
        await _action().ConfigureAwait(false);
    }
}