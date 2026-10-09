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
            const button = root.find('.icon-browser-source > button');
            const pending = new Set();
            let source, generation = 0, firstPage;

            function request(term, page, success, failure) {
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

            function format(item) {
                if (!item.id || item.loading) return item.text;
                const row = document.createElement('span');
                row.className = 'icon-browser-choice';
                let icon;
                if (item.inlineName) {
                    // Only mapping stroke multipliers need the shared component's inline fallback.
                    icon = document.createElement('sm-icon');
                    icon.setAttribute('name', item.inlineName);
                } else {
                    icon = document.createElementNS(ns, 'svg');
                    const use = document.createElementNS(ns, 'use');
                    use.setAttribute('href', item.spriteUrl + '#' + encodeURIComponent(item.text));
                    icon.append(use);
                }
                icon.setAttribute('class', `icon icon-${item.library} icon-${item.library}-${item.variant}`);
                icon.setAttribute('data-icon', item.address);
                icon.setAttribute('aria-hidden', 'true');
                const label = document.createElement('span');
                label.className = 'text-truncate';
                label.textContent = item.text;
                row.title = item.text;
                row.append(icon, label);
                return $(row);
            }

            select.select2({
                width: '100%', allowClear: true, minimumInputLength: 0, minimumResultsForSearch: 0,
                placeholder: select.data('placeholder'),
                dropdownCssClass: 'icon-browser-dropdown',
                templateResult: format, templateSelection: format,
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

            // Select2 detaches its dropdown from the picker. Keep the size variable and
            // controls on that instance's dropdown so multiple browsers remain independent.
            const dropdown = select.data('select2').$dropdown.find('.icon-browser-dropdown');
            const sizes = $('<div class="btn-group btn-group-sm flex-shrink-0" role="group" aria-label="Icon size"></div>');
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
                select.empty().append(new Option('', '')).val(null).prop('disabled', true).trigger('change');
                source = option.dataset.kit ? { kit: option.dataset.kit }
                    : { lib: option.dataset.lib, variant: option.dataset.variant };
                const label = option.dataset.label;
                const title = (option.dataset.kit ? 'Kit: ' : 'Library: ') + label;
                button.attr('title', title).find('.icon-browser-source-label').text(option.dataset.buttonLabel || label);
                // Reuse the server-rendered option icon without another resolution request.
                button.children('svg.icon').remove();
                $(option).children('svg.icon').first().clone().prependTo(button);
                root.find('.dropdown-item').removeClass('active').removeAttr('aria-current');
                $(option).addClass('active').attr('aria-current', 'true');
                root.attr('aria-busy', 'true');
                request('', 1, data => {
                    firstPage = data;
                    root.attr('aria-busy', 'false');
                    select.prop('disabled', false);
                    // Select2 observes the disabled attribute asynchronously. Open only
                    // after that observer has applied the new enabled state.
                    if (open) queueMicrotask(() => {
                        if (version === generation) select.select2('open');
                    });
                }, () => {
                    root.attr('aria-busy', 'false');
                });
            }

            root.on('click', '.icon-browser-sources .dropdown-item', function () { choose(this, true); });
            const initial = root.find('[data-initial="true"]')[0] || root.find('.dropdown-item')[0];
            if (initial) choose(initial, false);
        });
    };

    $(function () { $('.icon-browser').iconBrowser(); });
})(jQuery);
