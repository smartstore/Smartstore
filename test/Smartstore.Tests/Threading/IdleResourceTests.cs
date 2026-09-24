#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Threading;

namespace Smartstore.Tests.Threading;

[TestFixture]
public class IdleResourceTests
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMilliseconds(100);

    [Test]
    public async Task Retires_after_last_lease_and_creates_a_new_value()
    {
        var created = 0;
        var disposed = 0;
        await using var resource = new IdleResource<object>(
            () => { Interlocked.Increment(ref created); return new object(); },
            _ => { Interlocked.Increment(ref disposed); return ValueTask.CompletedTask; },
            IdleTimeout);

        var first = await resource.AcquireAsync();
        var second = await resource.AcquireAsync();
        Assert.That(second.Value, Is.SameAs(first.Value));

        await first.DisposeAsync();
        await first.DisposeAsync();
        await Task.Delay(IdleTimeout + TimeSpan.FromMilliseconds(100));
        Assert.That(disposed, Is.Zero);

        await second.DisposeAsync();
        await WaitUntilAsync(() => Volatile.Read(ref disposed) == 1);
        await using var next = await resource.AcquireAsync();
        Assert.That(next.Value, Is.Not.SameAs(first.Value));
        Assert.That(created, Is.EqualTo(2));
    }

    [Test]
    public async Task Reacquisition_invalidates_the_old_idle_timeout()
    {
        var disposed = 0;
        await using var resource = new IdleResource<object>(
            () => new object(),
            _ => { Interlocked.Increment(ref disposed); return ValueTask.CompletedTask; },
            IdleTimeout);

        var first = await resource.AcquireAsync();
        await first.DisposeAsync();

        await using var second = await resource.AcquireAsync();
        Assert.That(second.Value, Is.SameAs(first.Value));
        await Task.Delay(IdleTimeout + TimeSpan.FromMilliseconds(100));
        Assert.That(disposed, Is.Zero);
    }

    [Test]
    public async Task Retention_choice_is_applied_on_the_next_acquisition()
    {
        var disposed = 0;
        await using var resource = new IdleResource<object>(
            () => new object(),
            _ => { Interlocked.Increment(ref disposed); return ValueTask.CompletedTask; },
            IdleTimeout);

        var first = await resource.AcquireAsync(retainWhenIdle: true);
        await first.DisposeAsync();
        await Task.Delay(IdleTimeout + TimeSpan.FromMilliseconds(100));
        Assert.That(resource.HasValue, Is.True);

        var second = await resource.AcquireAsync(retainWhenIdle: false);
        Assert.That(second.Value, Is.SameAs(first.Value));
        await second.DisposeAsync();
        await WaitUntilAsync(() => Volatile.Read(ref disposed) == 1);
    }

    [Test]
    public async Task Shutdown_waits_for_active_leases()
    {
        var disposed = 0;
        var resource = new IdleResource<object>(
            () => new object(),
            _ => { Interlocked.Increment(ref disposed); return ValueTask.CompletedTask; },
            IdleTimeout);

        var lease = await resource.AcquireAsync();
        var shutdown = resource.DisposeAsync().AsTask();
        Assert.That(shutdown.IsCompleted, Is.False);
        Assert.ThrowsAsync<ObjectDisposedException>(async () => { await resource.AcquireAsync(); });

        await lease.DisposeAsync();
        await shutdown;
        Assert.That(disposed, Is.EqualTo(1));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
