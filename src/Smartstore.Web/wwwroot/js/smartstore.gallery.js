/*
 *  Project: smartstore.smartgallery
 *  Version: 2.0
 *  Author: Murat Cakir
 */

; (function ($, window, document, undefined) {

    var pluginName = 'smartGallery';
    var isTouch = window.touchable;

    var defaultZoomOpts = {
        // Prefix for generated element class names (e.g. `my-ns` will
        // result in classes such as `my-ns-pane`. Default `drift-`
        // prefixed classes will always be added as well.
        namespace: null,
        // Whether the ZoomPane should show whitespace when near the edges.
        showWhitespaceAtEdges: false,
        // Whether the inline ZoomPane should stay inside
        // the bounds of its image.
        containInline: true,
        // How much to offset the ZoomPane from the
        // interaction point when inline.
        inlineOffsetX: 0,
        inlineOffsetY: 0,
        // A DOM element to append the inline ZoomPane to.
        inlineContainer: document.body,
        // Which trigger attribute to pull the ZoomPane image source from.
        sourceAttribute: 'data-zoom',
        // How much to magnify the trigger by in the ZoomPane.
        // (e.g., `zoomFactor: 3` will result in a 900 px wide ZoomPane image
        // if the trigger is displayed at 300 px wide)
        zoomFactor: 3,
        // A DOM element to append the non-inline ZoomPane to.
        // Required if `inlinePane !== true`.
        paneContainer: document.body,
        // When to switch to an inline ZoomPane. This can be a boolean or
        // an integer. If `true`, the ZoomPane will always be inline,
        // if `false`, it will switch to inline when `windowWidth <= inlinePane`
        inlinePane: 768,
        // If `true`, touch events will trigger the zoom, like mouse events.
        handleTouch: true,
        // If present (and a function), this will be called
        // whenever the ZoomPane is shown.
        onShow: null,
        // If present (and a function), this will be called
        // whenever the ZoomPane is hidden.
        onHide: null,
        // Add base styles to the page.
        injectBaseStyles: true,
        // An optional number that determines how long to wait before
        // showing the ZoomPane because of a `mouseenter` event.
        hoverDelay: 150,
        // An optional number that determines how long to wait before
        // showing the ZoomPane because of a `touchstart` event.
        // It's unlikely that you would want to use this option, since
        // "tap and hold" is much more intentional than a hover event.
        touchDelay: 0,
        // If true, a bounding box will show the area currently being previewed
        // during mouse hover
        hoverBoundingBox: true,
        // If true, a bounding box will show the area currently being previewed
        // during touch events
        touchBoundingBox: true
    };

    function SmartGallery(element, options) {
        var self = this;

        this.element = element;
        var el = this.el = $(element);

        var meta = $.metadata ? $.metadata.get(element) : {};
        var opts = this.options = $.extend(true, {}, options, meta || {});

        this.init = function () {
            var self = this;

            var startAt = parseInt(opts.startIndex, 10);
            if (window.location.hash && window.location.hash.indexOf('#sg-image') === 0) {
                startAt = window.location.hash.replace(/[^0-9]+/g, '');
                if (_.isNumber(startAt)) {
                    opts.startIndex = startAt;
                }
            }

            this.zoomWindowContainer = $('.zoom-window-container');

            this.initNav();
            this.initGallery();

            if ($.isPlainObject(opts.zoom) && opts.zoom.enabled === true) {
                this.initZoom();
            }

            if ($.isPlainObject(opts.box) && opts.box.enabled) {
                this.initBox();
            }
        };

        this.initialized = false;
        this.init();
        this.initialized = true;
    }

    SmartGallery.prototype = {
        gallery: null,
        nav: null,
        navList: null,
        navTrack: null,
        navPrevArrow: null,
        navNextArrow: null,
        navItemsCount: 0,
        zoomWindowContainer: null,
        currentIndex: 0,
        currentImage: null,
        lightbox: null,

        initGallery: function () {
            var self = this;
            var gal = self.el.find(".gal");
            if (gal.length === 0) {
                return;
            }

            self.gallery = gal;

            var options = {
                infinite: false,
                lazyLoad: "ondemand",
                dots: true,
                arrows: false,
                //prevArrow: '<button type="button" class="btn btn-secondary btn-flat btn-icon rounded-circle slick-prev"><sm-icon name="chevron-left" ></sm-icon></button>',
                //nextArrow: '<button type="button" class="btn btn-secondary btn-flat btn-icon rounded-circle slick-next"><sm-icon name="chevron-right" ></sm-icon></button>',
                cssEase: 'ease-in-out',
                speed: isTouch ? 250 : 0,
                useCSS: true,
                useTransform: true,
                waitForAnimate: true,
                slidesToShow: 1,
                slidesToScroll: 1,
                initialSlide: self.options.startIndex
            };

            self.currentIndex = self.options.startIndex;
            self.currentImage = gal.find('.gal-item').eq(self.options.startIndex).first();

            gal.slick(options);

            // Recalculate sizes when the gallery changes width, including container-only changes.
            let galleryWidth;
            const resizeObserver = new ResizeObserver(([entry]) => {
                if (!gal[0].isConnected) {
                    resizeObserver.disconnect();
                    return;
                }

                const width = entry.contentRect.width;
                if (!width || width === galleryWidth) return;
                galleryWidth = width;

                requestAnimationFrame(() => {
                    if (!gal[0].isConnected) return;
                    gal.slick('setPosition');
                    self.initNav();
                });
            });
            resizeObserver.observe(gal[0]);
            gal.one('destroy.gal', () => resizeObserver.disconnect());

            gal.on('beforeChange', (e, slick, curIdx, nextIdx) => {
                if (!self.nav.data('glimpse')) {
                    // Sync with thumb nav
                    self._selectNavItem(nextIdx, false);
                }
            });

            gal.on('afterChange', (e, slick, currentSlide) => {
                self.currentIndex = currentSlide;
                self.currentImage = gal.find('.gal-item.slick-current img').first();
            });

            if (!isTouch) {
                gal
                    .on('mouseenter.gal', () => { gal.slick("slickSetOption", "speed", 250); })
                    .on('mouseleave.gal', () => { gal.slick("slickSetOption", "speed", 0); });
            }
        },

        initNav: function () {
            var self = this;
            var nav = self.nav = self.nav || self.el.find('.gal-nav');
            if (nav.length === 0)
                return;

            var isInitialized = nav.hasClass('gal-initialized');

            var list = self.navList = nav.find('.gal-list').first(),
                track = self.navTrack = list.find('.gal-track').first(),
                items = list.find('.gal-item');

            if (items.length > self.options.thumbsToShow && nav.is(':visible')) {
                list.height(items.first().outerHeight(true) * self.options.thumbsToShow);
            }

            // Subsequent calls only refresh geometry; keep selection and handlers intact.
            if (isInitialized) {
                list.scrollTop(list.height() * (nav.data('current-page') || 0));
                return;
            }

            self.navItemsCount = items.length;

            items.each(function (i) {
                var $el = $(this);
                $el.attr('data-gal-index', i);
            });

            // Lazy load thumbnails for video files.
            Smartstore.media.lazyLoadThumbnails(list);

            if (items.length > self.options.thumbsToShow) {
                if (!isInitialized) {
                    const cssClass = 'btn btn-secondary btn-no-border btn-icon rounded-circle btn-sm gal-arrow gal-disabled';
                    self.navPrevArrow = $(`<button type="button" class="${cssClass} gal-prev" aria-label="${Res['Common.MoveUp']}" aria-disabled="true"><sm-icon name="chevron-up"  style="vertical-align: top" aria-hidden="true"></sm-icon></button>`).prependTo(nav);
                    self.navNextArrow = $(`<button type="button" class="${cssClass} gal-next" aria-label="${Res['Common.MoveDown']}" aria-disabled="true"><sm-icon name="chevron-down"  aria-hidden="true"></sm-icon></button>`).appendTo(nav);
                }

                list.on('scroll', function (e) {
                    // We only handle native tab key scrolling here
                    if (list.data('sliding')) return;

                    e.preventDefault();

                    // Get the index of the currently focused thumb item
                    const activeItemIndex = parseInt($(document.activeElement).parent().data('gal-index'));
                    if (isNaN(activeItemIndex)) {
                        return;
                    }

                    // Find the page of the focused item...
                    let attemptedPage = self._findPageByItemIndex(activeItemIndex);

                    // ...and slide to target page to stay in sync
                    self._slideToNavPage(attemptedPage);

                    return false;
                });

                nav.on('click.gal', '.gal-arrow', function (e) {
                    e.preventDefault();
                    const btn = $(this);

                    if (btn.hasClass('gal-disabled')) {
                        return;
                    }
                    else if (btn.hasClass('gal-prev')) {
                        self._slideToPrevNavPage();
                    }
                    else if (btn.hasClass('gal-next')) {
                        self._slideToNextNavPage();
                    }

                    return false;
                });
            }

            self._selectNavItem(self.options.startIndex, false);

            nav.on('mouseenter.gal click.gal', '.gal-item-viewport', function (e) {
                e.preventDefault();

                if (e.type === "mouseenter") {
                    nav.data("glimpse", true);
                }

                var toIdx = $(this.parentElement).data('gal-index');
                self.goTo(toIdx);

                if (e.type === "click") {
                    // sync with gallery
                    nav.data("glimpse", false);
                    self._selectNavItem(toIdx, true);
                }

                return false;
            })
            .on('mouseleave.gal', function () {
                // Restore actual selected image
                var actualIdx = nav.find('.gal-current').data('gal-index');
                self.goTo(actualIdx);
                nav.data("glimpse", false);
            });

            nav.addClass("gal-initialized");
        },

        _findPageByItemIndex: function (idx) {
            return Math.floor(idx / this.options.thumbsToShow);
        },

        _selectNavItem: function (idx, sync) {
            let self = this;
            let curItem = self.nav.find('.gal-current');
            let curIdx = curItem.data('gal-index');
            if (curIdx === idx)
                return;

            curItem.removeClass('gal-current').find("> a").aria('selected', false);
            curItem = self.nav.find('[data-gal-index=' + idx + ']');
            curItem.addClass('gal-current').find("> a").aria('selected', true);

            let page = self._findPageByItemIndex(idx);
            self._slideToNavPage(page);

            if (sync) {
                self.goTo(idx);
            }
        },

        _slideToPrevNavPage: function () {
            const curPage = this.nav.data('current-page');
            this._slideToNavPage(curPage - 1);
        },

        _slideToNextNavPage: function () {
            const curPage = this.nav.data('current-page');
            this._slideToNavPage(curPage + 1);
        },

        _slideToNavPage: function (page) {
            if (this.nav.data('current-page') !== page) {
                this.nav.data('current-page', page);

                const hasArrows = !!(this.navPrevArrow) && !!(this.navNextArrow);
                if (page === 0 && hasArrows) {
                    this.navPrevArrow.addClass('gal-disabled').aria('disabled', true);
                    this.navNextArrow.removeClass('gal-disabled').aria('disabled', false);
                }
                else if (page > 0 && hasArrows) {
                    this.navPrevArrow.removeClass('gal-disabled').aria('disabled', false);
                    var totalPages = Math.ceil(this.navItemsCount / this.options.thumbsToShow);
                    var isLastPage = page >= totalPages - 1;
                    this.navNextArrow.toggleClass('gal-disabled', isLastPage).aria('disabled', isLastPage);
                }

                const navListHeight = this.navList.height();
                const maxOffsetY = (this.navTrack.height() - navListHeight);
                const offsetY = navListHeight * page;

                const scrollTop = Math.min(offsetY, maxOffsetY);
                this.navList.data('sliding', true);
                this.navList[0].scrollTo({ top: scrollTop, behavior: 'smooth' });
                setTimeout(() => this.navList.data('sliding', false), 1000);
            }
        },

        initZoom: function () {
            if (isTouch)
                return; // no zoom on touch devices

            var self = this;

            self.gallery.on('beforeChange.gal', (e, slick, curIdx, nextIdx) => {
                // destroy zoom
                if (self.nav.data('glimpse')) return;
                self.destroyZoom(curIdx);
            });

            self.gallery.on('afterChange.gal', (e, slick, idx) => {
                // apply zoom
                if (self.nav.data('glimpse')) return;
                applyZoom(self.gallery.find('.gal-item').eq(idx));
            });

            function applyZoom(slide) {
                var a = slide.find('> a');
                var img = slide.find('img');

                var zoomOpts = $.extend({}, defaultZoomOpts, self.options.zoom);

                if (img.data("drift") || !img.attr(zoomOpts.sourceAttribute) || self.zoomWindowContainer.length === 0)
                    return;

                var triggerW = img.width();
                var zoomW = img.data("zoom-width");
                if (_.isNumber(zoomW) && zoomW > 0) {
                    if (zoomW <= triggerW) {
                        // Cannot zoom smaller or equal sized image
                        return;
                    }
                    else {
                        // set correct zoomFactor
                        zoomOpts.zoomFactor = zoomW / triggerW;
                    }
                }

                zoomOpts = $.extend(zoomOpts, {
                    paneContainer: self.zoomWindowContainer[0],
                    onShow: function () {
                        _.delay(function () {
                            if (self.zoomWindowContainer && self.zoomWindowContainer.length) {
                                self.zoomWindowContainer.find('.drift-zoom-pane').height(a.outerHeight());
                            }
                        }, 10);

                        // Fix Drift issue: boundingBox parent must be body, NOT image's parent link/viewport
                        drift.trigger.boundingBox.settings.containerEl = document.body;

                        // We must clear title attribute when displaying zoom else a tooltip will appear 
                        // which will result in flickering of the zoom panel when hovering over the tooltip.
                        img.attr("title", "");
                    }
                });

                var drift = new Drift(img[0], zoomOpts);
                img.data('drift', drift);
            }

            // Apply on first init
            var curIndex = self.gallery.slick('slickCurrentSlide');
            applyZoom(self.gallery.find('.gal-item').eq(curIndex));
        },

        destroyZoom: function (currentIndex) {
            currentIndex = currentIndex || this.currentIndex;
            var img = this.gallery.find('.gal-item').eq(currentIndex).find('img');
            var drift = img.data("drift");
            if (drift) {
                drift.disable();
                img.data("drift", null);
            }
        },

        reset: function () {
            this.lightbox?.destroy();
            this.nav.removeClass('gal-initialized');

            if (this.gallery) {
                this.gallery.off('.gal');
                this.destroyZoom();
                this.gallery.slick('unslick');
            }

            if (this.nav) {
                this.nav.off('.gal');
                this.nav.data('current-page', null);
                this.nav.find('.gal-item').removeClass('gal-current').removeAttr('data-gal-index').removeAttr('aria-selected');
            }

            if (this.navPrevArrow) this.navPrevArrow.remove();
            if (this.navNextArrow) this.navNextArrow.remove();

            this.gallery = null;
            this.nav = null;
            this.navList = null;
            this.navTrack = null;
            this.navPrevArrow = null;
            this.navNextArrow = null;
            this.navItemsCount = 0;
            this.zoomWindowContainer = null;
            this.currentIndex = 0;
            this.currentImage = null;
            this.lightbox = null;
        },

        initBox: function () {
            var self = this;
            if (!self.gallery || !self.nav) return;

            self.gallery.on('click.gal', '.gal-item > a', function (e) {
                e.preventDefault();
                if (document.body.classList.contains('search-focused')) return;

                const root = document.getElementById('lightbox');
                if (!root || !window.Smartstore?.Lightbox) return;
                const items = [];
                const galleryIndices = [];
                self.nav.find('.gal-item > a').each((index, link) => {
                    const a = $(link);
                    const image = a.data('type') === 'image';
                    const width = a.data('width');
                    const height = a.data('height');
                    if (image && (!width || !height)) return;
                    galleryIndices.push(index);
                    items.push({
                        type: image ? 'image' : 'video',
                        src: link.href,
                        msrc: a.data('medium-image'),
                        w: width,
                        h: height,
                        alt: link.querySelector('img')?.alt || ''
                    });
                });
                if (!items.length) return;

                const options = {
                    index: Math.max(0, galleryIndices.indexOf(self.currentIndex)),
                    legacy: self.options.box.legacy,
                    ..._.pick(self.options.box, 'loop', 'history', 'closeOnScroll'),
                    getThumbnailRect: (index) => {
                        const item = self.gallery?.find('.gal-item').eq(galleryIndices[index])[0];
                        if (!item?.isConnected) return null;
                        const rect = item.getBoundingClientRect();
                        return { x: rect.left, y: rect.top + window.scrollY, w: rect.width };
                    },
                    onChange: (index) => {
                        const galleryIndex = galleryIndices[index];
                        if (self.gallery && galleryIndex !== self.currentIndex) self.goTo(galleryIndex);
                    },
                    onClose: () => { if (self.lightbox === lightbox) self.lightbox = null; }
                };
                const lightbox = new Smartstore.Lightbox(root, items, options);
                self.lightbox = lightbox;
                lightbox.open();
            });
        },

        goTo: function (index) {
            this.gallery.slick('slickGoTo', index);
        },

        next: function () {
            return this.gallery.slick('slickNext');
        },

        prev: function () {
            return this.gallery.slick('slickPrev');
        },

        fireCallback: function (fn) {
            if (_.isFunction(fn)) {
                return fn.call(this);
            };
        }

    }; // SmartGallery.prototype


    // the global, default plugin options
    _.provide('$.' + pluginName);
    $[pluginName].defaults = {
        thumbsToShow: 6,
        // 0-based index of image to start with
        startIndex: 0,
        // zoom options
        zoom: {
            enabled: true,
            /* {...} 'Drift' options are passed through */
        },
        // full size image box options
        box: {
            enabled: true,
            // Set legacy: true to test the retained PhotoSwipe implementation.
        },
        callbacks: {
            imageClick: null,
            thumbClick: null
        }
    };

    $.fn[pluginName] = function (options) {
        return this.each(function () {
            if (!$.data(this, pluginName)) {
                options = $.extend(true, {}, $[pluginName].defaults, options);
                $.data(this, pluginName, new SmartGallery(this, options));
            }
        });
    };

})(jQuery, window, document);
