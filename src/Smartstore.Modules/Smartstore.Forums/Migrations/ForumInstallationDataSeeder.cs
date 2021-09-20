using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging;
using Smartstore.Engine.Modularity;
using Smartstore.Forums.Domain;
using Smartstore.IO;

namespace Smartstore.Forums.Migrations
{
    internal class ForumInstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly bool _deSeedData;

        public ForumInstallationDataSeeder(ModuleInstallationContext installContext)
            : base(installContext.ApplicationContext, installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));

            _messageTemplateService = installContext.ApplicationContext.Services.Resolve<IMessageTemplateService>();

            _deSeedData = _installContext.Culture?.StartsWith("de", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        protected override async Task SeedCoreAsync()
        {
            await PopulateAsync("PopulateForumMessageTemplates", PopulateMessageTemplates);

            if (_installContext.SeedSampleData == null || _installContext.SeedSampleData == true)
            {
                if (await Context.Set<ForumGroup>().AnyAsync() || await Context.Set<Forum>().AnyAsync())
                {
                    return;
                }

                var groups = ForumGroups();

                await PopulateAsync("PopulateForumGroups", groups);
                await PopulateAsync("PopulateForums", Forums(groups));
            }
        }

        private async Task PopulateMessageTemplates()
        {
            await _messageTemplateService.ImportAllTemplatesAsync(
                _installContext.Culture,
                PathUtility.Combine(_installContext.ModuleDescriptor.Path, "App_Data/EmailTemplates"));
        }

        private List<ForumGroup> ForumGroups()
        {
            return new List<ForumGroup> 
            {
                new ForumGroup
                {
                    Name = _deSeedData ? "Allgemein" :  "General",
                    Description = string.Empty,
                    DisplayOrder = 1
                }
            };
        }

        private List<Forum> Forums(List<ForumGroup> groups)
        {
            var group = groups.FirstOrDefault(c => c.DisplayOrder == 1);

            return new List<Forum>
            {
                new Forum
                {
                    ForumGroup = group,
                    Name = _deSeedData ? "Neue Produkte" : "New Products",
                    Description = _deSeedData ? "Diskutieren Sie aktuelle oder neue Produkte" : "Discuss new products and industry trends",
                    DisplayOrder = 1
                },
                new Forum
                {
                    ForumGroup = group,
                    Name = _deSeedData ? "Verpackung & Versand" : "Packaging & Shipping",
                    Description = _deSeedData ? "Haben Sie Fragen oder Anregungen zu Verpackung & Versand?" : "Discuss packaging & shipping",
                    DisplayOrder = 20
                }
            };
        }
    }
}
