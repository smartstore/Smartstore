Smartstore.Stripe = (function () {
    let elements;
    let stripe;

    return {
        initPaymentElement: function (publicApiKey, secret) {
            stripe = Stripe(publicApiKey, {
                apiVersion: "2022-08-01",
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
            paymentElement.mount("#payment-element");

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
                if (e.target.value == "Smartstore.StripeElements") {
                    btnNext[0].disabled = true;
                } else {
                    btnNext[0].disabled = false;
                }
            });

            // Handle button state on page load
            if ($("input[name='paymentmethod']").val() == "Smartstore.StripeElements") {
                btnNext[0].disabled = true;
            }

            // Complete payment (must be done like this in order to be redirected correctly)
            var createdPaymentMethod = false;

            $("form").on("submit", async e => {
                if ($("input[name='paymentmethod']").val() == "Smartstore.StripeElements" && !createdPaymentMethod) {
                    e.preventDefault();
                    (async () => {
                        console.log(elements);
                        const { error, paymentMethod } = await stripe.createPaymentMethod({
                            elements
                        });
                        $.ajax({
                            type: 'POST',
                            data: {
                                paymentMethodId: paymentMethod.id
                            },
                            url: $("#payment-element").data("store-payment-selection-url"),
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
        initWalletButtonElement: function (publicApiKey, requestData) {
            stripe = Stripe(publicApiKey, {
                apiVersion: "2022-08-01",
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
                    prButton.mount('#payment-request-button');
                } else {
                    document.getElementById('payment-request-button').style.display = 'none';
                }
            })();

            var paymentRequestButton = $('#payment-request-button');

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