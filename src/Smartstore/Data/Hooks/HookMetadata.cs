using Microsoft.EntityFrameworkCore;
using Smartstore.Utilities;

namespace Smartstore.Data.Hooks
{
    public class HookMetadata : IEquatable<HookMetadata>
    {
        /// <summary>
        /// The type of entity
        /// </summary>
        public Type HookedType { get; set; }

        /// <summary>
        /// All possible types/interfaces used to resolve the hook implementation from service registry
        /// </summary>
        public Type[] ServiceTypes { get; set; }

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

        public override string ToString()
        {
            return $"{ImplType.Name}, HookedType: {HookedType.Name}, Importance: {Importance}";
        }

        public override bool Equals(object other)
        {
            return Equals(other as HookMetadata);
        }

        public bool Equals(HookMetadata other)
        {
            if (other == null)
            {
                return false;
            }

            return ImplType == other.ImplType && HookedType == other.HookedType;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(typeof(HookMetadata))
                .Add(ImplType)
                .Add(HookedType)
                .CombinedHash;
        }

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