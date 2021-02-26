using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Checkout.Payment.Hooks
{
    public class PaymentMethodHook : AsyncDbSaveHook<PaymentMethod>
    {
        private const string PaymentMethodsPatternKey = "SmartStore.paymentmethod.*";

        private readonly ICommonServices _services;

        public PaymentMethodHook(ICommonServices services)
        {
            _services = services;            
        }

        protected override Task<HookResult> OnInsertedAsync(PaymentMethod entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatedAsync(PaymentMethod entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            _services.RequestCache.RemoveByPattern(PaymentMethodsPatternKey);
            return Task.FromResult(HookResult.Ok);            
        }
    }
}
