using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Logging;
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
            await PopulateAsync("PopulateForumMenuItems", PopulateMenuItems);
            await PopulateAsync("PopulateForumActivityLogTypes", ForumActivityLogTypes());

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

        private async Task PopulateMenuItems()
        {
            // TODO: (mg) (core) verify forum route name.
            const string routeModel = "{\"routename\":\"Forums\"}";

            var menuItemsSet = Context.Set<MenuItemEntity>();

            // Add forum link to footer service menu.
            var refItem = await menuItemsSet
                .AsNoTracking()
                .Where(x => x.Menu.IsSystemMenu && x.Menu.SystemName == "FooterService")
                .OrderByDescending(x => x.DisplayOrder)
                .FirstOrDefaultAsync();

            if (refItem != null && !await menuItemsSet.AnyAsync(x => x.MenuId == refItem.MenuId && x.Model == routeModel))
            {
                var forumsEnabled = await Context.Set<Setting>()
                    .Where(x => x.StoreId == 0 && x.Name == "ForumSettings.ForumsEnabled")
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync() ?? "true";

                var forumMenuItem = new MenuItemEntity
                {
                    MenuId = refItem.MenuId,
                    ProviderName = "route",
                    Model = routeModel,
                    Title = _deSeedData ? "Forum" : "Forums",
                    DisplayOrder = refItem.DisplayOrder + 1,
                    Published = forumsEnabled.EqualsNoCase("true")
                };

                menuItemsSet.Add(forumMenuItem);
                await Context.SaveChangesAsync();
            }
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

        private List<ActivityLogType> ForumActivityLogTypes()
        {
            return new List<ActivityLogType>
            {
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.SendPM",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. PN an Kunden geschickt" : "Public store. Send PM"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.AddForumTopic",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Thema erstellt" : "Public store. Add forum topic"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.EditForumTopic",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Thema bearbeitet" : "Public store. Edit forum topic"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.DeleteForumTopic",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Thema gelöscht" : "Public store. Delete forum topic"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.AddForumPost",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Beitrag erstellt" : "Public store. Add forum post"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.EditForumPost",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Beitrag bearbeitet" : "Public store. Edit forum post"
                },
                new ActivityLogType
                {
                    SystemKeyword = "PublicStore.DeleteForumPost",
                    Enabled = false,
                    Name = _deSeedData ? "Öffentlicher Shop. Foren-Beitrag gelöscht" : "Public store. Delete forum post"
                }
            };
        }
    }
}
