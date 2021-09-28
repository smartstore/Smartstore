using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging;
using Smartstore.Core.Seo;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.News.Domain;
using Smartstore.News.Services;
using Smartstore.Utilities;

namespace Smartstore.News.Migrations
{
    internal class NewsInstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IWidgetService _widgetService;

        public NewsInstallationDataSeeder(ModuleInstallationContext installContext, IMessageTemplateService messageTemplateService, IWidgetService widgetService)
            : base(installContext.ApplicationContext, installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
            _messageTemplateService = Guard.NotNull(messageTemplateService, nameof(messageTemplateService));
            _widgetService = Guard.NotNull(widgetService, nameof(widgetService));
        }

        protected override async Task SeedCoreAsync()
        {
            await PopulateAsync("PopulateNewsMessageTemplates", PopulateMessageTemplates);
            await TryActivateWidgetAsync();

            if (_installContext.SeedSampleData == null || _installContext.SeedSampleData == true)
            {
                await PopulateAsync("PopulateNewsItems", PopulateNewsPosts);
            }
        }

        private async Task TryActivateWidgetAsync()
        {
            var showOnHomepagePropName = TypeHelper.NameOf<NewsSettings>(y => y.ShowNewsOnMainPage, true);
            var showOnHomepageSetting = await Context.Settings.FirstOrDefaultAsync(x => x.Name == showOnHomepagePropName);
            
            if (showOnHomepageSetting?.Value?.Convert<bool?>() == true)
            {
                // Activate the news homepage widget
                await _widgetService.ActivateWidgetAsync("Smartstore.News", true);
            }
        }

        private async Task PopulateMessageTemplates()
        {
            await _messageTemplateService.ImportAllTemplatesAsync(
                _installContext.Culture, 
                PathUtility.Combine(_installContext.ModuleDescriptor.Path, "App_Data/EmailTemplates"));
        }

        private async Task PopulateNewsPosts()
        {
            if (await Context.Set<NewsItem>().AnyAsync())
            {
                return;
            }

            var converter = new NewsItemConverter(Context, _installContext);
            var newsItems = await converter.ImportAllAsync();
            await PopulateUrlRecordsFor(newsItems, item => CreateUrlRecordFor(item));
        }

        public UrlRecord CreateUrlRecordFor(NewsItem item)
        {
            var name = BuildSlug(item.GetDisplayName()).Truncate(400);

            if (name.HasValue())
            {
                var result = new UrlRecord
                {
                    EntityId = item.Id,
                    EntityName = item.GetEntityName(),
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
