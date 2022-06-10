using FluentMigrator;
using FluentMigrator.Exceptions;
using FluentMigrator.Runner.Processors;
using Smartstore.Data;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Selects a migration processor based on <see cref="DataSettings.DbFactory"/>.
    /// </summary>
    public class MigrationProcessorAccessor : IProcessorAccessor
    {
        public MigrationProcessorAccessor(IEnumerable<IMigrationProcessor> processors)
        {
            SelectProcessor(processors);
        }

        public IMigrationProcessor Processor { get; protected set; }

        protected virtual void SelectProcessor(IEnumerable<IMigrationProcessor> processors)
        {
            if (!processors.Any())
            {
                throw new ProcessorFactoryNotFoundException("No migration processor found.");
            }

            var dbSystemName = DataSettings.Instance.DbFactory.DbSystem.ToString();

            Processor = processors.FirstOrDefault(x =>
                x.DatabaseType.EqualsNoCase(dbSystemName) ||
                x.DatabaseTypeAliases.Any(alias => alias.EqualsNoCase(dbSystemName)));

            if (Processor == null)
            {
                var availableTypeNames = string.Join(", ", processors.Select(x => x.DatabaseType).Union(processors.SelectMany(x => x.DatabaseTypeAliases)));
                throw new ProcessorFactoryNotFoundException($"Cannot find migration processor for {dbSystemName}. Available processors: {availableTypeNames}.");
            }
        }
    }
}
