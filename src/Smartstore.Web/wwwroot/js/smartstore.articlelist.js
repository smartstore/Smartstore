/*
*  Project: Smartstore Article List
*  Author: Murat Cakir, SmartStore AG
*/

; (function ($, window, document, undefined) {

    $('.artlist-grid').on('mouseenter', '.art', function (e) {
        if (window.touchable)
            return;

        var art = $(this);
        var list = art.closest('.artlist');

        if (list.parent().hasClass('artlist-carousel')) {
            return;
        }

        var drop = art.find('.art-drop');

        if (drop.length > 0) {
            var bottomMargin = (drop.outerHeight(true) * -1) + 2;
            drop.css('bottom', bottomMargin + 'px');
            art.addClass('active');
            // the Drop can be overlayed by succeeding elements otherwise
            list.css('z-index', 100);

            art.find('.sr-toggle').css("bottom", bottomMargin + "px") 
        }
    });

    $('.artlist-grid').on('mouseleave', '.art', function (e) {
        var art = $(this);

        if (art.hasClass('active')) {
            art.removeClass('active')
                .find('.art-drop')
                .css('bottom', 0)
                .closest('.artlist')
                .css('z-index', 'initial');

            art.find('.sr-toggle').attr("aria-expanded", false).css("bottom", 0) 
        }
    });

    $('.artlist-grid').on('ak-expand', '.art', (e) => {
        $(e.target).trigger("mouseenter");
    });

    $('.artlist-grid').on('ak-collapse', '.art', (e) => {
        $(e.target).trigger("mouseleave");
    });

    // Action panels
    // -------------------------------------------------------------------

    $('.artlist-actions').on('change', '.artlist-action-select', function (e) {
        var select = $(this),
            qname = select.data('qname'),
            url = select.data('url'),
            val = select.val();

        url = window.modifyUrl(url, qname, val);

        window.setLocation(url);
    });


    // Carousel handling
    // -------------------------------------------------------------------

})(jQuery, window, document);