﻿
/* smartstore.jquery.utils.js
-------------------------------------------------------------- */
;
(function ($) {

    var $w = $(window);

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

        moreLess: function () {
            return this.each(function () {
                var el = $(this);

                // iOS Safari freaks out when a YouTube video starts playing while the block is collapsed:
                // the video disapperars after a while! Other video embeds like Vimeo seem to behave correctly.
                // So: shit on moreLess in this case.
                if (window.touchable && /iPhone|iPad/.test(navigator.userAgent)) {
                    var containsToxicEmbed = el.find("iframe[src*='youtube.com']").length > 0;
                    if (containsToxicEmbed) {
                        el.removeClass('more-less');
                        return;
                    }
                }

                var inner = el.find('> .more-block');

                function getActualHeight() {
                    return inner.length > 0 ? inner.outerHeight(false) : el.outerHeight(false);
                }

                var actualHeight = getActualHeight();

                if (actualHeight === 0) {
                    el.evenIfHidden(function () {
                        actualHeight = getActualHeight();
                    });
                }

                const elId = el.attr('id') || '';
                const maxHeight = el.data('max-height') || 260;

                if (actualHeight <= maxHeight) {
                    el.css('max-height', 'none');
                    return;
                }
                else {
                    el.css('max-height', maxHeight + 'px');
                    el.addClass('collapsed');
                }

                el.on('click', '.btn-text-expander', function (e) {
                    e.preventDefault();
                    const expanding = $(this).hasClass('btn-text-expander--expand');

                    el.toggleClass('expanded', expanding).toggleClass('collapsed', !expanding);
                    el.find('.btn-text-expander--expand').aria('expanded', expanding);
                    el.find('.btn-text-expander--collapse').aria('expanded', !expanding);
                    return false;
                });

                var expander = el.find('.btn-text-expander--expand');
                if (expander.length === 0) {
                    el.append(`<a href="#" class="btn-text-expander btn-text-expander--expand" aria-expanded="false" aria-controls="${elId}">`
                        + `<i class="fa fa fa-angle-double-down pr-2" aria-hidden="true"></i><span>${Res['Products.Longdesc.More']}</span></a>`);
                }

                var collapser = el.find('.btn-text-expander--collapse');
                if (collapser.length === 0) {
                    el.append(`<a href="#" class="btn-text-expander btn-text-expander--collapse focus-inset" aria-expanded="true" aria-controls="${elId}">`
                        + `<i class="fa fa fa-angle-double-up pr-2" aria-hidden="true"></i><span>${Res['Products.Longdesc.Less']}</span></a>`);
                }
            });
        },

        // Element must be decorated with visibility:hidden
        masonryGrid: function (itemSelector, callback) {

            return this.each(function () {

                var self = $(this);
                var grid = self[0];

                if (typeof itemSelector === "function") {
                    callback = itemSelector;
                    itemSelector = undefined;
                }

                var viewport = ResponsiveBootstrapToolkit;
                if (viewport.is('<=sm')) {
                    self.css("visibility", "visible");
                    return false;
                }

                self.addClass("masonry-grid");

                var hasResized = false;

                function getGridItems() {
                    var items = self.children().filter(function () {
                        return this.nodeType === 1;
                    });

                    if (!items.length) {
                        items = self.find(".card");
                    }

                    return items;
                }

                function resolveInnerItem(item) {
                    if (typeof itemSelector !== "string" || !itemSelector.length) {
                        return item;
                    }

                    if (item.matches && item.matches(itemSelector)) {
                        return item;
                    }

                    try {
                        var inner = item.querySelector(itemSelector);
                        if (inner) {
                            return inner;
                        }
                    }
                    catch (e) {
                        // ignore selector errors and fall back to the grid item itself
                    }

                    return item;
                }

                function computeGridMetrics() {
                    var style = window.getComputedStyle(grid);
                    var rowHeight = parseFloat(style.getPropertyValue("grid-auto-rows")) || 0;
                    if (!rowHeight) {
                        var templateRows = style.getPropertyValue("grid-template-rows");
                        if (templateRows) {
                            rowHeight = parseFloat(templateRows.split(" ")[0]) || 0;
                        }
                    }
                    var rowGap = parseFloat(style.getPropertyValue("grid-row-gap")) || parseFloat(style.getPropertyValue("row-gap")) || 0;

                    return {
                        rowHeight: rowHeight,
                        rowGap: rowGap
                    };
                }

                function measureItemHeight(item) {
                    var rect = item.getBoundingClientRect();
                    var style = window.getComputedStyle(item);
                    var marginTop = parseFloat(style.marginTop) || 0;
                    var marginBottom = parseFloat(style.marginBottom) || 0;
                    return rect.height + marginTop + marginBottom;
                }

                function resizeGridItem(item, metrics) {
                    var innerItem = resolveInnerItem(item);

                    if (hasResized) {
                        item.style.removeProperty("grid-row-end");
                        if (innerItem !== item) {
                            innerItem.style.removeProperty("height");
                        }
                    }

                    if (!metrics.rowHeight) {
                        return;
                    }

                    var totalHeight = measureItemHeight(item);
                    var denominator = metrics.rowHeight + metrics.rowGap;
                    var rowSpan = denominator > 0 ? Math.max(Math.round((totalHeight + metrics.rowGap) / denominator), 1) : 1;
                    item.style.gridRowEnd = "span " + rowSpan;

                    if (innerItem !== item) {
                        innerItem.style.height = "100%";
                    }
                }

                function resizeAllGridItems() {
                    var metrics = computeGridMetrics();
                    getGridItems().each(function () {
                        resizeGridItem(this, metrics);
                    });
                    hasResized = true;
                }

                resizeAllGridItems();

                self.imagesLoaded(function () {
                    // second call to get correct size if pictures weren't loaded on the first call
                    resizeAllGridItems();
                    self.css("visibility", "visible");

                    if (typeof callback === 'function') {
                        _.defer(function () {
                            callback.call(grid);
                        });
                    }
                });

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