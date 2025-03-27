#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Data;
using Smartstore.Domain;

namespace Smartstore
{
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Checks whether a collection type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionLoaded<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            return IsCollectionLoaded(ctx, entity, navigationProperty, out _);
        }

        /// <summary>
        /// Checks whether a collection type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        public static bool IsCollectionLoaded<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            [NotNullWhen(true)] out CollectionEntry<TEntity, TCollection>? collectionEntry)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            collectionEntry = null;
            if (entity.Id == 0)
            {
                return false;
            }

            var entry = ctx.Entry(entity);
            collectionEntry = entry.Collection(navigationProperty);
            var isLoaded = collectionEntry.CurrentValue != null || collectionEntry.IsLoaded;

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (!isLoaded && (entry.State == EfState.Detached))
            {
                // Attaching an entity while another instance with same primary key is attached will throw.
                // First we gonna try to locate an already attached entity.
                var other = ctx.FindTracked<TEntity>(entity.Id);
                if (other != null)
                {
                    collectionEntry = ctx.Entry(entity).Collection(navigationProperty);
                }
                else
                {
                    ctx.Attach(entity);
                }
            }

            return isLoaded;
        }

        /// <summary>
        /// Checks whether a reference type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReferenceLoaded<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty?>> navigationProperty)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            return IsReferenceLoaded(ctx, entity, navigationProperty, out _);
        }

        /// <summary>
        /// Checks whether a reference type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        public static bool IsReferenceLoaded<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty?>> navigationProperty,
            [NotNullWhen(true)] out ReferenceEntry<TEntity, TProperty>? referenceEntry)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            referenceEntry = null;
            if (entity.Id == 0)
            {
                return false;
            }

            var entry = ctx.Entry(entity);
            referenceEntry = entry.Reference(navigationProperty);
            var isLoaded = referenceEntry.CurrentValue != null || referenceEntry.IsLoaded;

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (!isLoaded && (entry.State == EfState.Detached))
            {
                // Attaching an entity while another instance with same primary key is attached will throw.
                // First we gonna try to locate an already attached entity.
                var other = ctx.FindTracked<TEntity>(entity.Id);
                if (other != null)
                {
                    referenceEntry = ctx.Entry(entity).Reference(navigationProperty);
                }
                else
                {
                    ctx.Attach(entity);
                }
            }

            return isLoaded;
        }

        /// <summary>
        /// Loads entities referenced by a collection navigation property from database, unless data is already loaded.
        /// </summary>
        /// <param name="entity">Entity instance to load data for.</param>
        /// <param name="navigationProperty">The navigation property expression.</param>
        /// <param name="force"><c>false:</c> do nothing if data is already loaded. <c>true:</c> reload data even if already loaded.</param>
        /// <param name="queryModifier">Modifier for the query that is about to be executed against the database.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        public static async Task<CollectionEntry<TEntity, TCollection>?> LoadCollectionAsync<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>>? queryModifier = null,
            CancellationToken cancelToken = default)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            if (entity.Id == 0)
            {
                return null;
            }

            var entry = ctx.Entry(entity);
            if (entry.State == EfState.Deleted)
            {
                return null;
            }

            var collection = entry.Collection(navigationProperty);
            // TODO: (core) Entities with hashSets as collections always return true here, as they are never null (for example: Product.ProductVariantAttributes).
            var isLoaded = collection.CurrentValue != null || collection.IsLoaded;

            if (!isLoaded && entry.State == EfState.Detached)
            {
                try
                {
                    // Attaching an entity while another instance with same primary key is attached will throw.
                    // First we gonna try to locate an already attached entity.
                    var other = ctx.FindTracked<TEntity>(entity.Id);
                    if (other != null)
                    {
                        // An entity with same key is attached already. So we gonna load the navigation property of attached entity
                        // and copy the result to this detached entity. This way we don't need to attach the source entity.
                        var otherCollection = await ctx.LoadCollectionAsync(other, navigationProperty, force, queryModifier, cancelToken: cancelToken);

                        // Copy collection over to detached entity.
                        collection.CurrentValue = otherCollection?.CurrentValue;
                        collection.IsLoaded = true;
                        isLoaded = true;
                        force = false;
                    }
                    else
                    {
                        ctx.Attach(entity);
                    }
                }
                catch
                {
                    // Attach may throw!
                }
            }

            if (force)
            {
                collection.IsLoaded = false;
                isLoaded = false;
            }

            if (!isLoaded)
            {
                if (queryModifier != null)
                {
                    var query = queryModifier(collection.Query());
                    collection.CurrentValue = await query.ToListAsync(cancellationToken: cancelToken);
                }
                else
                {
                    await collection.LoadAsync(cancelToken);
                }

                collection.IsLoaded = true;
            }

            return collection;
        }

        /// <summary>
        /// Loads an entity referenced by a navigation property from database, unless data is already loaded.
        /// </summary>
        /// <param name="entity">Entity instance to load data for.</param>
        /// <param name="navigationProperty">The navigation property expression.</param>
        /// <param name="force"><c>false:</c> do nothing if data is already loaded. <c>true:</c> Reload data event if loaded already.</param>
        /// <param name="queryModifier">Modifier for the query that is about to be executed against the database.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        public static async Task<ReferenceEntry<TEntity, TProperty>?> LoadReferenceAsync<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty?>> navigationProperty,
            bool force = false,
            Func<IQueryable<TProperty>, IQueryable<TProperty>>? queryModifier = null,
            CancellationToken cancelToken = default)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            if (entity.Id == 0)
            {
                return null;
            }

            var entry = ctx.Entry(entity);
            if (entry.State == EfState.Deleted)
            {
                return null;
            }

            var reference = entry.Reference(navigationProperty);
            var isLoaded = reference.CurrentValue != null || reference.IsLoaded;

            if (!isLoaded && entry.State == EfState.Detached)
            {
                try
                {
                    // Attaching an entity while another instance with same primary key is attached will throw.
                    // First we gonna try to locate an already attached entity.
                    var other = ctx.FindTracked<TEntity>(entity.Id);
                    if (other != null)
                    {
                        // An entity with same key is attached already. So we gonna load the reference property of attached entity
                        // and copy the result to this detached entity. This way we don't need to attach the source entity.
                        var otherReference = await ctx.LoadReferenceAsync(other, navigationProperty, force, queryModifier, cancelToken: cancelToken);

                        // Copy reference over to detached entity.
                        reference.CurrentValue = otherReference?.CurrentValue;
                        reference.IsLoaded = true;
                        isLoaded = true;
                        force = false;
                    }
                    else
                    {
                        ctx.Attach(entity);
                    }
                }
                catch
                {
                    // Attach may throw!
                }
            }

            if (force)
            {
                reference.IsLoaded = false;
                isLoaded = false;
            }

            if (!isLoaded)
            {
                if (queryModifier != null)
                {
                    var query = queryModifier(reference.Query());
                    reference.CurrentValue = await query.FirstOrDefaultAsync(cancellationToken: cancelToken);
                }
                else
                {
                    await reference.LoadAsync(cancelToken);
                }

                reference.IsLoaded = true;
            }

            return reference;
        }
    }
}
