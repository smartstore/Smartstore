using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    /// <summary>
    /// The result of a database save hook operation.
    /// </summary>
    public enum HookResult
    {
        /// <summary>
        /// Signals the hook handler that it never should process the hook
        /// again for the current EntityType/State/Stage combination.
        /// </summary>
        Void = -1,

        /// <summary>
        /// Operation was handled but completed with errors.
        /// Failed hooks will be absent from <see cref="IDbSaveHook.OnBeforeSaveCompletedAsync(IEnumerable{IHookedEntity}, CancellationToken)"/>
        /// or <see cref="IDbSaveHook.OnAfterSaveCompletedAsync(IEnumerable{IHookedEntity}, CancellationToken)"/>
        /// </summary>
        Failed,

        /// <summary>
        /// Operation was handled and completed without errors.
        /// </summary>
        Ok
    }

    /// <summary>
    /// A hook that is executed before and after a database save operation.
    /// Raising <see cref="NotSupportedException"/> or
    /// <see cref="NotImplementedException"/> will be treated just like <see cref="HookResult.Void"/>.
    /// </summary>
    public interface IDbSaveHook
    {
        /// <summary>
        /// Called when an entity is about to be saved.
        /// </summary>
        /// <param name="entry">The entity entry</param>
        Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken);

        /// <summary>
        /// Called after an entity has been successfully saved.
        /// </summary>
        /// <param name="entry">The entity entry</param>
        Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken);

        /// <summary>
        /// Called after all entities in the current unit of work have been handled right before saving changes to the database
        /// </summary>
        Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken);

        /// <summary>
        /// Called after all entities in the current unit of work have been handled after saving changes to the database
        /// </summary>
        Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken);
    }

    /// <inheritdoc/>
    /// <typeparam name="TContext">
    /// Restricts the hook to the specified data context implementation type.
    /// To restrict to the core data context, implement the parameterless <see cref="IDbSaveHook"/> instead.
    /// Abstract base types can also be specified in order to bypass restrictions.
    /// </typeparam>
    public interface IDbSaveHook<TContext> : IDbSaveHook
        where TContext : DbContext
    {
    }
}