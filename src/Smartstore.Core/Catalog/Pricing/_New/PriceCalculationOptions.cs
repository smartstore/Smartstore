using System;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationOptions
    {
        private ProductBatchContext _batchContext;
        private Customer _customer;
        private Store _store;
        private Language _language;
        private Currency _targetCurrency;

        public PriceCalculationOptions(ProductBatchContext batchContext, Customer customer, Store store, Language language, Currency targetCurrency) 
        {
            BatchContext = batchContext;
            Customer = customer;
            Store = store;
            Language = language;
            TargetCurrency = targetCurrency;
            CashRounding = new();
        }

        public ProductBatchContext BatchContext 
        {
            get => _batchContext; 
            set => _batchContext = value ?? throw new ArgumentNullException(nameof(BatchContext));
        }

        public Customer Customer
        {
            get => _customer;
            set => _customer = value ?? throw new ArgumentNullException(nameof(Customer));
        }

        public Store Store
        {
            get => _store;
            set => _store = value ?? throw new ArgumentNullException(nameof(Store));
        }

        public Language Language
        {
            get => _language;
            set => _language = value ?? throw new ArgumentNullException(nameof(Language));
        }

        public Currency TargetCurrency
        {
            get => _targetCurrency;
            set => _targetCurrency = value ?? throw new ArgumentNullException(nameof(TargetCurrency));
        }


        public bool GrossPrices { get; set; }
        public CashRoundingOptions CashRounding { get; init; }

        public bool IgnoreDiscounts { get; set; }
        public bool IgnoreTierPrices { get; set; }
        public bool IgnoreAttributes { get; set; }
        public string TaxFormat { get; set; }

        public bool DetermineSelectionPrice { get; set; }
        public bool DetermineLowestPrice { get; set; }
        public bool DetermineMinTierPrice { get; set; }
        public bool DetermineMinAttributeCombinationPrice { get; set; }

        //public bool ApplyCartRules { get; set; }
    }
}
