Smartstore.Stripe = (function () {
    let elements;
    let stripe;
    
    const paymentRequestButtonId = "stripe-payment-request-button";
    const paymentRequestButtonSelector = "#" + paymentRequestButtonId;
    const paymentElementSelector = "#stripe-payment-element";
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
                }
            });
        });
    }

    return {
        initPaymentElement: function (publicApiKey, apiVersion, amount, currency, captureMethod) {
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

            const paymentElementOptions = { layout: "tabs" };
            const paymentElement = elements.create("payment", paymentElementOptions);
            paymentElement.mount(paymentElementSelector);

            paymentElement.on('change', function (event) {
                if (event.complete) {
                    // Enable next button.
                    var btnNext = $(".payment-method-next-step-button");
                    btnNext[0].disabled = false;
                }
            });
        },
        initPaymentSelectionPage: function (publicApiKey) {
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
            var createdPaymentMethod = false;

            $("form").on("submit", async e => {
                if ($("input[name='paymentmethod']:checked").val() == moduleSystemName && !createdPaymentMethod) {
                    e.preventDefault();

                    // Trigger form validation and wallet collection
                    const { error: submitError } = await elements.submit();
                    if (submitError) {
                        displayNotification(submitError.message, 'error');
                        return;
                    }

                    (async () => {
                        const { error, paymentMethod } = await stripe.createPaymentMethod({ elements });

                        $.ajax({
                            type: 'POST',
                            data: {
                                paymentMethodId: paymentMethod.id
                            },
                            url: $(paymentElementSelector).data("store-payment-selection-url"),
                            dataType: 'json',
                            success: function (data) {
                                createdPaymentMethod = true;
                                btnNext.trigger('click');
                            }
                        });
                    })();
                }
            });
        },
        initWalletButtonElement: function (publicApiKey, requestData, isCartPage, apiVersion) {
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

                                    // Redirect to billing address.
                                    location.href = paymentRequestButton.data("redirect-url");
                                } else {
                                    // Display error in stripe terminal.
                                    ev.complete('fail');
                                }
                            }
                        });
                    }
                })
            });

            if (isCartPage) {
                $(document).on('shoppingCartRefresh', function (e) {
                    if (e.success) {
                        var total = $('#CartSummaryTotal').data('total');
                        if (total == 0.0) {
                            total = $('#CartSummarySubtotal').data('subtotal');
                        }

                        // Convert total to smallest currency unit.
                        total = total * 100;

                        // Update payment request.
                        paymentRequest.update({ total: { label: "Updated total", amount: total } });
                    }
                });
            }
            else {
                EventBroker.subscribe("ajaxcart.updated", function (msg, data) {
                    // Convert total to smallest currency unit.
                    var total = data.SubTotalValue * 100;

                    // Update payment request.
                    paymentRequest.update({ total: { label: "Updated total", amount: total } });
                });
            }
        }
    };
})();