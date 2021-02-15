using System.Threading.Tasks;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class RewardPointsBalanceRule : IRule
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            // TODO: (mg) (core) Complete RewardPointsBalanceRule (RewardPointsHistory required).
            //var rewardPointsBalance = context.Customer.GetRewardPointsBalance();
            var rewardPointsBalance = 0;

            return Task.FromResult(expression.Operator.Match(rewardPointsBalance, expression.Value));
        }
    }
}
