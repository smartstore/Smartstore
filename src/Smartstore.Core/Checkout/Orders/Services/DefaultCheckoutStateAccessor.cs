using System;
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

        public bool IsStateLoaded => _state == null;

        public bool HasStateChanged => _dirty;

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
                        _state = new CheckoutState();
                    }

                    _state.PropertyChanged += OnPropertyChanged;
                    _state.PaymentData.CollectionChanged += OnCollectionChanged;
                    _state.CustomProperties.CollectionChanged += OnCollectionChanged;
                }
                
                return _state;
            }
        }

        public void Save()
        {
            if (_state != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Session.TrySetObject(CheckoutStateSessionKey, _state);
                }
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => _dirty = true;
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => _dirty = true;
    }
}
