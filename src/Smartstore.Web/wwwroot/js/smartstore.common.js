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

        const isLikelyJsonString = (input) => {
            if (typeof input !== "string") return false;
            const s = input.trim();
            if (!(s.startsWith("{") || s.startsWith("["))) return false;
            return true;
        }

        const notify = function (msg) {
            if (!msg) return;

            if (isLikelyJsonString(msg)) {
                try {
                    const obj = JSON.parse(msg);
                    msg = obj;
                }
                catch { }
            }

            if (typeof msg === "object") {
                msg = dumpObjectToHtml(msg);
            }

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

    /**
     * Render a JSON object as an HTML tree using <div> rows.
     *  - One <div> per entry/row.
     *  - Indentation via `padding-inline-start: depth * indentPx`.
     *  - Property name wrapped in <span class="fwm">, followed by ":" and the value.
     *  - If the value is an object or array, it is rendered on the next line with increased indentation.
     *
     * @param {unknown} payload        JSON value or a JSON string
     * @param {Object}  options
     * @param {number}  [options.maxDepth=8]       Maximum nesting levels to expand
     * @param {number}  [options.maxArrayItems=200] Maximum number of array items to render
     * @param {boolean} [options.sortKeys=true]     Sort object keys alphabetically
     * @param {number}  [options.indentPx=16]       Indentation in pixels per level
     * @returns {string} HTML string (no <pre>, safe-escaped content)
     */
    function dumpObjectToHtml(obj, options = {}) {
        if (!$.isPlainObject(obj)) {
            throw new Error("Payload must be a plain object.");
        }

        const opts = {
            maxDepth: 8,
            maxArrayItems: 200,
            sortKeys: true,
            indentPx: 16,
            ...options
        };

        /** Cache for circular reference detection (object -> path). */
        const seen = new WeakMap();

        /**
         * Create one row: `<div style="padding-inline-start:Xpx"><span class="fwm">name</span>: value?`
         * If `valueText` is undefined, we emit only `name:` to indicate a nested structure follows.
         */
        const makeLine = (depth, name, valueOrEmpty) =>
            `<div style="padding-inline-start:${depth * opts.indentPx}px"><span class="fwm">${name.escapeHtml()}</span>${valueOrEmpty !== undefined ? `:&nbsp;${valueOrEmpty.escapeHtml()}` : ":"}</div>`;

        /**
         * Render a (name, value) pair. Complex values (objects/arrays) render:
         *   - a header line "name:" at current depth
         *   - then their children one level deeper
         */
        function renderValue(name, value, depth, path) {
            const lines = [];

            // Inline primitives and common special types
            if (value === null) { lines.push(makeLine(depth, name, "null")); return lines; }
            const t = typeof value;
            if (t === "string" || t === "number" || t === "boolean" || t === "bigint" || t === "undefined" || t === "symbol") {
                lines.push(makeLine(depth, name, t === "string" ? `"${value}"` : String(value)));
                return lines;
            }
            if (value instanceof Date) { lines.push(makeLine(depth, name, `Date(${isNaN(value.getTime()) ? "Invalid" : value.toISOString()})`)); return lines; }
            if (value instanceof RegExp) { lines.push(makeLine(depth, name, value.toString())); return lines; }
            if (typeof value === "function") { lines.push(makeLine(depth, name, `[Function ${value.name || "anonymous"}]`)); return lines; }

            // Circular reference guard
            if (typeof value === "object") {
                if (seen.has(value)) {
                    lines.push(makeLine(depth, name, `[Circular ~ ${seen.get(value)}]`));
                    return lines;
                }
                seen.set(value, path.join("."));
            }

            // Arrays: show "name:" line, then entries as children
            if (Array.isArray(value)) {
                lines.push(makeLine(depth, name)); // nur "Name:"
                if (depth >= opts.maxDepth) { lines.push(makeLine(depth + 1, "[…]", `Array length=${value.length}`)); return lines; }
                const lim = Math.min(value.length, opts.maxArrayItems);
                for (let i = 0; i < lim; i++) {
                    lines.push(...renderValue(`[${i}]`, value[i], depth + 1, path.concat(`[${i}]`)));
                }
                if (value.length > lim) lines.push(makeLine(depth + 1, "...", `(${value.length - lim} more)`));
                return lines;
            }

            // Map/Set: represented similar to objects/arrays
            if (value instanceof Map) {
                lines.push(makeLine(depth, name)); // nur "Name:"
                if (depth >= opts.maxDepth) { lines.push(makeLine(depth + 1, "[Map]", `size=${value.size}`)); return lines; }
                let i = 0;
                for (const [k, v] of value.entries()) {
                    if (i++ >= opts.maxArrayItems) { lines.push(makeLine(depth + 1, "...", "(truncated)")); break; }
                    lines.push(...renderValue(String(k), v, depth + 1, path.concat(String(k))));
                }
                return lines;
            }
            if (value instanceof Set) {
                lines.push(makeLine(depth, name));
                if (depth >= opts.maxDepth) { lines.push(makeLine(depth + 1, "[Set]", `size=${value.size}`)); return lines; }
                let i = 0;
                for (const v of value.values()) {
                    if (i++ >= opts.maxArrayItems) { lines.push(makeLine(depth + 1, "...", "(truncated)")); break; }
                    lines.push(...renderValue(`[${i - 1}]`, v, depth + 1, path.concat(`[${i - 1}]`)));
                }
                return lines;
            }

            // Plain object: show "name:" line, then each key as a child row
            const keys = Object.keys(value);
            lines.push(makeLine(depth, name));
            if (depth >= opts.maxDepth) { lines.push(makeLine(depth + 1, "{…}", `keys=${keys.length}`)); return lines; }
            const list = opts.sortKeys ? keys.sort() : keys;
            if (list.length === 0) {
                lines.push(makeLine(depth + 1, "{ }", "empty"));
                return lines;
            }
            for (const k of list) {
                lines.push(...renderValue(k, value[k], depth + 1, path.concat(k)));
            }
            return lines;
        }

        // Top-level rendering:
        //  - For objects: render only children (no artificial root label).
        //  - For arrays: render a "$:" root header and the entries beneath it (keeps consistent labeling).
        let html;
        if (obj !== null && typeof obj === "object") {
            const lines = [];
            if (Array.isArray(obj)) {
                lines.push(...renderValue("$", obj, 0, ["$"])); // root label for arrays
            }
            else {
                const objKeys = Object.keys(obj);
                const list = opts.sortKeys ? objKeys.sort() : objKeys;
                for (const k of list) lines.push(...renderValue(k, obj[k], 0, [k]));
            }
            html = `<div class="json-dump small">${lines.join("")}</div>`;
        }
        else {
            html = `<div class="json-dump small">${makeLine(0, "value", String(obj))}</div>`;
        }

        return html;
    }

    window.dumpObjectToHtml = dumpObjectToHtml;

})(jQuery, this, document);