Smartstore.Stripe = (function () {
    let elements;
    let stripe;
    let createdPaymentMethod = false;
    
    const paymentRequestButtonId = "stripe-payment-request-button";
    const paymentRequestButtonSelector = "#" + paymentRequestButtonId;
    const paymentElementSelector = "#stripe-payment-element";
    const expressCheckoutElementSelector = "#stripe-express-checkout-element";
    const moduleSystemName = "Payments.StripeElements";

    function validateCart(container) {
        return new Promise(function (resolve) {
            $.ajax({
                type: 'POST',
                url: container.data("validate-cart-url"),
                data: $('#stripe-payment-request-button').closest('form').serialize(),
                cache: false,
                success: function (resp) {
                    if (resp.success) {
                        resolve(true);
                    }
                    else {
                        displayNotification(resp.message, 'error');
                        resolve(false);
                    }
                },
                error: function (xhr, status, error) {
                    displayNotification(error, 'error');
                    resolve(false);
                }
            });
        });
    }

    async function storePaymentMethod(paymentMethodId, paymentContainer) {
        // Ignore results after switching payment methods or replacing the Stripe panel.
        const isCurrent = () => paymentContainer
            && $(paymentElementSelector)[0] === paymentContainer
            && $("input[name='paymentmethod']:checked").val() === moduleSystemName;

        if (!isCurrent()) {
            return;
        }

        const data = await $.ajax({
            type: 'POST',
            data: { paymentMethodId: paymentMethodId },
            url: $(paymentContainer).data('store-payment-selection-url'),
            dataType: 'json'
        });

        if (!isCurrent()) {
            return;
        }

        if (!data.success) {
            throw new Error('Unable to store the selected payment method.');
        }

        createdPaymentMethod = true;
        $('.payment-method-next-step-button').prop('disabled', false).trigger('click');
    }

    return {
        initPaymentElement: function (publicApiKey, apiVersion, amount, currency, captureMethod, paymentPageButtonMethods = []) {
            stripe = Stripe(publicApiKey, { apiVersion: apiVersion });

            const options = {
                mode: 'payment',
                amount: amount,
                currency: currency,
                captureMethod: captureMethod,
                appearance: { theme: 'stripe' },
                paymentMethodCreation: "manual"
            };

            elements = stripe.elements(options);
            const paymentContainer = $(paymentElementSelector)[0];

            const paymentElementOptions = { layout: "tabs" };
            if (paymentPageButtonMethods.length > 0) {
                paymentElementOptions.wallets = {
                    applePay: paymentPageButtonMethods.includes('applePay') ? 'never' : 'auto',
                    googlePay: paymentPageButtonMethods.includes('googlePay') ? 'never' : 'auto'
                };
            }
            const paymentElement = elements.create("payment", paymentElementOptions);
            paymentElement.mount(paymentElementSelector);

            paymentElement.on('change', function (event) {
                if (event.complete) {
                    // Enable next button.
                    var btnNext = $(".payment-method-next-step-button");
                    btnNext[0].disabled = false;
                }
            });

            if (paymentPageButtonMethods.length > 0) {
                const walletElements = stripe.elements(options);
                const paymentMethods = {};
                for (const method of ['applePay', 'googlePay', 'link', 'paypal', 'amazonPay', 'klarna']) {
                    paymentMethods[method] = paymentPageButtonMethods.includes(method)
                        ? (method === 'applePay' || method === 'googlePay' ? 'always' : 'auto')
                        : 'never';
                }

                const expressCheckoutElement = walletElements.create('expressCheckout', {
                    paymentMethods: paymentMethods,
                    layout: { maxColumns: 2 }
                });

                expressCheckoutElement.on('availablepaymentmethodschange', function (event) {
                    $(expressCheckoutElementSelector).css('visibility', event.paymentMethods ? 'visible' : 'hidden');
                });

                expressCheckoutElement.on('confirm', async function (event) {
                    try {
                        const { error: submitError } = await walletElements.submit();
                        if (submitError) {
                            throw submitError;
                        }

                        const { error, paymentMethod } = await stripe.createPaymentMethod({ elements: walletElements });
                        if (error) {
                            throw error;
                        }

                        await storePaymentMethod(paymentMethod.id, paymentContainer);
                    }
                    catch (error) {
                        event.paymentFailed({ reason: 'fail' });
                        displayNotification(error.message, 'error');
                    }
                });

                expressCheckoutElement.mount(expressCheckoutElementSelector);
            }
        },
        initPaymentSelectionPage: function (publicApiKey) {
            createdPaymentMethod = false;
            var btnNext = $(".payment-method-next-step-button");

            // Listen for changes to the radio input elements.
            $(document, "input[name='paymentmethod']").on("change", function (e) {
                btnNext[0].disabled = e.target.value == moduleSystemName;
            });

            // Handle button state on page load
            if ($("input[name='paymentmethod']:checked").val() == moduleSystemName) {
                btnNext[0].disabled = true;
            }

            // Complete payment (must be done like this in order to be redirected correctly)
            $("form").off("submit.stripe").on("submit.stripe", async e => {
                if ($("input[name='paymentmethod']:checked").val() == moduleSystemName && !createdPaymentMethod) {
                    e.preventDefault();
                    const paymentContainer = $(paymentElementSelector)[0];

                    // Trigger form validation and wallet collection
                    const { error: submitError } = await elements.submit();
                    if (submitError) {
                        displayNotification(submitError.message, 'error');
                        return;
                    }

                    try {
                        const { error, paymentMethod } = await stripe.createPaymentMethod({ elements });
                        if (error) {
                            throw error;
                        }

                        await storePaymentMethod(paymentMethod.id, paymentContainer);
                    }
                    catch (error) {
                        displayNotification(error.message, 'error');
                    }
                }
            });
        },
        handleNextAction: function (publicApiKey, apiVersion, clientSecret) {
            if (!stripe) {
                stripe = Stripe(publicApiKey, { apiVersion: apiVersion });
            }

            return stripe.handleNextAction({ clientSecret: clientSecret });
        },
        initWalletButtonElement: function (publicApiKey, requestData, isCartPage, apiVersion, currencyMinorUnitFactor) {
            stripe = Stripe(publicApiKey, {
                apiVersion: apiVersion,
            });

            const paymentRequest = stripe.paymentRequest(requestData);
            const elements = stripe.elements();
            const prButton = elements.create('paymentRequestButton', { paymentRequest });

            (async () => {
                // Check the availability of the Payment Request API first.
                const result = await paymentRequest.canMakePayment();
                if (result) {
                    prButton.mount(paymentRequestButtonSelector);
                } else {
                    document.getElementById(paymentRequestButtonId).style.display = 'none';
                }
            })();

            var paymentRequestButton = $(paymentRequestButtonSelector);

            prButton.on('click', function (event) {
                // Get updated payment request
                $.ajax({
                    async: false,   // IMPORTANT INFO: we must wait to get the correct cart value.
                    type: 'POST',
                    url: paymentRequestButton.data("get-current-payment-request-url"),
                    data: $('#stripe-payment-request-button').closest('form').serialize(),
                    dataType: 'json',
                    success: function (data) {
                        if (data.success) {
                            paymentRequest.update(JSON.parse(data.paymentRequest))
                        }
                        else {
                            // This prevents the stripe terminal from opening.
                            event.preventDefault();

                            displayNotification(data.message, 'error');
                        }
                    }
                });
            });

            // Will be handled when payment is done in stripe terminal.
            paymentRequest.on('paymentmethod', async (ev) => {
                validateCart(paymentRequestButton).then(function (result) {
                    if (result) {
                        // Create payment intent.
                        $.ajax({
                            type: 'POST',
                            data: {
                                eventData: JSON.stringify(ev),
                                paymentRequest: requestData
                            },
                            url: paymentRequestButton.data("create-payment-intent-url"),
                            dataType: 'json',
                            success: function (data) {
                                if (data.success) {
                                    // Close stripe terminal.
                                    ev.complete('success');
                                    location.href = data.redirectUrl || paymentRequestButton.data("redirect-url");
                                }
                                else {
                                    // Display error in stripe terminal.
                                    ev.complete('fail');
                                }
                            },
                            error: function (xhr, status, error) {
                                ev.complete('fail');
                                displayNotification(error, 'error');
                            }
                        });
                    }
                    else {
                        ev.complete('fail');
                    }
                });
            });

            if (isCartPage) {
                $(document).on('shoppingCartRefresh', function (e) {
                    if (e.success) {
                        var total = $('#CartSummaryTotal').data('total');
                        if (total == 0.0) {
                            total = $('#CartSummarySubtotal').data('subtotal');
                        }

                        // Convert total to the currency-specific integer minor unit expected by Stripe.
                        total = Math.round(total * currencyMinorUnitFactor);

                        // Update payment request.
                        paymentRequest.update({ total: { label: "Updated total", amount: total } });
                    }
                });
            }
            else {
                EventBroker.subscribe("ajaxcart.updated", function (msg, data) {
                    // Convert total to the currency-specific integer minor unit expected by Stripe.
                    var total = Math.round(data.SubTotalValue * currencyMinorUnitFactor);

                    // Update payment request.
                    paymentRequest.update({ total: { label: "Updated total", amount: total } });
                });
            }
        }
    };
})();
