(function ($) {

    $.fn.extend({
        megaMenu: function (settings) {

            var defaults = {
                productRotatorInterval: 4000,
                productRotatorDuration: 300,
                productRotatorCycle: false,
                productRotatorAjaxUrl: ""
            };

            var rtl = Smartstore.globalization.culture.isRTL;

            settings = $.extend(defaults, settings);

            return this.each(function () {
                var megamenuContainer = $(this);
                var megamenu = $(".megamenu", megamenuContainer);
                var isSimple = megamenu.hasClass("simple");
                var megamenuNext = $(".megamenu-nav--next", megamenuContainer);
                var megamenuPrev = $(".megamenu-nav--prev", megamenuContainer);
                var megamenuDropdownContainer = $('.megamenu-dropdown-container');
                var navElems = megamenu.find(".navbar-nav .nav-item");	// li
                var zoomContainer = $(".zoomContainer");		// needed to fix elevateZoom z-index problem e.g. in product detail gallery
                var closingTimeout = 0;							// timeout to handle delay of dropdown closing
                var openTimeout;								// timeout to handle opening attempts in tryOpen
                var tempLink; 		// for temporary storage of the link that was passed into tryOpen, its needed to determine whether a new link was passed into tryOpen so the timeout for the old link can be cleared

                function closeMenuHandler(link, closeImmediatly) {
                    closingTimeout = setTimeout(function () { closeNow(link) }, 250);
                }

                function closeNow(link) {
                    $(link.data("target")).removeClass("show");

                    if (link.hasClass("dropdown-toggle")) {
                        link.closest("li").removeClass("active");
                        link.attr("aria-expanded", "false");
                    }

                    zoomContainer.css("z-index", "999");
                }

                function tryOpen(link) {
                    // if new link was passed into function clear tryOpen-timeout
                    if (tempLink && link.data("target") != tempLink.data("target")) {
                        clearTimeout(openTimeout);
                    }

                    // just open if there are no open menus, else wait and try again as long as there is a menu open
                    if (navElems.hasClass('active') || megamenuDropdownContainer.hasClass('show')) {
                        tempLink = link;
                        openTimeout = setTimeout(function () { tryOpen(link); }, 50);
                    }
                    else {
                        clearTimeout(openTimeout);
                        $(link.data("target")).addClass("show");

                        if (link.hasClass("dropdown-toggle")) {
                            link.closest("li").addClass("active");
                            link.attr("aria-expanded", "true");
                        }

                        initRotator(link.data("target"));

                        zoomContainer.css("z-index", "0");
                    }
                }

                if (window.touchable) {
                    // Handle opening events for touch devices
                    megamenuContainer.on('clickoutside', function (e) {
                        closeNow($(".nav-item.active .nav-link"));
                    });

                    navElems.on("click", function (e) {
                        var link = $(this).find(".nav-link");
                        var openedMenuSelector = $(".nav-item.active .nav-link").data("target");

                        if (openedMenuSelector != link.data("target")) {
                            e.preventDefault();
                            closeNow($(".nav-item.active .nav-link"));
                            tryOpen(link);
                        }
                    });
                }
                else {
                    // Handle opening events for desktop workstations
                    $(".dropdown-menu", megamenuContainer).on('mouseenter', function (e) {
                        clearTimeout(closingTimeout);
                    })
                    .on('mouseleave', function () {
                        var targetId = $(this).parent().attr("id");
                        var link = megamenu.find("[data-target='#" + targetId + "']");

                        closeMenuHandler(link);
                    });

                    navElems.on("mouseenter", function () {
                        var link = $(this).find(".nav-link");

                        // if correct dropdown is already open then don't open it again
                        var openedMenuSelector = $(".nav-item.active .nav-link").data("target");
                        var isActive = navElems.is(".active");

                        if ($(this).hasClass("nav-item") && openedMenuSelector != link.data("target")) {
                            closeNow($(".nav-item.active .nav-link"));
                        }
                        else if (openedMenuSelector == link.data("target")) {
                            clearTimeout(closingTimeout);
                            return;
                        }

                        // if one menu is already open, it means the user is currently using the menu, so either ...
                        if (isActive) {
                            // ... open at once 
                            tryOpen(link);
                        }
                        else {
                            // ... or open delayed
                            openTimeout = setTimeout(function () { tryOpen(link); }, 300);
                        }
                    })
                    .on("mouseleave", function () {
                        clearTimeout(openTimeout);

                        var link = $(this).find(".nav-link");
                        closeMenuHandler(link);
                    });
                }

                function alignDrop(popper, drop, container) {
                    var left,
                        right,
                        popperRect = popper[0].getBoundingClientRect(),
                        containerRect = container[0].getBoundingClientRect(),
                        popperWidth = popperRect.width,
                        dropWidth = drop.width(),
                        containerWidth = containerRect.width;

                    if (!rtl) {
                        left = Math.ceil(popperRect.left - containerRect.left);
                        right = "auto";

                        if (left < 0) {
                            left = 0;
                        }
                        else if (left + dropWidth > containerWidth) {
                            left = "auto";
                            right = 0;
                        }
                    }
                    else {
                        left = "auto";
                        right = Math.ceil(containerRect.right - popperRect.right);

                        if (right < 0) {
                            right = 0;
                        }
                        else if (right + dropWidth > containerWidth) {
                            left = 0;
                            right = "auto";
                        }
                    }

                    if (popperWidth > dropWidth) {
                        // ensure that drop is not smaller than popper
                        drop.width(popperWidth);
                    }

                    drop.toggleClass("ar", (rtl && left == "auto") || _.isNumber(right));

                    if (_.isNumber(left)) left = left + "px";
                    if (_.isNumber(right)) right = right + "px";

                    // jQuery does not accept "!important"
                    drop[0].style.setProperty('left', left, 'important');
                    drop[0].style.setProperty('right', right, 'important');
                }

                // correct dropdown position
                if (isSimple) {
                    var event = window.touchable ? "click" : "mouseenter";

                    navElems.on(event, function (e) {
                        var navItem = $(this);
                        var targetSelector = navItem.find(".nav-link").data("target");
                        if (!targetSelector)
                            return;

                        var drop = $(targetSelector).find(".dropdown-menu");
                        if (!drop.length)
                            return;

                        alignDrop(navItem, drop, megamenu);
                    });
                }

                megamenuContainer.evenIfHidden(function (el) {
                    var navSlider = $(".nav-slider", megamenu);

                    if ($.fn.scrollFade) {
                        navSlider.scrollFade();
                    }

                    if (!window.touchable) {
                        megamenuNext.on('click', function (e) {
                            e.preventDefault();
                            scrollToNextInvisibleNavItem(false);
                        });

                        megamenuPrev.on('click', function (e) {
                            e.preventDefault();
                            scrollToNextInvisibleNavItem(true);
                        });
                    }

                    function scrollToNextInvisibleNavItem(backwards) {
                        var sliderRect = navSlider[0].getBoundingClientRect();
                        var visibleItems = navElems.filter(function () {
                            var itemRect = this.getBoundingClientRect();
                            return itemRect.left >= sliderRect.left - 1 && itemRect.right <= sliderRect.right + 1;
                        });

                        if (visibleItems.length === 0) {
                            return;
                        }

                        var edgeItem = backwards ? visibleItems.first() : visibleItems.last();
                        var edgeIndex = navElems.index(edgeItem);
                        var nextIndex = backwards ? edgeIndex - 1 : edgeIndex + 1;
                        var nextItem = navElems.get(nextIndex);

                        if (nextItem) {
                            nextItem.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
                        }
                    }

                    // on touch
                    if (window.touchable) {
                        megamenu.tapmove(function () {
                            closeNow($(".nav-item.active .nav-link"));
                        });
                    }

                    function onPageResized() {
                        megamenuDropdownContainer.find('.megamenu-product-rotator > .artlist-grid').each(function (i, el) {
                            try {
                                $(this).slick('unslick');
                                $(this).attr('data-slick', '{"dots": false, "autoplay": true}');
                                applyCommonPlugins($(this).closest('.rotator-content'));
                            }
                            catch (err) { }
                        });
                    }

                    EventBroker.subscribe("page.resized", function (msg, viewport) {
                        onPageResized();
                    });

                    onPageResized();
                });

                function initRotator(containerId) {
                    var container = $(containerId);
                    var dropdownId = container.data("id") || 0;
                    var entityId = container.data("entity-id") || 0;
                    var entityName = container.data("entity-name") || 0;
                    var displayRotator = container.data("display-rotator");

                    // reinit slick product rotator
                    container.find('.megamenu-product-rotator > .artlist-grid').each(function (i, el) {
                        try {
                            $(this).slick('unslick');
                            $(this).attr('data-slick', '{"dots": false, "autoplay": true}');
                            applyCommonPlugins($(this).closest('.rotator-content'));
                        }
                        catch (err) {
                            console.log(err);
                        }
                    });

                    //if ($(".pl-slider", container).length == 0 && catId != null && displayRotator) {
                    if (displayRotator && entityId !== 0) {
                        var rotatorColumn = $(".rotator-" + dropdownId);

                        // clear content & init throbber
                        rotatorColumn.find(".rotator-content")
                            .html('<div class="placeholder"></div>')
                            .throbber({ white: true, small: true, message: '' });

                        // wait a little to imply hard work is going on ;-)
                        setTimeout(function () {
                            $.ajax({
                                cache: false,
                                type: "POST",
                                url: settings.productRotatorAjaxUrl,
                                data: { "entityId": entityId, "entityName": entityName },
                                success: function (data) {
                                    // add html view
                                    rotatorColumn.find(".rotator-content").html(data);

                                    var list = container.find('.megamenu-product-rotator > .artlist-grid');
                                    list.attr('data-slick', '{"dots": false, "autoplay": true}');

                                    // Init carousel
                                    applyCommonPlugins(container);

                                    if (container.hasClass("show")) {
                                        container.data("display-rotator", false);
                                    }
                                }
                            });
                        }, 300);
                    }
                    else {
                        container.find(".placeholder").addClass("empty");
                    }
                }

                // TODO: (mh) (wcag) Bad API design. These callback handlers should call methods that already exist here and not introduce new code and flow.
                // Accessibility event handling
                // Main nav elements
                megamenu.on("expand.ak", '.navbar-nav .nav-item', (e) => {
                    e.stopPropagation();
                    const el = $(e.detail.trigger);

                    if (isSimple) {
                        alignDrop(el.parent(), $(e.detail.target).find(".dropdown-menu"), megamenu);
                    }
                    tryOpen(el);
                });

                megamenu.on("collapse.ak", '.navbar-nav .nav-item', (e) => {
                    e.stopPropagation();
                    closeNow($(e.detail.trigger));
                });

                // Submenus (only available in simple menu)
                if (isSimple) {
                    megamenuDropdownContainer.on("expand.ak", '[role="menuitem"]', (e) => {
                        e.stopPropagation();
                        showDrop($(e.detail.trigger).parent());
                        $(e.detail.target).find('[role="menuitem"]:visible').first().trigger('focus');
                    });

                    megamenuDropdownContainer.on("collapse.ak", '[role="menuitem"]', (e) => {
                        e.stopPropagation();
                        if ($(e.detail.target).length) {
                            const currentItem = $(e.detail.trigger);
                            closeDrop(currentItem.parent());
                            currentItem.trigger('focus');
                        }
                    });
                }
            })
        }
    });
})(jQuery);
