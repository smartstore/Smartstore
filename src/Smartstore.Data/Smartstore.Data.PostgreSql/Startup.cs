using System;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Data.PostgreSql
{
    internal class Startup : StarterBase
    {
        public override int Order => int.MinValue;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            // See: https://github.com/npgsql/efcore.pg/issues/2000
            // See: https://stackoverflow.com/questions/69961449/net6-and-datetime-problem-cannot-write-datetime-with-kind-utc-to-postgresql-ty
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
