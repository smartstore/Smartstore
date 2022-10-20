
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public partial class LinkResolver : ILinkResolver
    {
        public const string SchemaUrl = "url";
        public const string SchemaFile = "file";

        /// <remarks>
        /// {0} : Expression w/o q
        /// {1} : LanguageId
        /// {2} : Store
        /// {3} : RolesIdent
        /// </remarks>
        public const string LinkCacheKey = "linkresolver:{0}-{1}-{2}-{3}";

        // 0: Expression
        public const string LinkCacheKeyPattern = "linkresolver:{0}-*";

        private readonly ILinkProvider[] _providers;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICacheFactory _cacheFactory;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly IUrlService _urlService;

        private static readonly object _lock = new();
        private static LinkBuilderMetadata[] _metadata;

        public LinkResolver(
            IEnumerable<ILinkProvider> providers,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICacheFactory cacheFactory,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            ILocalizedEntityService localizedEntityService,
            Lazy<IUrlHelper> urlHelper,
            IUrlService urlService)
        {
            _providers = providers.OrderBy(x => x.Order).ToArray();
            _workContext = workContext;
            _storeContext = storeContext;
            _cacheFactory = cacheFactory;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _localizedEntityService = localizedEntityService;
            _urlHelper = urlHelper;
            _urlService = urlService;

            InitializeMetadata(_providers);
        }

        private static void InitializeMetadata(ILinkProvider[] providers)
        {
            if (_metadata == null)
            {
                lock (_lock)
                {
                    if (_metadata == null)
                    {
                        _metadata = providers
                            .SelectMany(x => x.GetBuilderMetadata())
                            .Concat(new[]
                            {
                                new LinkBuilderMetadata { Schema = SchemaFile, Icon = "far fa-folder-open", ResKey = "Common.File", Order = 100 },
                                new LinkBuilderMetadata { Schema = SchemaUrl, Icon = "fa fa-link", ResKey = "Common.Url", Order = 200 }
                            })
                            .OrderBy(x => x.Order)
                            .ToArray();
                    }
                }
            }
        }

        public virtual IEnumerable<LinkBuilderMetadata> GetBuilderMetadata()
            => _metadata;

        public virtual async Task<LinkResolutionResult> ResolveAsync(LinkExpression expression, IEnumerable<CustomerRole> roles = null, int languageId = 0, int storeId = 0)
        {
            Guard.NotNull(expression, nameof(expression));

            if (expression.Target.IsEmpty())
            {
                return new LinkResolutionResult(expression, LinkStatus.NotFound);
            }

            if (expression.Schema == SchemaUrl)
            {
                var url = expression.TargetAndQuery;
                if (url.StartsWith('~'))
                {
                    url = _urlHelper.Value.Content(url);
                }

                return new LinkResolutionResult(expression, new LinkTranslationResult { Link = url }, LinkStatus.Ok);
            }
            else if (expression.Schema == SchemaFile)
            {
                return new LinkResolutionResult(
                    expression,
                    new LinkTranslationResult { Link = expression.Target, Label = expression.Target },
                    LinkStatus.Ok);
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

            var cacheKey = LinkCacheKey.FormatInvariant(
                expression.SchemaAndTarget.ToLower(),
                languageId,
                storeId,
                string.Join(",", roles.Where(x => x.Active).Select(x => x.Id)));

            var cachedResult = await _cacheFactory.GetMemoryCache().GetAsync(cacheKey, async () =>
            {
                LinkTranslationResult result = null;

                foreach (var translator in _providers)
                {
                    result = await translator.TranslateAsync(expression, storeId, languageId);
                    if (result != null)
                        break;
                }

                if (result == null)
                {
                    throw new InvalidOperationException($"Unknown schema or invalid link expression '{expression.RawExpression}'.");
                }

                if (result.Link == null && result.EntitySummary != null)
                {
                    var summary = result.EntitySummary;
                    var slug =
                        (await _urlService.GetActiveSlugAsync(summary.Id, result.EntityName, languageId)).NullEmpty() ??
                        await _urlService.GetActiveSlugAsync(summary.Id, result.EntityName, 0);

                    if (!string.IsNullOrEmpty(slug))
                    {
                        result.Link = _urlHelper.Value.RouteUrl(result.EntityName, new { SeName = slug });
                    }
                }

                EnsureLocalizedLabel(result, languageId);

                return result;
            });

            var entitySummary = cachedResult.EntitySummary;
            var status = cachedResult.Status;

            // Check final status by authorizing store & ACL
            if (entitySummary != null && status == LinkStatus.Ok)
            {
                if (entitySummary.LimitedToStores &&
                    !await _storeMappingService.AuthorizeAsync(cachedResult.EntityName, entitySummary.Id, storeId))
                {
                    status = LinkStatus.NotFound;
                }

                if (status == LinkStatus.Ok && entitySummary.SubjectToAcl &&
                    !await _aclService.AuthorizeAsync(cachedResult.EntityName, entitySummary.Id, roles))
                {
                    status = LinkStatus.Forbidden;
                }
            }

            return new LinkResolutionResult(expression, cachedResult, status);
        }

        public virtual void InvalidateLink(string schema, object target)
        {
            Guard.NotEmpty(schema, nameof(schema));
            Guard.NotNull(target, nameof(target));

            var keyPattern = LinkCacheKeyPattern.FormatInvariant(string.Concat(schema, ":", target));
            _cacheFactory.GetMemoryCache().RemoveByPattern(keyPattern);
        }

        private void EnsureLocalizedLabel(LinkTranslationResult result, int languageId)
        {
            if (result.Label.HasValue())
            {
                return;
            }

            var summary = result.EntitySummary;
            if (summary == null)
            {
                result.Label = result.Link;
                return;
            }

            if (summary.LocalizedPropertyNames != null)
            {
                foreach (var propName in summary.LocalizedPropertyNames)
                {
                    result.Label = _localizedEntityService.GetLocalizedValue(languageId, summary.Id, result.EntityName, propName);
                    if (result.Label.HasValue())
                    {
                        break;
                    }
                }
            }

            if (result.Label.IsEmpty())
            {
                result.Label = summary.Title.NullEmpty() ?? summary.ShortTitle.NullEmpty() ?? summary.Name;
            }
        }
    }
}
