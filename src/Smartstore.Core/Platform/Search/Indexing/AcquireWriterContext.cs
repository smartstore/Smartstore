using Smartstore.Collections;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Search.Indexing
{
    public enum AcquirementReason
    {
        Indexing,
        Deleting
    }

    public class AcquireWriterContext
    {
        public AcquireWriterContext(AcquirementReason reason, CancellationToken cancelToken = default)
        {
            Reason = reason;
            CancelToken = cancelToken;
            Languages = new List<Language>();
            Currencies = new List<Currency>();
            StoreMappings = new Multimap<int, int>();
            CustomerRoleIds = Array.Empty<int>();
            CustomerRoleMappings = new Multimap<int, int>();
            DeliveryTimes = new Dictionary<int, DeliveryTime>();
            Manufacturers = new Dictionary<int, Manufacturer>();
            Categories = new Dictionary<int, Category>();
            Translations = new Dictionary<string, LocalizedPropertyCollection>(StringComparer.OrdinalIgnoreCase);
            CustomProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Reason for writer acquirement
        /// </summary>
        public AcquirementReason Reason { get; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        public CancellationToken CancelToken { get; }

        /// <summary>
        /// All languages
        /// </summary>
        public IList<Language> Languages { get; set; }

        /// <summary>
        /// Currency codes used for indexing
        /// </summary>
        public IList<Currency> Currencies { get; set; }

        /// <summary>
        /// All stores
        /// </summary>
        public IList<Store> Stores { get; set; }

        /// <summary>
        /// Map of product to store identifiers if the product is limited to certain stores
        /// </summary>
        public Multimap<int, int> StoreMappings { get; set; }

        /// <summary>
        /// Array of all customer role identifiers
        /// </summary>
        public int[] CustomerRoleIds { get; set; }

        /// <summary>
        /// Map of product to customer role identifiers if the product is limited to certain customer roles
        /// </summary>
        public Multimap<int, int> CustomerRoleMappings { get; set; }

        /// <summary>
        /// All manufacturers
        /// </summary>
        public Dictionary<int, Manufacturer> Manufacturers { get; set; }

        /// <summary>
        /// All categories
        /// </summary>
        public Dictionary<int, Category> Categories { get; set; }

        /// <summary>
        /// All delivery times
        /// </summary>
        public Dictionary<int, DeliveryTime> DeliveryTimes { get; set; }

        /// <summary>
        /// All translations for global scopes (like Category, Manufacturer etc.)
        /// </summary>
        public Dictionary<string, LocalizedPropertyCollection> Translations { get; set; }

        /// <summary>
        /// Use this dictionary for any custom data required along indexing
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; }

        public void Clear()
        {
            Languages.Clear();
            Currencies.Clear();
            StoreMappings.Clear();
            CustomerRoleMappings.Clear();
            Manufacturers.Clear();
            Categories.Clear();
            DeliveryTimes.Clear();
            Translations.Clear();
            CustomProperties.Clear();
        }
    }
}