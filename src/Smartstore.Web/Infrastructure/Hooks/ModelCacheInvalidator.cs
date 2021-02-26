using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Web.Infrastructure.Hooks
{
	internal partial class ModelCacheInvalidator : IDbSaveHook
	{
        // TODO: (core) Move Blog/News/Forum stuff to external modules.
		
		#region Consts

        /// <summary>
        /// Key for ManufacturerNavigationModel caching
        /// </summary>
        /// <remarks>
        /// {0} : various settings values
        /// {1} : language id
        /// {2} : current store ID
        /// {3} : customer role IDs
        /// {4} : items to display
        /// </remarks>
        public const string MANUFACTURER_NAVIGATION_MODEL_KEY = "pres:manufacturer:navigation-{0}-{1}-{2}-{3}-{4}";
		public const string MANUFACTURER_NAVIGATION_PATTERN_KEY = "pres:manufacturer:navigation*";

		/// <summary>
		/// Indicates whether a manufacturer has featured products
		/// </summary>
		/// <remarks>
		/// {0} : manufacturer id
		/// {1} : roles of the current user
		/// {2} : current store ID
		/// </remarks>
		public const string MANUFACTURER_HAS_FEATURED_PRODUCTS_KEY = "pres:manufacturer:hasfeaturedproducts-{0}-{1}-{2}";
		public const string MANUFACTURER_HAS_FEATURED_PRODUCTS_PATTERN_KEY = "pres:manufacturer:hasfeaturedproducts*";

		/// <summary>
		/// Indicates whether a category has featured products
		/// </summary>
		/// <remarks>
		/// {0} : category id
		/// {1} : roles of the current user
		/// {2} : current store ID
		/// </remarks>
		public const string CATEGORY_HAS_FEATURED_PRODUCTS_KEY = "pres:category:hasfeaturedproducts-{0}-{1}-{2}";
		public const string CATEGORY_HAS_FEATURED_PRODUCTS_PATTERN_KEY = "pres:category:hasfeaturedproducts*";

		/// <summary>
		/// Key for ProductTagModel caching
		/// </summary>
		/// <remarks>
		/// {0} : product id
		/// {1} : language id
		/// {2} : current store ID
		/// </remarks>
		public const string PRODUCTTAG_BY_PRODUCT_MODEL_KEY = "pres:producttag:byproduct-{0}-{1}-{2}";
		public const string PRODUCTTAG_BY_PRODUCT_PATTERN_KEY = "pres:producttag:byproduct*";

		/// <summary>
		/// Key for PopularProductTagsModel caching
		/// </summary>
		/// <remarks>
		/// {0} : language id
		/// {1} : current store ID
		/// </remarks>
		public const string PRODUCTTAG_POPULAR_MODEL_KEY = "pres:producttag:popular-{0}-{1}";
		public const string PRODUCTTAG_POPULAR_PATTERN_KEY = "pres:producttag:popular*";

		/// <summary>
		/// Key for ProductSpecificationModel caching
		/// </summary>
		/// <remarks>
		/// {0} : product id
		/// {1} : language id
		/// </remarks>
		public const string PRODUCT_SPECS_MODEL_KEY = "pres:product:specs-{0}-{1}";
		public const string PRODUCT_SPECS_PATTERN_KEY = "pres:product:specs*";

		/// <summary>
		/// Key for TopicModel caching
		/// </summary>
		/// <remarks>
		/// {0} : topic id/systemname
		/// {1} : language id
		/// {2} : store id
		/// {3} : role ids
		/// </remarks>
		public const string TOPIC_BY_SYSTEMNAME_KEY = "pres:topic:page.bysystemname-{0}-{1}-{2}-{3}";
		public const string TOPIC_BY_ID_KEY = "pres:topic:page.byid-{0}-{1}-{2}-{3}";
		public const string TOPIC_PATTERN_KEY = "pres:topic:page*";

		/// <summary>
		/// Key for TopicWidget caching
		/// </summary>
		/// <remarks>
		/// {0} : store id
		/// {1} : language id
		/// {2} : role ids
		/// </remarks>
		public const string TOPIC_WIDGET_ALL_MODEL_KEY = "pres:topic:widget-all-{0}-{1}-{2}";
		public const string TOPIC_WIDGET_PATTERN_KEY = "pres:topic:widget*";

		/// <summary>
		/// Key for CategoryTemplate caching
		/// </summary>
		/// <remarks>
		/// {0} : category template id
		/// </remarks>
		public const string CATEGORY_TEMPLATE_MODEL_KEY = "pres:categorytemplate-{0}";
		public const string CATEGORY_TEMPLATE_PATTERN_KEY = "pres:categorytemplate*";

		/// <summary>
		/// Key for ManufacturerTemplate caching
		/// </summary>
		/// <remarks>
		/// {0} : manufacturer template id
		/// </remarks>
		public const string MANUFACTURER_TEMPLATE_MODEL_KEY = "pres:manufacturertemplate-{0}";
		public const string MANUFACTURER_TEMPLATE_PATTERN_KEY = "pres:manufacturertemplate*";

		/// <summary>
		/// Key for ProductTemplate caching
		/// </summary>
		/// <remarks>
		/// {0} : product template id
		/// </remarks>
		public const string PRODUCT_TEMPLATE_MODEL_KEY = "pres:producttemplate-{0}";
		public const string PRODUCT_TEMPLATE_PATTERN_KEY = "pres:producttemplate*";

		/// <summary>
		/// Key for bestsellers identifiers displayed on the home page
		/// </summary>
		/// <remarks>
		/// {0} : current store id
		/// </remarks>
		public const string HOMEPAGE_BESTSELLERS_IDS_KEY = "pres:bestsellers:homepage-{0}";
		public const string HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY = "pres:bestsellers:homepage*";

		/// <summary>
		/// Key for "also purchased" product identifiers displayed on the product details page
		/// </summary>
		/// <remarks>
		/// {0} : current product id
		/// {1} : current store id
		/// </remarks>
		public const string PRODUCTS_ALSO_PURCHASED_IDS_KEY = "pres:alsopuchased-{0}-{1}";
		public const string PRODUCTS_ALSO_PURCHASED_IDS_PATTERN_KEY = "pres:alsopuchased*";

		/// <summary>
		/// Key for home page polls
		/// </summary>
		/// <remarks>
		/// {0} : language ID
		/// {1} : current store ID
		/// </remarks>
		public const string HOMEPAGE_POLLS_MODEL_KEY = "pres:poll:homepage-{0}-{1}";
		/// <summary>
		/// Key for polls by system name
		/// </summary>
		/// <remarks>
		/// {0} : poll system name
		/// {1} : language ID
		/// {2} : current store ID
		/// </remarks>
		public const string POLL_BY_SYSTEMNAME_MODEL_KEY = "pres:poll:systemname-{0}-{1}-{2}";
		public const string POLLS_PATTERN_KEY = "pres:poll:*";

		/// <summary>
		/// Key for blog tag list model
		/// </summary>
		/// <remarks>
		/// {0} : language ID
		/// {1} : store ID
		/// </remarks>
		public const string BLOG_TAGS_MODEL_KEY = "pres:blog:tags-{0}-{1}";
		/// <summary>
		/// Key for blog archive (years, months) block model
		/// </summary>
		/// <remarks>
		/// {0} : language ID
		/// {1} : current store ID
		/// </remarks>
		public const string BLOG_MONTHS_MODEL_KEY = "pres:blog:months-{0}-{1}";
		public const string BLOG_PATTERN_KEY = "pres:blog:*";

		/// <summary>
		/// Key for home page news
		/// </summary>
		/// <remarks>
		/// {0} : language ID
		/// {1} : store ID
		/// {2} : news count setting.
		/// {3} : whether to include hidden news.
		/// </remarks>
		public const string HOMEPAGE_NEWSMODEL_KEY = "pres:news:homepage-{0}-{1}-{2}-{3}";
		public const string NEWS_PATTERN_KEY = "pres:news:*";

		/// <summary>
		/// Key for states by country id
		/// </summary>
		/// <remarks>
		/// {0} : country ID
		/// {1} : addEmptyStateIfRequired value
		/// {2} : language ID
		/// </remarks>
		public const string STATEPROVINCES_BY_COUNTRY_MODEL_KEY = "pres:stateprovinces:bycountry-{0}-{1}-{2}";
		public const string STATEPROVINCES_PATTERN_KEY = "pres:stateprovinces:*";

		/// <summary>
		/// Key for available languages
		/// </summary>
		/// <remarks>
		/// {0} : current store ID
		/// </remarks>
		public const string AVAILABLE_LANGUAGES_MODEL_KEY = "pres:languages:all-{0}";
		public const string AVAILABLE_LANGUAGES_PATTERN_KEY = "pres:languages:*";

		/// <summary>
		/// Key for available currencies
		/// </summary>
		/// <remarks>
		/// {0} : language ID
		/// {1} : current store ID
		/// </remarks>
		public const string AVAILABLE_CURRENCIES_MODEL_KEY = "pres:currencies:all-{0}-{1}";
		public const string AVAILABLE_CURRENCIES_PATTERN_KEY = "pres:currencies:*";

		#endregion

		private static readonly HashSet<string> _candidateSettingKeys = new(StringComparer.OrdinalIgnoreCase)
		{
			TypeHelper.NameOf<CatalogSettings>(x => x.NumberOfProductTags, true),
			TypeHelper.NameOf<CatalogSettings>(x => x.ManufacturerItemsToDisplayOnHomepage, true),
			TypeHelper.NameOf<CatalogSettings>(x => x.NumberOfBestsellersOnHomepage, true),
			//TypeHelper.NameOf<BlogSettings>(x => x.NumberOfTags, true),
			//TypeHelper.NameOf<NewsSettings>(x => x.MainPageNewsCount, true)
		};

		private readonly ICacheManager _cache;

		public ModelCacheInvalidator(ICacheManager cache)
		{
			_cache = cache;
		}

		public Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
			=> Task.FromResult(HookResult.Void);

		public async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
		{
			var state = entry.InitialState;
			var entity = entry.Entity;
			var result = HookResult.Ok;

			if (entity is Product && state == EntityState.Modified)
			{
				var deleted = ((Product)entity).Deleted;
				if (deleted)
				{
					await _cache.RemoveByPatternAsync(HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
					await _cache.RemoveByPatternAsync(PRODUCTS_ALSO_PURCHASED_IDS_PATTERN_KEY);
				}
				else
				{
					await _cache.RemoveByPatternAsync(HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
					await _cache.RemoveByPatternAsync(PRODUCTS_ALSO_PURCHASED_IDS_PATTERN_KEY);
					await _cache.RemoveByPatternAsync(PRODUCTTAG_POPULAR_PATTERN_KEY);
					await _cache.RemoveByPatternAsync(PRODUCTTAG_BY_PRODUCT_PATTERN_KEY);
				}
			}
			else if (entity is Manufacturer)
			{
				await _cache.RemoveByPatternAsync(MANUFACTURER_NAVIGATION_PATTERN_KEY);
			}
			else if (entity is ProductManufacturer)
			{
				await _cache.RemoveByPatternAsync(MANUFACTURER_HAS_FEATURED_PRODUCTS_PATTERN_KEY);
			}
			else if (entity is ProductCategory)
			{
				await _cache.RemoveByPatternAsync(CATEGORY_HAS_FEATURED_PRODUCTS_PATTERN_KEY);
			}
			else if (entity is ProductTag)
			{
				await _cache.RemoveByPatternAsync(PRODUCTTAG_POPULAR_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(PRODUCTTAG_BY_PRODUCT_PATTERN_KEY);
			}
			else if (entity is SpecificationAttribute && state != EntityState.Added)
			{
				await _cache.RemoveByPatternAsync(PRODUCT_SPECS_PATTERN_KEY);
			}
			else if (entity is SpecificationAttributeOption && state != EntityState.Added)
			{
				await _cache.RemoveByPatternAsync(PRODUCT_SPECS_PATTERN_KEY);
			}
			else if (entity is ProductSpecificationAttribute)
			{
				await _cache.RemoveByPatternAsync(PRODUCT_SPECS_PATTERN_KEY);
			}
			else if (entity is Topic)
			{
				await _cache.RemoveByPatternAsync(TOPIC_WIDGET_PATTERN_KEY);
				if (state != EntityState.Added)
				{
					await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
				}
			}
			else if (entity is CategoryTemplate)
			{
				await _cache.RemoveByPatternAsync(CATEGORY_TEMPLATE_PATTERN_KEY);
			}
			else if (entity is ManufacturerTemplate)
			{
				await _cache.RemoveByPatternAsync(MANUFACTURER_TEMPLATE_PATTERN_KEY);
			}
			else if (entity is ProductTemplate)
			{
				await _cache.RemoveByPatternAsync(PRODUCT_TEMPLATE_PATTERN_KEY);
			}
			else if (entity is Currency)
			{
				await _cache.RemoveByPatternAsync(AVAILABLE_CURRENCIES_PATTERN_KEY);
			}
			else if (entity is Language)
			{
				// Clear all localizable models
				await _cache.RemoveByPatternAsync(MANUFACTURER_NAVIGATION_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(PRODUCT_SPECS_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(STATEPROVINCES_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(AVAILABLE_LANGUAGES_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(AVAILABLE_CURRENCIES_PATTERN_KEY);
			}
			else if (entity is Order || entity is OrderItem)
			{
				await _cache.RemoveByPatternAsync(HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
				await _cache.RemoveByPatternAsync(PRODUCTS_ALSO_PURCHASED_IDS_PATTERN_KEY);
			}
			//else if (entity is Poll)
			//{
			//	_cache.RemoveByPatternAsync(POLLS_PATTERN_KEY);
			//}
			//else if (entity is BlogPost)
			//{
			//	_cache.RemoveByPatternAsync(BLOG_PATTERN_KEY);
			//}
			//else if (entity is NewsItem)
			//{
			//	_cache.RemoveByPatternAsync(NEWS_PATTERN_KEY);
			//}
			else if (entity is StateProvince)
			{
				await _cache.RemoveByPatternAsync(STATEPROVINCES_PATTERN_KEY);
			}
			else if (entity is LocalizedProperty lp)
			{
				if (lp.LocaleKeyGroup == nameof(Topic))
				{
					await _cache.RemoveByPatternAsync(TOPIC_WIDGET_PATTERN_KEY);
					if (state != EntityState.Added)
					{
						await _cache.RemoveByPatternAsync(TOPIC_PATTERN_KEY);
					}
				}
			}
			else if (entity is Setting)
			{
				var setting = entity as Setting;
				if (_candidateSettingKeys.Contains(setting.Name))
				{
					// Clear models which depend on settings
					await _cache.RemoveByPatternAsync(PRODUCTTAG_POPULAR_PATTERN_KEY); // depends on CatalogSettings.NumberOfProductTags
					await _cache.RemoveByPatternAsync(MANUFACTURER_NAVIGATION_PATTERN_KEY); // depends on CatalogSettings.ManufacturerItemsToDisplayOnHomepage
					await _cache.RemoveByPatternAsync(HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY); // depends on CatalogSettings.NumberOfBestsellersOnHomepage
					await _cache.RemoveByPatternAsync(BLOG_PATTERN_KEY); // depends on BlogSettings.NumberOfTags
					await _cache.RemoveByPatternAsync(NEWS_PATTERN_KEY); // depends on NewsSettings.MainPageNewsCount
				}
			}
			else
			{
				// Register as void hook for all other entity type/state combis
				result = HookResult.Void;
			}

			return result;
		}

		public Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
			=> Task.CompletedTask;

		public Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
			=> Task.CompletedTask;
	}
}
