using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    /// <summary>
    /// A hook that is executed before and after a database save operation.
    /// An implementor should raise <see cref="NotSupportedException"/> or
    /// <see cref="NotImplementedException"/> to signal the hook handler
    /// that it never should process the hook again for the current
    /// EntityType/State/Stage combination.
    /// </summary>
    public interface IDbSaveHook
    {
        /// <summary>
        /// Called when an entity is about to be saved.
        /// </summary>
        /// <param name="entry">The entity entry</param>
        Task OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken);

        /// <summary>
        /// Called after an entity has been successfully saved.
        /// </summary>
        /// <param name="entry">The entity entry</param>
        Task OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken);

        /// <summary>
        /// Called after all entities in the current unit of work has been handled right before saving changes to the database
        /// </summary>
        Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken);

        /// <summary>
        /// Called after all entities in the current unit of work has been handled after saving changes to the database
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