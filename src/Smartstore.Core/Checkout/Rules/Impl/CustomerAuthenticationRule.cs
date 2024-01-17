using System.Collections.Frozen;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CustomerAuthenticationRule : IRule<CartRuleContext>
    {
        // INFO: ExternalAuthenticationRecord.ProviderSystemName always contains the old system name, even for new logins!
        private readonly static FrozenDictionary<string, string> _legacyAuthenticationNamesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Smartstore.FacebookAuth", "Smartstore.Facebook.Auth" },
            { "Smartstore.TwitterAuth", "Smartstore.Twitter.Auth" },
            { "Smartstore.GoogleAuth", "Smartstore.Google.Auth" }
        }
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

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
                    .Select(x => _legacyAuthenticationNamesMap.Get(x.ProviderSystemName.EmptyNull()) ?? x.ProviderSystemName)
                    .ToArray();

                var match = expression.HasListsMatch(authenticationMethods, StringComparer.InvariantCultureIgnoreCase);
                return match;
            }

            return false;
        }
    }
}
