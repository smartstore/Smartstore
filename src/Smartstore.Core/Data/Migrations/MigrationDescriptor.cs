namespace Smartstore.Core.Data.Migrations
{
    public class MigrationDescriptor
    {
        internal MigrationDescriptor(Type migrationType)
        {
            Type = Guard.NotNull(migrationType);

            var attribute = migrationType.GetAttribute<MigrationVersionAttribute>(false);

            if (attribute != null)
            {
                Version = attribute.Version;
                Description = attribute.Description;
                BreakingChange = attribute.BreakingChange;
            }
        }

        public long Version { get; }

        /// <summary>
        /// The class name of the migration.
        /// </summary>
        public string Name => Type.Name;

        public string Description { get; }
        public Type Type { get; }
        public bool BreakingChange { get; }

        public override string ToString()
            => $"MigrationDescriptor: {Name}, Version: {Version}, Description: {Description}";
    }
}
