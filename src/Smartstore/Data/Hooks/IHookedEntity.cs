using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Domain;

namespace Smartstore.Data.Hooks
{
    public interface IHookedEntity
    {
        /// <summary>
        /// Gets the data context that triggered the hook.
        /// </summary>
        DbContext DbContext { get; }

        /// <summary>
        /// Gets the hooked entity entry
        /// </summary>
        EntityEntry Entry { get; }

        /// <summary>
        /// Gets the hooked entity instance
        /// </summary>
        BaseEntity Entity { get; }

        /// <summary>
        /// Gets the unproxied type of the hooked entity instance. 
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets or sets the initial (presave) state of the hooked entity.
        /// The setter is for internal use only, don't invoke!
        /// </summary>
        /// <remarks>
        /// The setter is intended for internal usage.
        /// </remarks>
        EntityState InitialState { get; set; }

        /// <summary>
        /// Gets or sets the current state of the hooked entity
        /// </summary>
        /// <remarks>
        /// The setter is intended for internal usage.
        /// </remarks>
        EntityState State { get; set; }

        /// <summary>
        /// Gets a value indicating whether the entity state has changed during hooking.
        /// </summary>
        bool HasStateChanged { get; }

        /// <summary>
        /// Gets a value indicating whether a property has been modified.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        bool IsPropertyModified(string propertyName);

        /// <summary>
        /// Gets a value indicating whether the entity is in soft deleted state.
        /// This is the case when the entity is an instance of <see cref="ISoftDeletable"/>
        /// and the value of its <c>Deleted</c> property is true AND has changed since tracking.
        /// But when the entity is not in modified state the snapshot comparison is omitted.
        /// </summary>
        /// <remarks>
        /// The setter is intended for internal usage.
        /// </remarks>
        bool? IsSoftDeleted { get; set; }
    }
}