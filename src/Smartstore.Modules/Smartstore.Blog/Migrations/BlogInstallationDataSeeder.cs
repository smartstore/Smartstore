using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Blog.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Blog.Migrations
{
    internal class BlogInstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;
        private readonly IMessageTemplateService _messageTemplateService;

        public BlogInstallationDataSeeder(ModuleInstallationContext installContext, IMessageTemplateService messageTemplateService)
            : base(installContext.ApplicationContext, installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
            _messageTemplateService = Guard.NotNull(messageTemplateService, nameof(messageTemplateService));
        }

        protected override async Task SeedCoreAsync()
        {
            await PopulateAsync("PopulateBlogMessageTemplates", PopulateMessageTemplates);
            await PopulateAsync("PopulateMenuItems", PopulateMenuItems);

            if (_installContext.SeedSampleData == null || _installContext.SeedSampleData == true)
            {
                await PopulateAsync("PopulateBlogPosts", PopulateBlogPosts);
            }
        }

        private async Task PopulateMessageTemplates()
        {
            await _messageTemplateService.ImportAllTemplatesAsync(
                _installContext.Culture, 
                PathUtility.Combine(_installContext.ModuleDescriptor.Path, "App_Data/EmailTemplates"));
        }

        private async Task PopulateBlogPosts()
        {
            if (await Context.Set<BlogPost>().AnyAsync())
            {
                return;
            }

            var converter = new BlogPostConverter(Context, _installContext);
            var blogPosts = await converter.ImportAllAsync();
            await PopulateUrlRecordsFor(blogPosts, post => CreateUrlRecordFor(post));
        }

        private async Task PopulateMenuItems()
        {
            const string routeModel = "{\"routename\":\"Blog\"}";

            var menuItemsSet = Context.Set<MenuItemEntity>();

            // Add blog link to footer service menu.
            var refItem = await menuItemsSet
                .AsNoTracking()
                .Where(x => x.Menu.IsSystemMenu && x.Menu.SystemName == "FooterService")
                .OrderByDescending(x => x.DisplayOrder)
                .FirstOrDefaultAsync();

            if (refItem != null && !await menuItemsSet.AnyAsync(x => x.MenuId == refItem.MenuId && x.Model == routeModel))
            {
                var blogSettings = await Context.Set<Setting>()
                    .Where(x => x.StoreId == 0 && x.Name == "BlogSettings.Enabled")
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync() ?? "true";

                var blogMenuItem = new MenuItemEntity
                {
                    MenuId = refItem.MenuId,
                    ProviderName = "route",
                    Model = routeModel,
                    Title = "Blog",
                    DisplayOrder = refItem.DisplayOrder + 1,
                    Published = blogSettings.EqualsNoCase("true")
                };

                menuItemsSet.Add(blogMenuItem);
                await Context.SaveChangesAsync();
            }
        }

        public UrlRecord CreateUrlRecordFor(BlogPost post)
        {
            var name = BuildSlug(post.GetDisplayName()).Truncate(400);

            if (name.HasValue())
            {
                var result = new UrlRecord
                {
                    EntityId = post.Id,
                    EntityName = post.GetEntityName(),
                    LanguageId = 0,
                    Slug = name,
                    IsActive = true
                };

                return result;
            }

            return null;
        }
    }
}
