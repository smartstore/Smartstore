// AJAX pages carry metadata only; all ordinary previews share one external sprite.
(function ($) {
    'use strict';
    const ns = 'http://www.w3.org/2000/svg';

    $.fn.iconBrowser = function () {
        return this.each(function () {
            const root = $(this);
            if (root.data('iconBrowser')) return;
            root.data('iconBrowser', true);
            const select = root.find('.icon-browser-select');
            const button = root.find('.icon-browser-source-dropdown > button');
            const optionFields = root.find('[data-icon-option]');
            const toolbar = root.find('.icon-browser-toolbar');
            let selectedAddress = select.val() || '';
            const pending = new Set();

            function updateToolbarState() {
                let overflowChanged = false;
                toolbar.find('[data-option]').each(function () {
                    const field = optionFields.toArray().find(field => field.dataset.iconOption === this.dataset.option);
                    const changed = field && field.value !== '';
                    const active = changed && (!this.dataset.option.startsWith('flip-') || field.value.toLowerCase() === 'true');
                    $(this).toggleClass('active', !!active);
                    if (this.hasAttribute('aria-pressed')) this.setAttribute('aria-pressed', active ? 'true' : 'false');
                    if (changed && this.hasAttribute('data-overflow')) overflowChanged = true;
                });
                toolbar.find('[data-more]').toggleClass('active', overflowChanged);
            }

            function applyPreviewOptions(icon) {
                for (const field of optionFields) {
                    const option = field.dataset.iconOption;
                    // Keep the picker stable; size and animation belong to a separate options preview.
                    if (field.value === '' || option === 'size' || option === 'animation') continue;
                    let value = field.value;
                    if (option.startsWith('flip-')) value = value.toLowerCase();
                    // Hidden MVC decimal values follow the form culture; SVG queries use a decimal point.
                    if (option === 'stroke-scale') value = value.replace(',', '.');
                    icon.setAttribute(option, value);
                }
            }
            updateToolbarState();
            let source, generation = 0, firstPage;

            function request(term, page, success, failure) {
                if (!source) {
                    queueMicrotask(() => success({ results: [], pagination: { more: false } }));
                    return { abort() {} };
                }
                const version = generation;
                const xhr = $.ajax({
                    url: root.data('search-url'), global: false, dataType: 'json',
                    data: { ...source, term, page }
                });
                pending.add(xhr);
                xhr.done(data => {
                    if (version !== generation) return;
                    // Store the shared URL on each page item locally, not repeatedly on the wire.
                    for (const item of data.results) item.spriteUrl = data.spriteUrl;
                    success(data);
                }).fail((xhr, reason) => {
                    if (version === generation && reason !== 'abort') failure(xhr);
                }).always(() => pending.delete(xhr));
                return xhr;
            }

            function format(item, isResult) {
                if (!item.id || item.loading) return item.text;
                const row = document.createElement('span');
                row.className = isResult
                    ? 'icon-browser-choice d-flex flex-column align-items-center justify-content-center gap-1'
                    : 'select2-option w-100 align-items-center';
                let icon;
                if (!isResult || item.inlineName) {
                    // The selection renders its original address and persisted overrides;
                    // result tiles retain the shared sprite optimization.
                    icon = document.createElement('sm-icon');
                    icon.setAttribute('name', isResult ? item.inlineName : item.id);
                    if (!isResult) applyPreviewOptions(icon);
                } else {
                    icon = document.createElementNS(ns, 'svg');
                    const use = document.createElementNS(ns, 'use');
                    use.setAttribute('href', item.spriteUrl + '#' + encodeURIComponent(item.text));
                    icon.append(use);
                }
                icon.setAttribute('class', 'icon');
                if (isResult && item.library && item.variant) {
                    icon.classList.add(`icon-${item.library}`, `icon-${item.library}-${item.variant}`);
                }
                if (!isResult) icon.classList.add('icon-fw', 'mr-2');
                if (isResult && item.address) icon.setAttribute('data-icon', item.address);
                icon.setAttribute('aria-hidden', 'true');
                const label = document.createElement('span');
                label.className = isResult ? 'text-truncate w-100 fs-xs lh-sm text-center' : 'text-truncate';
                label.textContent = item.text;
                row.title = item.text;
                if (isResult) {
                    row.append(icon, label);
                } else {
                    const choice = document.createElement('span');
                    choice.className = 'choice-item text-truncate';
                    choice.append(icon, label);
                    row.append(choice);
                }
                return $(row);
            }

            // Decorate the regular single selection so the toolbar survives Select2 updates.
            // Keep the standard placeholder, clear button and relayed selection events.
            const amd = $.fn.select2.amd;
            const utils = amd.require('select2/utils');
            let selectionAdapter = amd.require('select2/selection/single');
            for (const decorator of ['placeholder', 'allowClear', 'eventRelay']) {
                selectionAdapter = utils.Decorate(selectionAdapter, amd.require('select2/selection/' + decorator));
            }
            function ToolbarSelection() {}
            ToolbarSelection.prototype.update = function (decorated, data) {
                toolbar.detach();
                decorated.call(this, data);
                if (toolbar.length) {
                    this.$selection.addClass('icon-browser-selection-with-tools d-flex align-items-center');
                    this.$selection.find('.select2-selection__clear').addClass('flex-shrink-0 ml-2');
                    this.$selection.find('.select2-selection__rendered').addClass('w-100').append(toolbar.removeClass('d-none'));
                }
            };
            selectionAdapter = utils.Decorate(selectionAdapter, ToolbarSelection);
            toolbar.on('mousedown click dblclick', event => event.stopPropagation());
            toolbar.on('keydown', event => {
                event.stopPropagation();
                if (event.key === 'Enter' || event.key === ' ') event.preventDefault();
            });

            select.select2({
                selectionAdapter,
                width: '100%', allowClear: true, minimumInputLength: 0, minimumResultsForSearch: 0,
                placeholder: select.data('placeholder'),
                dropdownCssClass: 'icon-browser-dropdown',
                templateResult: item => format(item, true),
                templateSelection: item => format(item, false),
                ajax: {
                    delay: 250,
                    data: params => ({ term: params.term || '', page: params.page || 1 }),
                    transport: (params, success, failure) => {
                        if (!params.data.term && params.data.page === 1 && firstPage) {
                            const cachedPage = firstPage, version = generation;
                            let aborted = false;
                            // Keep cached responses asynchronous, too: Select2 must finish
                            // updating its query and pagination state before results arrive.
                            queueMicrotask(() => {
                                if (!aborted && version === generation) success(cachedPage);
                            });
                            return { abort() { aborted = true; } };
                        }
                        return request(params.data.term, params.data.page, success, failure);
                    },
                    processResults: data => data
                }
            });

            select.on('change.iconBrowser', function () {
                const address = select.val() || '';
                if (address === selectedAddress) return;
                selectedAddress = address;
                // Reset even options whose controls are hidden. Null restores mapping defaults.
                optionFields.val('');
                updateToolbarState();
                select.trigger('change.select2');
            });
            optionFields.on('change.iconBrowser', function () {
                updateToolbarState();
                select.trigger('change.select2');
            });

            // Select2 detaches its dropdown from the picker. Keep the size variable and
            // controls on that instance's dropdown so multiple browsers remain independent.
            const dropdown = select.data('select2').$dropdown.find('.icon-browser-dropdown');
            const sizes = $('<div class="btn-group btn-group-sm flex-shrink-0" role="group"></div>')
                .attr('aria-label', root.data('size-label'));
            for (const size of [16, 24, 32]) {
                $('<button type="button" class="btn btn-secondary"></button>')
                    .text(size).attr({ 'data-size': size, title: `${size} px`, 'aria-pressed': size === 24 ? 'true' : 'false' })
                    .toggleClass('active', size === 24).appendTo(sizes);
            }
            const search = dropdown.find('.select2-search--dropdown');
            search.css({ display: 'flex', alignItems: 'center', gap: '0.5rem' }).append(sizes);
            search.find('input').css({ flex: 1, minWidth: 0 });
            sizes.on('mousedown', event => event.preventDefault());
            sizes.on('click', 'button', function () {
                dropdown.css('--icon-browser-size', `${this.dataset.size}px`);
                sizes.find('button').removeClass('active').attr('aria-pressed', 'false');
                $(this).addClass('active').attr('aria-pressed', 'true');
            });

            function choose(option, open) {
                const version = ++generation;
                for (const xhr of pending) xhr.abort();
                pending.clear();
                firstPage = null;
                select.select2('close');
                // Browsing another source never changes the bound selection or its options.
                open = open && !select.val();
                source = option.dataset.kit ? { kit: option.dataset.kit }
                    : { lib: option.dataset.lib, variant: option.dataset.variant };
                const label = option.dataset.label;
                button.attr('title', option.title).find('.icon-browser-source-label').text(option.dataset.buttonLabel || label);
                // Reuse the server-rendered option icon without another resolution request.
                button.children('svg.icon').remove();
                $(option).children('svg.icon').first().clone().prependTo(button);
                root.find('.dropdown-item').removeClass('active').removeAttr('aria-current');
                $(option).addClass('active').attr('aria-current', 'true');
                root.attr('aria-busy', 'true');
                request('', 1, data => {
                    firstPage = data;
                    root.attr('aria-busy', 'false');
                    // Do not interrupt a selection made while this source was loading.
                    if (open) queueMicrotask(() => {
                        if (version === generation && !select.val()) select.select2('open');
                    });
                }, () => {
                    root.attr('aria-busy', 'false');
                });
            }

            root.on('click', '.icon-browser-source-menu .dropdown-item', function () { choose(this, true); });
            const initial = root.find('[data-initial="true"]')[0];
            if (initial) choose(initial, false);
        });
    };

    $(function () { $('.icon-browser').iconBrowser(); });
})(jQuery);
