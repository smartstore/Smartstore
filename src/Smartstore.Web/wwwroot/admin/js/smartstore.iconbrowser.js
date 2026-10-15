// AJAX pages carry metadata only; all ordinary previews share one external sprite.
(function ($) {
    'use strict';
    const ns = 'http://www.w3.org/2000/svg';

    $.fn.iconBrowser = function () {
        return this.each(function () {
            const root = $(this);
            if (root.data('iconBrowser')) return;
            const select = root.find('.icon-browser-select');
            const button = root.find('.icon-browser-source-dropdown > button');
            let options = Object.create(null);
            // Razor provides the canonical presentation. Decode it once, never during slider input.
            const aliases = { anim: 'animation', 'anim-dur': 'animation-duration', 'anim-rev': 'animation-reverse' };
            const tokens = { animation: 'anim', 'animation-duration': 'anim-dur', 'animation-reverse': 'anim-rev' };
            for (const pair of (root.attr('data-icon-presentation') || '').slice(1).split('&')) {
                if (!pair) continue;
                const equals = pair.indexOf('=');
                const key = equals < 0 ? pair : pair.slice(0, equals);
                const value = equals < 0 ? 'true' : decodeURIComponent(pair.slice(equals + 1));
                if (key === 'flip') {
                    options['flip-h'] = String(value.includes('x'));
                    options['flip-v'] = String(value.includes('y'));
                } else {
                    options[aliases[key] || key] = value;
                }
            }
            const toolbar = root.find('.icon-browser-toolbar');
            const expressionField = root.find('.icon-browser-expression');
            let supportsStrokeScaling = false;
            let selectedAddress = select.val() || '';
            const pending = new Set();

            function updateExpression() {
                const address = select.val() || '';
                const modifiers = [];
                for (const [option, raw] of Object.entries(options)) {
                    const key = tokens[option] || option;
                    if (['flip-h', 'flip-v', 'fw', 'inverse', 'mirror-rtl', 'animation-reverse'].includes(option)) {
                        modifiers.push(raw === 'true' ? key : key + '=false');
                        continue;
                    }
                    // Match IconPresentation: keep CSS colors readable, escape query delimiters only.
                    const value = raw.replace(/[%&=?]/g, c => ({ '%': '%25', '&': '%26', '=': '%3D', '?': '%3F' })[c]);
                    modifiers.push(key + '=' + value);
                }
                expressionField.val(address ? address + (modifiers.length ? '?' + modifiers.join('&') : '') : '').trigger('change');
            }

            function setOption(key, value) {
                value = value == null ? '' : String(value);
                if (key === 'stroke-scale') value = value.replace(',', '.');
                if (value === (options[key] || '')) return;
                if (value === '') delete options[key];
                else options[key] = value;
                updateExpression();
                select.trigger('change.select2');
            }

            // Consumers receive a snapshot; the tools own the mutable state.
            root.data('iconBrowser', {
                getState: () => ({ name: selectedAddress, options: { ...options }, expression: expressionField.val() || '' })
            });

            function updateToolbarState() {
                toolbar.find('[data-option], [data-more]').prop('disabled', !select.val()).attr('aria-disabled', String(!select.val()));
                toolbar.find('[data-option="stroke-scale"]').toggleClass('d-none', !supportsStrokeScaling);
                toolbar.find('.icon-tool-color-swatch').css('background-color', options.color || '#fff');
                let overflowChanged = false;
                toolbar.find('[data-option]').each(function () {
                    const value = options[this.dataset.option];
                    const changed = value != null;
                    const active = changed && (!this.dataset.option.startsWith('flip-') || value === 'true');
                    $(this).toggleClass('active', !!active);
                    if (this.hasAttribute('aria-pressed')) this.setAttribute('aria-pressed', active ? 'true' : 'false');
                    if (changed && !this.classList.contains('d-none') && this.hasAttribute('data-overflow')) overflowChanged = true;
                });
                toolbar.find('[data-more]').toggleClass('active', overflowChanged)
                    .toggleClass('d-none', !toolbar.find('[data-overflow]:not(.d-none)').length);
            }

            function applyPreviewOptions(icon) {
                for (const [option, value] of Object.entries(options)) {
                    // Keep the picker stable; size and animation belong to a separate options preview.
                    if (option === 'size' || option === 'animation' || (option === 'stroke-scale' && !supportsStrokeScaling)) continue;
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
                    for (const index of data.external || []) data.results[index].isExternal = true;
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
                const hasPreviewOptions = !isResult && Object.keys(options).some(key =>
                    key !== 'size' && key !== 'animation' && (key !== 'stroke-scale' || supportsStrokeScaling));
                if (!item.spriteUrl || item.inlineName || hasPreviewOptions) {
                    // Preserve the chosen source even when its kit belongs to another area.
                    // Unmodified selections and result tiles use that kit's sprite directly.
                    icon = document.createElement('sm-icon');
                    icon.setAttribute('name', isResult ? item.inlineName : (item.previewAddress || item.id));
                    if (!isResult) applyPreviewOptions(icon);
                } else {
                    icon = document.createElementNS(ns, 'svg');
                    const use = document.createElementNS(ns, 'use');
                    use.setAttribute('href', item.spriteUrl + '#' + encodeURIComponent(item.text));
                    icon.append(use);
                }
                icon.setAttribute('class', 'icon');
                if (item.isDuotone) icon.classList.add('icon-duotone');
                if (item.library && item.variant) {
                    icon.classList.add(`icon-${item.library}`, `icon-${item.library}-${item.variant}`);
                }
                if (!isResult) icon.classList.add('icon-fw', 'mr-2');
                if (isResult && item.address) icon.setAttribute('data-icon', item.address);
                icon.setAttribute('aria-hidden', 'true');
                const label = document.createElement('span');
                label.className = isResult ? 'text-truncate w-100 fs-xs lh-sm text-center' : 'text-truncate';
                label.textContent = item.text;
                row.title = item.text;
                if (root.attr('data-show-provider-indicators') === 'true' && (item.isExternal ?? item.element?.dataset.external === 'true')) {
                    const hint = root.attr('data-external-label');
                    if (isResult) {
                        const dot = document.createElement('span');
                        row.classList.add('position-relative');
                        dot.className = 'position-absolute rounded-circle bg-warning';
                        dot.style.cssText = 'width:6px;height:6px;top:-2px;left:calc(50% + var(--icon-browser-size, 24px) / 2 + 1px)';
                        dot.setAttribute('role', 'img');
                        dot.setAttribute('aria-label', hint);
                        dot.title = hint;
                        row.append(dot);
                    }
                }
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
                const item = data[0];
                supportsStrokeScaling = !!item?.id && (item.supportsStrokeScaling ?? item.element?.dataset.supportsStrokeScaling === 'true');
                updateToolbarState();
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
                options = Object.create(null);
                updateExpression();
                updateToolbarState();
                select.trigger('change.select2');
            });
            toolbar.find('[data-option="flip-h"], [data-option="flip-v"]').each(function () {
                const key = this.dataset.option;
                Smartstore.IconTools.bindFlip(this, {
                    getState: () => ({ name: selectedAddress, value: options[key] }),
                    onChange(value) { setOption(key, value); }
                });
            });

            const strokeToggle = toolbar.find('[data-option="stroke-scale"]')[0];
            if (strokeToggle) {
                const strokeTool = Smartstore.IconTools.bindStrokeScale(strokeToggle, {
                    showPreview: true,
                    getState() {
                        if (!supportsStrokeScaling) return null;
                        const previewOptions = {};
                        for (const key of ['flip-h', 'flip-v', 'rotate']) {
                            if (options[key] != null) previewOptions[key] = options[key];
                        }
                        return { name: select.select2('data')[0]?.previewAddress || select.val(), options: previewOptions,
                            value: options['stroke-scale'] };
                    },
                    onChange(value) { setOption('stroke-scale', value); }
                });
                select.on('change.iconTools select2:opening', () => strokeTool.close());
            }

            const rotateToggle = toolbar.find('[data-option="rotate"]')[0];
            if (rotateToggle) {
                const rotateTool = Smartstore.IconTools.bindRotate(rotateToggle, {
                    showPreview: true,
                    getState() {
                        const previewOptions = {};
                        for (const key of ['flip-h', 'flip-v', 'stroke-scale']) {
                            if (options[key] != null) previewOptions[key] = options[key];
                        }
                        return { name: select.select2('data')[0]?.previewAddress || select.val(), options: previewOptions,
                            value: options['rotate'] };
                    },
                    onChange(value) { setOption('rotate', value); }
                });
                select.on('change.iconTools select2:opening', () => rotateTool.close());
            }

            const sizeToggle = toolbar.find('[data-option="size"]')[0];
            if (sizeToggle) {
                const sizeTool = Smartstore.IconTools.bindSize(sizeToggle, {
                    getState: () => ({ name: select.val(), value: options['size'] }),
                    onChange(value) { setOption('size', value); }
                });
                select.on('change.iconTools select2:opening', () => sizeTool.close());
            }

            const colorToggle = toolbar.find('[data-option="color"]')[0];
            if (colorToggle) {
                const colorTool = Smartstore.IconTools.bindColor(colorToggle, {
                    getState: () => ({ name: select.val(), value: options.color }),
                    onChange(value) {
                        setOption('color', value);
                    }
                });
                select.on('change.iconTools select2:opening', () => colorTool.close());
            }

            const animationToggle = toolbar.find('[data-option="animation"]')[0];
            if (animationToggle) {
                const animationTool = Smartstore.IconTools.bindAnimation(animationToggle, {
                    noneLabel: animationToggle.dataset.noneLabel,
                    getState() {
                        const item = select.select2('data')[0];
                        const previewOptions = {};
                        for (const [key, value] of Object.entries(options)) {
                            if (key === 'size' || key === 'animation' || (key === 'stroke-scale' && !supportsStrokeScaling)) continue;
                            previewOptions[key] = value;
                        }
                        return {
                            name: item?.previewAddress || select.val(), options: previewOptions,
                            value: options['animation']
                        };
                    },
                    onChange(value) {
                        setOption('animation', value);
                    }
                });
                select.on('change.iconTools select2:opening', () => animationTool.close());
            }

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
