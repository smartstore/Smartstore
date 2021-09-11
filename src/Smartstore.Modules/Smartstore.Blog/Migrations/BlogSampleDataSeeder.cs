using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Data;
using Smartstore.Data.Migrations;
using Smartstore.Engine.Modularity;

namespace Smartstore.Blog.Migrations
{
    // TODO: (core) Handle MediaFile correctly (distinct between app and module installation, latter uses current storage provider)
    internal class BlogSampleDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;

        public BlogSampleDataSeeder(ModuleInstallationContext installContext)
            : base(installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
        }

        protected override async Task SeedCoreAsync()
        {
            await PopulateAsync("PopulateBlogPosts", PopulateBlogPosts);
        }

        private async Task PopulateBlogPosts()
        {
            if (await Context.Set<BlogPost>().AnyAsync())
            {
                return;
            }

            //var converter = new BlogPostConverter(_ctx);
            //var blogPosts = converter.ImportAll(_config.Language);
            //PopulateUrlRecordsFor(blogPosts);
        }
    }
}
