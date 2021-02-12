using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Net;

namespace Smartstore.Core.Content.Menus
{
    public partial class LinkResolver : ILinkResolver
    {
        /// <remarks>
        /// {0} : Expression w/o q
        /// {1} : LanguageId
        /// {2} : Store
        /// {3} : RolesIdent
        /// </remarks>
        internal const string LINKRESOLVER_KEY = "linkresolver:{0}-{1}-{2}-{3}";

        // 0: Expression
        internal const string LINKRESOLVER_PATTERN_KEY = "linkresolver:{0}-*";

        protected readonly SmartDbContext _db;
        protected readonly IWorkContext _workContext;
        protected readonly IStoreContext _storeContext;
        protected readonly ICacheManager _cache;
        protected readonly ILocalizedEntityService _localizedEntityService;
        protected readonly IAclService _aclService;
        protected readonly IStoreMappingService _storeMappingService;
        protected readonly IUrlHelper _urlHelper;
        protected readonly IUrlService _urlService;

        public LinkResolver(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICacheManager cache,
            ILocalizedEntityService localizedEntityService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IUrlHelper urlHelper,
            IUrlService urlService)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _cache = cache;
            _localizedEntityService = localizedEntityService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _urlHelper = urlHelper;
            _urlService = urlService;
        }

