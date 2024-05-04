; (function ($, window, document, undefined) {

    var pluginName = 'productDetail';
    var galPluginName = "smartGallery";

    function ProductDetail(element, options) {

        var self = this;

        this.element = element;
        var el = this.el = $(element);

        var meta = $.metadata ? $.metadata.get(element) : {};
        var opts = this.options = $.extend(true, {}, options, meta || {});
        var updating = false;

        this.init = function () {
            var opts = this.options;
            const associatedProducts = $('#associated-products');

            this.createGallery(opts.galleryStartIndex);

            $(el).on('click', '.stock-subscriber', function (e) {
                e.preventDefault();
                openPopup({ url: $(this).attr('href'), large: false, flex: false });
                return false;
            });

            $(el).on('keydown', '.qty-input .form-control, .choice-textbox', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    return false;
                }
            });

            // Update product data and gallery
            $(el).on('change', ':input:not(.skip-pd-ajax-update)', function (e) {
                if (updating) {
                    return;
                }

                var inputCtrl = $(this);
                var isNumberInput = inputCtrl.parent(".numberinput-group").length > 0;
                var isFileUpload = inputCtrl.data("fileupload");
                var isDateTime = inputCtrl.hasClass("date-part");
                var ctx = inputCtrl.closest('.update-container');

                if (ctx.length === 0) {
                    // It's an associated product or a bundle item.
                    ctx = el;
                }

                ctx.ajax({
                    data: ctx.find(':input').serialize(),
                    success: function (response) {
                        updating = true;
                        self.updateDetailData(response, ctx, isNumberInput, isFileUpload, isDateTime);

                        if (ctx.hasClass('pd-bundle-item')) {
                            // Update bundle price too.
                            $('#main-update-container').ajax({
                                data: $('.pd-bundle-items').find(':input').serialize(),
                                success: function (response2) {
                                    self.updateDetailData(response2, $('#main-update-container'), isNumberInput, isFileUpload, isDateTime);
                                }
                            });
                        }
                        updating = false;
                    }
                });
            });

            self.initAssociatedProducts(associatedProducts);

            return this;
        };

        this.initAssociatedProducts = function (associatedProducts) {
            if (!associatedProducts.length || !associatedProducts.find('.pd-assoc-list').length) {
                // No associated products nor collapsible. Nothing to init.
                return;
            }

            var elError = null;

            associatedProducts.on('click', '.pd-assoc-header', function (e) {
                // Collapse/expand body if the header was clicked (excluding controls with 'pd-interaction').
                if (!$(e.target).closest('.pd-interaction').length) {
                    $($(this).data('target')).collapse('toggle');
                }
            }).on('show.bs.collapse shown.bs.collapse hide.bs.collapse', function (e) {
                if (e.type === 'shown') {
                    if (elError !== null) {
                        scrollToCard(elError);
                        elError = null;
                    }
                }
                else {
                    // Toggle 'collapsed' class to display correct chevron.
                    $(e.target).prev().toggleClass('collapsed', e.type === 'hide');
                }
            });

            EventBroker.subscribe('ajaxcart.error', function (msg, data) {
                // Expand item to let the user select attributes.
                var el = $('#associated-product' + data.response.productId);
                if (el.hasClass('show')) {
                    scrollToCard(el);
                }
                else {
                    elError = el.collapse('show');
                }
            });

            function scrollToCard(el) {
                $('body, html').animate({ scrollTop: el.closest('.pd-assoc').offset().top }, 'slow');
            }
        };

        this.updateDetailData = function (data, ctx, isNumberInput, isFileUpload, isDateTime) {
            var gallery = $('#pd-gallery').data(galPluginName);

            // Image gallery needs special treatment.
            if (!isFileUpload) {
                if (data.GalleryHtml) {
                    var cnt = $('#pd-gallery-container');
                    gallery.reset();
                    cnt.html(data.GalleryHtml);
                    self.createGallery(data.GalleryStartIndex);
                }
                else if (data.GalleryStartIndex >= 0) {
                    if (data.GalleryStartIndex !== gallery.currentIndex) {
                        gallery.goTo(data.GalleryStartIndex);
                    }
                }
            }

            ctx.find('[data-partial]').each(function (i, el) {
                // Iterate all elements with [data-partial] attribute.
                var $el = $(el);
                var partial = $el.data('partial');

                if (partial && !(isNumberInput && partial === 'OfferActions') && !(isDateTime && partial === 'Variants')) {
                    // ...fetch the updated html from the corresponding AJAX result object's properties
                    if (data.Partials && data.Partials.hasOwnProperty(partial)) {
                        if (partial === 'Variants' || partial === 'BundleItemVariants') {
                            $el.find('[data-toggle=tooltip], .tooltip-toggle').tooltip('hide');
                        }

                        var updatedHtml = data.Partials[partial] || "";
                        // ...and update the inner html
                        $el.html($(updatedHtml.trim()));
                    }
                }
            });

            applyCommonPlugins(ctx);

            ctx.find(".pd-tierprices").html(data.Partials["TierPrices"]);

            if (data.DynamicThumblUrl && data.DynamicThumblUrl.length > 0) {
                $(ctx).find('.pd-dyn-thumb').attr('src', data.DynamicThumblUrl);
            }

            // Trigger event for plugins devs to subscribe.
            $('#main-update-container').trigger("updated");
        };

        this.initialized = false;
        this.init();
        this.initialized = true;
    }

    ProductDetail.prototype = {
        gallery: null,
        activePictureIndex: 0,

        createGallery: function (startIndex) {
            var self = this;
            var opts = this.options;

            this.gallery = $('#pd-gallery').smartGallery({
                startIndex: startIndex || 0,
                zoom: {
                    enabled: opts.enableZoom
                },
                box: {
                    enabled: true,
                    hidePageScrollbars: false
                }
            });
        }
    };

    // the global, default plugin options
    _.provide('$.' + pluginName);

    $[pluginName].defaults = {
        // The 0-based image index to start the gallery with
        galleryStartIndex: 0,
        // whether to enable image zoom
        enableZoom: true,
        // url to the ajax method, which loads variant combination data
        updateUrl: null,
    };

    $.fn[pluginName] = function (options) {

        return this.each(function () {
            if (!$.data(this, 'plugin_' + pluginName)) {
                options = $.extend(true, {}, $[pluginName].defaults, options);
                $.data(this, 'plugin_' + pluginName, new ProductDetail(this, options));
            }
        });
    };

})(jQuery, window, document);