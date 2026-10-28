/// <reference path="admin.common.js" />

(function ($, window, document, undefined) {

    var _commonPluginFactories = [
        // panel toggling
        function (ctx) {
            ctx.find('input[type=checkbox][data-toggler-for]').each((i, el) => {
                Smartstore.Admin.togglePanel(el, false);
            });
        },
        // select2 (generic)
        function (ctx) {
            const selects = ctx.find('select:not(.noskin)');
            selects.selectWrapper();
            selects.on('select2:selecting select2:unselecting', (e) => {
                try {
                    const $target = $(e.params?.args?.originalEvent?.target);
                    if ($target.length && ($target.hasClass('prevent-selection') || $target.parent().hasClass('prevent-selection'))) {
                        const data = e.params.args.data;

                        if (data.id === '-1' && !_.isEmpty(data.url)) {
                            window.location = data.url;
                        }

                        e.preventDefault();
                    }
                }
                catch (err) {
                    console.error(err);
                }
            });
        },
        // tooltips
        function (ctx) {
            ctx.find(".cph").tooltip({
                selector: "a.hint",
                boundary: 'viewport',
                placement: Smartstore.globalization.culture.isRTL ? "right" : "left",
                trigger: 'hover',
                delay: { show: 400, hide: 0 }
            });
        },
        // switch
        function (ctx) {
            ctx.find(".adminData > input[type=checkbox], .multi-store-setting-control > input[type=checkbox]").each(function (i, el) {
                $(el)
                    .addClass('form-check-input')
                    .wrap('<div class="form-check form-check-solo form-check-warning form-switch form-switch-lg"></div>');
            });
        },
        // .multi-store-override-option
        function (ctx) {
            ctx.find('.multi-store-override-option').each(function (i, el) {
                Smartstore.Admin.checkOverriddenStoreValue(el);
            });
        },
        // Copy to clipboard button
        function (ctx) {
            connectCopyToClipboard(ctx.find(".btn-clipboard"));
        },
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
                    let item = $el.closest(".module-item");
                    let signal = item.find(".module-signal");
                    let btnLabel = $el.find("> span");

                    item.toggleClass("inactive", !activate);

                    if (activate) {
                        $el.addClass("btn-outline-secondary btn-to-danger").removeClass("btn-success");
                        btnLabel.text(T.deactivate);
                        signal.attr('title', T.active);
                    }
                    else {
                        $el.addClass("btn-success").removeClass("btn-outline-secondary btn-to-danger");
                        btnLabel.text(T.activate);
                        signal.attr('title', T.inactive);
                    }

                    signal.toggleClass("d-none", !activate);
                    $el.find("> :is(.icon, sm-icon)").toggleClass("d-none", activate);

                    $el.attr("data-activate", !activate);
                }
            });

            return false;
        })
    }

    $(document).ready(() => {
        var html = $("html");

        html.removeClass("not-ready").addClass("ready");

        applyCommonPlugins($("body"));

        // Handle panel toggling
        $(document).on('change', 'input[type=checkbox][data-toggler-for]', (e) => {
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

        // Dropdown container
        $(document).on('shown.bs.dropdown hidden.bs.dropdown', '.dropdown-container .droptrap', (e) => {
            const container = $(e.currentTarget).closest('.dropdown-container');
            container.toggleClass('active', e.type === 'shown');
        });

        $(document).on('shown.bs.dropdown', '.dropdown-container.dropdown-focus', (e) => {
            $(e.currentTarget).find('.dropdown-menu :input:visible:enabled:first')[0].focus();
        });

        $(window).on('load', function () {
            // swap classes onload and domready
            html.removeClass("loading").addClass("loaded");
        });
    });
})(jQuery, this, document);