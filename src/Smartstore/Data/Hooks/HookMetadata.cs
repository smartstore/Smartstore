using System;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data.Hooks
{
    public class HookMetadata
    {
        /// <summary>
        /// The type of entity
        /// </summary>
        public Type HookedType { get; set; }

        /// <summary>
        /// The type of the hook class itself
        /// </summary>
        public Type ImplType { get; set; }

        /// <summary>
        /// The impl type of <see cref="DbContext"/> to which the hook belongs to.
        /// </summary>
        public Type DbContextType { get; set; }

        /// <summary>
        /// The importance level.
        /// </summary>
        public HookImportance Importance { get; set; }

        /// <summary>
        /// The execution order.
        /// </summary>
        public int Order { get; set; }

        public static HookMetadata Create<THook, TContext>(Type hookedType, HookImportance importance = HookImportance.Normal)
            where THook : IDbSaveHook
            where TContext : DbContext
        {
            Guard.NotNull(hookedType, nameof(hookedType));

            return new HookMetadata
            {
                ImplType = typeof(THook),
                DbContextType = typeof(TContext),
                HookedType = hookedType,
                Importance = importance
            };
        }
    }
}