using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Migrations;
using Smartstore.Engine.Modularity;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Migrations
{
    // TODO: (core) Handle MediaFile correctly (distinct between app and module installation, latter uses current storage provider)
    // TODO: (core) Implement concept to prevent duplicate entity seeding (e.g. "AddOrUpdate()")
    internal class ForumsSampleDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;

        public ForumsSampleDataSeeder(ModuleInstallationContext installContext)
            : base(installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
        }

        protected override async Task SeedCoreAsync()
        {
            if (await Context.Set<Forum>().AnyAsync())
            {
                return;
            } 
            
            var groups = ForumGroups();
            
            await PopulateAsync("PopulateForumGroups", groups);
            await PopulateAsync("PopulateForums", Forums(groups));
        }

        private List<ForumGroup> ForumGroups()
        {
            // TODO: (mg) (core) Handle localization.
            return new List<ForumGroup> 
            {
                new ForumGroup
                {
                    Name = "General",
                    Description = string.Empty,
                    DisplayOrder = 1
                }
            };
        }

        private List<Forum> Forums(List<ForumGroup> groups)
        {
            // TODO: (mg) (core) Handle localization.
            var group = groups.FirstOrDefault(c => c.DisplayOrder == 1);

            return new List<Forum>
            {
                new Forum
                {
                    ForumGroup = group,
                    Name = "New Products",
                    Description = "Discuss new products and industry trends",
                    DisplayOrder = 1
                },
                new Forum
                {
                    ForumGroup = group,
                    Name = "Packaging & Shipping",
                    Description = "Discuss packaging & shipping",
                    DisplayOrder = 20
                }
            };
        }
    }
}
