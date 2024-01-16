using System.Runtime.CompilerServices;
using Autofac;
using Smartstore.Core.Checkout.Cart;
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
        private readonly IShoppingCartService _shoppingCartService;

        public CartRuleProvider(
            IComponentContext componentContext,
            IRuleService ruleService,
            ICurrencyService currencyService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IShoppingCartService shoppingCartService)
            : base(RuleScope.Cart)
        {
            _componentContext = componentContext;
            _ruleService = ruleService;
            _currencyService = currencyService;
            _workContext = workContext;
            _storeContext = storeContext;
            _shoppingCartService = shoppingCartService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public IRule<CartRuleContext> GetProcessor(RuleExpression expression)
        {
            var group = expression as RuleExpressionGroup;
            var descriptor = expression.Descriptor as CartRuleDescriptor;

            if (group == null && descriptor == null)
            {
                throw new InvalidOperationException($"Missing cart rule descriptor for expression {expression.Id} ('{expression.RawValue.EmptyNull()}').");
            }

            IRule< CartRuleContext> instance;

            if (group == null && descriptor.ProcessorType != typeof(CartCompositeRule))
            {
                instance = _componentContext.ResolveKeyed<IRule<CartRuleContext>>(descriptor.ProcessorType);
            }
            else
            {
                instance = new CartCompositeRule(group, this);
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
                    ProcessorType = typeof(CartCompositeRule)
                }
            };

            return group;
        }

        public async Task<bool> RuleMatchesAsync(
            int[] ruleSetIds, 
            LogicalRuleOperator logicalOperator,
            Action<CartRuleContext> contextAction = null)
        {
            Guard.NotNull(ruleSetIds);

            if (ruleSetIds.Length == 0)
            {
                return true;
            }

            var expressions = await ruleSetIds
                .SelectAwait(id => _ruleService.CreateExpressionGroupAsync(id, this))
                .Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArrayAsync();

            return await RuleMatchesAsync(expressions, logicalOperator, contextAction);
        }

        public async Task<bool> RuleMatchesAsync(
            IRulesContainer entity, 
            LogicalRuleOperator logicalOperator = LogicalRuleOperator.Or,
            Action<CartRuleContext> contextAction = null)
        {
            Guard.NotNull(entity);

            if (entity.RuleSets.IsNullOrEmpty())
            {
                return true;
            }

            var ruleSets = entity.RuleSets.Where(x => x.Scope == RuleScope.Cart).ToArray();
            if (ruleSets.Length == 0)
            {
                return true;
            }

            var expressions = await ruleSets
                .SelectAwait(x => _ruleService.CreateExpressionGroupAsync(x, this))
                .Where(x => x != null)
                .Cast<RuleExpression>()
                .ToArrayAsync();

            return await RuleMatchesAsync(expressions, logicalOperator, contextAction);
        }

        public async Task<bool> RuleMatchesAsync(
            RuleExpression[] expressions, 
            LogicalRuleOperator logicalOperator,
            Action<CartRuleContext> contextAction = null)
        {
            Guard.NotNull(expressions);

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
                WorkContext = _workContext,
                ShoppingCartService = _shoppingCartService
            };

            if (contextAction != null)
            {
                contextAction.Invoke(context);
                // These cannot be null
                context.Customer ??= _workContext.CurrentCustomer;
                context.Store ??= _storeContext.CurrentStore;
            }

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
                Operators = [RuleOperator.IsEqualTo]
            };
            cartItemQuantity.Metadata["ValueTemplateName"] = "ValueTemplates/CartItemQuantity";
            cartItemQuantity.Metadata["ChildRuleDescriptor"] = new CartRuleDescriptor
            {
                Name = "CartItemQuantity.Product",
                RuleType = RuleType.Int,
                ProcessorType = typeof(CartItemQuantityRule),
                Operators = [RuleOperator.IsEqualTo],
                SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Product)
            };

            var cartItemFromCategoryQuantity = new CartRuleDescriptor
            {
                Name = "CartItemFromCategoryQuantity",
                DisplayName = T("Admin.Rules.FilterDescriptor.CartItemFromCategoryQuantity"),
                RuleType = RuleType.String,
                ProcessorType = typeof(CartItemFromCategoryQuantityRule),
                Operators = [RuleOperator.IsEqualTo]
            };
            cartItemFromCategoryQuantity.Metadata["ValueTemplateName"] = "ValueTemplates/CartItemQuantity";
            cartItemFromCategoryQuantity.Metadata["ChildRuleDescriptor"] = new CartRuleDescriptor
            {
                Name = "CartItemQuantity.Category",
                RuleType = RuleType.Int,
                ProcessorType = typeof(CartItemFromCategoryQuantityRule),
                Operators = [RuleOperator.IsEqualTo],
                SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Category)
            };

            var descriptors = new List<CartRuleDescriptor>
            {
                new()
                {
                    Name = "Currency",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Currency"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(CurrencyRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Currency) { Multiple = true }
                },
                new()
                {
                    Name = "Language",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Language"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(LanguageRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Language) { Multiple = true }
                },
                new()
                {
                    Name = "Store",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Store"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(StoreRule),
                    SelectList = new LocalRuleValueSelectList(stores) { Multiple = true }
                },
                new()
                {
                    Name = "IPCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IPCountry"),
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(IPCountryRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Country) { Multiple = true }
                },
                new()
                {
                    Name = "Weekday",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Weekday"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(WeekdayRule),
                    SelectList = new LocalRuleValueSelectList(WeekdayRule.GetDefaultValues(language)) { Multiple = true }
                },

                new()
                {
                    Name = "CustomerRole",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IsInCustomerRole"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(CustomerRoleRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.CustomerRole) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "CustomerAuthentication",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Authentication"),
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(CustomerAuthenticationRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.AuthenticationMethod) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "Affiliate",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Affiliate"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(CustomerAffiliateRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Affiliate) { Multiple = true }
                },
                new()
                {
                    Name = "CartBillingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BillingCountry"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(BillingCountryRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Country) { Multiple = true }
                },
                new()
                {
                    Name = "CartShippingCountry",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingCountry"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ShippingCountryRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Country) { Multiple = true }
                },
                new()
                {
                    Name = "CartShippingMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ShippingMethod"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ShippingMethodRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.ShippingMethod) { Multiple = true }
                },
                new()
                {
                    Name = "CartPaymentMethod",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaymentMethod"),
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(PaymentMethodRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.PaymentMethod) { Multiple = true }
                },

                new()
                {
                    Name = "CartTotal",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartTotal"),
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(CartTotalRule)
                },
                new()
                {
                    Name = "CartSubtotal",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartSubtotal"),
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(CartSubtotalRule)
                },
                new()
                {
                    Name = "CartProductCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.CartProductCount"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(CartProductCountRule)
                },
                cartItemQuantity,
                cartItemFromCategoryQuantity,
                new()
                {
                    Name = "ProductInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductInCartRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Product) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "ProductFromCategoryInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductFromCategoryInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductFromCategoryInCartRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Category) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "ProductFromManufacturerInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductFromManufacturerInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductFromManufacturerInCartRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Manufacturer) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "ProductWithDeliveryTimeInCart",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductWithDeliveryTimeInCart"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductWithDeliveryTimeInCartRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.DeliveryTime) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "ProductInWishlist",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductOnWishlist"),
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(ProductOnWishlistRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Product) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "ProductReviewCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductReviewCount"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(ProductReviewCountRule)
                },
                new()
                {
                    Name = "RewardPointsBalance",
                    DisplayName = T("Admin.Rules.FilterDescriptor.RewardPointsBalance"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(RewardPointsBalanceRule)
                },
                new()
                {
                    Name = "RuleSet",
                    DisplayName = T("Admin.Rules.FilterDescriptor.RuleSet"),
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(RuleSetRule),
                    Operators = [RuleOperator.IsEqualTo, RuleOperator.IsNotEqualTo],
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.CartRule),
                },

                new()
                {
                    Name = "CartOrderCount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OrderCount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(OrderCountRule)
                },
                new()
                {
                    Name = "CartSpentAmount",
                    DisplayName = T("Admin.Rules.FilterDescriptor.SpentAmount"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.Money,
                    ProcessorType = typeof(SpentAmountRule)
                },
                new()
                {
                    Name = "CartPaidBy",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PaidBy"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(PaidByRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.PaymentMethod) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "CartPurchasedProduct",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PurchasedProduct"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(PurchasedProductRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Product) { Multiple = true },
                    IsComparingSequences = true
                },
                new()
                {
                    Name = "CartPurchasedFromManufacturer",
                    DisplayName = T("Admin.Rules.FilterDescriptor.PurchasedFromManufacturer"),
                    GroupKey = "Admin.Orders",
                    RuleType = RuleType.IntArray,
                    ProcessorType = typeof(PurchasedFromManufacturerRule),
                    SelectList = new RemoteRuleValueSelectList(KnownRuleOptionDataSourceNames.Manufacturer) { Multiple = true },
                    IsComparingSequences = true
                },

                new()
                {
                    Name = "UserAgent.IsMobile",
                    DisplayName = T("Admin.Rules.FilterDescriptor.MobileDevice"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.Boolean,
                    ProcessorType = typeof(IsMobileRule)
                },
                new()
                {
                    Name = "UserAgent.Device",
                    DisplayName = T("Admin.Rules.FilterDescriptor.DeviceFamily"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(DeviceRule),
                    SelectList = new LocalRuleValueSelectList(DeviceRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new()
                {
                    Name = "UserAgent.OS",
                    DisplayName = T("Admin.Rules.FilterDescriptor.OperatingSystem"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(OSRule),
                    SelectList = new LocalRuleValueSelectList(OSRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new()
                {
                    Name = "UserAgent.Browser",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BrowserName"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.StringArray,
                    ProcessorType = typeof(BrowserRule),
                    SelectList = new LocalRuleValueSelectList(BrowserRule.GetDefaultOptions()) { Multiple = true, Tags = true }
                },
                new()
                {
                    Name = "UserAgent.BrowserMajorVersion",
                    DisplayName = T("Admin.Rules.FilterDescriptor.BrowserMajorVersion"),
                    GroupKey = "Admin.Rules.FilterDescriptor.Group.BrowserUserAgent",
                    RuleType = RuleType.Int,
                    ProcessorType = typeof(BrowserMajorVersionRule)
                },
                new()
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
