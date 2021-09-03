using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.Core.Content.Menus
{
    public interface ILinkTranslator
    {
        Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId);
    }
    
    public class DefaultLinkTranslator : ILinkTranslator
    {
        public const string SchemaTopic = "topic";
        public const string SchemaProduct = "product";
        public const string SchemaCategory = "category";
        public const string SchemaManufacturer = "manufacturer";
        public const string SchemaUrl = "url";
        public const string SchemaFile = "file";

        private static string[] _supportedSchemas =
            new[] { SchemaTopic, SchemaProduct, SchemaCategory, SchemaManufacturer, SchemaUrl, SchemaFile };

        private readonly SmartDbContext _db;
        private readonly IUrlHelper _urlHelper;
        protected readonly ILocalizedEntityService _localizedEntityService;

        public DefaultLinkTranslator(SmartDbContext db, IUrlHelper urlHelper, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _urlHelper = urlHelper;
            _localizedEntityService = localizedEntityService;
        }

        public async Task<LinkTranslationResult> TranslateAsync(LinkExpression expression, int storeId, int languageId)
        {
            if (!_supportedSchemas.Contains(expression.Schema))
            {
                return null;
            }

            Type entityType = null;
            string entityName = null;
            LinkTranslatorEntitySummary summary = null;

            switch (expression.Schema)
            {
                case SchemaUrl:
                    var url = expression.TargetAndQuery;
                    if (url.StartsWith('~'))
                    {
                        url = _urlHelper.Content(url);
                    }

                    return new LinkTranslationResult { Link = url };
                case SchemaFile:
                    return new LinkTranslationResult { Link = expression.Target };
                case SchemaTopic:
                    entityType = typeof(Topic);
                    entityName = nameof(Topic);
                    summary = await GetEntityDataAsync<Topic>(expression, storeId, languageId, x => null);
                    break;
                case SchemaProduct:
                    entityType = typeof(Product);
                    entityName = nameof(Product);
                    summary = await GetEntityDataAsync<Product>(expression, storeId, languageId, x => new LinkTranslatorEntitySummary
                    {
                        Name = x.Name,
                        Published = x.Published,
                        Deleted = x.Deleted,
                        SubjectToAcl = x.SubjectToAcl,
                        LimitedToStores = x.LimitedToStores,
                        PictureId = x.MainPictureId
                    });
                    break;
                case SchemaCategory:
                    entityType = typeof(Category);
                    entityName = nameof(Category);
                    summary = await GetEntityDataAsync<Category>(expression, storeId, languageId, x => new LinkTranslatorEntitySummary
                    {
                        Name = x.Name,
                        Published = x.Published,
                        Deleted = x.Deleted,
                        SubjectToAcl = x.SubjectToAcl,
                        LimitedToStores = x.LimitedToStores,
                        PictureId = x.MediaFileId
                    });
                    break;
                case SchemaManufacturer:
                    entityType = typeof(Manufacturer);
                    entityName = nameof(Manufacturer);
                    summary = await GetEntityDataAsync<Manufacturer>(expression, storeId, languageId, x => new LinkTranslatorEntitySummary
                    {
                        Name = x.Name,
                        Published = x.Published,
                        Deleted = x.Deleted,
                        SubjectToAcl = x.SubjectToAcl,
                        LimitedToStores = x.LimitedToStores,
                        PictureId = x.MediaFileId
                    });
                    break;
            }

            return new LinkTranslationResult
            { 
                EntitySummary = summary,
                EntityName = entityName,
                EntityType = entityType
            };
        }

        private async Task<LinkTranslatorEntitySummary> GetEntityDataAsync<T>(LinkExpression expression,
            int storeId,
            int languageId,
            Expression<Func<T, LinkTranslatorEntitySummary>> selector) where T : BaseEntity
        {
            LinkTranslatorEntitySummary summary = null;
            string systemName = null;

            if (!int.TryParse(expression.Target, out var entityId))
            {
                systemName = expression.Target;
            }

            if (expression.Schema == "topic")
            {
                Topic topic = null;

                if (string.IsNullOrEmpty(systemName))
                {
                    topic = await _db.Topics.FindByIdAsync(entityId, false);
                }
                else
                {
                    topic = await _db.Topics
                        .AsNoTracking()
                        .ApplyStandardFilter(true, null, storeId)
                        .FirstOrDefaultAsync(x => x.SystemName == systemName);
                }

                if (topic != null)
                {
                    summary = new LinkTranslatorEntitySummary
                    {
                        Id = topic.Id,
                        Name = topic.SystemName,
                        Title = topic.Title,
                        ShortTitle = topic.ShortTitle,
                        Published = topic.IsPublished,
                        SubjectToAcl = topic.SubjectToAcl,
                        LimitedToStores = topic.LimitedToStores
                    };

                    summary.Label = GetLocalized(topic.Id, nameof(Topic), nameof(Topic.ShortTitle), languageId, null)
                            ?? GetLocalized(topic.Id, nameof(Topic), "Title", languageId, null)
                            ?? summary.ShortTitle.NullEmpty()
                            ?? summary.Title.NullEmpty()
                            ?? summary.Name;
                }
            }
            else
            {
                summary = await _db.Set<T>()
                    .AsNoTracking()
                    .Where(x => x.Id == entityId)
                    .Select(selector)
                    .SingleOrDefaultAsync();

                if (summary != null)
                {
                    summary.Label = GetLocalized(summary.Id, typeof(T).Name, "Name", languageId, summary.Name);
                }
            }

            return summary;
        }

        private string GetLocalized(int entityId, string localeKeyGroup, string localeKey, int languageId, string defaultValue)
        {
            return _localizedEntityService.GetLocalizedValue(languageId, entityId, localeKeyGroup, localeKey).NullEmpty() ?? defaultValue.NullEmpty();
        }
    }
}
