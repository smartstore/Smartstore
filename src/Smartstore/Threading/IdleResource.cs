#nullable enable

namespace Smartstore.Threading;

/// <summary>
/// Shares one lazily created resource among concurrent leases. After the final lease is released,
/// the resource is either retained or disposed after the configured idle period.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public sealed class IdleResource<T> : IAsyncDisposable where T : class
{
    private readonly Func<T> _create;
    private readonly Func<T, ValueTask> _dispose;
    private readonly Action<Exception>? _onIdleDisposeError;
    private readonly TimeSpan _idleTimeout;

    // Serializes creation, lease counts and disposal. Callers use the leased value outside this gate.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private T? _value;

    // Created only if shutdown must wait for outstanding leases.
    private TaskCompletionSource? _leasesReleased;
    private int _leaseCount;

    // Invalidates an earlier idle timeout when a lease is acquired or shutdown begins.
    private long _idleGeneration;

    // The most recent acquisition decides what happens after the final lease ends.
    private bool _retainWhenIdle;
    private bool _disposed;

    /// <summary>
    /// Creates an idle resource owner. The factory is called only when a new resource is needed.
    /// </summary>
    /// <param name="create">Creates the shared resource on first acquisition or after idle disposal. Must return a non-null value.</param>
    /// <param name="dispose">Releases a resource when its idle period ends or this owner is disposed.</param>
    /// <param name="idleTimeout">Time to wait after the last lease is released before disposing the resource; zero disposes without a grace period.</param>
    /// <param name="onIdleDisposeError">Optional handler for disposal errors in the background idle task. Explicit shutdown errors are propagated to the caller instead.</param>
    public IdleResource(Func<T> create, Func<T, ValueTask> dispose, TimeSpan idleTimeout, Action<Exception>? onIdleDisposeError = null)
    {
        _create = Guard.NotNull(create);
        _dispose = Guard.NotNull(dispose);
        ArgumentOutOfRangeException.ThrowIfLessThan(idleTimeout, TimeSpan.Zero);
        _idleTimeout = idleTimeout;
        _onIdleDisposeError = onIdleDisposeError;
    }

    /// <summary>
    /// Gets whether a resource currently exists. Intended for diagnostics; it does not acquire a lease.
    /// </summary>
    public bool HasValue => Volatile.Read(ref _value) is not null;

    /// <summary>
    /// Acquires a lease, creating the resource if needed. The most recent retention choice applies after the last lease ends.
    /// </summary>
    /// <param name="retainWhenIdle">If true, keep the resource after the last lease ends; otherwise dispose it after <c>idleTimeout</c>.</param>
    /// <param name="cancellationToken">Cancels waiting to acquire the state gate. It does not cancel use of an acquired resource.</param>
    /// <returns>A lease whose disposal releases one reference to the shared resource.</returns>
    public async ValueTask<ResourceLease<T>> AcquireAsync(bool retainWhenIdle = false, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // State changes are serialized, but use of the leased value is not.
            _value ??= Guard.NotNull(_create());
            _idleGeneration++;
            _retainWhenIdle = retainWhenIdle;
            _leaseCount++;
            return new ResourceLease<T>(_value, this);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async ValueTask ReleaseAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (--_leaseCount == 0)
            {
                // Only the final lease can unblock shutdown or start an idle period.
                _leasesReleased?.TrySetResult();
                if (!_disposed && !_retainWhenIdle)
                {
                    // A later acquisition invalidates this timeout without canceling its delay task.
                    _ = RetireAfterIdleAsync(++_idleGeneration);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Stops accepting leases, waits for active leases, and disposes the resource.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Task? waitForLeases;
        await _gate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Prevent a pending idle task from disposing in parallel with explicit shutdown.
            _idleGeneration++;

            // Active leases may still be using the resource, so do not dispose it yet.
            waitForLeases = _leaseCount == 0
                ? null
                : (_leasesReleased ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }
        finally
        {
            _gate.Release();
        }

        if (waitForLeases is not null)
        {
            await waitForLeases;
        }

        await _gate.WaitAsync();
        try
        {
            // Keep explicit shutdown serialized with a possible idle-retirement task.
            await DisposeValueAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RetireAfterIdleAsync(long generation)
    {
        await Task.Delay(_idleTimeout);

        await _gate.WaitAsync();
        try
        {
            // A later lease, or shutdown, makes this timeout obsolete.
            if (_disposed || _leaseCount != 0 || generation != _idleGeneration)
            {
                return;
            }

            await DisposeValueAsync();
        }
        catch (Exception ex)
        {
            // Idle retirement is fire-and-forget; surface failures through the caller's handler.
            try
            {
                _onIdleDisposeError?.Invoke(ex);
            }
            catch
            {
                // A failing error handler must not leave an unobserved background exception.
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask DisposeValueAsync()
    {
        // Detach before awaiting disposal; a failed disposal must not be handed out again.
        var value = _value;
        _value = null;
        if (value is not null)
        {
            await _dispose(value);
        }
    }
}
