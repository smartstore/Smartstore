using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Data;
using EfState = Microsoft.EntityFrameworkCore.EntityState;

namespace Smartstore
{
    public static class EntityEntryExtensions
    {
        /// <summary>
        /// Sets the state of an entity entry to <see cref="EfState.Modified"/> if it is detached.
        /// </summary>
        /// <param name="entry">The entity entry.</param>
        /// <returns><c>true</c> if the state has been changed, <c>false</c> if entity is attached already.</returns>
        public static bool TryUpdate(this EntityEntry entry)
        {
            if (entry.State == EfState.Detached)
            {
                entry.State = EfState.Unchanged;
                entry.State = EfState.Modified;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Changes the state of an entity entry when requested state differs.
        /// </summary>
        /// <param name="entry">The entry instance</param>
        /// <param name="requestedState">The requested new state</param>
        /// <returns><c>true</c> if the state has been changed, <c>false</c> if current state did not differ from <paramref name="requestedState"/>.</returns>
        public static bool TryChangeState(this EntityEntry entry, EfState requestedState)
        {
            if (entry.State != requestedState)
            {
                // Only change state when requested state differs,
                // because EF internally sets all properties to modified
                // if necessary, even when requested state equals current state.
                entry.State = requestedState;

                return true;
            }

            return false;
        }

        public static void ReloadEntity(this EntityEntry entry)
        {
            try
            {
                entry.Reload();
            }
            catch
            {
                // Can occur when entity has been detached in the meantime (for whatever fucking reasons)
                if (entry.State == EfState.Detached)
                {
                    entry.State = EfState.Unchanged;
                    ReloadEntity(entry);
                }
            }
        }

        public static async Task ReloadEntityAsync(this EntityEntry entry, CancellationToken cancelToken = default)
        {
            try
            {
                await entry.ReloadAsync(cancelToken);
            }
            catch
            {
                // Can occur when entity has been detached in the meantime (for whatever fucking reasons)
                if (entry.State == EfState.Detached)
                {
                    entry.State = EfState.Unchanged;
                    await ReloadEntityAsync(entry, cancelToken);
                }
            }
        }

        /// <summary>
        /// Gets a dictionary with modified properties for the specified entity
        /// </summary>
        /// <param name="entry">The entity entry instance for which to get modified properties for</param>
        /// <returns>
        /// A dictionary, where the key is the name of the modified property
        /// and the value is its ORIGINAL value (which was tracked when the entity
        /// was attached to the context the first time)
        /// Returns an empty dictionary if no modification could be detected.
        /// </returns>
        public static IDictionary<string, object> GetModifiedProperties(this EntityEntry entry)
        {
            var props = GetModifiedPropertyEntries(entry).ToDictionary(k => k.Metadata.Name, v => v.OriginalValue);

            //System.Diagnostics.Debug.WriteLine("GetModifiedProperties: " + String.Join(", ", props.Select(x => x.Key)));

            return props;
        }

        /// <summary>
        /// Checks whether an entity entry has any modified property. 
        /// Only entities in <see cref="EfState.Modified"/> state are scanned for changes.
        /// Merged values provided by the <see cref="IMergedData"/> are ignored.
        /// </summary>
        /// <param name="entry">The entry instance</param>
        /// <returns><c>true</c> if any property has changed, <c>false</c> otherwise</returns>
        public static bool HasChanges(this EntityEntry entry)
        {
            var hasChanges = GetModifiedPropertyEntries(entry).Any();
            return hasChanges;
        }

        internal static IEnumerable<PropertyEntry> GetModifiedPropertyEntries(this EntityEntry entry)
        {
            // Be aware of the entity state. you cannot get modified properties for detached entities.
            EnsureChangesDetected(entry);

            if (entry.State != EfState.Modified)
            {
                yield break;
            }

            foreach (var efProp in entry.CurrentValues.Properties)
            {
                var prop = entry.Property(efProp.Name);
                if (prop != null && PropIsModified(prop))
                {
                    // INFO: under certain conditions PropertyEntry.IsModified returns true, even when values are equal
                    yield return prop;
                }
            }
        }

        public static bool IsPropertyModified(this EntityEntry entry, string propertyName)
        {
            return TryGetModifiedProperty(entry, propertyName, out _);
        }

        public static bool TryGetModifiedProperty(this EntityEntry entry, string propertyName, out object originalValue)
        {
            Guard.NotEmpty(propertyName, nameof(propertyName));

            EnsureChangesDetected(entry);

            originalValue = null;

            if (entry.State != EfState.Modified)
            {
                return false;
            }

            var prop = entry.Property(propertyName);
            if (prop != null && PropIsModified(prop))
            {
                // INFO: under certain conditions PropertyEntry.IsModified returns true, even when values are equal
                originalValue = prop.OriginalValue;
                return true;
            }

            return false;
        }

        private static void EnsureChangesDetected(EntityEntry entry)
        {
            var state = entry.State;
            var ctx = entry.Context;

            if (ctx.ChangeTracker.AutoDetectChangesEnabled && state == EfState.Modified)
                return;

            if ((ctx as HookingDbContext)?.IsInSaveOperation == true)
                return;

            if (state == EfState.Unchanged || state == EfState.Modified)
            {
                // When AutoDetectChanges is off we cannot be sure whether the entity is really unchanged,
                // because no detection was performed to verify this.
                entry.DetectChanges();
            }
        }

        private static bool PropIsModified(PropertyEntry prop)
        {
            // INFO: "CurrentValue" cannot be used for entities in the Deleted state.
            // INFO: "OriginalValues" cannot be used for entities in the Added state.
            //return !AreEqual(prop.CurrentValue, prop.OriginalValue);
            return prop.EntityEntry.Context.ChangeTracker.AutoDetectChangesEnabled
                ? prop.IsModified
                : !AreEqual(prop.CurrentValue, prop.OriginalValue);
        }

        private static bool AreEqual(object cur, object orig)
        {
            if (cur == null && orig == null)
                return true;

            return orig != null
                ? orig.Equals(cur)
                : cur.Equals(orig);
        }
    }
}
