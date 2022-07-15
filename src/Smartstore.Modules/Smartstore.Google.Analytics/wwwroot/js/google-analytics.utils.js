(function ($, window, document, undefined) {

    // Storage for list data.
    window.gaListDataStore = [];

    $(function () {
        // Event handler for product list clicks.
        $('.artlist').on('click', '.art-picture, .art-name > a, .add-to-cart-button, .add-to-wishlist-button, .product-details-button', function (e) {
            var $el = $(e.currentTarget);
            var id = $el.closest('.art').data('id');
            var eventType = getGAEventTypeForListClick($el);
            var listName = getProductListName($el.closest('.product-grid'));

            fireEvent(listName, id, eventType);
        });

        // Event handler for product detail clicks.
        $('.pd-offer').on('click', '.btn-add-to-cart, .action-add-to-wishlist', function (e) {
            var btn = $(e.currentTarget);
            var id = $('#main-update-container').data("id");
            var eventType = "add_to_cart";
            if (btn.hasClass("action-add-to-wishlist")) {
                eventType = "add_to_wishlist";
            }

            fireEvent('product-detail', id, eventType);
        });

        // Event handler for remove cart item on shopping cart page
        $('.cart-body').on('click', '[data-type="cart"]', function (e) {
            var btn = $(e.currentTarget);
            fireEvent('cart', btn.data('id'), 'remove_from_cart');
        });
    });

    function fireEvent(listName, entityId, eventType) {
        // Get list from data store
        let list = window.gaListDataStore.filter(function (obj) {
            return obj.item_list_name === listName;
        });

        if (list[0]) {
            let item = list[0].items.filter(function (obj) {
                return obj.entity_id === entityId;
            });

            // Fire event
            gtag('event', eventType, {
                item_list_name: item[0].item_list_name,
                currency: item[0].currency,
                value: item[0].price,
                items: item
            });
        }
    }

    function getGAEventTypeForListClick($el) {
        var eventType = 'select_item';

        if ($el.hasClass('add-to-cart-button')) {
            eventType = 'add_to_cart';
        }
        else if ($el.hasClass('add-to-wishlist-button')) {
            eventType = 'add_to_wishlist';
        }

        return eventType;
    }

    function getProductListName(grid) {
        var listName = "category";

        if (grid.hasClass("product-grid-home-page")) {
            listName = "HomeProducts";
        }
        else if (grid.hasClass("bestsellers")) {
            listName = "HomeBestSellers";
        }
        else if (grid.hasClass("recently-viewed-product-grid")) {
            listName = "RecentlyViewedProducts";
        }

        return listName;
    }
})(jQuery, this, document);