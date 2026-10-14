// Smartstore-owned picker tab; the editor fork only supplies the generic tab API.
(function ($) {
    'use strict';
    const settings = document.currentScript.dataset;
    const sources = [...JSON.parse(settings.kits), ...JSON.parse(settings.libraries)];
    const ns = 'http://www.w3.org/2000/svg';

    $.summernote.plugins.iconPicker = function (context) {
        let disposed = false, request, timer;
        this.initialize = function () {
            const tabIcon = document.createElement('sm-icon');
            tabIcon.setAttribute('name', 'laugh');
            tabIcon.setAttribute('area', 'admin');
            context.invoke('emoji.addTab', {
                id: 'icons', label: settings.label, icon: tabIcon,
                render(container, picker) {
                    const $pane = $(container);
                    const $tools = $('<div class="d-flex align-items-center gap-2 mb-2 flex-shrink-0"></div>').appendTo($pane);
                    const $dropdown = $('<div class="dropdown"></div>').appendTo($tools);
                    const $toggle = $('<button type="button" class="btn btn-sm btn-light btn-icon dropdown-toggle no-chevron" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"></button>')
                        .attr('aria-label', settings.source).appendTo($dropdown);
                    const $menu = $('<div class="dropdown-menu"></div>').css({ maxHeight: '300px', overflowY: 'auto' }).appendTo($dropdown);
                    let source = sources.find(x => x.kit === 'public') || sources[0];
                    function updateSource() {
                        $toggle.empty().attr('title', source.label);
                        const icon = document.createElement('sm-icon');
                        icon.setAttribute('name', source.icon);
                        $toggle.append(icon);
                        $menu.children().each(function () { $(this).toggleClass('active', $(this).data('source') === source); });
                    }
                    let previousSource;
                    for (const entry of sources) {
                        if (previousSource?.kit && !entry.kit) {
                            $('<div class="dropdown-divider" role="separator"></div>').appendTo($menu);
                        }
                        previousSource = entry;
                        const $entry = $('<button type="button" class="dropdown-item"></button>').data('source', entry).appendTo($menu);
                        const icon = document.createElement('sm-icon');
                        icon.setAttribute('name', entry.icon);
                        $entry.append(icon, document.createTextNode(entry.label)).on('click', () => {
                            source = entry;
                            updateSource();
                            load(true);
                        });
                    }
                    updateSource();
                    const $search = $('<input type="search" class="form-control form-control-sm bg-secondary">')
                        .attr({ placeholder: settings.search, 'aria-label': settings.search }).css('min-width', 0).appendTo($tools);
                    const $sizes = $('<div class="btn-group btn-group-sm flex-shrink-0" role="group"></div>')
                        .attr('aria-label', settings.size).appendTo($tools);
                    let size = 24;
                    for (const value of [16, 24, 32]) {
                        $('<button type="button" class="btn btn-light px-2"></button>').text(value)
                            .toggleClass('active', value === size).attr('aria-pressed', String(value === size))
                            .appendTo($sizes).on('click', function () {
                                size = value;
                                $sizes.children().removeClass('active').attr('aria-pressed', 'false');
                                $(this).addClass('active').attr('aria-pressed', 'true');
                                resize();
                            });
                    }
                    const $scroll = $('<div class="overflow-auto scrollbar-thin flex-grow-1"></div>')
                        .css({ minHeight: 0, marginInlineEnd: 'calc(-1 * var(--note-emoji-padding))',
                            paddingInlineEnd: 'var(--note-emoji-padding)', scrollbarGutter: 'stable' }).appendTo($pane);
                    const $grid = $('<div class="d-grid"></div>').css({ gap: '4px', alignContent: 'start' }).appendTo($scroll);
                    const $status = $('<div class="text-muted small" role="status"></div>').appendTo($scroll);
                    const $more = $('<button type="button" class="btn btn-light btn-sm w-100 mt-2">…</button>')
                        .attr('aria-label', settings.more).appendTo($scroll).hide().on('click', () => load(false));
                    let page = 0, generation = 0, more = false, loading = false;
                    function resize() {
                        $grid.css('grid-template-columns', `repeat(auto-fill, minmax(${size + 16}px, 1fr))`);
                        $grid.children().css('height', size + 16).find('.icon, sm-icon').css('--icon-size', size + 'px');
                    }
                    function load(reset) {
                        if (disposed || !source || (!reset && loading)) return;
                        clearTimeout(timer);
                        if (reset) {
                            generation++;
                            request?.abort();
                            page = 0;
                            more = false;
                            $grid.empty();
                            $scroll.scrollTop(0);
                        }
                        const current = generation;
                        loading = true;
                        $more.hide();
                        $status.empty().addClass('spinner-border spinner-border-sm');
                        request = $.ajax({ url: settings.searchUrl, global: false, dataType: 'json', data: {
                            kit: source.kit, lib: source.library, variant: source.variant,
                            term: $search.val().trim(), page: page + 1
                        }}).done(data => {
                            if (disposed || current !== generation) return;
                            page++;
                            more = data.pagination.more;
                            for (const item of data.results) {
                                const $button = $('<button type="button" class="btn btn-plain d-flex align-items-center justify-content-center p-2"></button>')
                                    .attr({ title: item.text, 'aria-label': item.text }).appendTo($grid);
                                let icon;
                                if (item.inlineName) {
                                    icon = document.createElement('sm-icon');
                                    icon.setAttribute('name', item.inlineName);
                                    icon.setAttribute('area', 'public');
                                } else {
                                    icon = document.createElementNS(ns, 'svg');
                                    const use = document.createElementNS(ns, 'use');
                                    use.setAttribute('href', data.spriteUrl + '#' + encodeURIComponent(item.text));
                                    icon.append(use);
                                }
                                icon.setAttribute('class', `icon icon-${item.library} icon-${item.library}-${item.variant}`);
                                if (item.isDuotone) icon.classList.add('icon-duotone');
                                icon.setAttribute('aria-hidden', 'true');
                                $button.append(icon).on('click', () => {
                                    // Inert markup avoids upgrading a temporary component before insertion.
                                    const template = document.createElement('template');
                                    template.innerHTML = '<sm-icon></sm-icon>';
                                    template.content.firstChild.setAttribute('name', item.id);
                                    picker.insertHtml(template.innerHTML);
                                });
                            }
                            resize();
                            $status.text($grid.children().length ? '' : settings.empty);
                        }).fail((xhr, reason) => {
                            if (!disposed && current === generation && reason !== 'abort') $status.text(settings.error);
                        }).always(() => {
                            if (disposed || current !== generation) return;
                            loading = false;
                            $status.removeClass('spinner-border spinner-border-sm');
                            $more.toggle(more);
                        });
                    }
                    $search.on('input', () => {
                        // Invalidate in-flight results immediately, before the next debounced request.
                        generation++;
                        request?.abort();
                        loading = false;
                        more = false;
                        $more.hide();
                        $grid.empty();
                        clearTimeout(timer);
                        timer = setTimeout(() => load(true), 200);
                    });
                    $scroll.on('scroll', () => {
                        const el = $scroll[0];
                        if (more && !loading && el.scrollTop + el.clientHeight >= el.scrollHeight - 40) load(false);
                    });
                    load(true);
                },
                activate(container) {
                    $(container).find('input[type=search]').trigger('focus');
                },
                destroy() {
                    disposed = true;
                    clearTimeout(timer);
                    request?.abort();
                }
            });
        };
    };
})(jQuery);
