
/* smartstore.jquery.utils.js
-------------------------------------------------------------- */
;
(function ($) {

    var $w = $(window);
    var textExpanderId = 0;
    var scrollFadeId = 0;

    $.extend({

        topZIndex: function (selector) {
            /*
            /// summary
            /// 	Returns the highest (top-most) zIndex in the document
            /// 	(minimum value returned: 0).
            /// param "selector"
            /// 	(optional, default = "body *") jQuery selector specifying
            /// 	the elements to use for calculating the highest zIndex.
            /// returns
            /// 	The minimum number returned is 0 (zero).
            */
            return Math.max(0, Math.max.apply(null, $.map($(selector || "body *"),
                function (v) {
                    return parseInt($(v).css("z-index")) || null;
                }
            )));
        }

    }); // $.extend

    $.fn.extend({

        topZIndex: function (opt) {
            /*
            /// summary:
            /// 	Increments the CSS z-index of each element in the matched set
            /// 	to a value larger than the highest current zIndex in the document.
            /// 	(i.e., brings all elements in the matched set to the top of the
            /// 	z-index order.)
            /// param "opt"
            /// 	(optional) Options, with the following possible values:
            /// 	increment: (Number, default = 1) increment value added to the
            /// 		highest z-index number to bring an element to the top.
            /// 	selector: (String, default = "body *") jQuery selector specifying
            /// 		the elements to use for calculating the highest zIndex.
            /// returns type="jQuery"
            */

            // Do nothing if matched set is empty
            if (this.length === 0) {
                return this;
            }

            opt = $.extend({ increment: 1, selector: "body *" }, opt);

            // Get the highest current z-index value
            var zmax = $.topZIndex(opt.selector), inc = opt.increment;

            // Increment the z-index of each element in the matched set to the next highest number
            return this.each(function () {
                $(this).css("z-index", zmax += inc);
            });
        },

        cushioning: function (withMargins) {
            var el = $(this[0]);
            // returns the differences between outer and inner
            // width, as well as outer and inner height
            withMargins = _.isBoolean(withMargins) ? withMargins : true;
            return {
                horizontal: el.outerWidth(withMargins) - el.width(),
                vertical: el.outerHeight(withMargins) - el.height()
            }
        },

        horizontalCushioning: function (withMargins) {
            var el = $(this[0]);
            // returns the difference between outer and inner width
            return el.outerWidth(_.isBoolean(withMargins) ? withMargins : true) - el.width();
        },

        verticalCushioning: function (withMargins) {
            var el = $(this[0]);
            // returns the difference between outer and inner height
            return el.outerHeight(_.isBoolean(withMargins) ? withMargins : true) - el.height();
        },

        outerHtml: function () {
            // returns the (outer)html of a new DOM element that contains
            // a clone of the first match
            return $(document.createElement("div"))
                .append($(this[0]).clone())
                .html();
        },

        isChildOverflowing: function (child) {
            var p = jQuery(this).get(0);
            var el = jQuery(child).get(0);
            return (el.offsetTop < p.offsetTop || el.offsetLeft < p.offsetLeft) ||
                (el.offsetTop + el.offsetHeight > p.offsetTop + p.offsetHeight || el.offsetLeft + el.offsetWidth > p.offsetLeft + p.offsetWidth);
        },

        evenIfHidden: function (callback) {
            return this.each(function () {
                var self = $(this);
                var styleBackups = [];

                var hiddenElements = self.parents().addBack().filter(':hidden');

                if (!hiddenElements.length) {
                    callback(self);
                    return true; // continue the loop
                }

                hiddenElements.each(function () {
                    var style = $(this).attr('style');
                    style = typeof style == 'undefined' ? '' : style;
                    styleBackups.push(style);
                    $(this).attr('style', style + ' display: block !important;');
                });

                hiddenElements.eq(0).css('left', -10000);

                callback(self);

                hiddenElements.each(function () {
                    $(this).attr('style', styleBackups.shift());
                });
            });
        },

        /*
            Binds a simple JSON object (no collection) to a set of html elements
            defining the 'data-bind-to' attribute
        */
        bindData: function (data, options) {
            var defaults = {
                childrenOnly: false,
                includeSelf: false,
                showFalsy: false,
                animate: false
            };
            var opts = $.extend(defaults, options);

            return this.each(function () {
                var el = $(this);

                var elems = el.find(opts.childrenOnly ? '>[data-bind-to]' : '[data-bind-to]');
                if (opts.includeSelf)
                    elems = elems.addBack();

                elems.each(function () {
                    var elem = $(this);
                    var val = data[elem.data("bind-to")];
                    if (val !== undefined) {

                        if (opts.animate) {
                            elem.html(val)
                                .addClass('data-binding')
                                .one('animationend', function (e) {
                                    elem.removeClass('data-binding');
                                });
                        }
                        else {
                            elem.html(val);
                        }

                        if (!opts.showFalsy && !val) {
                            // it's falsy, so hide it
                            elem.hide();
                        }
                        else {
                            elem.show();
                        }
                    }
                });
            });
        },

		/**
		 * @desc A small plugin that checks whether elements are within
		 *       the user visible viewport of a web browser.
		 *       only accounts for vertical position, not horizontal.
		*/
        visible: function (partial, hidden, direction, container) {
            if (this.length < 1)
                return false;

            // Set direction default to 'both'.
            direction = direction || 'both';

            var $t = this.length > 1 ? this.eq(0) : this,
                isContained = typeof container !== 'undefined' && container !== null,
                $c = isContained ? $(container) : $w,
                wPosition = isContained ? $c.offset() : 0,
                t = $t.get(0),
                vpWidth = $c.outerWidth(),
                vpHeight = $c.outerHeight(),
                clientSize = hidden === true ? t.offsetWidth * t.offsetHeight : true;

            var rec = t.getBoundingClientRect(),
                tViz = isContained ?
                    rec.top - wPosition.top >= 0 && rec.top < vpHeight + wPosition.top :
                    rec.top >= 0 && rec.top < vpHeight,
                bViz = isContained ?
                    rec.bottom - wPosition.top > 0 && rec.bottom <= vpHeight + wPosition.top :
                    rec.bottom > 0 && rec.bottom <= vpHeight,
                lViz = isContained ?
                    rec.left - wPosition.left >= 0 && rec.left < vpWidth + wPosition.left :
                    rec.left >= 0 && rec.left < vpWidth,
                rViz = isContained ?
                    rec.right - wPosition.left > 0 && rec.right < vpWidth + wPosition.left :
                    rec.right > 0 && rec.right <= vpWidth,
                vV = partial ? tViz || bViz : tViz && bViz,
                hV = partial ? lViz || rViz : lViz && rViz,
                vVisible = (rec.top < 0 && rec.bottom > vpHeight) ? true : vV,
                hVisible = (rec.left < 0 && rec.right > vpWidth) ? true : hV;

            if (direction === 'both')
                return clientSize && vVisible && hVisible;
            else if (direction === 'vertical')
                return clientSize && vVisible;
            else if (direction === 'horizontal')
                return clientSize && hVisible;
        },

        textExpander: function () {
            return this.each(function () {
                var el = $(this);

                if (el.data('text-expander-initialized')) {
                    return;
                }

                // TODO: Remove the `.more-block` fallback and class migration after
                // stored HtmlEditor content has been migrated to text-expander markup.
                var inner = el.children('.text-expander-content, .more-block').first();
                if (inner.length === 0) {
                    return;
                }

                el.removeClass('more-less').addClass('text-expander');
                inner.removeClass('more-block').addClass('text-expander-content');

                // iOS Safari can lose a playing YouTube video when its containing block is clipped.
                if (window.touchable && /iPhone|iPad/.test(navigator.userAgent)) {
                    var containsToxicEmbed = el.find("iframe[src*='youtube.com']").length > 0;
                    if (containsToxicEmbed) {
                        el.removeClass('text-expander');
                        return;
                    }
                }

                function getActualHeight() {
                    return inner.outerHeight(false);
                }

                const maxHeight = el.data('max-height') || 260;
                const innerId = inner.attr('id')
                    || (el.attr('id') ? el.attr('id') + '-content' : 'text-expander-content-' + (++textExpanderId));
                var expanded = false;

                inner.attr('id', innerId);
                el[0].style.setProperty('--text-expander-collapsed-height', maxHeight + 'px');

                var toggle = $(`<button type="button" class="text-expander-toggle btn btn-plain rounded-pill px-4" aria-expanded="false" aria-controls="${innerId}">`
                    + `<span class="text-expander-label-more"><i class="fa fa-angle-double-down pr-2" aria-hidden="true"></i>${Res['Products.Longdesc.More']}</span>`
                    + `<span class="text-expander-label-less"><i class="fa fa-angle-double-up pr-2" aria-hidden="true"></i>${Res['Products.Longdesc.Less']}</span></button>`);

                el.append(toggle).data('text-expander-initialized', true);

                function refresh() {
                    var actualHeight = getActualHeight();

                    if (actualHeight === 0) {
                        el.evenIfHidden(function () {
                            actualHeight = getActualHeight();
                        });
                    }

                    const isCollapsible = actualHeight > maxHeight;

                    el.toggleClass('is-collapsible', isCollapsible)
                        .toggleClass('expanded', isCollapsible && expanded)
                        .toggleClass('collapsed', isCollapsible && !expanded);
                    toggle.aria('expanded', expanded);
                }

                toggle.on('click.textExpander', function () {
                    expanded = !expanded;
                    refresh();
                });

                inner.on('focusin.textExpander', function (e) {
                    if (!expanded && el.hasClass('collapsed')) {
                        const containerRect = el[0].getBoundingClientRect();
                        const targetRect = e.target.getBoundingClientRect();

                        if (targetRect.top < containerRect.top || targetRect.bottom > containerRect.bottom) {
                            expanded = true;
                            refresh();
                        }
                    }
                });

                refresh();

                if (window.ResizeObserver) {
                    const resizeObserver = new ResizeObserver(refresh);
                    resizeObserver.observe(inner[0]);
                    el.data('text-expander-resize-observer', resizeObserver);
                }
            });
        },

        // TODO: Remove after stored HtmlEditor content and external callers have
        // migrated to `.text-expander` and `.textExpander()`.
        moreLess: function () {
            return this.textExpander();
        },

        scrollFade: function () {
            return this.each(function () {
                const node = this;
                const el = $(node);
                const existingInstance = el.data('scroll-fade-instance');

                if (existingInstance) {
                    existingInstance.refresh();
                    return;
                }

                const namespace = '.scrollFade' + (++scrollFadeId);
                const edgeTolerance = 1;
                let frameId;
                let lastState;
                let resizeObserver;
                let mutationObserver;

                function scheduleRefresh() {
                    if (frameId) {
                        return;
                    }

                    frameId = window.requestAnimationFrame(refresh);
                }

                function refresh() {
                    frameId = null;

                    const style = window.getComputedStyle(node);
                    const axisValue = style.getPropertyValue('--scroll-fade-axis').trim();
                    const axis = axisValue === 'x' || axisValue === 'y' ? axisValue : null;
                    const reverse = axis === 'x' && style.direction === 'rtl';
                    let atStart = true;
                    let atEnd = true;

                    if (axis === 'x') {
                        const maxScroll = Math.max(0, node.scrollWidth - node.clientWidth);
                        const scrollPosition = Math.min(maxScroll, Math.max(0, reverse ? Math.abs(node.scrollLeft) : node.scrollLeft));

                        atStart = scrollPosition <= edgeTolerance;
                        atEnd = maxScroll - scrollPosition <= edgeTolerance;
                    }
                    else if (axis === 'y') {
                        const maxScroll = Math.max(0, node.scrollHeight - node.clientHeight);
                        const scrollPosition = Math.min(maxScroll, Math.max(0, node.scrollTop));

                        atStart = scrollPosition <= edgeTolerance;
                        atEnd = maxScroll - scrollPosition <= edgeTolerance;
                    }

                    el.toggleClass('scroll-fade-reverse', reverse)
                        .toggleClass('scroll-fade-at-start', atStart)
                        .toggleClass('scroll-fade-at-end', atEnd)
                        .addClass('scroll-fade-ready');

                    const state = [axis, atStart, atEnd, reverse].join(':');
                    if (state !== lastState) {
                        lastState = state;
                        el.trigger('scrollfadechange', [{ axis, atStart, atEnd, reverse }]);
                    }
                }

                function observeResizeTargets() {
                    if (!resizeObserver) {
                        return;
                    }

                    resizeObserver.disconnect();
                    resizeObserver.observe(node);
                    Array.from(node.children).forEach(function (child) {
                        resizeObserver.observe(child);
                        Array.from(child.children).forEach(function (grandchild) {
                            resizeObserver.observe(grandchild);
                        });
                    });
                }

                node.addEventListener('scroll', scheduleRefresh, { passive: true });
                node.addEventListener('load', scheduleRefresh, true);
                $w.on('resize' + namespace, scheduleRefresh);

                if (window.ResizeObserver) {
                    resizeObserver = new ResizeObserver(scheduleRefresh);
                    observeResizeTargets();
                }

                if (window.MutationObserver) {
                    mutationObserver = new MutationObserver(function () {
                        observeResizeTargets();
                        scheduleRefresh();
                    });
                    mutationObserver.observe(node, { childList: true, subtree: true });
                }

                if (document.fonts?.ready) {
                    document.fonts.ready.then(scheduleRefresh);
                }

                el.data('scroll-fade-instance', {
                    refresh: scheduleRefresh,
                    destroy: function () {
                        window.cancelAnimationFrame(frameId);
                        node.removeEventListener('scroll', scheduleRefresh);
                        node.removeEventListener('load', scheduleRefresh, true);
                        $w.off(namespace);
                        resizeObserver?.disconnect();
                        mutationObserver?.disconnect();
                        el.removeData('scroll-fade-instance')
                            .removeClass('scroll-fade-ready scroll-fade-at-start scroll-fade-at-end scroll-fade-reverse');
                    }
                });

                refresh();
            });
        },

        // Element must be decorated with visibility:hidden
        masonryGrid: function (itemSelector, callback) {

            return this.each(function () {

                var self = $(this);
                var grid = self[0];
                var allItems = self.find(".card");

                var viewport = ResponsiveBootstrapToolkit;
                if (viewport.is('<=sm')) {
                    self.css("visibility", "visible");
                    return false;
                }

                self.addClass("masonry-grid");

                // first call so aos can be initialized correctly
                var hasResized = false;
                resizeAllGridItems();

                self.imagesLoaded(function () {
                    // second call to get correct size if pictures weren't loaded on the first call
                    resizeAllGridItems();
                    self.css("visibility", "visible");

                    if (typeof callback === 'function') {
                        _.defer(function () {
                            callback.call(this);
                        });
                    }
                });

                function resizeGridItem(item) {
                    let innerItem;
                    if (itemSelector.length) {
                        innerItem = item.querySelector(itemSelector);
                    }
                    else {
                        innerItem = item.firstElementChild;
                    }
                    
                    var computedStyle = window.getComputedStyle(grid);
                    var rowHeight = parseInt(computedStyle.getPropertyValue('grid-auto-rows'));
                    var rowGap = parseInt(computedStyle.getPropertyValue('grid-row-gap'));

                    if (hasResized) {
                        item.style.gridRowEnd = undefined;
                        innerItem.style.height = "";
                    }

                    var rowSpan = Math.ceil(innerItem.getBoundingClientRect().height / (rowHeight + rowGap));
                    item.style.gridRowEnd = "span " + rowSpan;
                    innerItem.style.height = "100%";
                }

                function resizeAllGridItems() {
                    allItems.each(function () {
                        resizeGridItem($(this)[0]);
                    });
                    hasResized = true;
                }

                var timeout;

                $w.on("resize", function () {
                    if (timeout) {
                        window.cancelAnimationFrame(timeout);
                    }

                    timeout = window.requestAnimationFrame(resizeAllGridItems);
                });
            });
        },

        /**
         * Gets or sets ARIA attributes on the matched elements.
         * 
         * @param {String|Object} key Either the ARIA attribute name (without 'aria-' prefix) 
         *                            or an object of key-value pairs
         * @param {String} [value] The value to set (if setting a single attribute)
         * @return {String|jQuery} Returns the attribute value when getting, or the jQuery object for chaining when setting
         */
        aria: function (key, value) {
            // Handle getting values
            if (value === undefined && typeof key === 'string') {
                // Get the first element's attribute
                if (this.length === 0) return undefined;
                var attrName = 'aria-' + key;
                var attrValue = this[0].getAttribute(attrName);

                // Convert "true"/"false" to booleans if they're not strings
                if (attrValue === 'true') return true;
                if (attrValue === 'false') return false;

                return attrValue;
            }

            // Handle setting values
            return this.each(function () {
                // Handle object of key-value pairs
                if (typeof key === 'object') {
                    for (var k in key) {
                        if (key.hasOwnProperty(k)) {
                            this.setAttribute('aria-' + k, key[k]);
                        }
                    }
                }
                // Handle single key-value pair
                else if (typeof key === 'string') {
                    this.setAttribute('aria-' + key, value);
                }
            });
        }
    }); // $.fn.extend

    // Shorter aliases
    $.fn.gap = $.fn.cushioning;
    $.fn.hgap = $.fn.horizontalCushioning;
    $.fn.vgap = $.fn.verticalCushioning;

})(jQuery);
