#nullable enable

namespace Smartstore.Threading;

/// <summary>
/// Keeps a shared value alive until the lease is disposed.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class ValueLease<T> : IAsyncDisposable where T : class
{
    private Ephemeral<T>? _owner;

    internal ValueLease(T value, Ephemeral<T> owner)
    {
        Value = value;
        _owner = owner;
    }

    /// <summary>
    /// Gets the value protected by this lease. Do not use it after disposing the lease.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Releases this lease. Repeated calls have no effect.
    /// </summary>
    public ValueTask DisposeAsync()
        // Exchange the owner before awaiting release, so concurrent disposals cannot decrement twice.
        => Interlocked.Exchange(ref _owner, null)?.ReleaseAsync() ?? ValueTask.CompletedTask;
}