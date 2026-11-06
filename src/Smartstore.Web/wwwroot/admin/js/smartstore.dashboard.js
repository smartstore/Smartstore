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

        static overlaps(a, b) {
            return a.column < b.column + b.size.columnSpan && b.column < a.column + a.size.columnSpan &&
                a.row < b.row + (b.size.rowSpan || 1) && b.row < a.row + (a.size.rowSpan || 1);
        }

        static allowsSpan(item, span) {
            return item.resizable && item.size.columnSpan >= (item.minSize?.columnSpan ?? 1) &&
                item.size.columnSpan <= (item.maxSize?.columnSpan ?? 12) && span >= (item.minSize?.rowSpan ?? 1) && span <= (item.maxSize?.rowSpan ?? 1000) &&
                (!item.allowedSizes?.length || item.allowedSizes.some(size =>
                    size.columnSpan === item.size.columnSpan && size.rowSpan === span));
        }

        // Resize only into existing, unoccupied cells. Inserting a row is an explicit button action.
        static resizeRows(items, id, span) {
            const item = items.find(item => item.id === id);
            if (!item || !this.allowsSpan(item, span)) return null;
            const target = { ...item, size: { ...item.size, rowSpan: span } };
            if (items.some(other => other.id !== id && this.overlaps(target, other))) return null;
            return items.map(other => other.id === id ? target : { ...other, size: { ...other.size } });
        }

        static extend(items, id, rowCount) {
            const item = items.find(item => item.id === id);
            if (!item) return null;
            const span = (item.size.rowSpan || 1) + 1;
            if (!this.allowsSpan(item, span)) return null;
            const row = item.row + span - 1;
            const expanded = this.resizeRows(items, id, span);
            if (row < rowCount && expanded) return expanded;

            const proposed = items.map(other => ({ ...other, size: { ...other.size } }));
            for (const other of proposed) {
                if (other.id === id) other.size.rowSpan = span;
                else if (other.row >= row) {
                    if (other.locked || other.row >= 10000) return null;
                    other.row++;
                }
                else if (other.row + (other.size.rowSpan || 1) > row) {
                    const rows = (other.size.rowSpan || 1) + 1;
                    if (!this.allowsSpan(other, rows)) return null;
                    other.size.rowSpan = rows;
                }
            }
            return proposed;
        }

        // Compact only on save. Rows preceding locked placements must retain their indices.
        static compact(items) {
            const fixedBottom = Math.max(0, ...items.filter(item => item.locked).map(item => item.row + (item.size.rowSpan || 1)));
            const occupied = new Set();
            for (const item of items) {
                for (let row = item.row; row < item.row + (item.size.rowSpan || 1); row++) occupied.add(row);
            }
            const rows = [...occupied].sort((a, b) => a - b);
            for (const item of items) {
                if (!item.locked && item.row >= fixedBottom) {
                    item.row = fixedBottom + rows.filter(row => row >= fixedBottom && row < item.row).length;
                }
            }
            return items;
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
        const breakpoints = config.layout.rules.map(rule => rule.breakpoint);
        const layoutRules = new Map(config.layout.rules.map(rule => [rule.breakpoint, rule]));
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
        const currentBreakpoint = () => ResponsiveBootstrapToolkit.current();
        let breakpoint = currentBreakpoint();
        const position = (widget, tier = breakpoint) => widget.positions
            .filter(position => breakpoints.indexOf(position.breakpoint) <= breakpoints.indexOf(tier))
            .sort((a, b) => breakpoints.indexOf(b.breakpoint) - breakpoints.indexOf(a.breakpoint))[0];
        container.querySelectorAll('[data-instance-id]').forEach(el => {
            elements.set(el.dataset.instanceId, el);
            if (el.querySelector('.dashboard-widget-overlay')) {
                el.querySelectorAll('.dashboard-widget-body > :not(.dashboard-widget-tools):not(.dashboard-resize):not(.dashboard-extend)').forEach(content => {
                    content.inert = true;
                });
            }
        });
        const renderedIds = new Set(elements.keys());
        const unavailableIds = new Set(layout.widgets.filter(widget => !renderedIds.has(widget.id)).map(widget => widget.id));
        for (const widget of layout.widgets) {
            if (renderedIds.has(widget.id)) {
                widget.positions = breakpoints.map(tier => ({ ...structuredClone(position(widget, tier)), breakpoint: tier }));
            }
        }
        const visible = () => layout.widgets.filter(widget => renderedIds.has(widget.id));
        const isNews = widget => elements.get(widget.id).classList.contains('news-feed');
        const columns = (tier = breakpoint) => layoutRules.get(tier).columnCount;
        function bounds(widget) {
            const el = elements.get(widget.id);
            const available = widget.allowMove ? columns() : columns() - position(widget).column;
            const max = layoutRules.get(breakpoint).fullWidthWidgets ? layout.columnCount : Math.min(available, Number(el.dataset.maxWidth));
            const min = layoutRules.get(breakpoint).fullWidthWidgets ? max : Math.min(max, Number(el.dataset.minWidth));
            const allowed = config.widgets.find(choice => choice.systemName === widget.widgetSystemName)?.allowedSizes;
            const widths = Array.from({ length: max - min + 1 }, (_, index) => min + index)
                .filter(width => !allowed?.length || allowed.some(size =>
                    size.columnSpan === width && size.rowSpan === position(widget).size.rowSpan));
            return { min, max, widths };
        }
        const engine = Smartstore.Admin.DashboardEngine;
        const gridCells = [];
        let gridShape;
        const previewCells = [];
        layout.rowCounts = {};
        function gridItems(tier = breakpoint) {
            return visible().filter(widget => !isNews(widget)).map(widget => {
                const descriptor = config.widgets.find(choice => choice.systemName === widget.widgetSystemName);
                return { ...structuredClone(position(widget, tier)), id: widget.id, locked: !widget.allowMove,
                    resizable: !!elements.get(widget.id).querySelector('[data-dashboard-resize="height"]'),
                    minSize: descriptor?.minSize, maxSize: descriptor?.maxSize, allowedSizes: descriptor?.allowedSizes };
            });
        }
        function applyItems(items) {
            for (const item of items) {
                const p = position(widgetsById.get(item.id));
                p.column = item.column;
                p.row = item.row;
                p.size = item.size;
            }
        }
        function drawGrid() {
            const bottom = Math.max(1, ...gridItems().map(item => item.row + (item.size.rowSpan || 1)));
            const count = layout.rowCounts[breakpoint] = Math.max(layout.rowCounts[breakpoint] || 0, bottom);
            const shape = `${columns()}:${count}`;
            if (shape === gridShape) return;
            gridShape = shape;
            gridCells.forEach(cell => cell.remove());
            gridCells.length = 0;
            for (let row = 0; row < count; row++) {
                for (let column = 0; column < columns(); column++) {
                    const cell = document.createElement('div');
                    cell.className = 'dashboard-grid-cell rounded pe-none';
                    cell.setAttribute('aria-hidden', 'true');
                    cell.style.gridArea = `${row + 1} / ${column + 1}`;
                    container.appendChild(cell);
                    gridCells.push(cell);
                }
            }
        }
        function clearExtensionPreview() {
            previewCells.forEach(cell => cell.remove());
            previewCells.length = 0;
        }
        function extensionPreview(button) {
            if (interaction) return;
            clearExtensionPreview();
            const id = button.closest('[data-instance-id]').dataset.instanceId;
            const items = gridItems();
            const proposed = engine.extend(items, id, layout.rowCounts[breakpoint]);
            button.disabled = !proposed;
            if (!proposed) return;
            const item = items.find(item => item.id === id);
            const row = item.row + (item.size.rowSpan || 1);
            const reuse = row < layout.rowCounts[breakpoint] && engine.resizeRows(items, id, (item.size.rowSpan || 1) + 1);
            const rect = elements.get(id).getBoundingClientRect();
            const gap = parseFloat(getComputedStyle(container).rowGap) || 0;
            const first = gridCells[row * columns() + item.column]?.getBoundingClientRect();
            const top = reuse ? first.top : rect.bottom;
            const height = reuse ? first.height : gap;
            function mark(left, width, insertion = false) {
                const marker = document.createElement('div');
                marker.className = 'dashboard-grid-preview position-fixed border border-primary rounded pe-none z-3';
                marker.classList.toggle('dashboard-grid-insertion', insertion);
                marker.setAttribute('aria-hidden', 'true');
                Object.assign(marker.style, { left: `${left}px`, top: `${top}px`, width: `${width}px`, height: `${height}px` });
                document.body.appendChild(marker);
                previewCells.push(marker);
            }
            // Fixed overlays cannot create tracks or contribute to the grid's intrinsic row heights.
            if (!reuse) {
                const start = gridCells[0].getBoundingClientRect();
                const end = gridCells[columns() - 1].getBoundingClientRect();
                mark(Math.min(start.left, end.left), Math.max(start.right, end.right) - Math.min(start.left, end.left), true);
            }
            mark(rect.left, rect.width);
        }
        container.addEventListener('pointerover', event => {
            const button = event.target.closest('[data-dashboard-extend]');
            if (button && !button.contains(event.relatedTarget)) extensionPreview(button);
        });
        container.addEventListener('pointerout', event => {
            const button = event.target.closest('[data-dashboard-extend]');
            if (button && !button.contains(event.relatedTarget)) clearExtensionPreview();
        });
        container.addEventListener('focusin', event => {
            const button = event.target.closest('[data-dashboard-extend]');
            if (button) extensionPreview(button);
        });
        container.addEventListener('focusout', clearExtensionPreview);
        document.addEventListener('scroll', clearExtensionPreview, true);
        container.addEventListener('click', event => {
            const button = event.target.closest('[data-dashboard-extend]');
            if (!button || interaction) return;
            clearExtensionPreview();
            const items = gridItems();
            const id = button.closest('[data-instance-id]').dataset.instanceId;
            const proposed = engine.extend(items, id, layout.rowCounts[breakpoint]);
            if (!proposed) return;
            const before = capture();
            const item = items.find(item => item.id === id);
            const end = item.row + (item.size.rowSpan || 1);
            if (end >= layout.rowCounts[breakpoint] || !engine.resizeRows(items, id, (item.size.rowSpan || 1) + 1)) {
                layout.rowCounts[breakpoint]++;
            }
            applyItems(proposed);
            render(false);
            remember(before);
        });

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
                if (isNews(widget)) {
                    const bottom = Math.max(0, ...visible().filter(w => !isNews(w)).map(w => {
                        const positionAtWidth = position(w);
                        return positionAtWidth.row + (positionAtWidth.size.rowSpan || 1);
                    }));
                    el.style.gridRow = layoutRules.get(breakpoint).hasNewsRail ? `1 / span ${Math.max(1, bottom)}` : `${bottom + 1} / span 1`;
                }
            }
            drawGrid();
            const items = gridItems();
            for (const button of container.querySelectorAll('[data-dashboard-extend]')) {
                button.disabled = !engine.extend(items, button.closest('[data-instance-id]').dataset.instanceId, layout.rowCounts[breakpoint]);
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
            clearExtensionPreview();
            const el = elements.get(widget.id);
            const gridRect = container.getBoundingClientRect();
            const style = getComputedStyle(container);
            const gap = parseFloat(style.columnGap) || 0;
            const tracks = style.gridTemplateColumns.split(' ').map(Number.parseFloat);
            const direction = style.direction === 'rtl' ? -1 : 1;
            const starts = [direction === 1 ? gridRect.left : gridRect.right];
            tracks.forEach(width => starts.push(starts.at(-1) + direction * (width + gap)));
            const rowGap = parseFloat(style.rowGap) || 0;
            let top = gridRect.top;
            const rows = style.gridTemplateRows.split(' ').map((value, row) => {
                const bottom = top + parseFloat(value);
                const track = { row, top, bottom };
                top = bottom + rowGap;
                return track;
            });
            interaction = { before: capture(), id: widget.id, kind, x: event.clientX, y: event.clientY,
                starts, rows, scroll: scrollContainer.scrollTop, pointer: event.pointerId,
                original: structuredClone(position(widget)), target: event.currentTarget };
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
                setLayout(structuredClone(state.before.layout));
                const y = event.clientY + scrollContainer.scrollTop - state.scroll;
                const row = state.rows.reduce((best, track) =>
                    Math.abs(track.bottom - y) < Math.abs(best.bottom - y) ? track : best, state.rows[state.original.row]);
                const proposed = engine.resizeRows(gridItems(), state.id, row.row + 1 - state.original.row);
                if (proposed) applyItems(proposed);
                render(false);
                scrollDuringInteraction(event.clientY);
                return;
            }
            const widget = widgetsById.get(state.id);
            const limits = bounds(widget);
            const nearest = x => state.starts.reduce((best, value, index) => Math.abs(value - x) < Math.abs(state.starts[best] - x) ? index : best, 0);
            // Every preview starts from the same draft so dragging back cannot accumulate displacement.
            setLayout(structuredClone(state.before.layout));
            const target = position(widgetsById.get(state.id));
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
                target.column = Math.min(columns() - target.size.columnSpan, Math.max(0,
                    nearest(state.starts[state.original.column] + event.clientX - state.x)));
                const row = state.rows.find(item => y <= item.bottom);
                target.row = row ? row.row : state.rows.length;
                arrange(state.id);
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
            if (cancel) setLayout(state.before.layout);
            else {
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
                const span = event.key === 'Home' ? 1 : (p.size.rowSpan || 1) + delta;
                if (p.row + span > layout.rowCounts[breakpoint]) return;
                const proposed = engine.resizeRows(gridItems(), widget.id, span);
                if (!proposed) return;
                applyItems(proposed);
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
        // Only layout data and editable settings are transferred; widget HTML and credentials never enter storage.
        let addedWidgetId;
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
                        if (history.addedWidget) {
                            const previousWidgets = history.undo.at(-1)?.layout.widgets;
                            if (Array.isArray(previousWidgets)) {
                                addedWidgetId = layout.widgets.find(widget => widget.widgetSystemName === history.addedWidget
                                    && !previousWidgets.some(previous => previous.id === widget.id))?.id;
                            }
                        }
                        layout.rowCounts = config.reset ? {} : history.rowCounts;
                    }
                }
            }
            catch { /* Continue without transferred history when storage is unavailable or invalid. */ }
            const url = new URL(window.location.href);
            url.searchParams.delete('dashboardHistory');
            window.history.replaceState(null, '', url);
        }

        function submit(action, command, widget, transfer = false) {
            clearExtensionPreview();
            // Keep empty editor rows until the user actually saves, including all responsive layouts.
            if (!action) {
                for (const tier of breakpoints) {
                    const items = engine.compact(gridItems(tier));
                    for (const item of items) position(widgetsById.get(item.id), tier).row = item.row;
                }
            }
            form.elements.changesJson.value = JSON.stringify({ widgets: layout.widgets.map(
                ({ id, widgetSystemName, positions, settings }) => ({ id, widgetSystemName, positions, settings })) });
            form.elements.reset.value = reset ? 'true' : 'false';
            form.elements.command.value = command || '';
            form.elements.widgetSystemName.value = widget || '';
            const url = new URL(action || form.action, window.location.href);
            if (transfer) {
                const token = crypto.randomUUID();
                try {
                    sessionStorage.setItem('dashboard-history:' + token, JSON.stringify({
                        addedWidget: command === 'add' ? widget : null,
                        layer: config.layer, undo, redo, dirty, rowCounts: layout.rowCounts
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
            const settingsChanged = state.layout.widgets.some(widget =>
                !_.isEqual(widget.settings, widgetsById.get(widget.id)?.settings));
            setLayout(structuredClone(state.layout));
            reset = state.reset;
            dirty = state.dirty;
            if (settingsChanged || layout.widgets.some(w => !elements.has(w.id) && !unavailableIds.has(w.id))) {
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
            confirm2({
                message: root.dataset.removeWidgetConfirm,
                icon: { type: 'delete' },
                callback: accepted => {
                    if (!accepted) return;

                    const before = capture();
                    setLayout({ ...layout, widgets: layout.widgets.filter(w => w.id !== el.dataset.instanceId) });
                    arrange(); render(false); remember(before);
                }
            });
        });
        function flipConfiguration(el, open) {
            const back = el.querySelector('[data-dashboard-configuration]');
            const front = el.querySelector('.dashboard-widget-body');
            const button = el.querySelector('[data-dashboard-configure]');
            el.classList.toggle('dashboard-configuring', open);
            front.inert = open;
            back.inert = !open;
            button.setAttribute('aria-expanded', String(open));
            (open ? back.querySelector('input') : button)?.focus();
        }
        $(container).on('click', '[data-dashboard-configure]', function () {
            const button = this;
            const el = button.closest('[data-instance-id]');
            const back = el.querySelector('[data-dashboard-configuration]');
            const error = back.querySelector('[data-dashboard-configuration-error]');
            const save = back.querySelector('[type="submit"]');
            if (button.disabled) return;

            button.disabled = true;
            save.disabled = true;
            error.classList.add('d-none');
            back.querySelector('[data-dashboard-configuration-content]').replaceChildren();
            flipConfiguration(el, true);
            $.ajax({
                url: back.dataset.configurationUrl,
                type: 'POST',
                data: {
                    instanceId: el.dataset.instanceId,
                    changesJson: JSON.stringify({ widgets: layout.widgets.map(
                        ({ id, widgetSystemName, positions, settings }) => ({ id, widgetSystemName, positions, settings })) }),
                    layer: config.layer,
                    reset
                }
            }).done(html => {
                $(back).find('[data-dashboard-configuration-content]').html(html);
                $(back).find('[data-fixed-or-percent]').fixedOrPercent();
                save.disabled = false;
                if (el.classList.contains('dashboard-configuring')) back.querySelector('input')?.focus();
            }).fail(() => {
                error.textContent = back.dataset.configurationError;
                error.classList.remove('d-none');
            }).always(() => {
                button.disabled = false;
            });
        });
        $(container).on('click', '[data-dashboard-configuration-cancel]', function () {
            flipConfiguration(this.closest('[data-instance-id]'), false);
        });
        $(container).on('keydown', '[data-dashboard-configuration]', function (event) {
            if (event.key === 'Escape') {
                event.preventDefault();
                flipConfiguration(this.closest('[data-instance-id]'), false);
            }
        });
        $(container).on('submit', '[data-dashboard-configuration]', function (event) {
            event.preventDefault();
            const invalidPane = this.querySelector(':invalid')?.closest('.tab-pane');
            if (invalidPane) {
                $(this).find('[data-toggle="tab"]').filter(function () {
                    return this.getAttribute('href') === '#' + invalidPane.id;
                }).tab('show');
            }
            if (!this.reportValidity()) return;
            const el = this.closest('[data-instance-id]');
            const widget = widgetsById.get(el.dataset.instanceId);
            const before = capture();
            const settings = { ...widget.settings };
            this.querySelectorAll('[data-dashboard-setting]').forEach(input => {
                if (input.type === 'radio' && !input.checked) return;
                if (input.type === 'number' && input.value === '') {
                    delete settings[input.dataset.dashboardSetting];
                }
                else {
                    settings[input.dataset.dashboardSetting] = input.type === 'checkbox' ? input.checked
                        : input.dataset.valueType === 'boolean' ? input.value === 'true'
                        : input.type === 'number' ? input.valueAsNumber : input.value;
                }
            });
            widget.settings = settings;
            remember(before);
            submit(config.previewUrl, null, null, true);
        });
        $(form).on('submit', event => { event.preventDefault(); save(); });
        $('#dashboard-cancel').on('click', () => guard(shouldSave => shouldSave ? save('cancel:') : navigate(config.cancelUrl)));
        $('#dashboard-reset').on('click', () => {
            confirm2({
                message: root.dataset.resetConfirm,
                icon: { type: 'warning' },
                callback: accepted => {
                    if (!accepted) return;

                    remember(capture(), true);
                    reset = true;
                    submit(config.previewUrl, 'reset', null, true);
                }
            });
        });
        $('[data-add-widget]').on('click', function () {
            remember(capture(), true);
            submit(config.previewUrl, 'add', this.dataset.addWidget, true);
        });
        window.addEventListener('resize', () => {
            finish(true);
            clearExtensionPreview();
            breakpoint = currentBreakpoint();
            render();
        });
        render();
        if (addedWidgetId) {
            const scrollToAddedWidget = () => requestAnimationFrame(() => {
                const element = elements.get(addedWidgetId);
                if (!element?.isConnected) return;

                $(scrollContainer).scrollTo(element, {
                    offset: { top: -$(scrollContainer).find('.section-header').outerHeight(true) },
                    behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth'
                });
            });
            if (document.readyState === 'complete') scrollToAddedWidget();
            else window.addEventListener('load', scrollToAddedWidget, { once: true });
        }
    });
})(jQuery);
