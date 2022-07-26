(function ($, window, document) {

    window.setLocation = function (url) {
        window.location.href = url;
    };

    window.openWindow = function (url, w, h, scroll) {
        w = w || (screen.availWidth - (screen.availWidth * 0.25));
        h = h || (screen.availHeight - (screen.availHeight * 0.25));

        var l = (screen.availLeft + (screen.availWidth / 2)) - (w / 2);
        var t = (screen.availTop + (screen.availHeight / 2)) - (h / 2);

        winprops = 'dependent=1,resizable=0,height=' + h + ',width=' + w + ',top=' + t + ',left=' + l;
        if (scroll) winprops += ',scrollbars=1';
        var f = window.open(url, "_blank", winprops);
    };

    window.modifyUrl = function (url, qsName, qsValue) {
        var search = null;

        if (!url) {
            url = window.location.protocol + "//" +
                window.location.host +
                window.location.pathname;
        }
        else {
            // strip query from url
            var idx = url.indexOf('?', 0);
            if (idx > -1) {
                search = url.substring(idx);
                url = url.substring(0, idx);
            }
        }

        var qs = getQueryStrings(search);

        // Add new params to the querystring dictionary
        qs[qsName] = qsValue;

        return url + createQueryString(qs);

        function createQueryString(dict) {
            var bits = [];
            for (var key in dict) {
                if (dict.hasOwnProperty(key) && dict[key]) {
                    bits.push(key + "=" + dict[key]);
                }
            }
            return bits.length > 0 ? "?" + bits.join("&") : "";
        }
    };

    // http://stackoverflow.com/questions/2907482
    // Gets Querystring from window.location and converts all keys to lowercase
    window.getQueryStrings = function (search) {
        var assoc = {};
        var decode = function (s) { return decodeURIComponent(s.replace(/\+/g, " ")); };
        var queryString = (search || location.search).substring(1);
        var keyValues = queryString.split('&');

        for (var i in keyValues) {
            var item = keyValues[i].split('=');
            if (item.length > 1) {
                var key = decode(item[0]).toLowerCase();
                var val = decode(item[1]);
                if (assoc[key] === undefined) {
                    assoc[key] = val;
                } else {
                    var v = assoc[key];
                    if (v.constructor != Array) {
                        assoc[key] = [];
                        assoc[key].push(v);
                    }
                    assoc[key].push(val);
                }
            }
        }

        return assoc;
    };

    window.htmlEncode = function (value) {
        return $('<div/>').text(value).html();
    };

    window.htmlDecode = function (value) {
        return $('<div/>').html(value).text();
    };

    window.displayNotification = function (message, type, sticky, delay) {
        if (window.EventBroker === undefined || window._ === undefined)
            return;

        var notify = function (msg) {
            if (!msg)
                return;

            EventBroker.publish("message", {
                text: msg,
                type: type,
                delay: delay || (type === "success" ? 2500 : 5000),
                hide: !sticky
            });
        };

        if (_.isArray(message)) {
            $.each(message, function (i, val) {
                notify(val);
            });
        }
        else {
            notify(message);
        }
    };

    window.Prefixer = (function () {
        var TransitionEndEvent = {
            WebkitTransition: 'webkitTransitionEnd',
            MozTransition: 'transitionend',
            OTransition: 'oTransitionEnd otransitionend',
            transition: 'transitionend'
        };

        var AnimationEndEvent = {
            WebkitAnimation: 'webkitAnimationEnd',
            MozAnimation: 'animationend',
            OAnimation: 'webkitAnimationEnd oAnimationEnd',
            animation: 'animationend'
        };

        var cssProps = {},
            cssValues = {},
            domProps = {};

        function prefixCss(prop) {
            return cssProps[prop] || (cssProps[prop] = Modernizr.prefixedCSS(prop));
        }

        function prefixCssValue(prop, value) {
            var key = prop + '.' + value;
            return cssValues[key] || (cssValues[key] = Modernizr.prefixedCSSValue(prop, value));
        }

        function prefixDom(prop) {
            return domProps[prop] || (domProps[prop] = Modernizr.prefixed(prop));
        }

        return {
            css: prefixCss,
            cssValue: prefixCssValue,
            dom: prefixDom,
            event: {
                transitionEnd: TransitionEndEvent[prefixDom('transition')],
                animationEnd: AnimationEndEvent[prefixDom('animation')]
            }
        };
    })();

    window.createCircularSpinner = function (size, active, strokeWidth, boxed, white, isProgress, showtext) {
        var spinner = $('<div class="{0}"></div>'.format(!isProgress ? "spinner" : "spinner circular-progress"));
        if (active) spinner.addClass('active');
        if (boxed) spinner.addClass('spinner-boxed').css('font-size', size + 'px');
        if (white) spinner.addClass('white');

        if (!_.isNumber(strokeWidth)) {
            strokeWidth = 4;
        }

        var svg = $('<svg style="width:{0}px; height:{0}px" viewBox="0 0 64 64">{3}<circle class="circle" cx="32" cy="32" r="{1}" fill="none" stroke-width="{2}"></circle></svg>'
            .format(size,
                32 - strokeWidth,
                strokeWidth,
                isProgress ? '<circle class="circle-below" cx="32" cy="32" r="{0}" fill="none" stroke-width="{1}"></circle>'.format(32 - strokeWidth, strokeWidth) : "" // SVG markup must be complete before turned into dom object
            ));

        spinner.append($(svg));

        if (isProgress) {
            svg.wrap('<div class="wrapper"></div>');

            if (showtext) {
                spinner.append('<div class="progress-text">0</div>');
                // TODO: set font-size according to size param :-/ maybe subtract a fixed value???
            }

            var circle = svg.find(".circle");
            var radius = circle.attr("r");
            var circumference = 2 * Math.PI * radius;
            circle.css({
                'stroke-dashoffset': circumference,
                'stroke-dasharray': circumference
            });
        }

        return spinner;
    };

    window.setCircularProgressValue = function (context, progress) {
        var value = Math.abs(parseInt(progress));
        if (!isNaN(value)) {

            var text = $(context).find(".progress-text");
            var circle = $(context).find(".circle");
            var radius = circle.attr("r");
            var circumference = 2 * Math.PI * radius;
            var percent = value / 100;
            var dashoffset = circumference * (1 - percent);

            circle.css('stroke-dashoffset', dashoffset);

            if (text.length > 0)
                text.text(value);
        }
    };

    window.copyTextToClipboard = function (text) {
        var result = false;

        if (window.clipboardData && window.clipboardData.setData) {
            result = clipboardData.setData('Text', text);
        }
        else if (document.queryCommandSupported && document.queryCommandSupported('copy')) {
            var textarea = document.createElement('textarea'),
                elFocus = document.activeElement,
                elContext = elFocus || document.body;

            textarea.textContent = text;
            textarea.style.position = 'fixed';
            textarea.style.width = '10px';
            textarea.style.height = '10px';

            elContext.appendChild(textarea);

            textarea.focus();
            textarea.setSelectionRange(0, textarea.value.length);

            try {
                result = document.execCommand('copy');
            }
            catch (ex) {
                elContext.removeChild(textarea);
                if (elFocus) {
                    elFocus.focus();
                }
            }
        }
        return result;
    };

    window.getImageSize = function (url, callback) {
        var img = new Image();
        img.src = url;
        img.onload = function () {
            callback.apply(this, [img.naturalWidth, img.naturalHeight]);
        };
    };

    window.renderGoogleRecaptcha = function (containerId, sitekey, invisible) {
        var frm = $('#' + containerId).closest('form');

        if (frm.length === 0)
            return;

        var holderId = grecaptcha.render(containerId, {
            sitekey: sitekey,
            size: invisible ? 'invisible' : undefined,
            badge: 'bottomleft',
            callback: function (token) {
                if (invisible) {
                    if (frm.data('ajax')) {
                        frm.find("#g-recaptcha-response").val(token);
                    }
                    else if (frm) {
                        frm[0].submit();
                    }
                }
            }
        });

        if (invisible) {

            // if form has attr data-ajax
            if (frm.data('ajax')) {
                frm.on('ajaxsubmit', function (e) {
                    grecaptcha.execute(holderId);
                });
            }

            frm.on('submit', function (e) {
                if ($.validator === undefined || frm.valid() == true) {
                    e.preventDefault();
                    grecaptcha.execute(holderId);
                }
            });
        }
    };

    window.rememberFormFields = function (contextId, storageId) {
        var context = document.getElementById(contextId);
        var rememberFields = context.querySelectorAll('.form-control.remember, .form-check-input.remember');
        var values = {};

        for (let el of rememberFields) {
            values[el.id] = el.classList.contains('form-check-input') ? el.checked : el.value;
        }

        localStorage.setItem(storageId, JSON.stringify(values));
    };

    window.setRememberedFormFields = function (storageId) {
        var values = localStorage.getItem(storageId);
        if (values) {
            values = JSON.parse(values);

            for (var key in values) {
                var val = values[key];

                if (val !== null && val !== undefined) {
                    var el = document.getElementById(key);

                    if (!el)
                        continue;

                    if (el.classList.contains('form-check-input'))
                        el.checked = val;
                    else
                        el.value = val;

                    el.dispatchEvent(new Event('change', { bubbles: true, cancelable: true }));
                }
            }
        }
    };

    window.reinitFormValidator = function (selector) {
        $(selector).each(function (i, el) {
            let form = $(el);
            if (!form.is('form')) {
                return;
            }

            let validator = form.data("validator");
            if (validator) {
                validator.hideErrors();
                validator.destroy();
            }

            form
                .removeData("validator")
                .removeData("unobtrusiveValidation")
                .off(".validate");

            $.validator.unobtrusive.parse(form);

            validator = form.validate();
            if (!validator.settings) {
                $.extend(validator.settings, $.validator.defaults);
            }

            $.extend(validator.settings, { ignore: "[type=hidden], .dg-cell-selector-checkbox, .btn" });
        });
    };

    window.getAntiforgeryToken = function () {
        return $('meta[name="__rvt"]').attr("content") || $('input[name="__RequestVerificationToken"]').val();
    };

    // on document ready
    $(function () {
        var rtl = Smartstore.globalization !== undefined ? Smartstore.globalization.culture.isRTL : false,
            win = $(window),
            body = $(document.body);

        function decode(str) {
            if (str) {
                try {
                    str = atob(str);
                }
                catch (e) { }

                try {
                    return decodeURIComponent(escape(str));
                }
                catch (e) {
                    return str;
                }
            }

            return str;
        }

        // Adjust initPNotify global defaults
        if (typeof PNotify !== 'undefined') {
            var stack = {
                "dir1": "up",
                "dir2": rtl ? "left" : "right",
                "push": "down",
                "firstpos1": $('html').data('pnotify-firstpos1') || 0,
                "spacing1": 0,
                "spacing2": 16,
                "context": $("body")
            };
            PNotify.prototype.options = $.extend(PNotify.prototype.options, {
                styling: "fontawesome",
                stack: stack,
                addclass: 'stack-bottom' + (rtl ? 'right' : 'left'),
                width: "500px",
                mobile: { swipe_dismiss: true, styling: true },
                animate: {
                    animate: true,
                    in_class: "fadeInDown",
                    out_class: "fadeOut" + (rtl ? 'Right' : 'Left')
                }
            });
        }

        // Adjust datetimepicker global defaults
        var dtp = $.fn.datetimepicker;
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

        // Global notification subscriber
        if (window.EventBroker && window._ && typeof PNotify !== 'undefined') {
            EventBroker.subscribe("message", function (message, data) {
                var opts = _.isString(data) ? { text: data } : data;
                new PNotify(opts);
            });
        }

        // Confirm
        $(document).on('click', '.confirm', function (e) {
            var msg = $(this).data("confirm-message") || window.Res["Admin.Common.AskToProceed"];
            return confirm(msg);
        });

        // Switch toggle
        $(document).on('click', 'label.switch', function (e) {
            if ($(this).children('input[type="checkbox"]').is('[readonly]')) {
                e.preventDefault();
            }
        });

        // Handle ajax notifications
        $(document)
            .ajaxSend(function (e, xhr, opts) {
                if (opts.data == null || opts.data == undefined) {
                    opts.data = '';
                }
            })
            .ajaxSuccess(function (e, xhr) {
                var msg = xhr.getResponseHeader('X-Message');
                if (msg) {
                    displayNotification(decode(msg), xhr.getResponseHeader('X-Message-Type'));
                }
            })
            .ajaxError(function (e, xhr) {
                var msg = xhr.getResponseHeader('X-Message');
                if (msg) {
                    displayNotification(decode(msg), xhr.getResponseHeader('X-Message-Type'));
                }
                else {
                    try {
                        var data = JSON.parse(xhr.responseText);
                        if (data.message) {
                            displayNotification(decode(data.message), "error");
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


        (function () {
            var currentDrop,
                currentSubDrop,
                closeTimeout,
                closeTimeoutSub;

            function closeDrop(drop, fn) {
                drop.removeClass('show').find('> .dropdown-menu').removeClass('show');
                if (_.isFunction(fn)) fn();
            }

            // drop dropdown menus on hover
            $(document).on('mouseenter mouseleave', '.dropdown-hoverdrop', function (e) {
                var li = $(this),
                    a = $('> .dropdown-toggle', this);

                if (a.data("toggle") === 'dropdown')
                    return;

                var afterClose = function () { currentDrop = null; };

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
                    closeTimeout = window.setTimeout(function () { closeDrop(li, afterClose); }, 250);
                }
            });

            // Handle nested dropdown menus
            $(document).on('mouseenter mouseleave click', '.dropdown-group', function (e) {
                let li = $(this);
                let type;
                let leaveDelay = 250;

                if (e.type === 'click') {
                    let item = $(e.target).closest('.dropdown-item');
                    if (item.length && item.parent().get(0) == this) {
                        type = $(this).is('.show') ? 'leave' : 'enter';
                        leaveDelay = 0;
                        item.blur();
                        e.preventDefault();
                        e.stopPropagation();
                    }
                }

                type = type || (e.type == 'mouseenter' ? 'enter' : 'leave');

                if (type == 'enter') {
                    if (currentSubDrop) {
                        clearTimeout(closeTimeoutSub);
                        closeDrop(currentSubDrop);
                    }
                    li.addClass('show').find('> .dropdown-menu').addClass('show');
                    currentSubDrop = li;
                }
                else {
                    li.removeClass('show');
                    closeTimeoutSub = window.setTimeout(function () { closeDrop(li); }, leaveDelay);
                }
            });
        })();


        // html text collapser
        if ($.fn.moreLess) {
            $('.more-less').moreLess();
        }

        // Unselectable radio button groups
        $(document).on('click', '.btn-group-toggle.unselectable > .btn', function (e) {
            var btn = $(this);
            var radio = btn.find('input:radio');

            if (radio.length && radio.prop('checked')) {
                _.delay(function () {
                    radio.prop('checked', false);
                    btn.removeClass('active focus');

                    e.preventDefault();
                    e.stopPropagation();
                }, 50);
            }
        });

        // state region dropdown
        $(document).on('change', '.country-selector', function () {
            var el = $(this);
            var selectedCountryId = el.val();
            var ddlStates = $(el.data("region-control-selector"));

            if (selectedCountryId == '0') {
                // No data to load.
                ddlStates.empty().val(null).trigger('change');
                return;
            }

            var ajaxUrl = el.data("states-ajax-url");
            var addEmptyStateIfRequired = el.data("addemptystateifrequired");
            var addAsterisk = el.data("addasterisk");
            var initialLoad = ddlStates.children('option').length == 0;
            var selectedId = ddlStates.data('select-selected-id');

            $.ajax({
                cache: false,
                type: "GET",
                url: ajaxUrl,
                data: { "countryId": selectedCountryId, "addEmptyStateIfRequired": addEmptyStateIfRequired, "addAsterisk": addAsterisk },
                success: function (data) {
                    if (data.error)
                        return;

                    ddlStates.empty();

                    $.each(data, function (id, option) {
                        var selected = initialLoad && option.Value == selectedId;
                        ddlStates.append(new Option(option.Text, option.Value, selected, selected));
                    });

                    if (!initialLoad) {
                        ddlStates.val(null);
                    }

                    ddlStates.trigger('change');
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    alert('Failed to retrieve states.');
                }
            });
        });

        // Waypoint / scroll top
        (function () {
            $(document).on('click', 'a.scrollto', function (e) {
                e.preventDefault();
                var href = $(this).attr('href');
                var target = href === '#' ? $('body') : $(href);
                var offset = $(this).data('offset') || 0;

                $(window).scrollTo(target, { duration: 800, offset: offset });
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

        // Modal stuff
        $(document).on('hide.bs.modal', '.modal', function (e) { body.addClass('modal-hiding'); });
        $(document).on('hidden.bs.modal', '.modal', function (e) { body.removeClass('modal-hiding'); });

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

})(jQuery, this, document);

