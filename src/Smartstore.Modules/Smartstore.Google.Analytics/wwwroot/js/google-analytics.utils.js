(function ($, window, document, undefined) {

    // Storage for list data.
    window.gaListDataStore = [];

    // Event handler for product list clicks.
    $('.artlist').on('click', '.art-picture, .art-name > a, .add-to-cart-button, .add-to-wishlist-button, .product-details-button', function (e) {
        //e.preventDefault(); // Uncomment when testing

        var $el = $(e.target);
        var eventType = getGAEventTypeForListClick($el.closest('.btn') || $el);
        var id = $el.closest('.art').data('id');

        // Get list from data store
        let list = window.gaListDataStore.filter(function (obj) {
            // TODO: (mh) (core) Get real list name
            return obj.item_list_name === 'RecentlyViewedProducts';
        });

        if (list[0]) {
            let item = list[0].items.filter(function (obj) {
                return obj.entity_id === id;
            });

            // Fire event
            gtag('event', eventType, {
                item_list_name: item[0].item_list_name,
                currency: item[0].currency,
                value: item[0].price,
                items: [item]
            });
        }
    });

    // Event handler for remove cart item
    $('.cart-body').on('click', '[data-type="cart"]', function (e) {
        // There's only one product list on cart page
        let list = window.gaListDataStore[0];
        var $el = $(e.target);
        var btn = $el.closest('.btn') || $el;
        var id = btn.data('id');

        let item = list.filter(function(obj) {
	        return obj.entity_id === id;
        });

        // Fire event
        gtag('event', 'remove_from_cart', {
            item_list_name: item[0].item_list_name,
            currency: item[0].currency,
            value: item[0].price,
            items: [item]
        });
    });

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
})(jQuery, this, document);