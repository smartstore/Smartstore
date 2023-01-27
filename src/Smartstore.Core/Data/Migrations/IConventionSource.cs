using FluentMigrator.Runner.Conventions;

namespace Smartstore.Core.Data.Migrations
{
    public interface IConventionSource
    {
        void Configure(IConventionSet conventionSet);
    }
}
