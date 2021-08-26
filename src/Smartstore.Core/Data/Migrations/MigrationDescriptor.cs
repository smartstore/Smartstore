using System;

namespace Smartstore.Core.Data.Migrations
{
    public class MigrationDescriptor
    {
        internal MigrationDescriptor(Type migrationType)
        {
            Type = Guard.NotNull(migrationType, nameof(migrationType));

            var attribute = migrationType.GetAttribute<MigrationVersionAttribute>(false);

            if (attribute != null)
            {
                Version = attribute.Version;
                Description = attribute.Description;
                BreakingChange = attribute.BreakingChange;
            }
        }

        public long Version { get; }
        public string Description { get; }
        public Type Type { get; }
        public bool BreakingChange { get; }
    }
}
