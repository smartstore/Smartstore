; (function (factory) {
    'use strict';
    if (typeof define === 'function' && define.amd) {
        // AMD
        define(['jquery'], factory);
    } else if (typeof module !== 'undefined' && module.exports) {
        // CommonJS
        module.exports = factory(require('jquery'));
    } else {
        // Global
        factory(jQuery);
    }
})(function ($) {
    'use strict';

    const $scrollTo = $.scrollTo = function (target, settings) {
        return $(window).scrollTo(target, settings);
    };

    $scrollTo.defaults = {
        axis: 'y', // Default scroll axis is vertical
        limit: true,
        offset: { top: 0, left: 0 }, // Default offset
        over: { top: 0, left: 0 }, // Default over value
        behavior: 'smooth' // Default scroll behavior
    };

    function isWin(el) {
        return el === window || el === document || el === document.body || el === document.documentElement;
    }

    $.fn.scrollTo = function (target, settings) {
        if (target === 'max') {
            target = 9e9;
        }

        settings = $.extend({}, $scrollTo.defaults, settings);
        settings.offset = both(settings.offset);
        settings.over = both(settings.over);

        return this.each(function () {
            if (target == null) return;

            const elem = this,
                $elem = $(elem),
                targ = target;

            const scrollOptions = {
                behavior: settings.behavior || 'smooth'
            };

            let finalLeft, finalTop;

            if (typeof targ === 'number' || typeof targ === 'string') {
                // Numeric or string-based target position
                let position = targ;

                if (typeof position === 'string' && /^([+-]=?)?\d+(\.\d+)?(px)?$/.test(position)) {
                    position = parseInt(position, 10);
                }

                if (settings.axis === 'x' || settings.axis === 'xy') {
                    finalLeft = position + settings.offset.left;
                }
                else {
                    finalLeft = $elem.scrollLeft();
                }

                if (settings.axis === 'y' || settings.axis === 'xy') {
                    finalTop = position + settings.offset.top;
                }
                else {
                    finalTop = $elem.scrollTop();
                }

            }
            else if (targ instanceof $ || targ.nodeType) {
                // If the target is a DOM element or a jQuery object
                const $targetElement = $(targ);

                if ($targetElement.length) {
                    const targetOffset = $targetElement.offset();

                    if (isWin(elem)) {
                        // Scrolling relative to the document
                        if (settings.axis === 'x' || settings.axis === 'xy') {
                            finalLeft = targetOffset.left + settings.offset.left - (settings.over.left * $targetElement.outerWidth());
                        }
                        else {
                            finalLeft = $elem.scrollLeft();
                        }

                        if (settings.axis === 'y' || settings.axis === 'xy') {
                            finalTop = targetOffset.top + settings.offset.top - (settings.over.top * $targetElement.outerHeight());
                        }
                        else {
                            finalTop = $elem.scrollTop();
                        }
                    }
                    else {
                        // Scrolling within an element
                        const elemOffset = $elem.offset();

                        if (settings.axis === 'x' || settings.axis === 'xy') {
                            finalLeft = targetOffset.left - elemOffset.left + $elem.scrollLeft() + settings.offset.left - (settings.over.left * $targetElement.outerWidth());
                        }
                        else {
                            finalLeft = $elem.scrollLeft();
                        }

                        if (settings.axis === 'y' || settings.axis === 'xy') {
                            finalTop = targetOffset.top - elemOffset.top + $elem.scrollTop() + settings.offset.top - (settings.over.top * $targetElement.outerHeight());
                        }
                        else {
                            finalTop = $elem.scrollTop();
                        }
                    }
                }
            }

            // Limit the scroll position if needed
            if (settings.limit) {
                const maxLeft = $scrollTo.max(elem, 'x');
                const maxTop = $scrollTo.max(elem, 'y');

                if (finalLeft != null) {
                    finalLeft = Math.max(0, Math.min(finalLeft, maxLeft));
                }

                if (finalTop != null) {
                    finalTop = Math.max(0, Math.min(finalTop, maxTop));
                }
            }

            // Set scroll options
            if (finalLeft != null) {
                scrollOptions.left = finalLeft;
            }

            if (finalTop != null) {
                scrollOptions.top = finalTop;
            }

            // Perform scrolling
            if (isWin(elem)) {
                window.scrollTo(scrollOptions);
            }
            else if (elem.scrollTo) {
                elem.scrollTo(scrollOptions);
            }
            else {
                if (scrollOptions.left != null) elem.scrollLeft = scrollOptions.left;
                if (scrollOptions.top != null) elem.scrollTop = scrollOptions.top;
            }
        });
    };

    $scrollTo.max = function (elem, axis) {
        const Dim = axis === 'x' ? 'Width' : 'Height',
            scroll = 'scroll' + Dim;

        if (elem === window || elem === document || elem === document.body || elem === document.documentElement) {
            const size = 'client' + Dim;
            const doc = document.documentElement;
            const body = document.body;

            return Math.max(doc[scroll], body[scroll]) - Math.min(doc[size], body[size]);
        }
        else {
            return elem[scroll] - elem['client' + Dim];
        }
    };

    function both(val) {
        return $.isFunction(val) || $.isPlainObject(val) ? val : { top: val, left: val };
    }

    return $scrollTo;
});
