using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Checkout.Orders
{
    public class DefaultCheckoutStateAccessor : ICheckoutStateAccessor
    {
        const string CheckoutStateSessionKey = ".Smart.CheckoutState";

        private CheckoutState _state;
        private bool _dirty;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultCheckoutStateAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public bool IsStateLoaded => _state != null;

        public bool HasStateChanged
        {
            get => _dirty;
            set => _dirty = value;
        }

        public CheckoutState CheckoutState
        {
            get
            {
                if (_state == null)
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext == null)
                    {
                        return null;
                    }

                    if (httpContext.Session.TryGetObject<CheckoutState>(CheckoutStateSessionKey, out var state))
                    {
                        _state = state;
                    }
                    else
                    {
                        _state = new CheckoutState
                        {
                            IsPaymentRequired = true
                        };

                        // INFO: 'Save' is necessary because in UseCheckoutState it is too late for the very first saving of CheckoutState.
                        // It would produce InvalidOperationException 'The session cannot be established after the response has started'.
                        Save();
                    }

                    _state.PropertyChanged += OnPropertyChanged;
                    _state.PaymentData.PropertyChanged += OnPropertyChanged;
                    _state.CustomProperties.PropertyChanged += OnPropertyChanged;

                    _state.PaymentData.StartObserveValues();
                    _state.CustomProperties.StartObserveValues();
                }

                return _state;
            }
        }

        public void Save()
        {
            if (_state != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;

                if (httpContext?.Session != null)
                {
                    Execute(() => httpContext.Session.TrySetObject(CheckoutStateSessionKey, _state));
                }
            }
        }

        public void Abandon()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                Execute(() => httpContext.Session?.Remove(CheckoutStateSessionKey));
                _state = null;
                _dirty = false;
            }
        }

        private void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Error while persisting checkout state data");
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => _dirty = true;
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => _dirty = true;
    }
}
