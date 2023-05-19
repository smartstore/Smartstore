;
$(function () {
    var orderSummary = $(".cart-content");
    var isWishlist = orderSummary.is('.wishlist-content');
    var updatingCart = false;

    // Remove cart item and move to wishlist.
    orderSummary.on("click", ".ajax-action-link", function (e) {
        e.preventDefault();
        var link = $(this);
        // What is "link.data"?
        var data = link.data('post-form') ? $(this).closest('form').serialize() : link.data;
        
        updateShoppingCartItems(link, data);
        return false;
    });

    // Quantity numberinput change.
    var debouncedUpdate = _.debounce(function (e) {
        e.preventDefault();

        var form = $(this).closest("form");
        if (!form[0].reportValidity()) {
            return false;
        }

        var link = $(this);
        updateShoppingCartItems(link, { sciItemId: link.data("sci-id"), newQuantity: link.val(), isCartPage: true, isWishlist: isWishlist });
        return false;
    }, 350, false);

    orderSummary.on('change', '.qty-input .form-control', debouncedUpdate);

    function updateShoppingCartItems(link, data) {
        if (updatingCart)
            return;

        updatingCart = true;
        showThrobber();

        $.ajax({
            cache: false,
            url: link.data("href"),
            data: data,
            type: 'POST',
            success: function (response) {
                if (!_.isEmpty(response.redirect)) {
                    location.href = response.redirect;
                    return false;
                }

                if (response.cartItemCount == 0) {
                    orderSummary.html('<div class="alert alert-warning fade show">' + orderSummary.data('empty-text') + '</div>');
                }

                var cartBody = $(".cart-body");
                var totals = $("#order-totals");
                var discountCoupon = $(".cart-action-coupon");
                var giftCardCoupon = $(".cart-action-giftcard");

                if (response.success) {
                    $("#start-checkout-buttons").toggleClass("d-none", !response.displayCheckoutButtons);

                    // Replace HTML.
                    cartBody.html(response.cartHtml);
                    totals.html(response.totalsHtml);

                    if (discountCoupon) {
                        discountCoupon.html(response.discountHtml);
                    }
                    if (giftCardCoupon) {
                        giftCardCoupon.html(response.giftCardHtml);
                    }
                }

                displayNotification(response.message, response.success ? "success" : "error");

                // Reinit qty controls.
                applyCommonPlugins(cartBody);

                // Update shopbar.
                ShopBar.loadSummary('cart', true);
                ShopBar.loadSummary('wishlist', true);

                hideThrobber();

                var cartRefreshEvent = jQuery.Event('shoppingCartRefresh');
                cartRefreshEvent.success = response.success;
                $(document).trigger(cartRefreshEvent);
            },
            complete: function () {
                updatingCart = false;
            }
        });
    }

    function showThrobber() {
        var cnt = $("#cart-items");
        var throbber = cnt.data('throbber');
        if (!throbber) {
            throbber = cnt.throbber({ white: true, small: true, message: '', show: false, speed: 0 }).data('throbber');
        }

        throbber.show();
    }

    function hideThrobber() {
        var cnt = $("#cart-items");
        _.delay(function () {
            if (cnt.data("throbber"))
                cnt.data("throbber").hide();
        }, 100);
    }
})