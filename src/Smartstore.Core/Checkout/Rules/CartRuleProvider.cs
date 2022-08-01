using System.Runtime.CompilerServices;
using Autofac;

using Smartstore.Core.Checkout.Rules.Impl;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartRuleProvider : RuleProviderBase, ICartRuleProvider
    {
        private readonly IComponentContext _componentContext;
        private readonly IRuleService _ruleService;
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public CartRuleProvider(
            IComponentContext componentContext,
            IRuleService ruleService,
            ICurrencyService currencyService,
            IWorkContext workContext,
            IStoreContext storeContext)
            : base(RuleScope.Cart)
        {
            _componentContext = componentContext;
            _ruleService = ruleService;
            _currencyService = currencyService;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public IRule GetProcessor(RuleExpression expression)
        {
            var group = expression as RuleExpressionGroup;
            var descriptor = expression.Descriptor as CartRuleDescriptor;

            if (group == null && descriptor == null)
            {
                throw new InvalidOperationException($"Missing cart rule descriptor for expression {expression.Id} ('{expression.RawValue.EmptyNull()}').");
            }

            IRule instance;

            if (group == null && descriptor.ProcessorType != typeof(CompositeRule))
            {
                instance = _componentContext.ResolveKeyed<IRule>(descriptor.ProcessorType);
            }
            else
            {
                instance = new CompositeRule(group, this);
            }

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<RuleExpressionGroup> CreateExpressionGroupAsync(int ruleSetId)
        {
            return await _ruleService.CreateExpressionGroupAsync(ruleSetId, this) as RuleExpressionGroup;
        }

        public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
        {
            var expression = new RuleExpression();
            await base.ConvertRuleAsync(rule, expression);
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            var group = new RuleExpressionGroup
            {
                Id = ruleSet.Id,
                LogicalOperator = ruleSet.LogicalOperator,
                IsSubGroup = ruleSet.IsSubGroup,
                Value = ruleSet.Id,
                RawValue = ruleSet.Id.ToString(),
                Provider = this,
                Descriptor = new CartRuleDescriptor
                {
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(CompositeRule)
                }
            };

            return group;
        }

        public async Task<bool> RuleMatchesAsync(int[] ruleSetIds, LogicalRuleOperator logicalOperator)
        {
            Guard.NotNull(ruleSetIds, nameof(ruleSetIds));

            if (ruleSetIds.Length == 0)
            {
                return true;
            }

            var expressions = await ruleSetIds
                .SelectAwait(id => _ruleService.CreateExpressionGroupAsync(id, this))
                .Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArrayAsync();

            return await RuleMatchesAsync(expressions, logicalOperator);
        }

        public async Task<bool> RuleMatchesAsync(IRulesContainer entity, LogicalRuleOperator logicalOperator = LogicalRuleOperator.Or)
        {
            Guard.NotNull(entity, nameof(entity));

            var ruleSets = entity.RuleSets.Where(x => x.Scope == RuleScope.Cart).ToArray();
            if (!ruleSets.Any())
            {
                return true;
            }

            var expressions = await ruleSets
                .SelectAwait(x => _ruleService.CreateExpressionGroupAsync(x, this))
                .Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArrayAsync();

            return await RuleMatchesAsync(expressions, logicalOperator);
        }

        public async Task<bool> RuleMatchesAsync(RuleExpression[] expressions, LogicalRuleOperator logicalOperator)
        {
            Guard.NotNull(expressions, nameof(expressions));

            if (expressions.Length == 0)
            {
                return true;
            }

            RuleExpressionGroup group;

            if (expressions.Length == 1 && expressions[0] is RuleExpressionGroup group2)
            {
                group = group2;
            }
            else
            {
                group = new RuleExpressionGroup { LogicalOperator = logicalOperator };
                group.AddExpressions(expressions);
            }

            var context = new CartRuleContext(() => group.GetHashCode())
            {
                Customer = _workContext.CurrentCustomer,
                Store = _storeContext.CurrentStore,
                WorkContext = _workContext
            };

            var processor = GetProcessor(group);
            var result = await processor.MatchAsync(context, group);

            return result;
        }

        protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
        {
            var language = _workContext.WorkingLanguage;
            var currencyCode = _currencyService.PrimaryCurrency.CurrencyCode;

            var stores = _storeContext.GetAllStores()
                .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                .ToArray();

            var cartItemQuantity = new CartRuleDescriptor
            {
                Name = "CartItemQuantity",
                DisplayName = T("Admin.Rules.FilterDescriptor.CartItemQuantity"),
                RuleType = RuleType.String,
                ProcessorType = typeof(CartItemQuantityRule),
                Operators = new[] { RuleOperator.IsEqualTo }
            };
            cartItemQuantity.Metadata["ValueTemplateName"] = "ValueTemplates/CartItemQuantity";
            cartItemQuantity.Metadata["ChildRuleDescriptor"] = new CartRuleDescriptor
            {
                Name = "CartItemQuantity.Product",
                RuleType = RuleType.Int,
                ProcessorType = typeof(CartItemQuantityRule),
                Operators = new[] { RuleOperator.IsEqualTo },
                SelectList = new RemoteRuleValueSelectList("Product")
            };

            var cartItemFromCategoryQuantity = new CartRuleDescriptor
            {
                Name = "CartItemFromCategoryQuantity",
                DisplayName = T("Admin.Rules.FilterDescriptor.CartItemFromCategoryQuantity"),
                RuleType = RuleType.String,
                ProcessorType = typeof(CartItemFromCategoryQuantityRule),
                Operators = new[] { RuleOperator.IsEqualTo }
            };
            cartItemFromCategoryQuantity.Metadata["ValueTemplateName"] = "ValueTemplates/CartItemQuantity";
            cartItemFromCategoryQuantity.Metadata["ChildRuleDescriptor"] = new CartRuleDescriptor
            {
                Name = "CartItemQuantity.Category",
                RuleType = RuleType.Int,
                ProcessorType = typeof(CartItemFromCategoryQuantityRule),
                Operators = new[] { RuleOperator.IsEqualTo },
                SelectList = new RemoteRuleValueSelectList("Category")
            };

            var descriptors = new List<CartRuleDescriptor>
            {
                new CartRuleDescriptor
                {
                    Name = "Currency",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Currency"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(CurrencyRule),
                    SelectList = new RemoteRuleValueSelectList("Currency") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "Language",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Language"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(LanguageRule),
                    SelectList = new RemoteRuleValueSelectList("Language") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "Store",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Store"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(StoreRule),
                    SelectList = new LocalRuleValueSelectList(stores) { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "IPCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IPCountry"),
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(IPCountryRule),
                    SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "Weekday",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Weekday"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(WeekdayRule),
                    SelectList = new LocalRuleValueSelectList(WeekdayRule.GetDefaultValues(language)) { Multiple = true }
                },

                new CartRuleDescriptor
                {
                    Name = "CustomerRole",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IsInCustomerRole"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(CustomerRoleRule),
                    SelectList = new RemoteRuleValueSelectList("CustomerRole") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "CartBillingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BillingCountry"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(BillingCountryRule),
                    SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "CartShippingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingCountry"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ShippingCountryRule),
                    SelectList = new RemoteRuleValueSelectList("Country") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "CartShippingMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingMethod"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ShippingMethodRule),
                    SelectList = new RemoteRuleValueSelectList("ShippingMethod") { Multiple = true }
                },
                new CartRuleDescriptor
                {
                    Name = "CartPaymentMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaymentMethod"),
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(PaymentMethodRule),
                    SelectList = new RemoteRuleValueSelectList("PaymentMethod") { Multiple = true }
                },

                new CartRuleDescriptor
                {
                    Name = "CartTotal",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartTotal"),
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(CartTotalRule)
                },
                new CartRuleDescriptor
                {
                    Name = "CartSubtotal",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartSubtotal"),
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(CartSubtotalRule)
                },
                new CartRuleDescriptor
                {
                    Name = "CartProductCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartProductCount"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(CartProductCountRule)
                },
                cartItemQuantity,
                cartItemFromCategoryQuantity,
                new CartRuleDescriptor
                {
                    Name = "ProductInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductInCartRule),
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "ProductFromCategoryInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductFromCategoryInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductFromCategoryInCartRule),
                    SelectList = new RemoteRuleValueSelectList("Category") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "ProductFromManufacturerInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductFromManufacturerInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductFromManufacturerInCartRule),
                    SelectList = new RemoteRuleValueSelectList("Manufacturer") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "ProductInWishlist",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductOnWishlist"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductOnWishlistRule),
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "ProductReviewCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductReviewCount"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(ProductReviewCountRule)
                },
                new CartRuleDescriptor
                {
                    Name = "RewardPointsBalance",
                    DisplayName = T("Admin.Rules.FilterDescriptor.RewardPointsBalance"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(RewardPointsBalanceRule)
                },
                new CartRuleDescriptor
                {
                    Name = "RuleSet",
                    DisplayName = T("Admin.Rules.FilterDescriptor.RuleSet"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(RuleSetRule),
                    Operators = new[] { RuleOperator.IsEqualTo, RuleOperator.IsNotEqualTo },
                    SelectList = new RemoteRuleValueSelectList("CartRule"),
                },

                new CartRuleDescriptor
                {
                    Name = "CartOrderCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderCount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(OrderCountRule)
                },
                new CartRuleDescriptor
                {
                    Name = "CartSpentAmount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.SpentAmount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(SpentAmountRule)
                },
                new CartRuleDescriptor
                {
                    Name = "CartPaidBy",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaidBy"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(PaidByRule),
                    SelectList = new RemoteRuleValueSelectList("PaymentMethod") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "CartPurchasedProduct",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PurchasedProduct"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(PurchasedProductRule),
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true },
                    IsComparingSequences = true
                },
                new CartRuleDescriptor
                {
                    Name = "CartPurchasedFromManufacturer",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PurchasedFromManufacturer"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(PurchasedFromManufacturerRule),
                    SelectList = new RemoteRuleValueSelectList("Manufacturer") { Multiple = true },
                    IsComparingSequences = true
                },

                new CartRuleDescriptor
                {
                    Name = "UserAgent.IsMobile",
                    DisplayName = T("Admin.Rules.FilterDescriptor.MobileDevice"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(IsMobileRule)
                },
                new CartRuleDescriptor
                {
                    Name = "UserAgent.Device",
                    DisplayName = T("Admin.Rules.FilterDescriptor.DeviceFamily"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(DeviceRule),
                    SelectList = new LocalRuleValueSelectList(DeviceRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new CartRuleDescriptor
                {
                    Name = "UserAgent.OS",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OperatingSystem"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(OSRule),
                    SelectList = new LocalRuleValueSelectList(OSRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new CartRuleDescriptor
                {
                    Name = "UserAgent.Browser",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BrowserName"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(BrowserRule),
                    SelectList = new LocalRuleValueSelectList(BrowserRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new CartRuleDescriptor
                {
                    Name = "UserAgent.BrowserMajorVersion",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BrowserMajorVersion"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(BrowserMajorVersionRule)
                },
                new CartRuleDescriptor
                {
                    Name = "UserAgent.BrowserMinorVersion",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BrowserMinorVersion"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(BrowserMinorVersionRule)
                },
            };

            descriptors
                .Where(x => x.RuleType == RuleType.Money)
                .Each(x => x.Metadata["postfix"] = currencyCode);

            return Task.FromResult(descriptors.Cast<RuleDescriptor>());
        }
    }
}
