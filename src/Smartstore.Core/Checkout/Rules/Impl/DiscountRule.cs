using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl;

internal class DiscountRule(IDiscountService discountService) : IRule<CartRuleContext>
{
    public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var couponCode = context.Customer.GenericAttributes.DiscountCouponCode;
        var hasValidDiscount = false;

        if (couponCode.HasValue())
        {
            var discounts = await discountService.GetAllDiscountsAsync(null, couponCode, true);
            var validationFlags = DiscountValidationFlags.All & ~DiscountValidationFlags.CartRules;

            foreach (var discount in discounts.Where(x => x.RequiresCouponCode))
            {
                // Cart rules must not be evaluated recursively from within a cart rule.
                if (await discountService.IsDiscountValidAsync(
                    discount,
                    context.Customer,
                    couponCode,
                    context.Store,
                    validationFlags))
                {
                    hasValidDiscount = true;
                    break;
                }
            }
        }

        return expression.Operator.Match(hasValidDiscount, expression.Value);
    }
}
