/// <reference path="admin.common.js" />

(function ($, window, document, undefined) {

    var _commonPluginFactories = [
        // panel toggling
        function (ctx) {
            ctx.find('input[type=checkbox][data-toggler-for]').each(function (i, el) {
                Smartstore.Admin.togglePanel(el, false);
            });
        },
        // select2 (generic)
        function (ctx) {
            ctx.find("select:not(.noskin)").selectWrapper();
        },
        // tooltips
        function (ctx) {
            ctx.find(".cph").tooltip({
                selector: "a.hint",
                boundary: 'window',
                placement: Smartstore.globalization.culture.isRTL ? "right" : "left",
                trigger: 'hover',
                delay: { show: 400, hide: 0 }
            });
        },
        // switch
        function (ctx) {
            ctx.find(".adminData > input[type=checkbox], .multi-store-setting-control > input[type=checkbox], .switcher > input[type=checkbox]").each(function (i, el) {
                $(el)
                    .wrap('<label class="switch"></label>')
                    .after('<span class="switch-toggle" data-on="' + window.Res['Common.On'] + '" data-off="' + window.Res['Common.Off'] + '"></span>');
            });
        },
        // btn-trigger
        function (ctx) {
            // Temp only: delegates anchor clicks to corresponding form-button.
            ctx.find("a[rel='btn-trigger']").click(function () {
                var el = $(this);
                var target = el.data("target");
                var action = el.data("action");
                var button = el.closest("form").find("button[type=submit][name=" + target + "][value=" + action + "]");
                button.click();
                return false;
            });
        },
        //// Lazy summernote
        //function (ctx) {
        //    ctx.find(".html-editor-root").each(function (i, el) {
        //        var $el = $(el);
        //        summernote_image_upload_url = $el.data("summernote-image-upload-url");

        //        if (!$el.data("lazy")) {
        //            $el.find("> .summernote-editor").summernote($.extend(true, {}, summernote_global_config, { lang: $el.data("lang") }));
        //        }
        //    });
        //},
        // Edit controls (select2, range, colorpicker, numberinput etc.)
        function (ctx) {
            initializeEditControls(ctx);
        }
    ];

	/* 
	Helpful in AJAX scenarios, where jQuery plugins has to be applied 
	to newly created html.
	*/
    window.applyCommonPlugins = function (/* jQuery */ context) {
        $.each(_commonPluginFactories, function (i, val) {
            val.call(this, $(context));
        });
    };

    window.providerListInit = function (context) {
        $(context || "body").on("click", ".activate-provider", function (e) {
            e.preventDefault();

            var $el = $(this);
            var activate = $el.attr("data-activate") == "true" ? true : false;
            var T = window.Res.Provider;

            $({}).ajax({
                url: $el.data('href'),
                data: {
                    "systemName": $el.attr("data-systemname"),
                    "activate": activate
                },
                success: function () {
                    var item = $el.closest(".module-item");
                    var badge = item.find(".badge");

                    item.toggleClass("inactive", !activate);

                    if (activate) {
                        $el.addClass("btn-secondary btn-to-danger").removeClass("btn-success");
                        $el.text(T.deactivate);
                        badge.text(T.active);
                        badge.addClass("badge-success").removeClass("badge-secondary");
                    }
                    else {
                        $el.addClass("btn-success").removeClass("btn-secondary btn-to-danger");
                        $el.text(T.activate);
                        badge.text(T.inactive);
                        badge.addClass("badge-secondary").removeClass("badge-success");
                    }

                    $el.attr("data-activate", !activate);
                }
            });

            return false;
        })
    }

    $(document).ready(function () {
        var html = $("html");

        html.removeClass("not-ready").addClass("ready");

        applyCommonPlugins($("body"));

        // Handle panel toggling
        $(document).on('change', 'input[type=checkbox][data-toggler-for]', function (e) {
            Smartstore.Admin.togglePanel(e.target, true);
        });

        // Tooltips
        $("#page").tooltip({
            selector: "a[rel=tooltip], .tooltip-toggle",
            trigger: 'hover'
        });

        // Temp only
        $(".options button[value=save-continue]").on('click', function () {
            var btn = $(this);
            btn.closest("form").append('<input type="hidden" name="save-continue" value="true" />');
        });

        // Ajax activity indicator bound to ajax start/stop document events
        $(document).ajaxStart(function () {
            $('#ajax-busy').addClass("busy");
        }).ajaxStop(function () {
            window.setTimeout(function () {
                $('#ajax-busy').removeClass("busy");
            }, 300);
        });

        // Check overridden store settings
        $('.multi-store-override-option').each(function (i, el) {
            Smartstore.Admin.checkOverriddenStoreValue(el);
        });

        // Publish entity commit messages
        $('.entity-commit-trigger').on('click', function (e) {
            var el = $(this);
            if (el.data('commit-type')) {
                EventBroker.publish("entity-commit", {
                    type: el.data('commit-type'),
                    action: el.data('commit-action'),
                    id: el.data('commit-id')
                });
            }
        });

        // Sticky section-header
        var navbar = $("#navbar");
        var navbarHeight = navbar.height() || 1;
        var sectionHeader = $('.section-header');
        var sectionHeaderHasButtons = undefined;

        if (!sectionHeader.hasClass('nofix')) {
            $(window).on("scroll resize", function (e) {
                if (sectionHeaderHasButtons === undefined) {
                    sectionHeaderHasButtons = sectionHeader.find(".options").children().length > 0;
                }
                if (sectionHeaderHasButtons === true) {
                    var y = $(this).scrollTop();
                    sectionHeader.toggleClass("sticky", y >= navbarHeight);
                    $(document.body).toggleClass("sticky-header", y >= navbarHeight);
                }
            }).trigger('resize');
        }

        // Pane resizer
        $(document).on('mousedown', '.resizer', function (e) {
            var resizer = this;
            var resizeNext = resizer.classList.contains('resize-next');
            var initialPageX = e.pageX;
            var pane = resizeNext ? resizer.nextElementSibling : resizer.previousElementSibling;

            if (!pane)
                return;

            var container = resizer.parentNode;
            var initialPaneWidth = pane.offsetWidth;

            var usePercentage = !!(pane.style.width + '').match('%');

            var addEventListener = document.addEventListener;
            var removeEventListener = document.removeEventListener;

            var resize = function (initialSize, offset) {
                if (offset === void 0) offset = 0;

                if (resizeNext)
                    offset = offset * -1;

                var containerWidth = container.clientWidth;
                var paneWidth = initialSize + offset;

                return (pane.style.width = usePercentage
                    ? paneWidth / containerWidth * 100 + '%'
                    : paneWidth + 'px');
            };

            resizer.classList.add('is-resizing');

            // Resize once to get current computed size
            var size = resize();

            var onMouseMove = function (ref) {
                var pageX = ref.pageX;
                size = resize(initialPaneWidth, pageX - initialPageX);
            };

            var onMouseUp = function () {
                // Run resize one more time to set computed width/height.
                size = resize(pane.clientWidth);

                resizer.classList.remove('is-resizing');

                removeEventListener('mousemove', onMouseMove);
                removeEventListener('mouseup', onMouseUp);

                // Create resized event
                var data = { "pane": pane, "resizer": resizer, "width": pane.style.width, "initialWidth": initialPaneWidth };
                var event = new CustomEvent("resized", { "detail": data });

                // Trigger the event
                resizer.dispatchEvent(event);
            };

            addEventListener('mousemove', onMouseMove);
            addEventListener('mouseup', onMouseUp);
        });

        // Popup toggle
        $(document).on('click', '.popup-toggle', function (e) {
            e.preventDefault();
            openPopup({
                url: this.href,
                large: false,
                flex: true,
                closer: false,
                keyboard: true,
                backdrop: 'invisible',
                centered: true,
                scrollable: true
            });
            return false;
        });

        $(window).on('load', function () {
            // swap classes onload and domready
            html.removeClass("loading").addClass("loaded");
        });

    });


})(jQuery, this, document);