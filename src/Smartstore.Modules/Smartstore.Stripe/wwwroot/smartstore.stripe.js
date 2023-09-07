Smartstore.Stripe = (function () {
    let elements;
    let stripe;
    
    const paymentRequestButtonId = "stripe-payment-request-button";
    const paymentRequestButtonSelector = "#" + paymentRequestButtonId;
    const paymentElementSelector = "#stripe-payment-element";
    const moduleSystemName = "Payments.StripeElements";

    return {
        initPaymentElement: function (publicApiKey, secret, apiVersion) {
            stripe = Stripe(publicApiKey, {
                apiVersion: apiVersion,
                betas: ['elements_enable_deferred_intent_beta_1'],
            });

            const { clientSecret } = { clientSecret: secret };

            const appearance = {
                theme: 'stripe',
            };

            elements = stripe.elements({ appearance, clientSecret });

            const paymentElementOptions = {
                layout: "tabs",
            };

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
                if (e.target.value == moduleSystemName) {
                    btnNext[0].disabled = true;
                } else {
                    btnNext[0].disabled = false;
                }
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
                    (async () => {
                        const { error, paymentMethod } = await stripe.createPaymentMethod({
                            elements
                        });
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
        initWalletButtonElement: function (publicApiKey, requestData, apiVersion) {
            stripe = Stripe(publicApiKey, {
                apiVersion: apiVersion,
            });

            const paymentRequest = stripe.paymentRequest(requestData);

            const elements = stripe.elements();
            const prButton = elements.create('paymentRequestButton', {
                paymentRequest,
            });

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
                    dataType: 'json',
                    success: function (data) {
                        if (data.success) {
                            paymentRequest.update(JSON.parse(data.paymentRequest))
                        }
                    }
                });
            });

            // Will be handled when payment is done in stripe terminal.
            paymentRequest.on('paymentmethod', async (ev) => {
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
            });
        }
    };
})();