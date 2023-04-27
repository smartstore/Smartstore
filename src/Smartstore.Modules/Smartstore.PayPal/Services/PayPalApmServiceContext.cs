using FluentValidation;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.PayPal.Client;

namespace Smartstore.PayPal.Services
{
    public class PayPalApmServiceContext
    {
        private readonly SmartDbContext _db;
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IValidator<PublicApmModel> _validator;

        public PayPalApmServiceContext(
            SmartDbContext db,
            PayPalHttpClient client,
            PayPalSettings settings,
            ICheckoutStateAccessor checkoutStateAccessor,
            IValidator<PublicApmModel> validator)
        {
            _db = db;
            _client = client;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _validator = validator;
        }

        public SmartDbContext Db => _db;
        public PayPalHttpClient Client => _client;
        public PayPalSettings Settings => _settings;
        public ICheckoutStateAccessor CheckoutStateAccessor => _checkoutStateAccessor;
        public IValidator<PublicApmModel> Validator => _validator;
    }
}
