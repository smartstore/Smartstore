namespace Smartstore.Utilities;

/// <summary>
/// Allows action to be executed when it is disposed
/// </summary>
public readonly struct ActionDisposable : IDisposable
{
    readonly Action _action;

    public static readonly ActionDisposable Empty = new(() => { });

    public ActionDisposable(Action action)
    {
        _action = Guard.NotNull(action);
    }

    public void Dispose()
    {
        _action();
    }
}
