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
        
        updateShoppingCartItems(link.data('href'), data);
        return false;
    });

    // Activate/deactivate cart item.
    orderSummary.on('change', '.select-cart-item-checkbox', function () {
        const self = $(this);
        const cartItemId = self.closest('.cart-row').data('key');
        const url = orderSummary.data('update-item-url');

        updateShoppingCartItems(url, {
            cartItemId,
            active: this.checked,
            isCartPage: true,
            isWishlist
        });
    });

    // Activate/deactivate all cart items.
    $('#SelectAllCartItems').on('click', '.select-cart-items, .deselect-cart-items', function (e) {
        e.preventDefault();

        const activateAll = $(this).hasClass('select-cart-items');
        const url = orderSummary.data('update-item-url');

        updateShoppingCartItems(url, {
            activateAll,
            isCartPage: true,
            isWishlist
        });
        
        return false;
    });

    // Quantity number input change.
    var debouncedUpdate = _.debounce(function (e) {
        e.preventDefault();

        var form = $(this).closest("form");
        if (!form[0].reportValidity()) {
            return false;
        }

        var link = $(this);
        updateShoppingCartItems(link.data('href'), {
            cartItemId: link.data("sci-id"),
            newQuantity: link.val(),
            isCartPage: true,
            isWishlist
        });

        return false;
    }, 350, false);

    orderSummary.on('change', '.qty-input .form-control', debouncedUpdate);

    function updateShoppingCartItems(url, data) {
        if (updatingCart)
            return;

        updatingCart = true;
        showThrobber();

        $.ajax({
            cache: false,
            url,
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

                if (response.success) {
                    if (!_.isEmpty(response.cartHtml)) {
                        cartBody.html(response.cartHtml);
                    }
                    if (!_.isEmpty(response.totalsHtml)) {
                        $('#order-totals').html(response.totalsHtml);
                    }
                    if (!_.isEmpty(response.discountHtml)) {
                        $('.cart-action-coupon').replaceWith(response.discountHtml);
                    }
                    if (!_.isEmpty(response.giftCardHtml)) {
                        $('.cart-action-giftcard').replaceWith(response.giftCardHtml);
                    }
                    if (!_.isEmpty(response.rewardPointsHtml)) {
                        $('.cart-action-rewardpoints').replaceWith(response.rewardPointsHtml);
                    }
                    if (!_.isEmpty(response.estimateShippingHtml)) {
                        $('.cart-action-shipping').replaceWith(response.estimateShippingHtml);
                    }

                    $('#SelectAllCartItems').html(response.itemSelectionHtml);
                    $('#cart-warnings').html(response.warningsHtml);

                    const displayCheckoutButtons = response.displayCheckoutButtons !== false;
                    $('#start-checkout-buttons').toggleClass('d-none', !displayCheckoutButtons);

                    const checkoutAllowed = response.checkoutAllowed !== false;
                    $('#cart-summary-container').toggleClass('d-none', !checkoutAllowed);
                    $('#cart-select-products-warning').toggleClass('d-none', checkoutAllowed);
                }

                displayNotification(response.message, response.success ? "success" : "error");

                // Reinit quantity controls.
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