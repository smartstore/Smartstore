using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Smartstore.Caching;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Content.Menus
{
    public class LinkResolver2
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

        protected readonly IEnumerable<ILinkTranslator> _translators;
        protected readonly IWorkContext _workContext;
        protected readonly IStoreContext _storeContext;
        protected readonly ICacheFactory _cacheFactory;
        protected readonly IAclService _aclService;
        protected readonly IStoreMappingService _storeMappingService;
        protected readonly IUrlService _urlService;

        public LinkResolver2(
            IEnumerable<ILinkTranslator> translators,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICacheFactory cacheFactory,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IUrlService urlService)
        {
            _translators = translators;
            _workContext = workContext;
            _storeContext = storeContext;
            _cacheFactory = cacheFactory;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _urlService = urlService;
        }

        public virtual async Task<LinkResolverResult> ResolveAsync(LinkExpression expression, IEnumerable<CustomerRole> roles = null, int languageId = 0, int storeId = 0)
        {
            Guard.NotNull(expression, nameof(expression));

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

            var cacheKey = LINKRESOLVER_KEY.FormatInvariant(
                expression.SchemaAndTarget.ToLower(),
                languageId,
                storeId,
                string.Join(",", roles.Where(x => x.Active).Select(x => x.Id)));

            var cachedTranslationResult = await _cacheFactory.GetMemoryCache().GetAsync(cacheKey, async () => 
            {
                LinkTranslationResult translationResult = null;

                foreach (var translator in _translators)
                {
                    translationResult = await translator.TranslateAsync(expression, storeId, languageId);
                    if (translationResult != null)
                        break;
                }

                if (translationResult == null)
                {
                    throw new SmartException($"Unknown schema or invalid link expression '{expression.RawExpression}'.");
                }

                return translationResult;
            });

            var entity = cachedTranslationResult?.EntitySummary;

            return new LinkResolverResult
            {
                Expression = expression.SchemaAndTarget,
                Id = entity?.Id ?? 0,
                Label = entity?.Label,
                Link = null, // TODO
                PictureId = entity?.PictureId,
                QueryString = expression.Query,
                Slug = null, // TODO,
                Status = LinkStatus.Ok, // TODO
                Type = LinkType.Url, // TODO
                Value = null // TODO
            };
        }
    }
}
