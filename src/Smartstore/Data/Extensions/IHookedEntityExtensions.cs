using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore
{
    public static class IHookedEntityExtensions
    {
        /// <summary>
        /// Resets the <see cref="IHookedEntity.State"/> to <see cref="EntityState.Unchanged"/> or <see cref="EntityState.Detached"/>.
        /// </summary>
        /// <param name="entry"><see cref="IHookedEntity"/>.</param>
        public static void ResetState(this IHookedEntity entry)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Unchanged;
            }
            else if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
