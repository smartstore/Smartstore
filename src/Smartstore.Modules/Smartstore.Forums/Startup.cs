using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Forums.Search.Modelling;

namespace Smartstore.Forums
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            // Search.
            services.AddScoped<IForumSearchQueryAliasMapper, ForumSearchQueryAliasMapper>();
            services.AddScoped<IForumSearchQueryFactory, ForumSearchQueryFactory>();
            //builder.RegisterType<ForumSearchService>().As<IForumSearchService>().InstancePerRequest();
            //builder.RegisterType<LinqForumSearchService>().Named<IForumSearchService>("linq").InstancePerRequest();
        }

        class SmartDbContextConfigurer : IDbContextConfigurationSource<SmartDbContext>
        {
            public void Configure(IServiceProvider services, DbContextOptionsBuilder builder)
            {
                builder.UseDbFactory(b => 
                {
                    b.AddModelAssembly(this.GetType().Assembly);
                });
            }
        }
    }
}
