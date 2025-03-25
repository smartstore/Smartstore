// On document ready
jQuery(function () {
    let rtl = Smartstore.globalization !== undefined ? Smartstore.globalization.culture.isRTL : false,
        win = $(window),
        body = $(document.body);

    // Adjust PNotify global defaults
    if (typeof PNotify !== 'undefined') {
        var stack = {
            dir1: "up",
            dir2: rtl ? "right" : "left",
            push: "down",
            firstpos1: $('html').data('pnotify-firstpos1') || 0,
            firstpos2: $('html').data('pnotify-firstpos2') || 16,
            spacing1: 0,
            spacing2: 16,
            context: $("body")
        };
        PNotify.prototype.options = $.extend(PNotify.prototype.options, {
            styling: "fontawesome",
            stack: stack,
            addclass: 'stack-bottom' + (rtl ? 'left' : 'right'),
            width: "500px",
            mobile: { swipe_dismiss: true, styling: true },
            animate_speed: 350,
            animate: {
                animate: true,
                in_class: "fadeInUp",
                out_class: "fadeOut" + (rtl ? 'Left' : 'Right')
            }
        });
    }

    // Adjust datetimepicker global defaults
    const dtp = $.fn.datetimepicker;
    if (typeof dtp !== 'undefined' && dtp.Constructor && dtp.Constructor.Default) {
        dtp.Constructor.Default = $.extend({}, dtp.Constructor.Default, {
            locale: 'glob',
            keepOpen: false,
            collapse: true,
            widgetPositioning: {
                horizontal: 'right',
                vertical: 'auto'
            },
            icons: {
                time: 'far fa-clock',
                date: 'fa fa-calendar',
                up: 'fa fa-angle-up',
                down: 'fa fa-angle-down',
                previous: 'fa fa-angle-left',
                next: 'fa fa-angle-right',
                today: 'far fa-calendar-check',
                clear: 'fa fa-delete',
                close: 'fa fa-times'
            }
        });
    }

    // Adjust Popper global defaults
    if (Popper?.Defaults) {
        Popper.Defaults.modifiers.computeStyle.gpuAcceleration = false;
    }

    // Global notification subscriber
    if (window.EventBroker && window._ && typeof PNotify !== 'undefined') {
        EventBroker.subscribe("message", (_message, data) => {
            var opts = _.isString(data) ? { text: data } : data;
            if (opts.text) {
                opts.text = '<div class="ui-pnotify-text-inner">' + opts.text + '</div>';
            }
            
            new PNotify(opts);
        });
    }

    // Confirm
    $(document).on('click', '.confirm', function () {
        const msg = $(this).data("confirm-message") || window.Res["Admin.Common.AskToProceed"];
        return confirm(msg);
    });

    // Prevent (button) multiclick
    $(document).on('click', '.btn-prevent-multiclick', function () {
        let el = $(this);
        let containingForm = el.closest("form");

        if (containingForm.length) {
            el.prop('disabled', true);
            containingForm.trigger('submit');

            if (!containingForm.valid()) {
                el.prop('disabled', false);
            }
        }

        return true;
    });

    // Report validity for native form controls.
    let formWithNativeValidation = $("form.native-validation");
    if (formWithNativeValidation.length) {
        // TODO/INFO: (mh) This will not run in AJAX scenarios when forms are injected after page load.
        formWithNativeValidation.on("submit", function () {
            if (!formWithNativeValidation[0].checkValidity()) {
                formWithNativeValidation[0].reportValidity();
                return false;
            }
            return true;
        });
    }

    // Switch toggle
    $(document).on('click', 'label.switch', function (e) {
        if ($(this).children('input[type="checkbox"]').is('[readonly]')) {
            e.preventDefault();
        }
    });

    // Hacky fix for select2 not focusing search field on container open
    $(document).on('select2:open', function () {
        document.querySelector('.select2-container--open .select2-search__field').focus();
    });

    // Handle ajax notifications
    $(document)
        .ajaxSend(function (_e, _xhr, opts) {
            if (opts.data == null || opts.data == undefined) {
                opts.data = '';
            }
        })
        .ajaxSuccess(function (_e, xhr) {
            var msg = xhr.getResponseHeader('X-Message');
            if (msg) {
                displayNotification(base64Decode(msg), xhr.getResponseHeader('X-Message-Type'));
            }
        })
        .ajaxError(function (_e, xhr) {
            var msg = xhr.getResponseHeader('X-Message');
            if (msg) {
                displayNotification(base64Decode(msg), xhr.getResponseHeader('X-Message-Type'));
            }
            else {
                try {
                    var data = JSON.parse(xhr.responseText);
                    if (data.message) {
                        displayNotification(base64Decode(data.message), "error");
                    }
                }
                catch (ex) {
                    function tryStripHeaders(message) {
                        // Removes the annoying HEADERS part of message that
                        // DeveloperExceptionPageMiddleware adds to the output.
                        var idx = message?.indexOf("\r\nHEADERS\r\n=======");
                        if (idx === undefined || idx === -1) {
                            return message;
                        }
                        return message.substring(0, idx).trim();
                    }

                    displayNotification(tryStripHeaders(xhr.responseText), "error");
                }
            }
        });

    // .mf-dropdown (mobile friendly dropdown)
    (function () {
        $('.mf-dropdown').each(function (i, el) {
            var elLabel = $('> .btn [data-bind]', el);
            if (elLabel.length == 0 || elLabel.text().length > 0)
                return;

            var sel = $('select > option:selected', el).text() || $('select > option', el).first().text();
            elLabel.text(sel);
        });

        body.on('mouseenter mouseleave mousedown change', '.mf-dropdown > select', function (e) {
            var btn = $(this).parent().find('> .btn');
            if (e.type == "mouseenter") {
                btn.addClass('hover');
            }
            else if (e.type == "mousedown") {
                btn.addClass('active focus').removeClass('hover');
                _.delay(function () {
                    body.one('mousedown touch', function (e) { btn.removeClass('active focus'); });
                }, 50);
            }
            else if (e.type == "mouseleave") {
                btn.removeClass('hover');
            }
            else if (e.type == "change") {
                btn.removeClass('hover active focus');
                var elLabel = btn.find('[data-bind]');
                elLabel.text(elLabel.data('bind') == 'value' ? $(this).val() : $('option:selected', this).text());
            }
        });
    })();


    // .dropdown-group (nested dropdowns) && .dropdown-hoverdrop (hover dropdowns)
    (function () {
        let currentDrop, closeTimeout;

        function showDrop(group, fn) {
            let menu = group.find('> .dropdown-menu');
            if (menu.length) {
                let popper = new Popper(group[0], menu[0], {
                    placement: (rtl ? 'left' : 'right') + '-start',
                    modifiers: {
                        computeStyle: { gpuAcceleration: false },
                    },
                    preventOverflow: {
                        boundariesElement: 'viewport'
                    }
                });

                group.data('popper', popper);

                group.addClass('show');
                menu.addClass('show');

                // Show any parent groups
                group.parents('.dropdown-group, .dropdown-group > .dropdown-menu').addClass('show');

                if (_.isFunction(fn)) fn();
            }
        }

        function closeDrop(group, fn) {
            group.find('.dropdown-group, .dropdown-menu').addBack().each(function () {
                const el = $(this);
                el.removeClass('show');

                if (el.is('.dropdown-group')) {
                    // Destroy and remove Popper instance
                    let popper = el.data('popper');
                    if (popper) {
                        popper.destroy();
                        el.removeData('popper');
                    }
                }
            });

            if (_.isFunction(fn)) fn();
        }

        function handleEnter(group) {
            clearTimeout(group.data('closeTimeout'));

            // Close all open dropdowns that are not ancestors or descendants
            $('.dropdown-group.show').each(function () {
                let openGroup = $(this);
                if (!$.contains(openGroup[0], group[0]) && !$.contains(group[0], openGroup[0])) {
                    closeDrop(openGroup);
                }
            });

            showDrop(group);
            currentSubGroup = group;
        }

        function handleLeave(group, leaveDelay) {
            let closeTimeout = setTimeout(() => {
                // Ensure child dropdowns are also closed
                group.find('.dropdown-group.show').addBack().each(function () {
                    closeDrop($(this));
                });
            }, leaveDelay);

            group.data('closeTimeout', closeTimeout);
        }

        // Drop dropdown menus on hover
        $(document).on('mouseenter mouseleave', '.dropdown-hoverdrop', function (e) {
            const li = $(this);
            const a = $('> .dropdown-toggle', this);

            if (a.data("toggle") === 'dropdown')
                return;

            const afterClose = () => currentDrop = null;

            if (e.type == 'mouseenter') {
                if (currentDrop) {
                    clearTimeout(closeTimeout);
                    closeDrop(currentDrop, afterClose);
                }
                li.addClass('show').find('> .dropdown-menu').addClass('show');
                currentDrop = li;
            }
            else {
                li.removeClass('show');
                closeTimeout = setTimeout(() => closeDrop(li, afterClose), 250);
            }
        });

        // Handle nested dropdown menus
        $(document).on('mouseenter mouseleave click', '.dropdown-group', function (e) {
            let group = $(this);
            let type;
            let leaveDelay = 250;

            if (e.type === 'click') {
                let item = $(e.target).closest('.dropdown-item');
                if (item.length && item.parent()[0] == this) {
                    type = $(this).is('.show') ? 'leave' : 'enter';
                    leaveDelay = 0;
                    item.trigger("blur");
                    e.preventDefault();
                    e.stopPropagation();
                }
            }

            type = type || (e.type == 'mouseenter' ? 'enter' : 'leave');

            if (type === 'enter') {
                handleEnter(group);
            } else {
                // Close drop delayed to allow for mouse re-entry
                handleLeave(group, leaveDelay);
            }
        });
    })();

    // HTML text collapser
    if ($.fn.moreLess) {
        $('.more-less').moreLess();
    }

    // Toggle grouped buttons
    $(document).on('mouseup', '.btn-group-toggle.unselectable > .btn', function (e) {
        let btn = $(this);
        let radio = btn.find('input:radio');

        if (radio.length && radio.prop('checked')) {
            e.preventDefault();
            e.stopPropagation();

            _.delay(function () {
                radio.prop('checked', false);
                btn.removeClass('active focus');
            }, 50);
        }
    });

    // State region dropdown
    $(document).on('change', '.country-selector', function () {
        var el = $(this);
        var selectedCountryId = el.val();
        var ddlStates = $(el.data("region-control-selector"));
        var ajaxUrl = el.data("states-ajax-url");

        if (!ajaxUrl || !ddlStates) {
            // // No data to load.
            return;
        }

        if (selectedCountryId == '0') {
            // No data to load.
            ddlStates.empty().val(null).trigger('change');
            return;
        }

        var addEmptyStateIfRequired = el.data("addemptystateifrequired");
        var addAsterisk = el.data("addasterisk");
        var selectedId = ddlStates.data('select-selected-id');
        var options = ddlStates.children('option');
        var firstOption = options.first();
        var hasOptionLabel = firstOption.length && (firstOption[0].attributes['value'] === undefined || firstOption.val().isEmpty());
        var initialLoad = options.length == 0 || (options.length == 1 && hasOptionLabel);

        $.ajax({
            cache: false,
            type: "GET",
            url: ajaxUrl,
            data: { "countryId": selectedCountryId, "addEmptyStateIfRequired": addEmptyStateIfRequired, "addAsterisk": addAsterisk },
            success: function (data) {
                if (data.error)
                    return;

                ddlStates.empty();

                if (hasOptionLabel) {
                    ddlStates.append(firstOption);
                }

                $.each(data, function (id, option) {
                    var selected = initialLoad && option.Value == selectedId;
                    ddlStates.append(new Option(option.Text, option.Value, selected, selected));
                });

                if (!initialLoad) {
                    ddlStates.val(null);
                }

                ddlStates.trigger('change');
            }
        });
    });

    // Paginator link to load content using AJAX.
    $(document).on('click', '.page-link', function (e) {
        const link = $(this);
        const url = link.attr('href');
        const contentTarget = link.closest('.pagination-container').data('target');

        if (!_.isEmpty(url) && !_.isEmpty(contentTarget)) {
            e.preventDefault();

            $.ajax({
                cache: false,
                type: "GET",
                url: url,
                success: function (response) {
                    const target = $(contentTarget);
                    target.html(response.content);
                    target.trigger('updated');
                    applyCommonPlugins(target);
                }
            });

            return false;
        }
    });

    // Waypoint / scroll top
    (function () {
        $(document).on('click', 'a.scrollto', function (e) {
            e.preventDefault();
            const href = $(this).attr('href');
            const target = href === '#' ? $('body') : $(href);
            const offset = $(this).data('offset') || 0;

            $(window).scrollTo(target, { offset: offset });
            return false;
        });

        var prevY;

        var throttledScroll = _.throttle(function (e) {
            var y = win.scrollTop();
            if (_.isNumber(prevY)) {
                // Show scroll button only when scrolled up
                if (y < prevY && y > 500) {
                    $('#scroll-top').addClass("in");
                }
                else {
                    $('#scroll-top').removeClass("in");
                }
            }

            prevY = y;
        }, 100);

        win.on("scroll", throttledScroll);
    })();

    // Toggle password visibility
    $(document).on('click', '.btn-toggle-pwd', function () {
        const input = $(this).prev('.form-control')[0];
        if (input?.type == 'text') {
            input.type = 'password';
        }
        else if (input?.type == 'password') {
            input.type = 'text';
        }
    });

    // Swap Popper x placement when RTL
    $(document).on('show.bs.dropdown', '.dropdown', (e) => {
        if (!rtl) {
            return;
        }

        let dropdown = $(e.currentTarget).find('> .dropdown-toggle, > [data-toggle=dropdown]').data('bs.dropdown');
        if (!dropdown) {
            return;
        }

        let popperConfig = dropdown._config.popperConfig;
        if (!popperConfig) {
            dropdown._config.popperConfig = popperConfig = {};
        }

        if (!popperConfig.placement) {
            let bsPlacement = dropdown._getPlacement();
            if (bsPlacement.endsWith('-start')) {
                bsPlacement = bsPlacement.replace('-start', '-end');
            }
            else if (bsPlacement.endsWith('-end')) {
                bsPlacement = bsPlacement.replace('-end', '-start');
            }

            popperConfig.placement = bsPlacement;
        }
    });

    // Fix Dropdown & Tooltip UI "collision" issues
    $(document).on('shown.bs.dropdown hidden.bs.dropdown', '.dropdown', (e) => {
        const $tooltip = $(e.currentTarget).find('> .tooltip-toggle, > [data-toggle=tooltip]');

        if ($tooltip.data('bs.tooltip')) {
            if (e.type === 'shown') {
                // Hide tooltip if dropdown is shown...
                $tooltip.tooltip('hide');
                // and disable it.
                $tooltip.tooltip('disable');
            }
            else {
                // Re-enable tooltip if dropdown is hidden.
                $tooltip.tooltip('enable');
            }
        }
    });

    // Modal stuff
    $(document).on('hide.bs.modal', '.modal', () => { body.addClass('modal-hiding'); });
    $(document).on('hidden.bs.modal', '.modal', () => { body.removeClass('modal-hiding'); });

    // Bootstrap Tooltip & Popover custom classes
    // TODO: Remove customization after BS4 has been updated to latest version or to BS5
    extendTipComponent($.fn.popover);
    extendTipComponent($.fn.tooltip);

    function extendTipComponent(component) {
        if (component) {
            var ctor = component.Constructor;
            $.extend(ctor.Default, { customClass: '' });

            var _show = ctor.prototype.show;
            ctor.prototype.show = function () {
                _show.apply(this);

                if (this.config.customClass) {
                    var tip = this.getTipElement();
                    $(tip).addClass(this.config.customClass);
                }
            };
        }
    }
});