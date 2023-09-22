"use strict";

(function ($, window, document, undefined) {

    var PayPalButton = window.PayPalButton = (function () {
        function PayPalButton(buttonContainerSelector, funding) {
            this.buttonContainer = $(buttonContainerSelector);
            this.initPayPalScript(funding);
        }

        PayPalButton.prototype.initPayPalScript = function (funding) {
            var self = this;
            if (typeof paypal !== 'undefined') {
                self.initPayPalButton(funding, false);
            } else {
                var script = document.getElementById("paypal-js");

                if (script != null) {
                    script.onload = function () {
                        self.initPayPalButton(funding, false);
                    };
                }
                else {
                    // PayPal Scripts weren't loaded. This can occur e.g. when a third party consent tool or other JS blocking browser extensions are used.
                    $(function () {
                        // Only execute this and display warnings on payment selection page.
                        if (!$(".payment-methods").length) {
                            return;
                        }

                        // Display warning message informing the user that PayPal scripts weren't loaded.
                        displayNotification(window.Res.PayPal["NoScriptsLoaded"], 'error');

                        // Enable next buttons, that may have been hidden by PayPalPaymentSelection ViewComponent.
                        var btnNext = $(".payment-method-next-step-button");
                        var btnContainer = $("#paypal-button-container");
                        btnNext.css('display', 'block');
                        btnContainer.css('display', 'none');

                        // Intercept click handler of checkout next button to prevent checkout with PayPal JS SDK methods.
                        var form = $("form[data-form-type='payment']");
                        if (!form.length) {
                            form = $(".checkout-data > form");
                        }

                        $(form).on("submit", function (e) {
                            var selectedPaymentSystemName = $("input[name='paymentmethod']:checked").val();
                            if (selectedPaymentSystemName == "Payments.PayPalStandard"
                                || selectedPaymentSystemName == "Payments.PayPalSepa"
                                || selectedPaymentSystemName == "Payments.PayPalPayLater") {

                                displayNotification(window.Res.PayPal["NoScriptsLoaded"], 'error');
                                return false;
                            }
                        });
                    });
                }
            }
        };

        PayPalButton.prototype.initPayPalButton = function (fundingSource, refresh) {
            var self = this;

            if (!paypal.isFundingEligible(fundingSource)) {
                console.log("Not eligible: " + fundingSource);
                return;
            }

            if (refresh) {
                this.buttonContainer.empty();
            }

            // Render PayPal buttons into #paypal-button-container.
            paypal.Buttons({
                fundingSource: fundingSource,
                style: {
                    layout: 'horizontal',
                    label: 'checkout',
                    shape: self.buttonContainer.data("shape"),
                    color: fundingSource == "paypal" || fundingSource == "paylater" ? self.buttonContainer.data("color") : 'white'
                },
                // Create order
                createOrder: function (data, actions) {
                    return createOrder(self.buttonContainer.data("create-order-url"), self.buttonContainer.attr("id"));
                },
                // Save obtained order id in checkout state.
                onApprove: function (data, actions) {
                    initTransaction(data, self.buttonContainer);
                },
                onCancel: function (data) {
                    // Do nothing here, just let the user have it's way
                },
                onError: function (err) {
                    console.log(err);
                    displayNotification(err, 'error');
                }
            })
            .render(self.buttonContainer[0]);
        };

        return PayPalButton;
    })();

    var PayPalHostedFieldsMethod = window.PayPalHostedFieldsMethod = (function () {
        function PayPalHostedFieldsMethod(hostedFieldsContainerSelector) {
            this.hostedFieldsContainer = $(hostedFieldsContainerSelector);
            this.initPayPalScript();
        }

        PayPalHostedFieldsMethod.prototype.initPayPalScript = function () {
            var self = this;

            if (typeof paypal !== 'undefined') {
                self.initPayPalHostedFields();
            } else {
                var script = document.getElementById("paypal-js");
                script.onload = function () {
                    self.initPayPalHostedFields();
                };
            }
        };

        PayPalHostedFieldsMethod.prototype.initPayPalHostedFields = function () {
            var self = this;

            if (!paypal.isFundingEligible("card")) {
                console.log("Not eligible: card");
                return;
            }

            if (!paypal.HostedFields.isEligible()) {
                console.log("Not eligible: hosted fields");
                return;
            }

            // Render PayPal hosted fields into #paypal-creditcard-hosted-fields-container
            paypal.HostedFields.render({
                styles: {
                    'input': {
                        'color': '#596167',
                        'padding': '0',
                        'font-size': '0.9375rem'
                    },
                    '.valid': {
                        'color': 'green'
                    },
                    '.invalid': {
                        'color': 'red'
                    }
                },
                fields: {
                    number: { selector: '#card-number' },
                    cvv: { selector: '#cvv' },
                    expirationDate: { selector: '#expiration-date' }
                },
                // Create order
                createOrder: function (data, actions) {
                    var orderId = createOrder(self.hostedFieldsContainer.data("create-order-url"), self.hostedFieldsContainer.attr("id"));
                    initTransaction({ orderID: orderId }, self.hostedFieldsContainer);
                    return orderId;
                },
                // Save obtained order id in checkout state.
                onApprove: function (data, actions) {
                    console.log("onApprove", data);
                    initTransaction(data, self.hostedFieldsContainer);
                },
                onError: function (err) {
                    console.log(err);
                    displayNotification(err, 'error');
                }
            }).then((cardFields) => {
                $("#nextstep").on("click", function (e) {

                    var selectedPaymentSystemName = $("input[name='paymentmethod']:checked").val();

                    if (selectedPaymentSystemName != "Payments.PayPalCreditCard") {
                        return true;
                    }

                    if (!cardFields._state.fields.cvv.isValid
                        || !cardFields._state.fields.number.isValid
                        || !cardFields._state.fields.expirationDate.isValid) {
                        e.preventDefault();

                        console.log("CVV is valid: " + cardFields._state.fields.cvv.isValid);
                        console.log("Number is valid: " + cardFields._state.fields.number.isValid);
                        console.log("Expiration date is valid: " + cardFields._state.fields.expirationDate.isValid);

                        var err = self.hostedFieldsContainer.data("creditcard-error-message");
                        displayNotification(err, 'error');

                        $.scrollTo($(".payment-method-item.active"), 400);

                        return false;
                    }

                    var form = $("form[data-form-type='payment']");
                    if (form.length == 0) {
                        form = $(".checkout-data > form");
                    }

                    var validator = form.data('validator');
                    if (validator) {
                        validator.settings.ignore = "";
                        form.validate();
                    }

                    if (form.valid()) {
                        cardFields
                            .submit({
                                // Cardholder's first and last name
                                cardholderName: document.getElementById("CardholderName").value,

                            })
                            .catch((err) => {
                                e.preventDefault();
                                console.log(err);
                                displayNotification(err.message, 'error');
                                return false;
                            });

                        console.log("Redirecting now");
                        e.preventDefault();
                        location.href = self.hostedFieldsContainer.data("forward-url");
                        return false;
                    }
                    else {
                        e.preventDefault();
                        return false;
                    }
                })
            });
        };

        return PayPalHostedFieldsMethod;
    })();

    function createOrder(createOrderUrl, paymentSource) {
        var orderId;

        $.ajax({
            async: false,   // IMPORTANT INFO: we must wait to get the order id.
            type: 'POST',
            data: { paymentSource: paymentSource },
            url: createOrderUrl,
            cache: false,
            success: function (resp) {
                orderId = resp.id;
            }
        });

        return orderId;
    }

    function initTransaction(data, container) {
        $.ajax({
            type: 'POST',
            url: container.data("init-transaction-url"),
            data: { orderId: data.orderID },
            cache: false,
            success: function (resp) {
                if (resp.success) {
                    // Lead customer to address selection or to confirm page if PayPal was choosen from payment selection page.
                    location.href = container.data("forward-url");
                }
                else {
                    displayNotification(resp.message, 'error');
                }
            }
        });
    }
})(jQuery, this, document);