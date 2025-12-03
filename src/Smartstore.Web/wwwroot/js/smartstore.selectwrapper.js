/*
*  Project: Smartstore select wrapper
*  Author: Murat Cakir, SmartStore AG
*/

(function ($, window, document, undefined) {

    // Customize select2 defaults
    $.extend($.fn.select2.defaults.defaults, {
        minimumResultsForSearch: 12,
        theme: 'bootstrap',
        width: 'style', // 'resolve',
        dropdownAutoWidth: false
    });

    var lists = [];

    function load(url, selectedId, callback) {
        $.ajax({
            url: url,
            dataType: 'json',
            async: true,
            data: { selectedId: selectedId || 0 },
            success: function (data, status, jqXHR) {
                lists[url] = data;
                callback(data);
            }
        });
    }

    function initLoad(select, url) {
        $.ajax({
            url: url,
            dataType: 'json',
            success: function (data) {
                select.empty();
                _.each(data, function (item) {
                    var option = new Option(item.text, item.id, item.selected, item.selected);
                    select.append(option);
                });
            }
        });
    }

    $.fn.select2.amd.define('select2/data/lazyAdapter', [
        'select2/data/array',
        'select2/utils',
        'select2/data/tags'
    ],
        function (ArrayData, Utils, Tags) {

            function LazyAdapter($element, options) {
                this._isInitialized = false;
                LazyAdapter.__super__.constructor.call(this, $element, options);
            }

            Utils.Extend(LazyAdapter, ArrayData);

            // Replaces the old 'initSelection()' callback method
            LazyAdapter.prototype.current = function (callback) {
                var select = this.$element,
                    opts = this.options.options;

                if (!this._isInitialized) {
                    var init = opts.init || {},
                        initId = init.id || select.data('select-selected-id'),
                        initText = init.text || select.data('select-init-text');

                    if (initId) {
                        // Add the option tag to the select element,
                        // otherwise the current val() will not be resolved.
                        var $option = select.find('option').filter(function (i, elm) {
                            // Do not === otherwise infinite loop ;-)
                            return elm.value == initId;
                        });

                        if ($option.length === 0) {
                            $option = this.option({ id: initId, text: initText, selected: true });
                            this.addOptions($option);
                        }

                        callback([{
                            id: initId,
                            text: initText || ''
                        }]);

                        return;
                    }
                }

                LazyAdapter.__super__.current.call(this, callback);
            };

            LazyAdapter.prototype.query = function (params, callback) {
                var select = this.$element,
                    opts = this.options.options;

                if (!opts.lazy && !opts.lazy.url) {
                    callback({ results: [] });
                }
                else {
                    var url = opts.lazy.url,
                        init = opts.init || {},
                        initId = init.id || select.data('select-selected-id'),
                        term = params.term,
                        list = null;

                    list = lists[url];

                    var doQuery = function (data) {
                        list = data;
                        if (term) {
                            var isGrouped = data.length && data[0].children;
                            if (isGrouped) {
                                // In a grouped list, find the optgroup marked with "main"
                                var mainGroup = _.find(data, function (x) { return x.children && x.main; });
                                data = mainGroup ? mainGroup.children : data[0].children;
                            }
                            list = _.filter(data, function (val) {
                                var rg = new RegExp(term, "i");
                                return rg.test(val.text);
                            });
                        }
                        select.data("loaded", true);
                        callback({ results: list });
                    };

                    if (!list) {
                        load(url, initId, doQuery);
                    }
                    else {
                        doQuery(list);
                    }
                }

                this._isInitialized = true;
            };

            // INFO: This enables addding tags when using lazy-loading.
            return Utils.Decorate(LazyAdapter, Tags);
        }
    );

    $.fn.select2.amd.define('select2/data/remoteAdapter', ['select2/data/array', 'select2/utils'],
        function (ArrayData, Utils) {

            function RemoteAdapter($element, options) {
                this._url = options.get('remoteUrl');
                this._global = options.get('remoteGlobal') || false;
                this._cachedPage = null;
                this._load = true;

                RemoteAdapter.__super__.constructor.call(this, $element, options);
            }

            Utils.Extend(RemoteAdapter, ArrayData);

            RemoteAdapter.prototype.query = function (params, callback) {
                params.page = params.page || 0;
                params.term = params.term || '';

                if (this._load || params.term.length > 0) {

                    // Avoid loading first page multiple times.
                    if (params.page === 0 && params.term.length === 0 && this._cachedPage) {
                        callback({
                            results: this._cachedPage,
                            pagination: { more: true }
                        });
                        return;
                    }

                    var self = this;

                    $.ajax({
                        type: 'GET',
                        url: this._url,
                        global: this._global,
                        dataType: 'json',
                        cache: false,
                        timeout: 5000,
                        data: { page: params.page, term: params.term },
                        success: function (data, status, jqXHR) {
                            self._load = data.hasMoreData || params.term.length > 0;
                            self._cachedPage = null;

                            if (data.hasMoreData && params.page === 0 && params.term.length === 0) {
                                self._cachedPage = data.results;
                            }

                            callback({
                                results: data.results,
                                pagination: { more: data.hasMoreData }
                            });
                        }
                    });
                }
                else {
                    // No more data. Remove "Search..." list item.
                    this.container.$results.find('.loading-results').remove();
                }
            };

            return RemoteAdapter;
        }
    );

    $.fn.selectWrapper = function (options) {
        if (options && options.resetDataUrl?.hasValue() && lists[options.resetDataUrl]) {
            lists[options.resetDataUrl] = null;
            return this.each(function () { });
        }

        options = options || {};

        var originalMatcher = $.fn.select2.defaults.defaults.matcher;

        return this.each(function () {
            let sel = $(this);

            if (sel.data("select2")) {
                // skip process if select is skinned already
                return;
            }

            let placeholder = getPlaceholder();

            if (sel.is('.theme-color-chooser')) {
                // Special handling for color chooser: read global theme colors and add them as data-color attributes to matching options.
                const colorVars = Smartstore.Admin.getThemeColorVars();
                sel.find('option:not([data-color])').each(function () {
                    const colorVar = colorVars['--' + this.value];
                    if (colorVar) {
                        this.setAttribute('data-color', colorVar);
                    }
                });
            }

            // Following code only applicable to select boxes (not input:hidden)
            let firstOption = sel.children("option:first-of-type").first();
            let hasOptionLabel = firstOption.length &&
                (firstOption[0].attributes['value'] === undefined || firstOption.val().isEmpty());

            if (placeholder && hasOptionLabel) {
                // Clear first option text in nullable dropdowns.
                // "allowClear" doesn't work otherwise.
                firstOption.text("");
            }

            if (placeholder && !hasOptionLabel && !sel.is('[multiple=multiple]')) {
                // Create empty first option
                // "allowClear" doesn't work otherwise.
                firstOption = $('<option></option>').prependTo(sel);
            }

            if (!placeholder && hasOptionLabel && firstOption.text() && !sel.data("tags")) {
                // Use first option text as placeholder
                placeholder = firstOption.text();
                firstOption.text("");
            }

            if (window.touchable && !sel.hasClass("skin") && !sel.data("select-url") && !sel.data("remote-url")) {
                if (sel.find('option[data-color], option[data-imageurl]').length === 0
                    || (sel.data("tags") && window.document.documentElement.classList.contains("mobile-device"))) {
                    // Skip skinning if device is mobile and no rich content exists (color & image)
                    // or if device is mobile and no data tags exist.
                    return;
                }
            }

            function normalizeText(value) {
                if (value && value.length > 0) {
                    value = value.trim();
                }
                return value || null;
            }

            function renderSelectItem(item, isResult) {
                try {
                    var option = $(item.element),
                        imageUrl = option.data('imageurl'),
                        color = option.attr('data-color'),
                        text = item.text,
                        title = '',
                        classes = item.cssClass || option.data('item-class') || '',
                        styles = item.cssStyle || option.data('item-style') || '',
                        hint = item.hint || option.attr('data-hint'),
                        hintClass = item.hintClass || option.attr('data-hint-class') || 'text-muted',
                        description = item.description || option.attr('data-description'),
                        icon = option.data('icon'),
                        truncateText = options.maxTextLength > 0 && text.length > options.maxTextLength,
                        appendHint = !isResult && hint && hint.length > 0;

                    let itemTitle = item.title || option.data('title') || '';

                    if (!isResult && sel.prop('multiple')) {
                        // Should not be applied. Looks ugly for selected options.
                        classes = '';
                    }

                    if (truncateText || appendHint || itemTitle.length > 0) {
                        title = text;
                        if (itemTitle.length > 0) {
                            title += ' [' + itemTitle + ']';
                        }
                        if (appendHint) {
                            title += ' [' + hint + ']';
                        }
                    }

                    if (truncateText) {
                        text = text.substring(0, options.maxTextLength) + '…';
                    }

                    let preResult = null;
                    let postResult = null;
                    let result = $('<span class="select2-option"></span>')
                        .addClass(classes)
                        .attr('title', normalizeText(title))
                        .attr('style', normalizeText(styles));

                    if (isResult) {
                        if (!_.isEmpty(item.id) && !_.isEmpty(item.url)) {
                            if (item.id === '-1') {
                                // Item is a link to open add-entity page.
                                result.addClass('select2-item-link prevent-selection');
                            }
                            else {
                                // Add small item button to open detail page.
                                // From inner to outer.
                                preResult = $('<i class="fa fa-ellipsis fa-fw prevent-selection"></i>')
                                    .wrap('<a/>')
                                    .parent()
                                    .attr('href', item.url.replace('__id__', item.id))
                                    .attr('title', item.urlTitle)
                                    .addClass('btn btn-clear-dark btn-no-border btn-sm btn-icon rounded-circle prevent-selection')
                                    .wrap('<span class="select2-item-btn">')
                                    .parent();
                            }
                        }

                        if (!_.isEmpty(description)) {
                            postResult = $('<span/>').addClass('select2-item-description text-muted').html(description);
                        }
                    }

                    if (!isResult && hint) {
                        result.addClass('w-100');
                    }

                    let textClass = isResult ? null : 'text-truncate';
                    let textResult = $('<span/>').addClass(textClass).html(normalizeText(text));

                    if (color || imageUrl) {
                        let choice = $('<span/>')
                            .addClass('choice-item')
                            .addClass(textClass)
                            .appendTo(result);

                        // Image...
                        if (imageUrl) {
                            $('<img/>')
                                .attr('src', imageUrl)
                                .attr('alt', normalizeText(text))
                                .addClass('choice-item-img')
                                .appendTo(choice);
                        }
                        // ... or Color
                        else if (color) {
                            $('<span/>')
                                .addClass('choice-item-color')
                                .css('background-color', color)
                                .appendTo(choice);
                        }

                        // Text
                        choice.append(textResult);
                    }
                    else if (icon) {
                        // Icon
                        preResult = null;
                        let icons = _.isArray(icon) ? icon : [icon];
                        let len = icons.length;
                        for (i = 0; i < len; i++) {
                            let iconClass = icons[i] + " fa-fw mr-2 fs-h6";
                            $('<i/>')
                                .addClass(iconClass)
                                .css('font-size', '16px')
                                .appendTo(result);
                        }

                        // Text
                        result.append(textResult);
                    }
                    else {
                        // Text
                        result.append(textResult);
                    }

                    if (hint) {
                        // Hint/Badge
                        $('<span/>')
                            .addClass('option-hint ml-auto')
                            .append($('<span/>').addClass(hintClass).html(hint))
                            .appendTo(result);
                    }

                    if (preResult || postResult) {
                        return $(result
                            .wrap('<div/>')
                            .before(preResult)
                            .after(postResult)
                            .parent()
                            .html());
                    }
                    else {
                        return result;
                    }
                }
                catch (e) {
                    console.log(e);
                }

                return item.text;
            }

            var opts = {
                allowClear: !!placeholder, // assuming that a placeholder indicates nullability
                placeholder: placeholder,
                templateResult: (item) => {
                    return renderSelectItem(item, true);
                },
                templateSelection: (item) => {
                    return renderSelectItem(item, false);
                },
                closeOnSelect: !sel.prop('multiple'), //|| sel.data("tags"),
                adaptContainerCssClass: function (c) {
                    if (c?.startsWith("select-"))
                        return c;
                    else
                        return null;
                },
                adaptDropdownCssClass: function (c) {
                    if (c?.startsWith("drop-"))
                        return c;
                    else
                        return null;
                },
                matcher: function (params, data) {
                    var fallback = true;
                    var terms = $(data.element).data("terms");

                    if (terms) {
                        terms = _.isArray(terms) ? terms : [terms];
                        if (terms.length > 0) {
                            //fallback = false;
                            for (var i = 0; i < terms.length; i++) {
                                if (terms[i].indexOf(params.term) > -1) {
                                    return data;
                                }
                            }
                        }
                    }

                    if (fallback) {
                        return originalMatcher(params, data);
                    }

                    return null;
                }
            };

            if (sel.data('tags') && !sel.prop('multiple')) {
                // The search input field is required to be able to enter custom tags.
                opts.minimumResultsForSearch = 0;
            }

            if (!options.lazy && sel.data("select-url")) {
                opts.lazy = {
                    url: sel.data("select-url"),
                    loaded: sel.data("select-loaded")
                };
            }

            if (!options.init && sel.data("select-init-text") && sel.data("select-selected-id")) {
                opts.init = {
                    id: sel.data("select-selected-id"),
                    text: sel.data("select-init-text")
                };
            }

            if ($.isPlainObject(options)) {
                opts = $.extend({}, opts, options);
            }

            if (opts.lazy && opts.lazy.url) {
                if (opts.initLoad || sel.data('select-init-load')) {
                    initLoad(sel, opts.lazy.url);
                }

                // URL specified: load data remotely (lazily on first open)...
                opts.dataAdapter = $.fn.select2.amd.require('select2/data/lazyAdapter');
            }
            else if (sel.data('remote-url')) {
                opts.ajax = {};

                if (opts.initLoad || sel.data('select-init-load')) {
                    initLoad(sel, sel.data('remote-url'));
                }

                opts.dataAdapter = $.fn.select2.amd.require('select2/data/remoteAdapter');
            }
            else if (opts.ajax && opts.init && opts.init.text && sel.find('option[value="' + opts.init.text + '"]').length === 0) {
                // In AJAX mode: add initial option when missing
                sel.append('<option value="' + opts.init.id + '" selected>' + opts.init.text + '</option>');
            }

            sel.select2(opts);

            // Cancel event bubbling for unselect button in multiselect control.
            sel.on("select2:unselect", stopPropagation);

            if (sel.hasClass("autowidth")) {
                // move special "autowidth" class to plugin container,
                // so we are able to omit min-width per css
                sel.data("select2").$container.addClass("autowidth");
            }

            // WCAG.
            const $sel = sel.data("select2").$container.find('.select2-selection');
            if ($sel.length) {
                const labelledBy = sel.aria("labelledby");
                if (!_.isEmpty(labelledBy)) {
                    // Apply aria-labelledby attribute of the native select.
                    $sel.aria('labelledby', labelledBy);
                }
                else {
                    // Apply label text of native select (if any).
                    const id = sel.attr('id') || sel.attr('name');
                    const $elLabel = $('label[for="' + id + '"]');
                    if ($elLabel) {
                        $sel.aria('label', $elLabel.text())
                            .removeAttr('aria-labelledby');
                    }
                }

                // Remove role from rendered selection element. It is not editable.
                $sel.find('.select2-selection__rendered[role="textbox"]')
                    .removeAttr('role aria-readonly');
            }

            function getPlaceholder() {
                return options.placeholder ||
                    sel.attr("placeholder") ||
                    sel.data("placeholder") ||
                    sel.data("select-placeholder");
            }

            function stopPropagation(e) {
                if (!e.params.originalEvent) {
                    return;
                }

                e.params.originalEvent.stopPropagation();
            }
        });

    };

})(jQuery, window, document);
