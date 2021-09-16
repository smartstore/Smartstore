using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Blog.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging.Utilities;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Blog.Migrations
{
    internal class BlogInstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly ModuleInstallationContext _installContext;

        public BlogInstallationDataSeeder(ModuleInstallationContext installContext)
            : base(installContext.Logger)
        {
            _installContext = Guard.NotNull(installContext, nameof(installContext));
        }

        protected override async Task SeedCoreAsync()
        {
            await PopulateAsync("PopulateBlogMessageTemplates", PopulateMessageTemplates);

            if (_installContext.SeedSampleData == null || _installContext.SeedSampleData == true)
            {
                await PopulateAsync("PopulateBlogPosts", PopulateBlogPosts);
            }
        }

        private async Task PopulateMessageTemplates()
        {
            var converter = new MessageTemplateConverter(Context, _installContext.ApplicationContext);
            await converter.ImportAllAsync(
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
            //PopulateUrlRecordsFor(blogPosts);
        }
    }
}
