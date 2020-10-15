using System;
using System.Collections.Generic;
using System.Text;
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
        /// Whether the hook should run in any case, even if hooking has been turned off.
        /// </summary>
        public bool Important { get; set; }

        public static HookMetadata Create<THook, TContext>(Type hookedType, bool important = false)
            where THook : IDbSaveHook
            where TContext : DbContext
        {
            Guard.NotNull(hookedType, nameof(hookedType));

            return new HookMetadata
            {
                ImplType = typeof(THook),
                DbContextType = typeof(TContext),
                HookedType = hookedType,
                Important = important
            };
        }
    }
}