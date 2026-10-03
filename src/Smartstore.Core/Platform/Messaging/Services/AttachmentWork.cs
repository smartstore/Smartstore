#nullable enable

using Autofac;

namespace Smartstore.Core.Messaging;

/// <summary>
/// Describes the work that will fill an email attachment in a background dependency scope.
/// </summary>
/// <remarks>
/// Creating an instruction does not resolve a generator or start a task.
/// </remarks>
public sealed class AttachmentWork
{
    private readonly Func<ILifetimeScope, QueuedEmailAttachment, CancellationToken, Task> _generate;

    private AttachmentWork(
        Func<ILifetimeScope, QueuedEmailAttachment, CancellationToken, Task> generate,
        TimeSpan timeout)
    {
        _generate = generate;
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the cooperative execution timeout, starting when the generator is invoked.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Creates an instruction using explicitly captured input data and a generator resolved during execution.
    /// </summary>
    /// <typeparam name="TGenerator">The generator service to resolve in the executing dependency scope.</typeparam>
    /// <typeparam name="TState">The type of the captured input data.</typeparam>
    /// <param name="state">Copied input values, without request services or tracked entities.</param>
    /// <param name="generate">A function that fills the target attachment and observes the cancellation token.</param>
    /// <param name="timeout">The execution timeout, or <c>null</c> to use thirty seconds.</param>
    /// <returns>An instruction that can be registered on a message queuing event.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The timeout is not positive.</exception>
    /// <remarks>
    /// Use a static generator function to avoid capturing request dependencies. The function receives
    /// an attachment belonging to the background operation and must not persist it. It can fill BLOB data,
    /// a file reference, or a path, and update metadata such as the final file name.
    /// The executing infrastructure applies the timeout and persists the generated content.
    /// </remarks>
    public static AttachmentWork Create<TGenerator, TState>(
        TState state,
        Func<TGenerator, TState, QueuedEmailAttachment, CancellationToken, Task> generate,
        TimeSpan? timeout = null)
        where TGenerator : notnull
        where TState : notnull
    {
        Guard.NotNull(state);
        Guard.NotNull(generate);

        var budget = timeout ?? TimeSpan.FromSeconds(30);
        if (budget <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The execution timeout must be positive.");
        }

        return new AttachmentWork(
            (scope, target, token) => generate(scope.Resolve<TGenerator>(), state, target, token),
            budget);
    }

    internal Task GenerateAsync(ILifetimeScope scope, QueuedEmailAttachment target, CancellationToken cancelToken)
    {
        Guard.NotNull(scope);
        Guard.NotNull(target);
        cancelToken.ThrowIfCancellationRequested();

        return _generate(scope, target, cancelToken);
    }
}