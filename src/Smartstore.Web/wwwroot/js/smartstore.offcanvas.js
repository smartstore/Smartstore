/*
*  Project: OffCanvas SideBar
*  Author: Murat Cakir, SmartStore AG
*/

; (function ($, window, document, undefined) {

    var viewport = ResponsiveBootstrapToolkit;
    let breakpoints = ['xxl', 'xl', 'lg', 'md', 'sm', 'xs'];

    // OFFCANVAS PUBLIC CLASS DEFINITION
    // ======================================================

    var OffCanvas = function (element, opts) {
        let self = this;
        
        this.el = $(element);
        this.options = opts;
        this.canvas = $(this.options.canvas || '.wrapper');
        this.state = null;

        // Normalize placement option
        let placement = this.options.placement || { xs: "start" };
        if (_.isString(placement)) {
            placement = placement.trim();
            if (placement[0] == '{') {
                placement = JSON.parse(placement);
            }
            else {
                placement = { xs: placement };
            }
        }

        // Reassign normalized placement option
        this.options.placement = placement;

        // Remove accidentally set placement classes.
        // Adding the .no-anims class is very important here, 
        // otherwise the offcanvas will take time to reposition because of the transition
        this.el.addClass('no-anims').removeClass('offcanvas-start offcanvas-end offcanvas-top offcanvas-bottom');

        // Determine and remember current placement
        this._refreshPlacement();

        if (this.options.fullscreen) {
            this.el.addClass('offcanvas-fullscreen');
        }

        if (this.options.lg) {
            this.el.addClass('offcanvas-lg');
        }

        if (_.isBoolean(this.options.disablescrolling)) {
            this.options.disableScrolling = this.options.disablescrolling;
            delete this.options.disablescrolling;
        }

        if (this.options.hideonresize) {
            var _hide = _.throttle(function () {
                self.hide();
            }, 250);
            $(window).on('resize', _hide); 
        }
        else {
            EventBroker.subscribe("page.resized", function (msg, viewport) {
                if (viewport.is('>sm')) {
                    self.hide();
                }

                // Change current placement
                self._refreshPlacement();
            });
        }

        if (this.options.autohide) {
            $('body, .canvas-blocker').on('click', $.proxy(this.autohide, this));
        }
                
        $(window).on('popstate', function (event) {
            if (event.originalEvent.state) {
                if (event.originalEvent.state.offcanvasHide) {
                    // Hide offcanvas menu/cart when back button is pressed
                    self.hide();
                }
                else if (event.originalEvent.state.offcanvasShow) {
                    // Show offcanvas menu/cart when forward button is pressed
                    self.show();
                }                
            }
        });

        requestAnimationFrame(() => {
            // Animations are allowed from here on
            this.el.removeClass("no-anims");

            // Toggle
            if (this.options.toggle) {
                this.toggle();
            }

            // Set up events to properly handle (touch) gestures
            this._makeTouchy();
        });
    }


    // OFFCANVAS DEFAULT OPTIONS
    // ======================================================

    OffCanvas.defaults = {
        canvas: '.wrapper',
        toggle: true,
        placement: { xs: 'start' },
        fullscreen: false,
        overlay: false,
        autohide: true,
        hideonresize: false,
        disableScrolling: false,
        blocker: true
    };


    // OFFCANVAS internal
    // ======================================================

    OffCanvas.prototype._refreshPlacement = function () {
        let placement = this._getPlacement();

        if (placement == this.currentPlacement) {
            // Nothing to change
            return;
        }

        if (this.currentPlacement) {
            // Remove previous placement class if any
            this.el.removeClass('offcanvas-' + this.currentPlacement);
        }

        this.currentPlacement = placement;
        this.el.addClass('offcanvas-' + placement);
    }

    OffCanvas.prototype._getPlacement = function () {
        let currentBreakpoint = viewport.current();
        let index = breakpoints.indexOf(currentBreakpoint);
        let placement;

        while (!placement) {
            if (index >= breakpoints.length) {
                break;
            }

            placement = this.options.placement[breakpoints[index]];
            if (placement) {
                break;
            }

            index++;
        }

        if (!placement) {
            // Fix lowest tier
            this.options.placement.xs = placement = 'start';
        }

        return placement;
    }

    OffCanvas.prototype._makeTouchy = function (fn) {
        let self = this;
        let el = this.el;

        // Move offcanvas on pan[left|right|top|bottom] and close on swipe
        let panDir = '', // Always resolve on tapstart
            panning = false,
            scrolling = false,
            nodeScrollable = null;

        function getPanDirection() {
            let rtl = Smartstore.globalization.culture.isRTL;
            switch (self.currentPlacement) {
                case 'top': return 'up';
                case 'bottom': return 'down';
                case 'start': return (rtl ? 'right' : 'left');
                case 'end': return (rtl ? 'left' : 'right');
            }
        }

        function getDelta(g) {
            switch (panDir) {
                case 'left': return Math.min(0, g.delta.x);
                case 'right': return Math.max(0, g.delta.x);
                case 'up': return Math.min(0, g.delta.y);
                case 'down': return Math.max(0, g.delta.y);
            }
        }

        function isScrolling(e, g) {
            if (nodeScrollable === null || nodeScrollable.length === 0) {
                return false;
            }

            let scrollDeltaOnTapStart = nodeScrollable.data('initial-scroll-top');
            let currentScrollTop = nodeScrollable.scrollTop();

            if (!_.isNumber(scrollDeltaOnTapStart)) {
                return false;
            }  
            
            return currentScrollTop != scrollDeltaOnTapStart;
        }

        function handleMove(e, g) {
            // when inner scrolling started, do NOT attempt to pan.
            if (scrolling || (scrolling = isScrolling(e, g))) {
                return;
            }
            
            var delta = getDelta(g);
            panning = !scrolling && delta != 0;

            if (panning) {
                // prevent scrolling during panning
                e.preventDefault();

                const transformName = (panDir == 'left' || panDir == 'right') ? 'translateX' : 'translateY';
                $(e.currentTarget).css('transform', transformName + '(' + delta + 'px)');
            }
            else {
                if (nodeScrollable !== null && nodeScrollable.length > 0) {
                    if (nodeScrollable.height() >= nodeScrollable[0].scrollHeight) {
                        // Content is NOT scrollable. Don't let iOS Safari scroll the body.
                        e.preventDefault();
                    }
                }
                else {
                    // Touch occurs outside of any scrollable element. Again: prevent body scrolling.
                    e.preventDefault();
                }
            }
        }

        el.on('tapstart.offcanvas', function (e, gesture) {
            panDir = getPanDirection();

            // Special handling for horizontally scrollable stacks
            let hstack = $(e.target).closest('.offcanvas-hstack');
            if (hstack.length > 0) {
                // Let hstack scroll, don't move offcanvas.
                scrolling = true;
                return;
            }

            // Special handling for tabs
            var tabs = $(e.target).closest('.offcanvas-tabs');
            if (tabs.length > 0) {
                var tabsWidth = 0;
                var cntWidth = el.width();
                tabs.find('.nav-item').each(function () { tabsWidth += $(this).width(); });
                if (tabsWidth > cntWidth) {
                    // Header tabs width exceed offcanvas width. Let it scroll, don't move offcanvas.
                    scrolling = true;
                    return;
                }
            }

            nodeScrollable = $(e.target).closest('.offcanvas-scrollable');
            if (nodeScrollable.length > 0) {
                if (panDir == 'up' || panDir == 'down') {
                    // Better not to mess around with deltas if offcanvas is vertical.
                    // Just don't allow swipe close in this case.
                    scrolling = true;
                    return;
                }

                nodeScrollable.data('initial-scroll-top', nodeScrollable.scrollTop());
            }

            $(".select2-hidden-accessible", el).select2("close");

            el.css('transition', 'none');
            el.on('tapmove.offcanvas', handleMove);
        });

        el.on('tapend.offcanvas', function (e, gesture) {
            el.off('tapmove.offcanvas')
                .css('transform', '')
                .css('transition', '');

            if (!scrolling && Math.abs(getDelta(gesture)) >= 100) {
                self.hide();
            }

            nodeScrollable = null;
            panning = false;
            scrolling = false;
        });
    }


    // OFFCANVAS METHODS
    // ======================================================

    OffCanvas.prototype.show = function (fn) {
        if (this.state) return;

        let body = $('body');
        let self = this;

        var startEvent = $.Event('show.sm.offcanvas');
        this.el.trigger(startEvent);
        if (startEvent.isDefaultPrevented()) return;

        this.state = 'slide-in';

        if (this.options.blocker) {
            body.addClass('canvas-blocking');
        }

        if (this.options.disableScrolling) {
            body.addClass('canvas-noscroll');
        }

        if (this.options.overlay) {
            body.addClass('canvas-overlay');
        }

        body.one("tapend", "[data-dismiss=offcanvas]", function (e) {
            e.preventDefault();
            self.hide();
        });

        body.addClass('canvas-sliding canvas-sliding-'
            + (this.currentPlacement)
            + (this.options.fullscreen ? ' canvas-fullscreen' : ''));

        this.el.addClass("show").one(Prefixer.event.transitionEnd, function (e) {
            if (self.state !== 'slide-in') return;
            body.addClass('canvas-slid');
            self.state = 'slid';
            self.el.trigger('shown.sm.offcanvas');
        });
        
        // Add history states for offcanvas browser navigation button handling (back / forward)
        if (!history.state || !history.state.offcanvasShow) {
            history.replaceState({ offcanvasHide: true }, "offcanvas");
            history.pushState({ offcanvasShow: true }, "offcanvas");
        }
    };

    OffCanvas.prototype.hide = function (fn) {
        if (this.state !== 'slid') return;

        var self = this;
        var body = $('body');

        $(".select2-hidden-accessible", this.el).select2("close");

        var startEvent = $.Event('hide.sm.offcanvas');
        this.el.trigger(startEvent);
        if (startEvent.isDefaultPrevented()) return;

        self.state = 'slide-out';

        body.addClass('canvas-sliding-out');
        body.removeClass('canvas-blocking canvas-noscroll canvas-slid canvas-sliding canvas-sliding-start canvas-sliding-end canvas-sliding-top canvas-sliding-bottom canvas-lg canvas-fullscreen');

        this.el.removeClass("show").one(Prefixer.event.transitionEnd, function (e) {
            if (self.state !== 'slide-out') return;

            body.removeClass('canvas-sliding-out');
            self.state = null;
            self.el.trigger('hidden.sm.offcanvas');
        });

        // If current state is offcanvasShow, pop current history backwards, 
        // so that the history state (offcanvasHide) matches with offcanvas display (hide)
        if (history.state && history.state.offcanvasShow) {
            history.back();
        }
    };

    OffCanvas.prototype.toggle = function (fn) {
        if (this.state === 'slide-in' || this.state === 'slide-out') return;
        this[this.state === 'slid' ? 'hide' : 'show']();
    };

    OffCanvas.prototype.autohide = function (e) {
        var target = $(e.target);
        if (target.closest(this.el).length === 0 && !target.hasClass("select2-results__option"))
            this.hide();
    };


    // OFFCANVAS PLUGIN DEFINITION
    // ======================================================

    $.fn.offcanvas = function (option) {
        return this.each(function () {
            let self = $(this),
                data = self.data('sm.offcanvas'),
                options = $.extend({}, OffCanvas.defaults, self.data(), typeof option === 'object' && option);

            if (!data) self.data('sm.offcanvas', (data = new OffCanvas(this, options)));
            if (typeof option === 'string') data[option]();
        })
    }

    $.fn.offcanvas.Constructor = OffCanvas;


    // OFFCANVAS DATA API
    // ======================================================

    $(document).on('click.sm.offcanvas.data-api', '[data-toggle=offcanvas]', function (e) {
        let self = $(this);
        let target = self.data('target') || self.attr('href');
        let $canvas = $(target);
        let data = $canvas.data('sm.offcanvas');
        let options = data ? 'toggle' : self.data();

        e.stopPropagation();
        e.preventDefault();

        if (data) {
            data.toggle();
        }
        else {
            $canvas.offcanvas(options);
        }

        return false;
    })

    $('.canvas-blocker').on('touchmove', function (e) {
        e.preventDefault();
    });
})(jQuery, window, document);
