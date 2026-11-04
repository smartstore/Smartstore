(function ($) {
    'use strict';

    // Placement rules are independent of the DOM; both preview and history use these rectangles.
    Smartstore.Admin.DashboardEngine = class {
        static resizeStart(position, column, minWidth, maxWidth, widths) {
            const end = position.column + position.size.columnSpan;
            let width = Math.max(Math.min(minWidth, end), Math.min(maxWidth, end, end - column));
            if (widths) width = widths.filter(value => value <= end).reduce((best, value) =>
                Math.abs(value - width) < Math.abs(best - width) ? value : best, position.size.columnSpan);
            position.column = end - width;
            position.size.columnSpan = width;
        }

        // Split only on drop. Until then the shortened card still occupies its original grid area.
        static insertBelow(items, anchorId, changedId, column, height = 0, splitRow = null) {
            const anchor = items.find(item => item.id === anchorId);
            const changed = items.find(item => item.id === changedId);
            if (!anchor || !changed || changed.locked || anchor.stretch !== false) return null;
            const width = anchor.size.columnSpan;
            const fill = changed.resizable && width >= (changed.minSize?.columnSpan ?? 1) &&
                width <= (changed.maxSize?.columnSpan ?? 12) &&
                (!changed.allowedSizes?.length || changed.allowedSizes.some(size =>
                    size.columnSpan === width && size.rowSpan === changed.size.rowSpan));
            if (column < anchor.column || (!fill && column + changed.size.columnSpan > anchor.column + width)) return null;
            const end = anchor.row + (anchor.size.rowSpan || 1);
            const row = splitRow ?? end;
            const count = changed.size.rowSpan || 1;
            const reuse = row < end;
            if (reuse && (!anchor.resizable || row <= anchor.row || row + count > end ||
                row - anchor.row < (anchor.minSize?.rowSpan ?? 1) ||
                (anchor.allowedSizes?.length && !anchor.allowedSizes.some(size =>
                    size.columnSpan === width && size.rowSpan === row - anchor.row)))) return null;
            const proposed = items.map(item => ({ ...item, size: { ...item.size } }));
            for (const item of proposed) {
                if (item.id === changedId) {
                    item.column = column;
                    item.row = row;
                    if (item.resizable) {
                        item.stretch = true;
                        item.minHeight = height;
                        if (fill) {
                            item.column = anchor.column;
                            item.size.columnSpan = width;
                        }
                    }
                }
                else if (reuse) {
                    if (item.id === anchorId) {
                        item.size.rowSpan = row - anchor.row;
                        item.stretch = true;
                    }
                    else if (item.column < (fill ? anchor.column + width : column + changed.size.columnSpan) &&
                        item.column + item.size.columnSpan > (fill ? anchor.column : column) &&
                        item.row < row + count && item.row + (item.size.rowSpan || 1) > row) return null;
                }
                else if (item.row >= row) {
                    if (item.locked) return null;
                    item.row += count;
                }
                else if (item.id !== anchorId && item.row + (item.size.rowSpan || 1) >= row) {
                    const span = (item.size.rowSpan || 1) + count;
                    if (!item.resizable || span > (item.maxSize?.rowSpan ?? 1000) || span < (item.minSize?.rowSpan ?? 1) ||
                        item.size.columnSpan < (item.minSize?.columnSpan ?? 1) || item.size.columnSpan > (item.maxSize?.columnSpan ?? 12) ||
                        (item.allowedSizes?.length && !item.allowedSizes.some(size =>
                            size.columnSpan === item.size.columnSpan && size.rowSpan === span))) return null;
                    item.size.rowSpan = span;
                    item.stretch = true;
                }
            }
            return proposed;
        }

        static arrange(items, columns, changedId) {
            const overlap = (a, b) => a.column < b.column + b.size.columnSpan && b.column < a.column + a.size.columnSpan &&
                a.row < b.row + (b.size.rowSpan || 1) && b.row < a.row + (a.size.rowSpan || 1);
            const ordered = [...items].sort((a, b) => Number(!!b.locked) - Number(!!a.locked) ||
                Number(b.id === changedId) - Number(a.id === changedId) || a.row - b.row || a.column - b.column);
            const placed = [];
            for (const item of ordered) {
                if (!item.locked) {
                    item.size.columnSpan = Math.min(columns, item.size.columnSpan);
                    item.column = Math.max(0, Math.min(columns - item.size.columnSpan, item.column));
                    item.row = Math.max(0, item.row);
                    while (placed.some(other => overlap(item, other))) item.row++;
                }
                placed.push(item);
            }
            // Remove empty rows as a group, retaining shared rows rather than packing individual cards upward.
            const rows = [...new Set(items.map(item => item.row))].sort((a, b) => a - b);
            let bottom = 0;
            for (const row of rows) {
                const group = items.filter(item => item.row === row);
                const target = group.some(item => item.locked) ? row : Math.min(row, bottom);
                group.forEach(item => { item.row = target; });
                bottom = Math.max(bottom, ...group.map(item => item.row + (item.size.rowSpan || 1)));
            }
            return items;
        }
    };

    $(function () {
        const root = document.getElementById('dashboard-editor');
        if (root?.dataset.editing !== 'true') return;
        const config = JSON.parse(document.getElementById(root.dataset.editorConfig).textContent);
        const container = document.getElementById(config.layout.id + '-grid');
        const scrollContainer = document.getElementById('content');
        const form = document.getElementById('dashboard-draft-form');
        const elements = new Map();
        const breakpoints = [...new Set([0, 768, 992, 1600,
            ...config.layout.widgets.flatMap(widget => widget.positions.map(p => p.minViewportWidth))])].sort((a, b) => a - b);
        let layout;
        let widgetsById;
        function setLayout(value) {
            layout = value;
            widgetsById = new Map(layout.widgets.map(widget => [widget.id, widget]));
        }
        setLayout(structuredClone(config.layout));
        let dirty = config.dirty;
        let reset = config.reset;
        let leaving = false;
        let pending;
        let interaction;
        let undo = [];
        let redo = [];
        const currentBreakpoint = () => breakpoints.filter(width => width <= window.innerWidth).at(-1);
        let breakpoint = currentBreakpoint();
        const position = (widget, width = breakpoint) => widget.positions.filter(p => p.minViewportWidth <= width)
            .sort((a, b) => b.minViewportWidth - a.minViewportWidth)[0];
        container.querySelectorAll('[data-instance-id]').forEach(el => {
            elements.set(el.dataset.instanceId, el);
            if (el.querySelector('.dashboard-widget-overlay')) {
                el.querySelectorAll('.dashboard-widget-body > :not(.dashboard-widget-tools):not(.dashboard-resize)').forEach(content => {
                    content.inert = true;
                });
            }
        });
        const renderedIds = new Set(elements.keys());
        const unavailableIds = new Set(layout.widgets.filter(widget => !renderedIds.has(widget.id)).map(widget => widget.id));
        for (const widget of layout.widgets) {
            if (renderedIds.has(widget.id)) {
                widget.positions = breakpoints.map(width => ({ ...structuredClone(position(widget, width)), minViewportWidth: width }));
            }
        }
        const visible = () => layout.widgets.filter(widget => renderedIds.has(widget.id));
        const isNews = widget => elements.get(widget.id).classList.contains('news-feed');
        const columns = () => breakpoint >= 992 && visible().some(isNews) ? layout.columnCount - 2 : layout.columnCount;
        function bounds(widget) {
            const el = elements.get(widget.id);
            const available = widget.allowMove ? columns() : columns() - position(widget).column;
            const max = breakpoint === 0 ? layout.columnCount : Math.min(available, Number(el.dataset.maxWidth));
            const min = breakpoint === 0 ? max : Math.min(max, Number(el.dataset.minWidth));
            const allowed = config.widgets.find(choice => choice.systemName === widget.widgetSystemName)?.allowedSizes;
            const widths = Array.from({ length: max - min + 1 }, (_, index) => min + index)
                .filter(width => !allowed?.length || allowed.some(size =>
                    size.columnSpan === width && size.rowSpan === position(widget).size.rowSpan));
            return { min, max, widths };
        }
        function arrange(changedId) {
            const items = visible().filter(widget => !isNews(widget)).map(widget => ({
                ...structuredClone(position(widget)), id: widget.id, locked: !widget.allowMove
            }));
            Smartstore.Admin.DashboardEngine.arrange(items, columns(), changedId);
            for (const item of items) {
                const p = position(widgetsById.get(item.id));
                p.column = item.column; p.row = item.row; p.size = item.size;
            }
        }
        function render(updateToolbar = true) {
            for (const [id, el] of elements) {
                const widget = widgetsById.get(id);
                if (!widget) { el.remove(); continue; }
                if (!el.isConnected) container.appendChild(el);
                const p = position(widget);
                el.style.gridColumn = `${p.column + 1} / span ${p.size.columnSpan}`;
                el.style.gridRow = `${p.row + 1} / span ${p.size.rowSpan || 1}`;
                el.style.minHeight = `${p.minHeight || 0}px`;
                el.style.alignSelf = p.stretch === false ? 'start' : 'stretch';
                if (isNews(widget)) {
                    const bottom = Math.max(0, ...visible().filter(w => !isNews(w)).map(w => {
                        const positionAtWidth = position(w);
                        return positionAtWidth.row + (positionAtWidth.size.rowSpan || 1);
                    }));
                    el.style.gridRow = breakpoint >= 992 ? `1 / span ${Math.max(1, bottom)}` : `${bottom + 1} / span 1`;
                }
            }
            if (updateToolbar) updateControls();
        }
        // Geometry is already stored in the draft; never infer heights from stretched grid cells.
        function capture() { return structuredClone({ layout, reset, dirty }); }
        function remember(state, preview = false) {
            if (!preview && _.isEqual(state.layout, layout) && state.reset === reset) return;
            undo.push(state);
            if (undo.length > 40) undo.shift();
            redo.length = 0;
            dirty = true;
            reset = false;
            updateControls();
        }
        function updateControls() {
            $('#dashboard-undo').prop('disabled', !undo.length);
            $('#dashboard-redo').prop('disabled', !redo.length);
            $('.dashboard-status').toggleClass('d-none', !dirty && !reset);
            $('#dashboard-reset-status').toggleClass('d-none', !reset);
            $('#dashboard-dirty-status').toggleClass('d-none', !dirty || reset);
            document.querySelectorAll('[data-add-widget]').forEach(button => {
                const descriptor = config.widgets.find(w => w.systemName === button.dataset.addWidget);
                button.disabled = !descriptor?.allowMultipleInstances && layout.widgets.some(w => w.widgetSystemName === button.dataset.addWidget);
            });
        }

        function start(event, widget, kind) {
            if (interaction || (event.button !== undefined && event.button !== 0)) return;
            const el = elements.get(widget.id);
            const rect = el.getBoundingClientRect();
            const gridRect = container.getBoundingClientRect();
            const style = getComputedStyle(container);
            const gap = parseFloat(style.columnGap) || 0;
            const tracks = style.gridTemplateColumns.split(' ').map(Number.parseFloat);
            const direction = style.direction === 'rtl' ? -1 : 1;
            const starts = [direction === 1 ? gridRect.left : gridRect.right];
            tracks.forEach(width => starts.push(starts.at(-1) + direction * (width + gap)));
            const rowGap = parseFloat(style.rowGap) || 0;
            let top = gridRect.top;
            const rows = kind === 'move' ? style.gridTemplateRows.split(' ').map((value, row) => {
                const bottom = top + parseFloat(value);
                const track = { row, top, bottom };
                top = bottom + rowGap;
                return track;
            }) : [];
            const items = kind === 'move' ? visible().filter(w => !isNews(w)).map(w => {
                const element = elements.get(w.id);
                const descriptor = config.widgets.find(choice => choice.systemName === w.widgetSystemName);
                return { ...structuredClone(position(w)), id: w.id, locked: !w.allowMove,
                    resizable: !!element.querySelector('[data-dashboard-resize="height"]'),
                    minSize: descriptor?.minSize, maxSize: descriptor?.maxSize,
                    allowedSizes: descriptor?.allowedSizes,
                    rect: element.getBoundingClientRect() };
            }) : [];
            interaction = { before: capture(), id: widget.id, kind, x: event.clientX, y: event.clientY,
                height: rect.height, starts, rows, items, rowGap, scroll: scrollContainer.scrollTop, pointer: event.pointerId,
                original: structuredClone(position(widget)), target: event.currentTarget };
            if (kind === 'height') {
                const p = position(widget);
                const below = visible().find(candidate => {
                    const next = position(candidate);
                    return !isNews(candidate) && next.row === p.row + (p.size.rowSpan || 1) &&
                        next.column === p.column && next.size.columnSpan === p.size.columnSpan &&
                        elements.get(candidate.id).querySelector('[data-dashboard-resize="height"]');
                });
                if (below) {
                    interaction.below = { id: below.id, minHeight: position(below).minHeight,
                        gap: Math.max(0, elements.get(below.id).getBoundingClientRect().top - rowGap - rect.bottom) };
                }
            }
            if (event.pointerId !== undefined) event.currentTarget.setPointerCapture(event.pointerId);
            el.classList.add('dashboard-interacting');
            event.preventDefault();
        }
        let interactionFrame;
        let pendingPointer;
        function move(event) {
            if (!interaction || event.pointerId !== interaction.pointer) return;
            pendingPointer = { clientX: event.clientX, clientY: event.clientY };
            if (interactionFrame !== undefined) return;
            interactionFrame = requestAnimationFrame(() => {
                interactionFrame = undefined;
                const pointer = pendingPointer;
                pendingPointer = null;
                if (interaction && pointer) applyPointer(pointer);
            });
        }
        function applyPointer(event) {
            const state = interaction;
            if (state.kind === 'height') {
                // CSS Grid stretches the shared row itself; no packing or toolbar work is needed.
                const target = position(widgetsById.get(state.id));
                const height = Math.max(0, Math.min(config.maxHeight, Math.round(state.height + event.clientY - state.y + scrollContainer.scrollTop - state.scroll)));
                if (target.minHeight !== height) {
                    target.minHeight = height;
                    target.stretch = height < state.height ? false : state.original.stretch;
                    const element = elements.get(state.id);
                    element.style.minHeight = `${height}px`;
                    element.style.alignSelf = target.stretch === false ? 'start' : 'stretch';
                }
                if (state.below) {
                    // Release the lower card's requested space before growing the combined rows.
                    // Always use the original value so reversing the drag restores it exactly.
                    const below = position(widgetsById.get(state.below.id));
                    const growth = Math.max(0, height - state.height - state.below.gap);
                    below.minHeight = growth > 0 ? Math.max(0, (state.below.minHeight || 0) - growth) : state.below.minHeight;
                    elements.get(state.below.id).style.minHeight = `${below.minHeight || 0}px`;
                }
                scrollDuringInteraction(event.clientY);
                return;
            }
            const widget = widgetsById.get(state.id);
            const limits = bounds(widget);
            const nearest = x => state.starts.reduce((best, value, index) => Math.abs(value - x) < Math.abs(state.starts[best] - x) ? index : best, 0);
            // Every preview starts from the same draft so dragging back cannot accumulate displacement.
            setLayout(structuredClone(state.before.layout));
            const target = position(widgetsById.get(state.id));
            state.drop = null;
            if (state.marker) state.marker.hidden = true;
            if (state.kind === 'width-start') {
                const column = nearest(state.starts[state.original.column] + event.clientX - state.x);
                Smartstore.Admin.DashboardEngine.resizeStart(target, column, limits.min, limits.max, limits.widths);
                arrange(state.id);
            }
            else if (state.kind === 'width') {
                target.size.columnSpan = Math.max(limits.min, Math.min(limits.max,
                    nearest(state.starts[state.original.column + state.original.size.columnSpan] + event.clientX - state.x) - target.column));
                target.size.columnSpan = limits.widths.reduce((best, width) =>
                    Math.abs(width - target.size.columnSpan) < Math.abs(best - target.size.columnSpan) ? width : best, state.original.size.columnSpan);
                arrange(state.id);
            }
            else {
                const y = event.clientY + scrollContainer.scrollTop - state.scroll;
                const anchor = state.items.find(item => item.id !== state.id && item.stretch === false &&
                    event.clientX >= item.rect.left && event.clientX <= item.rect.right && y > item.rect.bottom &&
                    y <= state.rows[item.row + (item.size.rowSpan || 1) - 1]?.bottom);
                if (anchor) {
                    const column = Math.max(anchor.column, Math.min(
                        nearest(state.starts[state.original.column] + event.clientX - state.x),
                        anchor.column + anchor.size.columnSpan - target.size.columnSpan));
                    const bottom = state.rows[anchor.row + (anchor.size.rowSpan || 1) - 1].bottom;
                    const split = state.rows.find(row => row.row > anchor.row &&
                        row.row + (target.size.rowSpan || 1) <= anchor.row + (anchor.size.rowSpan || 1) &&
                        row.top >= anchor.rect.bottom + state.rowGap - 1);
                    const top = split ? split.top : anchor.rect.bottom + state.rowGap;
                    const height = Math.max(0, Math.min(config.maxHeight, Math.round(bottom - top)));
                    state.drop = Smartstore.Admin.DashboardEngine.insertBelow(state.items, anchor.id, state.id, column, height, split?.row);
                    if (state.drop) {
                        if (!state.marker) {
                            state.marker = document.createElement('div');
                            state.marker.className = 'position-fixed border border-primary rounded-3 pe-none z-3';
                            state.marker.setAttribute('aria-hidden', 'true');
                            document.body.appendChild(state.marker);
                        }
                        const dropped = state.drop.find(item => item.id === state.id);
                        const start = state.starts[dropped.column];
                        const end = state.starts[dropped.column + dropped.size.columnSpan];
                        const gap = parseFloat(getComputedStyle(container).columnGap) || 0;
                        Object.assign(state.marker.style, { left: `${Math.min(start, end) + (start > end ? gap : 0)}px`,
                            top: `${top - scrollContainer.scrollTop + state.scroll}px`,
                            width: `${Math.abs(end - start) - gap}px`, height: `${Math.max(0, bottom - top)}px` });
                        state.marker.hidden = false;
                    }
                }
                else {
                    target.column = Math.min(columns() - target.size.columnSpan, Math.max(0,
                        nearest(state.starts[state.original.column] + event.clientX - state.x)));
                    const row = state.rows.find(item => y <= item.bottom);
                    target.row = row ? row.row : state.rows.length;
                    arrange(state.id);
                }
            }
            render(false);
            scrollDuringInteraction(event.clientY);
        }
        function scrollDuringInteraction(y) {
            const rect = scrollContainer.getBoundingClientRect();
            if (y > rect.bottom - 60) scrollContainer.scrollTop += 15;
            else if (y < rect.top + 60) scrollContainer.scrollTop -= 15;
        }
        function finish(cancel = false) {
            if (!interaction) return;
            cancelAnimationFrame(interactionFrame);
            interactionFrame = undefined;
            // Commit the final queued pointer sample even if release precedes the next frame.
            const pointer = pendingPointer;
            pendingPointer = null;
            if (!cancel && pointer) applyPointer(pointer);
            const state = interaction;
            interaction = null;
            elements.get(state.id).classList.remove('dashboard-interacting');
            if (state.pointer !== undefined && state.target.hasPointerCapture(state.pointer)) state.target.releasePointerCapture(state.pointer);
            state.marker?.remove();
            if (cancel) setLayout(state.before.layout);
            else {
                if (state.drop) {
                    for (const item of state.drop) {
                        const p = position(widgetsById.get(item.id));
                        p.column = item.column; p.row = item.row; p.size = item.size; p.stretch = item.stretch; p.minHeight = item.minHeight;
                    }
                    arrange(state.id);
                }
                remember(state.before);
            }
            render();
        }
        container.addEventListener('pointerdown', event => {
            const handle = event.target.closest('[data-dashboard-resize], .dashboard-widget-overlay');
            if (!handle) return;
            const widget = widgetsById.get(handle.closest('[data-instance-id]').dataset.instanceId);
            const kind = handle.dataset.dashboardResize || 'move';
            if ((kind === 'move' || kind === 'width-start') && !widget.allowMove) return;
            start(event, widget, kind);
        });
        container.addEventListener('pointermove', move);
        container.addEventListener('pointerup', () => finish());
        container.addEventListener('pointercancel', () => finish(true));
        window.addEventListener('blur', () => finish(true));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape') finish(true);
            const handle = event.target.closest('[data-dashboard-resize], .dashboard-widget-overlay');
            if (!handle) return;
            const widget = widgetsById.get(handle.closest('[data-instance-id]').dataset.instanceId);
            const kind = handle.dataset.dashboardResize;
            const horizontal = event.key === 'ArrowLeft' || event.key === 'ArrowRight';
            const vertical = event.key === 'ArrowUp' || event.key === 'ArrowDown';
            if (kind === 'height' ? !vertical && event.key !== 'Home' : kind ? !horizontal : !horizontal && !vertical) return;
            if ((!kind || kind === 'width-start') && !widget.allowMove) return;
            event.preventDefault();
            const before = capture(), p = position(widget), limits = bounds(widget);
            const direction = getComputedStyle(container).direction === 'rtl' ? -1 : 1;
            const delta = (event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1) * (horizontal ? direction : 1);
            if (kind === 'height') {
                p.minHeight = event.key === 'Home' ? null : Math.max(0, Math.min(config.maxHeight,
                    (p.minHeight ?? elements.get(widget.id).offsetHeight) + delta * 10));
                if (event.key === 'Home') p.stretch = true;
                else if (delta < 0) p.stretch = false;
            }
            else if (kind === 'width-start' || kind === 'width') {
                const change = kind === 'width-start' ? -delta : delta;
                const widths = limits.widths.filter(width => kind !== 'width-start' || width <= p.column + p.size.columnSpan);
                const width = (change > 0 ? widths.find(width => width > p.size.columnSpan)
                    : widths.findLast(width => width < p.size.columnSpan)) ?? p.size.columnSpan;
                if (kind === 'width-start') p.column += p.size.columnSpan - width;
                p.size.columnSpan = width;
            }
            else if (horizontal) p.column = Math.max(0, Math.min(columns() - p.size.columnSpan, p.column + delta));
            else p.row = Math.max(0, p.row + delta);
            if (_.isEqual(before.layout, layout)) return;
            if (kind !== 'height') arrange(widget.id);
            render(false);
            remember(before);
        });

        // A one-use, tab-local transfer keeps undo across server-rendered add/reset previews.
        // Only placement data is transferred; widget HTML, settings and credentials never enter storage.
        const transferToken = new URL(window.location.href).searchParams.get('dashboardHistory');
        if (transferToken) {
            const key = 'dashboard-history:' + transferToken;
            try {
                const stored = sessionStorage.getItem(key);
                sessionStorage.removeItem(key);
                if (stored) {
                    const history = JSON.parse(stored);
                    if (history.layer === config.layer && Array.isArray(history.undo) && Array.isArray(history.redo)) {
                        undo = history.undo;
                        redo = history.redo;
                        dirty = history.dirty;
                    }
                }
            }
            catch { /* Continue without transferred history when storage is unavailable or invalid. */ }
            const url = new URL(window.location.href);
            url.searchParams.delete('dashboardHistory');
            window.history.replaceState(null, '', url);
        }

        function submit(action, command, widget, transfer = false) {
            form.elements.changesJson.value = JSON.stringify({ widgets: layout.widgets.map(
                ({ id, widgetSystemName, positions }) => ({ id, widgetSystemName, positions })) });
            form.elements.reset.value = reset ? 'true' : 'false';
            form.elements.command.value = command || '';
            form.elements.widgetSystemName.value = widget || '';
            const url = new URL(action || form.action, window.location.href);
            if (transfer) {
                const token = crypto.randomUUID();
                try {
                    sessionStorage.setItem('dashboard-history:' + token, JSON.stringify({
                        layer: config.layer, undo, redo, dirty
                    }));
                }
                catch {
                    displayNotification(root.dataset.error, 'error');
                    return;
                }
                url.searchParams.set('dashboardHistory', token);
            }
            form.action = url.href;
            leaving = true;
            HTMLFormElement.prototype.submit.call(form);
        }
        function navigate(url) { leaving = true; window.location.assign(url); }
        function guard(action) {
            if (!dirty) { action(false); return; }
            pending = action;
            $('#dashboard-leave').modal('show');
        }
        function save(next) {
            const commit = () => {
                if (next) {
                    const input = document.createElement('input');
                    input.type = 'hidden'; input.name = 'next'; input.value = next;
                    form.appendChild(input);
                }
                submit();
            };
            if (config.layer === 'Global') {
                confirm2({
                    message: root.dataset.saveGlobalConfirm,
                    icon: { type: 'question' },
                    callback: accepted => { if (accepted) commit(); }
                }).find('.btn-accept').text(root.dataset.saveGlobalLabel);
            }
            else commit();
        }
        $('[data-dashboard-edit]').on('click', function () {
            const layer = this.dataset.dashboardEdit;
            if (layer === config.layer) return;
            guard(shouldSave => shouldSave ? save('edit:' + layer) : navigate(config.editUrl + '&layer=' + encodeURIComponent(layer)));
        });
        $('#dashboard-discard').on('click', () => {
            $('#dashboard-leave').modal('hide');
            if (pending) pending(false);
        });
        $('#dashboard-save-leave').on('click', () => {
            $('#dashboard-leave').one('hidden.bs.modal', () => { if (pending) pending(true); }).modal('hide');
        });
        window.addEventListener('pageshow', () => { leaving = false; });
        window.addEventListener('beforeunload', event => {
            if (dirty && !leaving) { event.preventDefault(); event.returnValue = ''; }
        });

        function historyStep(from, to) {
            finish(true);
            if (!from.length) return;
            to.push(capture());
            const state = from.pop();
            setLayout(structuredClone(state.layout));
            reset = state.reset;
            dirty = state.dirty;
            if (layout.widgets.some(w => !elements.has(w.id) && !unavailableIds.has(w.id))) {
                // A reset can introduce widgets not rendered on this page. Use their normal server lifecycle.
                submit(config.previewUrl, null, null, true);
                return;
            }
            render();
        }
        $('#dashboard-undo').on('click', () => historyStep(undo, redo));
        $('#dashboard-redo').on('click', () => historyStep(redo, undo));
        document.addEventListener('keydown', event => {
            if (!(event.ctrlKey || event.metaKey) || event.altKey || event.target.closest('input, textarea, select, [contenteditable]')) return;
            const key = event.key.toLowerCase();
            if (key !== 'z' && key !== 'y') return;
            event.preventDefault();
            if (key === 'y' || event.shiftKey) historyStep(redo, undo);
            else historyStep(undo, redo);
        });
        $(container).on('click', '[data-dashboard-remove]', function () {
            const el = this.closest('[data-instance-id]');
            const before = capture();
            setLayout({ ...layout, widgets: layout.widgets.filter(w => w.id !== el.dataset.instanceId) });
            arrange(); render(false); remember(before);
        });
        $(form).on('submit', event => { event.preventDefault(); save(); });
        $('#dashboard-cancel').on('click', () => guard(shouldSave => shouldSave ? save('cancel:') : navigate(config.cancelUrl)));
        $('#dashboard-reset').on('click', () => {
            remember(capture(), true);
            reset = true;
            submit(config.previewUrl, 'reset', null, true);
        });
        $('[data-add-widget]').on('click', function () {
            remember(capture(), true);
            submit(config.previewUrl, 'add', this.dataset.addWidget, true);
        });
        window.addEventListener('resize', () => {
            finish(true);
            breakpoint = currentBreakpoint();
            render();
        });
        render();
    });
})(jQuery);
