using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Web.Api.Models.Checkout
{
    public class PaymentMethodInfoMapper : IMapper<Provider<IPaymentMethod>, ProviderInfo<PaymentMethodInfo>>
    {
        private readonly ModuleManager _moduleManager;
        private readonly IUrlHelper _urlHelper;

        public PaymentMethodInfoMapper(
            ModuleManager moduleManager,
            IUrlHelper urlHelper)
        {
            _moduleManager = moduleManager;
            _urlHelper = urlHelper;
        }

        public Task MapAsync(Provider<IPaymentMethod> from, ProviderInfo<PaymentMethodInfo> to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            int languageId = parameters?.LanguageId ?? 0;

            if (from.Value != null)
            {
                MiniMapper.Map(from.Value, to.Provider);
            }

            var metadata = from.Metadata;
            if (metadata != null)
            {
                MiniMapper.Map(metadata, to);

                to.FriendlyName = _moduleManager.GetLocalizedFriendlyName(metadata, languageId).NullEmpty() ?? metadata.FriendlyName;
                to.Description = _moduleManager.GetLocalizedDescription(metadata, languageId).NullEmpty() ?? metadata.Description;

                var module = metadata.ModuleDescriptor;
                if (module != null)
                {
                    MiniMapper.Map(module, to.Module);
                    MiniMapper.Map(module.Version, to.Module.Version);
                    MiniMapper.Map(module.MinAppVersion, to.Module.MinAppVersion);

                    to.Module.FriendlyName = _moduleManager.GetLocalizedFriendlyName(module, languageId).NullEmpty() ?? module.FriendlyName;
                    to.Module.Description = _moduleManager.GetLocalizedDescription(module, languageId).NullEmpty() ?? module.Description;

                    var iconUrl = _moduleManager.GetIconUrl(module, metadata.SystemName);
                    var brandImageUrl = _moduleManager.GetBrandImage(metadata)?.DefaultImageUrl;
                    var request = _urlHelper.ActionContext.HttpContext.Request;

                    if (iconUrl.HasValue())
                    {
                        to.IconUrl = WebHelper.GetAbsoluteUrl(_urlHelper.Content(iconUrl), request);
                    }

                    if (brandImageUrl.HasValue())
                    {
                        to.Module.BrandImageUrl = WebHelper.GetAbsoluteUrl(_urlHelper.Content(brandImageUrl), request);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
