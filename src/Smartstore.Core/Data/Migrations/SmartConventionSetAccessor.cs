using FluentMigrator.Runner;
using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;

namespace Smartstore.Core.Data.Migrations
{
    public sealed class SmartConventionSetAccessor : IConventionSetAccessor
    {
        public IConventionSet GetConventionSet()
        {
            var defaultSchemaName = EngineContext.Current.Application.AppConfiguration.DbDefaultSchema;
            return new DefaultConventionSet(defaultSchemaName, null);
        }
    }
}
