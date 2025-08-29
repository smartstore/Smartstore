/*
*  Project: Smartstore Article List
*  Author: Murat Cakir, SmartStore AG
*/

; (function ($, window, document, undefined) {

    $('.artlist').each((_, list) => {
        const $list = $(list);
        const isCarousel = $list.parent().is('.artlist-carousel');
        const isGrid = $list.is('.artlist-grid');

        // Add "Skip list" button to list
        const isInDropdown = $list.closest('.dropdown-menu').length;
        if (!isInDropdown) {
            const numItems = isCarousel ? $list.find('.art').length : list.children.length;
            if (numItems > 3) {
                const $container = isCarousel ? $list.parent() : $list;
                const $btn = $(`<a href="javascript:;" class="btn-skip-content btn btn-primary rounded-pill">${Res['Common.SkipList']}</a>`).prependTo($container);
                if (!isCarousel) {
                    $btn.wrap('<div class="skip-content-container" role="listitem"></div>');
                }
            }
        }

        if (isGrid) {
            $list.on('mouseenter', '.art', function (e) {
                if (window.touchable || isCarousel)
                    return;

                const art = $(this);
                const drop = art.find('.art-drop');

                if (drop.length > 0) {
                    const bottomMargin = (drop.outerHeight(true) * -1) + 2;
                    drop.css('bottom', bottomMargin + 'px');
                    art.addClass('active');
                    // the Drop can be overlayed by succeeding elements otherwise
                    $list.css('z-index', 100);

                    art.find('.sr-toggle').css("bottom", bottomMargin + "px")
                }
            });

            $list.on('mouseleave', '.art', function (e) {
                const art = $(this);

                if (art.hasClass('active')) {
                    art.removeClass('active')
                        .find('.art-drop')
                        .css('bottom', 0)
                        .closest('.artlist')
                        .css('z-index', 'initial');

                    art.find('.sr-toggle').attr("aria-expanded", false).css("bottom", 0)
                }
            });

            $list.on('expand.ak collapse.ak', '.art', (e) => {
                e.stopPropagation();
                const mouseEvent = e.type == 'expand' ? 'mouseenter' : 'mouseleave';
                $(e.target).trigger(mouseEvent);
            });
        }
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

})(jQuery, window, document);