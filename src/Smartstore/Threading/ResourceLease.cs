#nullable enable

namespace Smartstore.Threading;

/// <summary>
/// Keeps a shared resource alive until the lease is disposed.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public sealed class ResourceLease<T> : IAsyncDisposable where T : class
{
    private IdleResource<T>? _owner;

    internal ResourceLease(T value, IdleResource<T> owner)
    {
        Value = value;
        _owner = owner;
    }

    /// <summary>
    /// Gets the resource protected by this lease. Do not use it after disposing the lease.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Releases this lease. Repeated calls have no effect.
    /// </summary>
    public ValueTask DisposeAsync()
        // Exchange the owner before awaiting release, so concurrent disposals cannot decrement twice.
        => Interlocked.Exchange(ref _owner, null)?.ReleaseAsync() ?? ValueTask.CompletedTask;
}