        public virtual async Task<LinkResolverResult> ResolveAsync(string linkExpression, IEnumerable<CustomerRole> roles = null, int languageId = 0, int storeId = 0)
        {
            if (linkExpression.IsEmpty())
            {
                return new LinkResolverResult { Type = LinkType.Url, Status = LinkStatus.NotFound };
            }

            if (roles == null)
            {
                roles = _workContext.CurrentCustomer.CustomerRoleMappings.Select(x => x.CustomerRole);
            }

            if (languageId == 0)
            {
                languageId = _workContext.WorkingLanguage.Id;
            }

            if (storeId == 0)
            {
                storeId = _storeContext.CurrentStore.Id;
            }

            var d = Parse(linkExpression);
            var queryString = d.QueryString;

            if (d.Type == LinkType.Url)
            {
                var url = d.Value.ToString();
                if (url.EmptyNull().StartsWith("~"))
                {
                    url = _urlHelper.Content(url);
                }
                d.Link = d.Label = url;
            }
            else if (d.Type == LinkType.File)
            {
                d.Link = d.Label = d.Value.ToString();
            }
            else
            {
                var cacheKey = LINKRESOLVER_KEY.FormatInvariant(
                    d.Expression.EmptyNull().ToLower(),
                    languageId,
                    storeId,
                    string.Join(",", roles.Where(x => x.Active).Select(x => x.Id)));

                d = await _cache.GetAsync(cacheKey, async () =>
                {
                    var d2 = d.Clone();

                    switch (d2.Type)
                    {
                        case LinkType.Product:
                            await GetEntityDataAsync<Product>(d2, storeId, languageId, x => new ResolverEntitySummary
                            {
                                Name = x.Name,
                                Published = x.Published,
                                Deleted = x.Deleted,
                                SubjectToAcl = x.SubjectToAcl,
                                LimitedToStores = x.LimitedToStores,
                                PictureId = x.MainPictureId
                            });
                            break;
                        case LinkType.Category:
                            await GetEntityDataAsync<Category>(d2, storeId, languageId, x => new ResolverEntitySummary
                            {
                                Name = x.Name,
                                Published = x.Published,
                                Deleted = x.Deleted,
                                SubjectToAcl = x.SubjectToAcl,
                                LimitedToStores = x.LimitedToStores,
                                PictureId = x.MediaFileId
                            });
                            break;
                        case LinkType.Manufacturer:
                            await GetEntityDataAsync<Manufacturer>(d2, storeId, languageId, x => new ResolverEntitySummary
                            {
                                Name = x.Name,
                                Published = x.Published,
                                Deleted = x.Deleted,
                                LimitedToStores = x.LimitedToStores,
                                PictureId = x.MediaFileId
                            });
                            break;
                        case LinkType.Topic:
                            await GetEntityDataAsync<Topic>(d2, storeId, languageId, x => null);
                            break;

                        // TODO: (mh) (core) Develop LinkExpressionProvider so modules can hook into this.

                        //case LinkType.BlogPost:
                        //    await GetEntityDataAsync<BlogPost>(d2, storeId, languageId, x => new ResolverEntitySummary
                        //    {
                        //        Name = x.Title,
                        //        Published = x.IsPublished,
                        //        LimitedToStores = x.LimitedToStores,
                        //        PictureId = x.MediaFileId
                        //    });
                        //    break;
                        //case LinkType.NewsItem:
                        //    await GetEntityDataAsync<NewsItem>(d2, storeId, languageId, x => new ResolverEntitySummary
                        //    {
                        //        Name = x.Title,
                        //        Published = x.Published,
                        //        LimitedToStores = x.LimitedToStores,
                        //        PictureId = x.MediaFileId
                        //    });
                        //    break;
                        default:
                            throw new SmartException("Unknown link builder type.");
                    }

                    return d2;
                });
            }

            var result = new LinkResolverResult
            {
                Type = d.Type,
                Status = d.Status,
                Value = d.Value,
                Link = d.Link,
                QueryString = queryString,
                Label = d.Label,
                Id = d.Id,
                PictureId = d.PictureId
            };
            
            // Check ACL and limited to stores.
            switch (d.Type)
            {
                case LinkType.Product:
                case LinkType.Category:
                case LinkType.Manufacturer:
                case LinkType.Topic:
                case LinkType.BlogPost:
                case LinkType.NewsItem:
                    var entityName = d.Type.ToString();

                    if (d.CheckLimitedToStores &&
                        d.LimitedToStores &&
                        d.Status == LinkStatus.Ok &&
                        !await _storeMappingService.AuthorizeAsync(entityName, d.Id, storeId))
                    {
                        result.Status = LinkStatus.NotFound;
                    }
                    else if (d.SubjectToAcl &&
                        d.Status == LinkStatus.Ok &&
                        !_db.QuerySettings.IgnoreAcl &&
                        !await _aclService.AuthorizeAsync(entityName, d.Id, roles))
                    {
                        result.Status = LinkStatus.Forbidden;
                    }
                    break;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TokenizeExpression(string expression, out string type, out string path, out string query)
        {
            type = null;
            path = null;
            query = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            var colonIndex = expression.IndexOf(':');
            if (colonIndex > -1)
            {
                type = expression.Substring(0, colonIndex);
                if (type.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    type = null;
                    colonIndex = -1;
                }
            }

            path = expression[(colonIndex + 1)..];

            var qmIndex = path.IndexOf('?');
            if (qmIndex > -1)
            {
                query = path[(qmIndex + 1)..];
                path = path.Substring(0, qmIndex);
            }

            return true;
        }

        protected virtual string GetLocalized(int entityId, string localeKeyGroup, string localeKey, int languageId, string defaultValue)
        {
            return _localizedEntityService.GetLocalizedValue(languageId, entityId, localeKeyGroup, localeKey).NullEmpty() ?? defaultValue.NullEmpty();
        }

        protected virtual LinkResolverData Parse(string linkExpression)
        {
            if (TokenizeExpression(linkExpression, out var type, out var path, out var query))
            {
                if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse(type, true, out LinkType linkType))
                {
                    var result = new LinkResolverData { Type = linkType, Expression = string.Concat(type, ":", path) };

                    switch (linkType)
                    {
                        case LinkType.Product:
                        case LinkType.Category:
                        case LinkType.Manufacturer:
                        case LinkType.Topic:
                        case LinkType.BlogPost:
                        case LinkType.NewsItem:
                            if (int.TryParse(path, out var id))
                            {
                                // Reduce thrown exceptions in console
                                result.Value = id;
                            }
                            else
                            {
                                result.Value = path;
                            }

                            result.QueryString = query;
                            break;
                        case LinkType.Url:
                            result.Value = path + (query.HasValue() ? "?" + query : "");
                            break;
                        case LinkType.File:
                            result.Value = path;
                            result.QueryString = query;
                            break;
                        default:
                            throw new SmartException("Unknown link builder type.");
                    }

                    return result;
                }
            }

            return new LinkResolverData { Type = LinkType.Url, Value = linkExpression.EmptyNull() };
        }

        internal async Task GetEntityDataAsync<T>(
            LinkResolverData data,
            int storeId,
            int languageId,
            Expression<Func<T, ResolverEntitySummary>> selector) where T : BaseEntity
        {
            ResolverEntitySummary summary = null;
            string systemName = null;

            if (data.Value is string str)
            {
                data.Id = 0;
                systemName = str;
            }
            else
            {
                data.Id = (int)data.Value;
            }

            if (data.Type == LinkType.Topic)
            {
                Topic topic = null;

                if (string.IsNullOrEmpty(systemName))
                {
                    topic = await _db.Topics.FindByIdAsync(data.Id, false);
                }
                else
                {
                    topic = await _db.Topics
                        .AsNoTracking()
                        .ApplyStandardFilter(true, null, storeId)
                        .FirstOrDefaultAsync(x => x.SystemName == systemName);

                    data.CheckLimitedToStores = false;
                }

                if (topic != null)
                {
                    summary = new ResolverEntitySummary
                    {
                        Id = topic.Id,
                        Name = topic.SystemName,
                        Title = topic.Title,
                        ShortTitle = topic.ShortTitle,
                        Published = topic.IsPublished,
                        SubjectToAcl = topic.SubjectToAcl,
                        LimitedToStores = topic.LimitedToStores
                    };
                }
            }
            else
            {
                summary = await _db.Set<T>()
                    .AsNoTracking()
                    .Where(x => x.Id == data.Id)
                    .Select(selector)
                    .SingleOrDefaultAsync();
            }

            if (summary != null)
            {
                var entityName = data.Type.ToString();

                data.Id = summary.Id != 0 ? summary.Id : data.Id;
                data.SubjectToAcl = summary.SubjectToAcl;
                data.LimitedToStores = summary.LimitedToStores;
                data.PictureId = summary.PictureId;
                data.Status = summary.Deleted
                    ? LinkStatus.NotFound
                    : summary.Published ? LinkStatus.Ok : LinkStatus.Hidden;

                switch (data.Type)
                {
                    case LinkType.Topic:
                        data.Label = GetLocalized(data.Id, entityName, nameof(Topic.ShortTitle), languageId, null)
                            ?? GetLocalized(data.Id, entityName, "Title", languageId, null)
                            ?? summary.ShortTitle.NullEmpty()
                            ?? summary.Title.NullEmpty()
                            ?? summary.Name;
                        break;
                    case LinkType.BlogPost:
                    case LinkType.NewsItem:
                        data.Label = GetLocalized(data.Id, entityName, "Title", languageId, summary.Name);
                        break;
                    default:
                        data.Label = GetLocalized(data.Id, entityName, "Name", languageId, summary.Name);
                        break;
                }

                var slug = await _urlService.GetActiveSlugAsync(data.Id, entityName, languageId);
                data.Slug = slug.NullEmpty() ?? await _urlService.GetActiveSlugAsync(data.Id, entityName, 0);
                if (!string.IsNullOrEmpty(data.Slug))
                {
                    data.Link = _urlHelper.RouteUrl(entityName, new { SeName = data.Slug });
                }
            }
            else
            {
                data.Label = systemName;
                data.Status = LinkStatus.NotFound;
            }
        }
    }

    internal class ResolverEntitySummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public bool Deleted { get; set; }
        public bool Published { get; set; }
        public bool SubjectToAcl { get; set; }
        public bool LimitedToStores { get; set; }
        public int? PictureId { get; set; }
    }
}
