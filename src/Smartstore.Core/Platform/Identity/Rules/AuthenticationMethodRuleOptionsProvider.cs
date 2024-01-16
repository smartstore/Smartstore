using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Identity.Rules
{
    public partial class AuthenticationMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;

        public AuthenticationMethodRuleOptionsProvider(
            IProviderManager providerManager,
            ModuleManager moduleManager)
        {
            _providerManager = providerManager;
            _moduleManager = moduleManager;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.AuthenticationMethod;

        public Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            RuleOptionsResult result = null;

            if (context.DataSource == KnownRuleOptionDataSourceNames.AuthenticationMethod)
            {
                var authProviders = _providerManager.GetAllProviders<IExternalAuthenticationMethod>();

                var options = authProviders.Select(x =>
                {
                    var friendlyName = _moduleManager.GetLocalizedFriendlyName(x.Metadata).NullEmpty();

                    return new RuleValueSelectListOption
                    {
                        Value = x.Metadata.SystemName,
                        Text = friendlyName ?? x.Metadata.SystemName,
                        Hint = friendlyName == null ? null : x.Metadata.SystemName
                    };
                });

                result = RuleOptionsResult.Create(context, options);
            }

            return Task.FromResult(result);
        }
    }
}
