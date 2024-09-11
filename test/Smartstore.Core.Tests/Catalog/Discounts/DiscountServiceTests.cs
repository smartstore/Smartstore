using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Discounts
{
    [TestFixture]
    public class DiscountServiceTests : ServiceTestBase
    {
        IDiscountService _discountService;
        IStoreContext _storeContext;

        [OneTimeSetUp]
        public new async Task SetUp()
        {
            var discount1 = new Discount
            {
                Id = 1,
                DiscountType = DiscountType.AssignedToCategories,
                Name = "Discount 1",
                UsePercentage = true,
                DiscountPercentage = 10,
                DiscountAmount = 0,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                LimitationTimes = 0
            };
            var discount2 = new Discount
            {
                Id = 2,
                DiscountType = DiscountType.AssignedToSkus,
                Name = "Discount 2",
                UsePercentage = false,
                DiscountPercentage = 0,
                DiscountAmount = 5,
                RequiresCouponCode = true,
                CouponCode = "SecretCode",
                DiscountLimitation = DiscountLimitationType.NTimesPerCustomer,
                LimitationTimes = 3
            };

            DbContext.Discounts.AddRange([discount1, discount2]);

            DbContext.GenericAttributes.AddRange(
                new GenericAttribute { Key = "", KeyGroup = nameof(Customer), Value = "" },
                new GenericAttribute { Key = SystemCustomerAttributeNames.DiscountCouponCode, KeyGroup = nameof(Customer), Value = "CouponCode 1" });

            await DbContext.SaveChangesAsync();

            var storeContextMock = new Mock<IStoreContext>();
            _storeContext = storeContextMock.Object;
            storeContextMock.Setup(x => x.CurrentStore).Returns(new Store
            {
                Id = 1,
                Name = "MyStore"
            });

            var cartRuleProviderMock = new Mock<ICartRuleProvider>();
            cartRuleProviderMock.Setup(x => x.RuleMatchesAsync(discount1, LogicalRuleOperator.Or, null)).ReturnsAsync(true);
            cartRuleProviderMock.Setup(x => x.RuleMatchesAsync(discount2, LogicalRuleOperator.Or, null)).ReturnsAsync(true);

            var ruleProviderFactoryMock = new Mock<IRuleProviderFactory>();
            ruleProviderFactoryMock.Setup(x => x.GetProvider(RuleScope.Cart, null)).Returns(cartRuleProviderMock.Object);

            var cartServiceMock = new Mock<IShoppingCartService>();
            cartServiceMock.Setup(x => x.GetCartAsync(null, ShoppingCartType.ShoppingCart, 0, true)).Returns(Task.FromResult<ShoppingCart>(null));

            _discountService = new DiscountService(
                DbContext,
                NullRequestCache.Instance,
                _storeContext,
                ruleProviderFactoryMock.Object,
                new Lazy<IShoppingCartService>(cartServiceMock.Object));
        }

        [Test]
        public async Task Can_get_all_discount()
        {
            var discounts = await _discountService.GetAllDiscountsAsync(null);
            discounts.ShouldNotBeNull();
            discounts.Any().ShouldBeTrue();
        }

        [Test]
        public async Task Should_accept_valid_discount_code()
        {
            var discount = new Discount
            {
                Id = 1,
                DiscountType = DiscountType.AssignedToSkus,
                Name = "Discount 2",
                UsePercentage = false,
                DiscountPercentage = 0,
                DiscountAmount = 5,
                RequiresCouponCode = true,
                CouponCode = "CouponCode 1",
                DiscountLimitation = DiscountLimitationType.Unlimited
            };

            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                AdminComment = "",
                Active = true,
                Deleted = false,
                CreatedOnUtc = new DateTime(2010, 01, 01),
                LastActivityDateUtc = new DateTime(2010, 01, 02)
            };

            var validationFlags = DiscountValidationFlags.DiscountLimitations | DiscountValidationFlags.GiftCards;
            var result1 = await _discountService.IsDiscountValidAsync(discount, customer, flags: validationFlags);
            result1.ShouldEqual(true);
        }

        [Test]
        public async Task Should_not_accept_wrong_discount_code()
        {
            var discount = new Discount
            {
                Id = 1,
                DiscountType = DiscountType.AssignedToSkus,
                Name = "Discount 2",
                UsePercentage = false,
                DiscountPercentage = 0,
                DiscountAmount = 5,
                RequiresCouponCode = true,
                CouponCode = "CouponCode 2",
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                AdminComment = "",
                Active = true,
                Deleted = false,
                CreatedOnUtc = new DateTime(2010, 01, 01),
                LastActivityDateUtc = new DateTime(2010, 01, 02)
            };

            var result2 = await _discountService.IsDiscountValidAsync(discount, customer);
            result2.ShouldEqual(false);
        }

        [Test]
        public async Task Can_validate_discount_dateRange()
        {
            var customer = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                AdminComment = "",
                Active = true,
                Deleted = false,
                CreatedOnUtc = new DateTime(2010, 01, 01),
                LastActivityDateUtc = new DateTime(2010, 01, 02)
            };

            var discount1 = new Discount
            {
                Id = 1,
                DiscountType = DiscountType.AssignedToSkus,
                Name = "Discount 1",
                UsePercentage = false,
                DiscountPercentage = 0,
                DiscountAmount = 5,
                StartDateUtc = DateTime.UtcNow.AddDays(-1),
                EndDateUtc = DateTime.UtcNow.AddDays(1),
                RequiresCouponCode = false,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };

            var discount2 = new Discount
            {
                Id = 2,
                DiscountType = DiscountType.AssignedToSkus,
                Name = "Discount 2",
                UsePercentage = false,
                DiscountPercentage = 0,
                DiscountAmount = 5,
                StartDateUtc = DateTime.UtcNow.AddDays(1),
                EndDateUtc = DateTime.UtcNow.AddDays(2),
                RequiresCouponCode = false,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };

            var validationFlags = DiscountValidationFlags.DiscountLimitations | DiscountValidationFlags.GiftCards;

            var result1 = await _discountService.IsDiscountValidAsync(discount1, customer, flags: validationFlags);
            result1.ShouldEqual(true);

            var result2 = await _discountService.IsDiscountValidAsync(discount2, customer, flags: validationFlags);
            result2.ShouldEqual(false);
        }
    }
}