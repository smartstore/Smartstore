using FluentMigrator.Runner.Conventions;

namespace Smartstore.Core.Data.Migrations
{
    public interface IConventionProvider
    {
        void Configure(IConventionSet conventionSet);
    }
}
