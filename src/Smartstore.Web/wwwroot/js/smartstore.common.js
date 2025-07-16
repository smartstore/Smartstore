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

    window.base64Encode = function (value) {
        if (value) {
            const bytes = new TextEncoder().encode(value);
            return btoa(Array.from(bytes, (byte) => String.fromCodePoint(byte)).join(""));
        }

        return value;
    };

    // https://developer.mozilla.org/en-US/docs/Glossary/Base64#the_unicode_problem
    // https://stackoverflow.com/a/30106551/23705546
    window.base64Decode = function (value) {
        if (value) {
            const bytes = Uint8Array.from(atob(value), (m) => m.codePointAt(0));
            return new TextDecoder().decode(bytes);
        }

        return value;
    };

    // TODO: Move to another location when current summernote developments are finished.
    window.insertHtmlInSummernote = function (field, value) {
        field.val(value);

        if (field.hasClass("summernote-editor")) {
            var preview = field.parent().find(".note-editor-preview");

            if (preview.length > 0) {
                // if editor is preview
                preview.html(value);
                preview.removeClass("empty");
            }
            else {
                // if editor is expanded
                field.summernote('code', value);
            }
        }
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

    window.notifyTip = function (element, message, placement = 'top', duration = 2000) {
        let btn = $(element);
        let tooltip = btn.data('bs.tooltip') || btn.tooltip({
            boundary: 'window',
            placement: placement,
            trigger: 'manual'
        }).data('bs.tooltip');

        const originalPlacement = tooltip.config.placement;
        tooltip.config.placement = placement;

        message = message || Res['Common.Done'];
        const originalTitle = btn.attr('data-original-title');
        if (originalTitle != message) {
            btn.attr('data-original-title', message);
        }

        // --> Show tooltip
        tooltip.show();

        setTimeout(() => {
            // --> Hide tooltip after [duration] ms.
            tooltip.hide();
            btn.one('hidden.bs.tooltip', () => {
                // Restore originals from already existing tooltip.
                tooltip.config.placement = originalPlacement;
                if (originalTitle) {
                    btn.attr('data-original-title', originalTitle);
                }
            })
        }, duration);
    };

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
                isProgress ? '<circle class="circle-below" cx="32" cy="32" r="{0:D}" fill="none" stroke-width="{1:D}"></circle>'.format(32 - strokeWidth, strokeWidth) : "" // SVG markup must be complete before turned into dom object
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
        return new Promise((resolve, reject) => {
            if (navigator.clipboard) {
                navigator.clipboard.writeText(text)
                    .then(resolve)
                    .catch(() => {
                        executeFallback().then(resolve).catch(ex => reject(ex));
                    });
            }
            else {
                executeFallback().then(resolve).catch(ex => reject(ex));
            }
        });

        // Classic legacy way of writing to the clipboard
        function executeFallback() {
            return new Promise((resolve, reject) => {
                const textArea = document.createElement("textarea");
                const activeElement = document.activeElement;

                textArea.style.position = "absolute";
                textArea.style.left = "-9999px";
                textArea.value = text;
                document.body.appendChild(textArea);
                textArea.select();

                try {
                    document.execCommand("copy");
                    resolve();
                }
                catch (ex) {
                    reject(ex);
                }
                finally {
                    document.body.removeChild(textArea);
                    if (activeElement) {
                        activeElement.focus();
                    }
                }
            });
        }
    };

    window.connectCopyToClipboard = function (selector) {
        const btn = $(selector);

        if (btn.data('copy-connected')) {
            return;
        }

        btn.tooltip({
            boundary: 'window',
            placement: "top",
            trigger: 'hover',
            title: Res['Common.CopyToClipboard'],
            container: btn.attr('data-container') || false
        }).on('click', function (e) {
            e.preventDefault();
            let btn = $(this);
            let text = btn.data('copy');

            if (!text) {
                // Try to copy text from another element
                let copyFromSelector = btn.data('copy-from');
                if (copyFromSelector) {
                    let copyFrom = $(copyFromSelector);
                    if (copyFrom.length) {
                        if (copyFrom.is('input, select, textarea')) {
                            text = copyFrom.val();
                        }
                        else {
                            text = copyFrom.html();
                        }
                    }
                }
            }

            if (text) {
                copyTextToClipboard(text)
                    .then(() => btn.attr('data-original-title', Res['Common.CopyToClipboard.Succeeded']).tooltip('show'))
                    .catch(() => btn.attr('data-original-title', Res['Common.CopyToClipboard.Failed']).tooltip('show'))
                    .finally(() => {
                        setTimeout(() => {
                            btn.attr('data-original-title', Res['Common.CopyToClipboard']).tooltip('hide');
                        }, 2000);
                    });
            }

            return false;
        }).data('copy-connected', true);
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
                        frm.trigger('recaptchasuccess');
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
        var rememberFields = context.querySelectorAll('input.remember, select.remember, textarea.remember');
        var values = {};

        for (let el of rememberFields) {
            const isCheck = el.matches('input[type=checkbox], input[type=radio]');
            values[el.id] = isCheck ? el.checked : el.value;
        }

        localStorage.setItem(storageId, JSON.stringify(values));
    };

    window.restoreRememberedFormFields = function (storageId, fieldId = null) {
        var values = localStorage.getItem(storageId);
        if (values) {
            values = JSON.parse(values);

            if (fieldId) {
                restoreField(fieldId);
                return;
            }

            for (var key in values) {
                restoreField(key);
            }

            function restoreField(key) {
                const val = values[key];
                if (val !== null && val !== undefined) {
                    const el = document.getElementById(key);

                    if (!el || !el.matches('input, select, textarea')) {
                        return;
                    }

                    if (el.matches('input[type=checkbox], input[type=radio]')) {
                        el.checked = val;
                    }
                    else {
                        if (val === '' && el.matches('.remember-disallow-empty')) {
                            return;
                        }

                        el.value = val;
                    }

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

})(jQuery, this, document);