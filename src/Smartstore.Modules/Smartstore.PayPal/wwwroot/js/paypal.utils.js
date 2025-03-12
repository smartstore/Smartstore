"use strict";

(function ($, window, document, undefined) {
    var PayPalButton = window.PayPalButton = (function () {
        function PayPalButton(buttonContainerSelector, funding) {
            this.buttonContainer = $(buttonContainerSelector);
            this.initPayPalScript(funding);
        }

        PayPalButton.prototype.initPayPalScript = function (funding, refreshBtnContainer = false) {
            var self = this;
            if (typeof paypal !== 'undefined') {
                self.initPayPalButton(funding, refreshBtnContainer);
            } else {
                var script = document.getElementById("paypal-js");
                
                if (script != null) {
                    script.addEventListener("load", function () {
                        self.initPayPalButton(funding, refreshBtnContainer);
                    });
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
                    return createOrder(self.buttonContainer.data("create-order-url"), self.buttonContainer.attr("id"), self.buttonContainer.data("route-ident"));
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

                    // Do not display error message if no order id was passed. It means that the cart may not be valid.
                    if (error.message.indexOf('Expected an order id to be passed') >= 0)
                        return;

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
                    initTransaction({ orderID: orderId }, self.hostedFieldsContainer, false);
                    return orderId;
                },
                onApprove: function (data, actions) {
                    console.log("onApprove", data);
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

                    e.preventDefault();

                    if (!cardFields._state.fields.cvv.isValid
                        || !cardFields._state.fields.number.isValid
                        || !cardFields._state.fields.expirationDate.isValid) {
                        console.log("CVV is valid: " + cardFields._state.fields.cvv.isValid);
                        console.log("Number is valid: " + cardFields._state.fields.number.isValid);
                        console.log("Expiration date is valid: " + cardFields._state.fields.expirationDate.isValid);

                        var err = self.hostedFieldsContainer.data("creditcard-error-message");
                        displayNotification(err, 'error');

                        $.scrollTo($(".payment-method-item.active"));

                        return false;
                    }

                    cardFields
                        .submit({
                            contingencies: ['SCA_ALWAYS']
                        })
                        .then(function (payload) {
                            if (payload.liabilityShifted) {
                                // Handle buyer confirmed 3D Secure successfully.
                                console.log("Redirecting to confirm now");
                                location.href = self.hostedFieldsContainer.data("forward-url");
                            } else {
                                console.log(payload);
                                var err = self.hostedFieldsContainer.data("3dsecure-error-message");
                                displayNotification(err, 'error');
                            }
                        })
                        .catch((err) => {
                            console.log(err);
                            displayNotification(err.message, 'error');
                        });

                    return false;
                })
            });
        };

        return PayPalHostedFieldsMethod;
    })();

    var GooglePayPayPalButton = window.GooglePayPayPalButton = (function () {
        let buttonContainer = null;

        class GooglePayPayPalButton {
            constructor() {
                buttonContainer = $("#paypal-google-pay-container");

                // In OffCanvasCart the button isn't there on pageload. 
                // So we must check if the button was rendered already and if not init it.
                if (buttonContainer && buttonContainer.html().trim() == '') {
                    window.GooglePayPayPalButton.onGooglePayLoaded();
                }
            }
            async onGooglePayLoaded() {
                if (!buttonContainer.length)
                    return;

                await waitForPaypal();

                const paymentsClient = getGooglePaymentsClient();
                const { allowedPaymentMethods } = await getGooglePayConfig();
                paymentsClient
                    .isReadyToPay(getGoogleIsReadyToPayRequest(allowedPaymentMethods))
                    .then(function(response) {
                        if (response.result) {
                            addGooglePayButton();
                        }
                    })
                    .catch(function(err) {
                        console.error(err);
                    });
            }
        }

        // Define the version of the Google Pay API referenced when creating your configuration
        const baseRequest = {
            apiVersion: 2,
            apiVersionMinor: 0
        };

        let paymentsClient = null, allowedPaymentMethods = null, merchantInfo = null;

        // Wait for the PayPal JS SDK to be loaded.
        function waitForPaypal() {
            return new Promise((resolve) => {
                function checkPaypal() {
                    if (typeof paypal !== 'undefined') {
                        resolve();
                    } else {
                        setTimeout(checkPaypal, 100);
                    }
                }
                checkPaypal();
            });
        }

        function onPaymentAuthorized(paymentData) {
            return new Promise(function (resolve, reject) {
                processPayment(paymentData)
                    .then(function (data) {
                        resolve({ transactionState: "SUCCESS" });
                    })
                    .catch(function (errDetails) {
                        resolve({ transactionState: "ERROR" });
                    });
            });
        }

        function getGooglePaymentsClient() {
            if (paymentsClient === null) {
                paymentsClient = new google.payments.api.PaymentsClient({
                    environment: buttonContainer.attr("data-is-sandbox") == "true" ? "TEST" : "PRODUCTION",
                    //paymentDataCallbacks: {
                    //    onPaymentAuthorized: onPaymentAuthorized,
                    //}
                });
            }
            return paymentsClient;
        }

        function addGooglePayButton() {
            const paymentsClient = getGooglePaymentsClient();
            const button = paymentsClient.createButton({
                onClick: onGooglePaymentButtonClicked,
            });

            buttonContainer.append(button);
        }

        // Configure support for payment methods supported by the Google Pay
        function getGoogleIsReadyToPayRequest(allowedPaymentMethods) {
            return Object.assign({}, baseRequest, {
                allowedPaymentMethods: allowedPaymentMethods,
            });
        }

        // Fetch Default Config from PayPal via PayPal SDK
        async function getGooglePayConfig() {
            if (allowedPaymentMethods == null || merchantInfo == null) {
                const googlePayConfig = await paypal.Googlepay().config();
                allowedPaymentMethods = googlePayConfig.allowedPaymentMethods;
                merchantInfo = googlePayConfig.merchantInfo;
            }
            return {
                allowedPaymentMethods,
                merchantInfo,
            };
        }

        /* Configure support for the Google Pay API */
        async function getGooglePaymentDataRequest() {
            const paymentDataRequest = Object.assign({}, baseRequest);
            const { allowedPaymentMethods, merchantInfo } = await getGooglePayConfig();
            paymentDataRequest.allowedPaymentMethods = allowedPaymentMethods;
            paymentDataRequest.transactionInfo = getGoogleTransactionInfo();
            paymentDataRequest.merchantInfo = merchantInfo;
            //paymentDataRequest.callbackIntents = ["PAYMENT_AUTHORIZATION"];
            return paymentDataRequest;
        }

        function getGoogleTransactionInfo() {
            let transactionInfo;
            $.ajax({
                async: false,
                type: 'POST',
                url: buttonContainer.data("get-transaction-info-url"),
                data: {
                    routeIdent: buttonContainer.data("route-ident")
                },
                cache: false,
                success: function (resp) {
                    transactionInfo = resp;
                }
            });
            return transactionInfo;
        }

        async function onGooglePaymentButtonClicked() {
            const paymentDataRequest = await getGooglePaymentDataRequest();
            const paymentsClient = getGooglePaymentsClient();
            paymentsClient.loadPaymentData(paymentDataRequest).then(function (paymentData) {
                processPayment(paymentData);
            })
            .catch(function (err) {
                console.error(err);
            });
        }

        async function processPayment(paymentData) {
            return new Promise(async function (resolve, reject) {
                const orderId = createOrder(buttonContainer.data("create-order-url"), buttonContainer.attr("id"), buttonContainer.data("route-ident"));
                try {
                    const { status } = await paypal.Googlepay().confirmOrder({
                        orderId: orderId,
                        paymentMethodData: paymentData.paymentMethodData,
                    });

                    if (status === "APPROVED" || status === "COMPLETED") {
                        initTransaction({ orderID: orderId }, buttonContainer);
                    }
                    else if (status === "PAYER_ACTION_REQUIRED") {
                        paypal
                            .Googlepay()
                            .initiatePayerAction({ orderId: orderId })
                            .then(async () => {
                                initTransaction({ orderID: orderId }, buttonContainer);
                            });
                    }
                    else {
                        displayNotification(buttonContainer.data("transaction-error"), "error");
                        resolve({
                            transactionState: 'ERROR',
                            error: {
                                intent: 'PAYMENT_AUTHORIZATION',
                                message: 'TRANSACTION FAILED',
                            }
                        })
                    }
                } catch (err) {
                    displayNotification(buttonContainer.data("transaction-error"), "error");
                    
                    resolve({
                        transactionState: 'ERROR',
                        error: {
                            intent: 'PAYMENT_AUTHORIZATION',
                            message: err.message,
                        }
                    })
                }
            });
        }   
        
        GooglePayPayPalButton.onGooglePayLoaded = GooglePayPayPalButton.prototype.onGooglePayLoaded;

        return GooglePayPayPalButton;
    })();

    function createOrder(createOrderUrl, paymentSource, routeIdent) {
        var orderId;
        
        $.ajax({
            async: false,   // IMPORTANT INFO: we must wait to get the order id.
            type: 'POST',
            data: $('#startcheckout').closest('form').serialize() + "&paymentSource=" + paymentSource + "&routeIdent=" + routeIdent,
            url: createOrderUrl,
            cache: false,
            success: function (response) {
                if (response.success) {
                    orderId = response.data.id;
                }
                else {
                    displayNotification(response.message, response.messageType);
                }
            }
        });

        return orderId;
    }

    function initTransaction(data, container, redirect = true) {
        $.ajax({
            type: 'POST',
            url: container.data("init-transaction-url"),
            data: {
                orderId: data.orderID,
                routeIdent: container.data("route-ident")
            },
            cache: false,
            success: function (resp) {
                if (resp.success) {
                    if (resp.redirectUrl && redirect) {
                        location.href = resp.redirectUrl;
                    }
                    else if (!container.data("skip-redirect-oninit")) {
                        // Lead customer to address selection or to confirm page if PayPal was choosen from payment selection page.
                        location.href = container.data("forward-url");
                    }
                }
                else {
                    displayNotification(resp.message, 'error');
                }
            }
        });
    }
})(jQuery, this, document);