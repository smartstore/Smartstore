using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CustomerAuthenticationRule : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db;

        public CustomerAuthenticationRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var customer = context.Customer;
            if (customer != null && !customer.IsSystemAccount)
            {
                await _db.LoadCollectionAsync(customer, x => x.ExternalAuthenticationRecords);

                // INFO: checking whether the authentication methods are also active is probably not necessary here.
                var authenticationMethods = customer.ExternalAuthenticationRecords
                    .Select(x => MapLegacyAuthenticationName(x.ProviderSystemName))
                    .ToArray();

                var match = expression.HasListsMatch(authenticationMethods, StringComparer.InvariantCultureIgnoreCase);
                return match;
            }

            return false;
        }

        private static string MapLegacyAuthenticationName(string systemName)
        {
            switch (systemName.NullEmpty().ToLowerInvariant())
            {
                case "smartstore.facebookauth":
                    return "Smartstore.Facebook.Auth";
                case "smartstore.twitterauth":
                    return "Smartstore.Twitter.Auth";
                case "smartstore.googleauth":
                    return "Smartstore.Google.Auth";
                default:
                    return systemName;
            }
        }
    }
}
