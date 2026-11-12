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

        // Resize into existing, unoccupied cells, keeping the opposite edge fixed.
        static resizeRows(items, id, span, fromStart = false) {
            const item = items.find(item => item.id === id);

            if (!item || !this.allowsSpan(item, span)) return null;

            const target = { ...item, row: fromStart ? item.row + (item.size.rowSpan || 1) - span : item.row,
                size: { ...item.size, rowSpan: span } };

            if (target.row < 0) return null;

            if (items.some(other => other.id !== id && this.overlaps(target, other))) return null;

            return items.map(other => other.id === id ? target : { ...other, size: { ...other.size } });
        }

        static extend(items, id, rowCount, fromStart = false) {
            const item = items.find(item => item.id === id);

            if (!item) return null;

            const span = (item.size.rowSpan || 1) + 1;

            if (!this.allowsSpan(item, span)) return null;

            const row = fromStart ? item.row : item.row + span - 1;
            const expanded = this.resizeRows(items, id, span, fromStart);

            if ((fromStart ? item.row > 0 : row < rowCount) && expanded) return expanded;

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

        // Each gesture extends by one adjacent row, reusing free cells before inserting a row.
        static resizeEdge(items, id, span, rowCount, fromStart = false, columns) {
            const original = items.find(item => item.id === id);

            if (span <= (original.size.rowSpan || 1)) {
                return { items: this.resizeRows(items, id, span, fromStart), insertionRow: null };
            }

            if (fromStart && original.row === 0 && original.column === 0 && original.size.columnSpan === columns &&
                !items.some(item => item.id !== id && item.row === 0)) {
                const blocked = items.some(item => item.locked || item.row >= 10000);
                return {
                    items: blocked ? null : items.map(item => ({ ...item, row: item.row + 1, size: { ...item.size } })),
                    insertionRow: 0
                };
            }

            const next = (original.size.rowSpan || 1) + 1;
            const row = fromStart ? original.row : original.row + next - 1;
            const free = this.resizeRows(items, id, next, fromStart);

            if ((fromStart ? original.row > 0 : row < rowCount) && free) {
                return { items: free, insertionRow: null };
            }

            return { items: this.extend(items, id, rowCount, fromStart), insertionRow: row };
        }

        static fits(items, target, columns) {
            return target.row >= 0 && target.column >= 0 && target.column + target.size.columnSpan <= columns &&
                !items.some(other => other.id !== target.id && this.overlaps(target, other));
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

        static move(items, columns, changedId, original) {
            const moved = items.find(item => item.id === changedId);

            if (moved.locked) return items;

            const collisions = items.filter(item => item.id !== changedId && this.overlaps(moved, item));

            if (collisions.length === 1 && !collisions[0].locked) {
                const neighbor = collisions[0];
                const destination = { ...moved, column: neighbor.column, row: neighbor.row };
                const replacement = { ...neighbor, column: original.column, row: original.row };
                // Adjacent cards exchange order within their combined width, keeping their sizes.
                if (original.row === neighbor.row && moved.row === neighbor.row) {
                    if (original.column + original.size.columnSpan === neighbor.column) {
                        destination.column = original.column + neighbor.size.columnSpan;
                    }
                    else if (neighbor.column + neighbor.size.columnSpan === original.column) {
                        replacement.column = neighbor.column + moved.size.columnSpan;
                    }
                }

                const others = items.filter(item => item.id !== changedId && item.id !== neighbor.id);

                if (!this.overlaps(destination, replacement) &&
                    this.fits(others, destination, columns) && this.fits(others, replacement, columns)) {
                    moved.column = destination.column;
                    moved.row = destination.row;
                    neighbor.column = replacement.column;
                    neighbor.row = replacement.row;
                    return items;
                }
            }

            return this.arrange(items, columns, changedId);
        }

        static arrange(items, columns, changedId) {
            const ordered = [...items].sort((a, b) => Number(!!b.locked) - Number(!!a.locked) ||
                Number(b.id === changedId) - Number(a.id === changedId) || a.row - b.row || a.column - b.column);
            const placed = [];

            for (const item of ordered) {
                if (!item.locked) {
                    item.size.columnSpan = Math.min(columns, item.size.columnSpan);
                    item.column = Math.max(0, Math.min(columns - item.size.columnSpan, item.column));
                    item.row = Math.max(0, item.row);
                    while (placed.some(other => this.overlaps(item, other))) item.row++;
                }

                placed.push(item);
            }

            return items;
        }
    };

    $(function () {
        const editorData = document.getElementById('dashboard-editor-data');

        if (!editorData) return;

        const config = JSON.parse(editorData.textContent);
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
        let leaving = false;
        let previewing = false;
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
            el.classList.add('z-1');
        });
        const isNews = widget => widget.widgetSystemName === 'Smartstore.Admin.Dashboard.NewsFeed';
        const renderedIds = new Set(elements.keys());

        for (const widget of layout.widgets) {
            if (renderedIds.has(widget.id)) {
                widget.positions = breakpoints.map(tier => ({ ...structuredClone(position(widget, tier)), breakpoint: tier }));
            }
        }

        const visible = () => layout.widgets.filter(widget => renderedIds.has(widget.id));
        const columns = (tier = breakpoint) => layoutRules.get(tier).columnCount;
        function bounds(widget) {
            const el = elements.get(widget.id);
            const available = widget.allowMove ? columns() : columns() - position(widget).column;
            const max = layoutRules.get(breakpoint).fullWidthWidgets ? layout.columnCount : Math.min(
                available,
                Number(el.dataset.maxWidth));
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
        layout.rowCounts = {};
        layout.rowMinimums = {};
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
            const items = gridItems();
            const bottom = Math.max(1, ...items.map(item => item.row + (item.size.rowSpan || 1)));
            const count = layout.rowCounts[breakpoint] = Math.max(layout.rowCounts[breakpoint] || 0, bottom);
            const minimums = layout.rowMinimums[breakpoint];
            const occupied = new Set(items.flatMap(item =>
                Array.from({ length: item.size.rowSpan || 1 }, (_, offset) => item.row + offset)));
            container.style.gridTemplateRows = minimums ? Array.from({ length: count }, (_, row) =>
                `minmax(${occupied.has(row) ? Math.max(minimumRowHeight(), minimums[row] || 0) : minimumRowHeight()}px, auto)`).join(' ') : '';
            const shape = `${columns()}:${count}`;

            if (shape === gridShape) return;

            gridShape = shape;
            gridCells.forEach(cell => cell.remove());
            gridCells.length = 0;

            for (let row = 0; row < count; row++) {
                for (let column = 0; column < columns(); column++) {
                    const cell = document.createElement('div');
                    cell.className = 'dashboard-grid-cell rounded pe-none z-0';
                    cell.setAttribute('aria-hidden', 'true');
                    cell.style.gridArea = `${row + 1} / ${column + 1}`;
                    container.appendChild(cell);
                    gridCells.push(cell);
                }
            }
        }
        function minimumRowHeight() {
            return parseFloat(getComputedStyle(gridCells[0]).minHeight);
        }
        function reserveRows(rows, insertionRow) {
            const minimums = rows.map(row => row.bottom - row.top);

            if (insertionRow !== null) minimums.splice(insertionRow, 0, minimumRowHeight());

            layout.rowMinimums[breakpoint] = minimums;
        }
        function clearInsertionPreview(state) {
            state.insertionPreview?.remove();
            state.insertionPreview = null;

            for (const [element, translate] of state.translated || []) {
                if (translate === undefined) delete element.style.translate;
                else element.style.translate = translate;
            }

            state.translated = [];
        }
        function arrange(changedId, original) {
            const items = visible().filter(widget => !isNews(widget)).map(widget => ({
                ...structuredClone(position(widget)), id: widget.id, locked: !widget.allowMove
            }));

            if (original) engine.move(items, columns(), changedId, original);
            else engine.arrange(items, columns(), changedId);

            for (const item of items) {
                const p = position(widgetsById.get(item.id));
                p.column = item.column; p.row = item.row; p.size = item.size;
            }
        }
        function updateNewsPositions() {
            const news = layout.widgets.find(isNews);

            if (!news) return;

            for (const tier of breakpoints) {
                const bottom = Math.max(0, ...layout.widgets.filter(widget => !isNews(widget)).map(widget => {
                    const p = position(widget, tier);
                    return p.row + (p.size.rowSpan || 1);
                }));
                const p = position(news, tier);
                p.row = layoutRules.get(tier).hasNewsRail ? 0 : bottom;
                p.size.rowSpan = layoutRules.get(tier).hasNewsRail ? Math.max(1, bottom) : 1;
            }
        }
        function render(updateToolbar = true) {
            updateNewsPositions();

            for (const [id, el] of elements) {
                const widget = widgetsById.get(id);

                if (!widget) { el.remove(); continue; }

                if (!el.isConnected) container.appendChild(el);

                const p = position(widget);
                el.style.gridColumn = `${p.column + 1} / span ${p.size.columnSpan}`;
                el.style.gridRow = `${p.row + 1} / span ${p.size.rowSpan || 1}`;
            }

            drawGrid();

            if (updateToolbar) updateControls();
        }
        // History includes editor-only row minimums; saving submits only widget placements and settings.
        function capture() { return structuredClone({ layout, dirty }); }
        function remember(state, preview = false) {
            if (!preview && _.isEqual(state.layout, layout)) return;

            undo.push(state);

            if (undo.length > 40) undo.shift();

            redo.length = 0;
            dirty = true;
            updateControls();
        }
        function updateControls() {
            $('#dashboard-undo').prop('disabled', !undo.length);
            $('#dashboard-redo').prop('disabled', !redo.length);
            $(form.elements.save).prop('disabled', !dirty || previewing || leaving);
            $('#dashboard-cancel').prop('disabled', !dirty || previewing || leaving);
            document.querySelectorAll('[data-add-widget]').forEach(button => {
                const descriptor = config.widgets.find(w => w.systemName === button.dataset.addWidget);
                button.disabled = !descriptor?.allowMultipleInstances && layout.widgets.some(w => w.widgetSystemName === button.dataset.addWidget);
            });
        }

        function start(event, widget, kind) {
            if (interaction || (event.button !== undefined && event.button !== 0)) return;

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
                starts, rows, gap, rowGap, scroll: scrollContainer.scrollTop, pointer: event.pointerId,
                original: structuredClone(position(widget)), target: event.currentTarget, cursor: document.body.style.cursor };
            document.body.style.cursor = kind === 'move' ? 'move' : kind.startsWith('height') ? 'ns-resize' : 'ew-resize';
            document.body.classList.add('dashboard-dragging');

            if (kind === 'move') document.body.classList.add('dashboard-moving');

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

            if (state.kind === 'height' || state.kind === 'height-start') {
                setLayout(structuredClone(state.before.layout));
                const fromStart = state.kind === 'height-start';
                const y = event.clientY + scrollContainer.scrollTop - state.scroll;
                const originalEnd = state.original.row + (state.original.size.rowSpan || 1);
                const edge = fromStart ? state.rows[state.original.row].top : state.rows[originalEnd - 1].bottom;
                const expanding = fromStart ? y < edge - 12 : y > edge + 12;
                let span;

                if (expanding) {
                    span = (state.original.size.rowSpan || 1) + 1;
                }
                else {
                    const row = state.rows.reduce(
                        (best, track) =>
                        Math.abs((fromStart ? track.top : track.bottom) - y) <
                        Math.abs((fromStart ? best.top : best.bottom) - y) ? track : best,
                        state.rows[fromStart ? state.original.row : originalEnd - 1]);
                    span = fromStart ? originalEnd - row.row : row.row + 1 - state.original.row;
                }

                const result = engine.resizeEdge(gridItems(), state.id, span, layout.rowCounts[breakpoint], fromStart, columns());
                state.valid = !!result.items;
                state.insertionRow = result.items ? result.insertionRow : null;

                if (result.items) {
                    applyItems(result.items);
                    const resized = position(widgetsById.get(state.id));

                    if (resized.row !== state.original.row || resized.size.rowSpan !== state.original.size.rowSpan) {
                        reserveRows(state.rows, result.insertionRow);
                    }

                    if (result.insertionRow !== null) layout.rowCounts[breakpoint]++;
                }

                scrollDuringInteraction(event.clientY);
                drawInteractionPreview(state, position(widgetsById.get(state.id)));
                return;
            }

            const widget = widgetsById.get(state.id);
            const limits = bounds(widget);
            const nearest = x => state.starts.reduce(
                (best, value, index) => Math.abs(value - x) < Math.abs(state.starts[best] - x) ? index : best,
                0);
            // Every preview starts from the same draft so dragging back cannot accumulate displacement.
            setLayout(structuredClone(state.before.layout));
            const target = position(widgetsById.get(state.id));

            if (state.kind === 'width-start') {
                const column = nearest(state.starts[state.original.column] + event.clientX - state.x);
                Smartstore.Admin.DashboardEngine.resizeStart(target, column, limits.min, limits.max, limits.widths);
            }
            else if (state.kind === 'width') {
                target.size.columnSpan = Math.max(limits.min, Math.min(
                    limits.max,
                    nearest(state.starts[state.original.column + state.original.size.columnSpan] + event.clientX - state.x) - target.column));
                target.size.columnSpan = limits.widths.reduce((best, width) =>
                    Math.abs(width - target.size.columnSpan) < Math.abs(best - target.size.columnSpan) ? width : best, state.original.size.columnSpan);
            }
            else {
                const y = event.clientY + scrollContainer.scrollTop - state.scroll;
                target.column = Math.min(columns() - target.size.columnSpan, Math.max(
                    0,
                    nearest(state.starts[state.original.column] + event.clientX - state.x)));
                const row = state.rows.find(item => y <= item.bottom);
                target.row = row ? row.row : state.rows.length;
            }

            if (state.kind === 'move') arrange(state.id, state.original);

            state.valid = engine.fits(gridItems(), { ...target, id: state.id }, columns());
            scrollDuringInteraction(event.clientY);
            drawInteractionPreview(state, target);
        }
        function drawInteractionPreview(state, target) {
            if (!state.valid) {
                state.preview?.remove();
                clearInsertionPreview(state);
                state.preview = null;
                return;
            }

            if (!state.preview) {
                state.preview = document.createElement('div');
                state.preview.className = 'dashboard-grid-preview position-fixed border border-primary rounded pe-none z-3';
                state.preview.setAttribute('aria-hidden', 'true');
                document.body.appendChild(state.preview);
            }

            const last = state.rows.at(-1);
            const rowTop = row => state.rows[row]?.top ?? last.bottom + state.rowGap +
                (row - state.rows.length) * (last.bottom - last.top + state.rowGap);
            const insertion = state.insertionRow != null;
            const height = minimumRowHeight();
            // Map the proposed rows to the frozen tracks, including the new minimum-height row.
            const previewRow = row => {
                if (insertion && row === state.insertionRow) return { top: rowTop(row), height };

                const shifted = insertion && row > state.insertionRow;
                const source = shifted ? row - 1 : row;
                return {
                    top: rowTop(source) + (shifted ? height + state.rowGap : 0),
                    height: rowTop(source + 1) - rowTop(source) - state.rowGap
                };
            };
            const start = state.starts[target.column];
            const end = state.starts[target.column + target.size.columnSpan];
            const top = previewRow(target.row).top;
            const bottomRow = previewRow(target.row + (target.size.rowSpan || 1) - 1);
            Object.assign(state.preview.style, {
                left: `${Math.min(start, end) + (start > end ? state.gap : 0)}px`,
                top: `${top - (scrollContainer.scrollTop - state.scroll)}px`,
                width: `${Math.abs(end - start) - state.gap}px`,
                height: `${bottomRow.top + bottomRow.height - top}px`
            });
            clearInsertionPreview(state);

            if (insertion) {
                const marker = document.createElement('div');
                marker.className = 'dashboard-grid-insertion position-fixed d-grid pe-none z-3';
                marker.setAttribute('aria-hidden', 'true');
                Object.assign(marker.style, {
                    top: `${rowTop(state.insertionRow) - (scrollContainer.scrollTop - state.scroll)}px`,
                    height: `${height}px`,
                    gridTemplateColumns: `repeat(${columns()}, minmax(0, 1fr))`,
                    columnGap: `${state.gap}px`,
                    direction: state.starts[0] > state.starts[1] ? 'rtl' : 'ltr',
                    left: `${Math.min(state.starts[0], state.starts[columns()]) + (state.starts[0] > state.starts[1] ? state.gap : 0)}px`,
                    width: `${Math.abs(state.starts[columns()] - state.starts[0]) - state.gap}px`
                });

                for (let column = 0; column < columns(); column++) {
                    const cell = document.createElement('div');
                    cell.className = 'dashboard-grid-cell rounded';
                    marker.appendChild(cell);
                }
                // Open a visual slot without moving grid tracks; the container retains pointer capture.
                const translate = element => {
                    state.translated.push([element, element.style.translate]);
                    element.style.translate = `0 ${height + state.rowGap}px`;
                };

                for (const widget of state.before.layout.widgets) {
                    const element = elements.get(widget.id);

                    if (element && !isNews(widget) && position(widget).row >= state.insertionRow) translate(element);
                }

                gridCells.forEach((cell, index) => {
                    if (Math.floor(index / columns()) >= state.insertionRow) translate(cell);
                });
                document.body.appendChild(marker);
                state.insertionPreview = marker;
            }
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
            document.body.classList.remove('dashboard-dragging', 'dashboard-moving');
            document.body.style.cursor = state.cursor;
            state.preview?.remove();
            clearInsertionPreview(state);
            elements.get(state.id).classList.remove('dashboard-interacting');

            if (state.pointer !== undefined && state.target.hasPointerCapture(state.pointer)) state.target.releasePointerCapture(state.pointer);

            if (cancel || state.valid === false) setLayout(state.before.layout);
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

            if ((kind === 'move' || kind === 'width-start' || kind === 'height-start') && !widget.allowMove) return;

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

            if (kind === 'height' || kind === 'height-start' ? !vertical && event.key !== 'Home' : kind ? !horizontal : !horizontal && !vertical) return;

            if ((!kind || kind === 'width-start' || kind === 'height-start') && !widget.allowMove) return;

            event.preventDefault();
            const before = capture(), p = position(widget), limits = bounds(widget);
            const direction = getComputedStyle(container).direction === 'rtl' ? -1 : 1;
            const delta = (event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1) * (horizontal ? direction : 1);

            if (kind === 'height' || kind === 'height-start') {
                const fromStart = kind === 'height-start';
                const span = event.key === 'Home' ? 1 : (p.size.rowSpan || 1) + delta * (fromStart ? -1 : 1);
                const result = engine.resizeEdge(gridItems(), widget.id, span, layout.rowCounts[breakpoint], fromStart, columns());

                if (!result.items) return;

                const resized = result.items.find(item => item.id === widget.id);

                if (resized.row === p.row && resized.size.rowSpan === p.size.rowSpan) return;

                const rows = getComputedStyle(container).gridTemplateRows.split(' ').map(height => ({ top: 0, bottom: parseFloat(height) }));
                applyItems(result.items);
                reserveRows(rows, result.insertionRow);

                if (result.insertionRow !== null) layout.rowCounts[breakpoint]++;
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

            if (!kind) arrange(widget.id, position(before.layout.widgets.find(item => item.id === widget.id)));

            if (kind !== 'height' && kind !== 'height-start' &&
                !engine.fits(gridItems(), { ...p, id: widget.id }, columns())) {
                setLayout(before.layout);
                return;
            }

            render(false);
            remember(before);
        });

        // A one-use, tab-local transfer keeps undo across server-rendered add previews.
        // Only placement data is transferred; widget HTML, settings and credentials never enter storage.
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

                        layout.rowCounts = history.rowCounts;
                        layout.rowMinimums = history.rowMinimums;
                    }
                }
            }
            catch { /* Continue without transferred history when storage is unavailable or invalid. */ }
        }

        // Preview and failed-save responses use POST-only routes. Reload must use the normal editor GET.
        const editorUrl = new URL(config.editUrl, window.location.href);
        editorUrl.searchParams.set('layer', config.layer);

        if (window.location.href !== editorUrl.href) {
            window.history.replaceState(null, '', editorUrl);
        }

        function serializeLayout(layout) {
            return JSON.stringify({ widgets: layout.widgets.map(
                ({ id, widgetSystemName, positions, settings }) => ({ id, widgetSystemName, positions, settings })) });
        }
        function submit(action, command, widget, transfer = false) {
            // Keep empty editor rows until the user actually saves, including all responsive layouts.
            if (!action) {
                for (const tier of breakpoints) {
                    const items = engine.compact(gridItems(tier));

                    for (const item of items) position(widgetsById.get(item.id), tier).row = item.row;
                }
            }

            updateNewsPositions();
            form.elements.changesJson.value = serializeLayout(layout);
            form.elements.reset.value = command === 'reset' ? 'true' : 'false';
            form.elements.command.value = command || '';
            form.elements.widgetSystemName.value = widget || '';
            const url = new URL(action || form.action, window.location.href);

            if (transfer) {
                const token = crypto.randomUUID();
                try {
                    sessionStorage.setItem('dashboard-history:' + token, JSON.stringify({
                        addedWidget: command === 'add' ? widget : null,
                        layer: config.layer, undo, redo, dirty, rowCounts: layout.rowCounts, rowMinimums: layout.rowMinimums
                    }));
                }
                catch {
                    displayNotification(window.Res.Dashboard.invalid, 'error');
                    return;
                }
                url.searchParams.set('dashboardHistory', token);
            }

            form.action = url.href;
            leaving = true;
            updateControls();
            HTMLFormElement.prototype.submit.call(form);
        }
        function navigate(url) { leaving = true; window.location.assign(url); }
        function guard(action) {
            if (!dirty) { action(); return; }

            confirm2({
                message: window.Res.Dashboard.discardConfirm,
                icon: { type: 'warning' },
                callback: accepted => { if (accepted) action(); }
            });
        }
        function save() {
            if (!dirty || previewing || leaving) return;

            if (config.layer === 'Global') {
                confirm2({
                    message: window.Res.Dashboard.saveGlobalConfirm,
                    icon: { type: 'question' },
                    callback: accepted => { if (accepted) submit(); }
                }).find('.btn-accept').text(window.Res.Dashboard.saveGlobalLabel);
            }
            else submit();
        }
        $('[data-dashboard-edit]').on('click', function () {
            const layer = this.dataset.dashboardEdit;

            if (layer === config.layer) return;

            guard(() => navigate(config.editUrl + '&layer=' + encodeURIComponent(layer)));
        });
        window.addEventListener('pageshow', () => { leaving = false; updateControls(); });
        window.addEventListener('beforeunload', event => {
            if (dirty && !leaving) { event.preventDefault(); event.returnValue = ''; }
        });

        function historyStep(from, to) {
            finish(true);

            if (previewing || !from.length) return;

            const previous = capture();
            const state = from[from.length - 1];
            const settingsChanged = state.layout.widgets.filter(widget =>
                !_.isEqual(widget.settings, widgetsById.get(widget.id)?.settings));
            const missing = state.layout.widgets.some(w => !elements.has(w.id));
            const apply = () => {
                to.push(previous);
                from.pop();
                setLayout(structuredClone(state.layout));
                dirty = state.dirty;

                if (missing) {
                    // Restored widgets may not be rendered on this page. Use their normal server lifecycle.
                    submit(config.previewUrl, null, null, true);
                }
                else render();
            };

            if (!missing && settingsChanged.length) {
                previewing = true;
                updateControls();
                const changesJson = serializeLayout(state.layout);
                const requests = settingsChanged.filter(widget => elements.get(widget.id)?.isConnected).map(widget => {
                    const el = elements.get(widget.id);
                    return $.ajax({
                        url: container.dataset.dashboardLoadUrl,
                        type: 'POST',
                        dataType: 'html',
                        data: {
                            instanceId: widget.id,
                            parameters: $(el).data('dashboardParameters'),
                            changesJson,
                            layer: config.layer
                        }
                    }).done(html => {
                        Smartstore.Admin.Dashboard.updateWidget(el, html);
                    }).fail(() => {
                        displayNotification(window.Res.Dashboard.invalid, 'error');
                    });
                });
                Promise.allSettled(requests).then(results => {
                    if (results.every(result => result.status === 'fulfilled')) apply();
                }).finally(() => { previewing = false; updateControls(); });
            }
            else apply();
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
        $(container).on('click', '.dashboard-remove', function () {
            const el = this.closest('[data-instance-id]');
            confirm2({
                message: window.Res.Dashboard.removeWidgetConfirm,
                icon: { type: 'delete' },
                callback: accepted => {
                    if (!accepted) return;

                    const before = capture();
                    setLayout({ ...layout, widgets: layout.widgets.filter(w => w.id !== el.dataset.instanceId) });
                    arrange(); render(false); remember(before);
                }
            });
        });
        function setConfigurationContent(form, html) {
            const $form = $(form);
            $form.data('validator')?.destroy();
            $form.children('.card-body').html(html);
            initializeEditControls(form);
            reinitFormValidator(form);
            $form.off('submit.validate');
        }
        function focusConfigurationError(form) {
            const field = form.querySelector('.is-invalid, .input-validation-error');
            const pane = field?.closest('.tab-pane');

            if (pane) {
                $(form).find('[data-toggle="tab"]').filter(function () {
                    return this.getAttribute('href') === '#' + pane.id;
                }).tab('show');
            }

            field?.focus();
        }
        function flipConfiguration(el, open) {
            const back = el.querySelector('.dashboard-widget-config');
            const front = el.querySelector('.dashboard-widget-body');
            const button = el.querySelector('.dashboard-configure');
            el.classList.toggle('dashboard-configuring', open);
            front.inert = open;
            back.inert = !open;
            button.setAttribute('aria-expanded', String(open));
            (open ? back.querySelector('input') : button)?.focus();
        }
        $(container).on('click', '.dashboard-configure', function () {
            const button = this;
            const el = button.closest('[data-instance-id]');
            const back = el.querySelector('.dashboard-widget-config');
            const save = back.querySelector('[type="submit"]');

            if (button.disabled) return;

            button.disabled = true;
            save.disabled = true;
            $(back).data('validator')?.destroy();
            back.querySelector('.card-body').replaceChildren();
            flipConfiguration(el, true);
            $.ajax({
                url: back.dataset.configurationUrl,
                type: 'POST',
                data: {
                    instanceId: el.dataset.instanceId,
                    changesJson: serializeLayout(layout),
                    layer: config.layer
                }
            }).done(html => {
                setConfigurationContent(back, html);
                save.disabled = false;

                if (el.classList.contains('dashboard-configuring')) back.querySelector('input')?.focus();
            }).fail(() => {
                displayNotification(window.Res.Dashboard.invalid, 'error');
            }).always(() => {
                button.disabled = false;
            });
        });
        $(container).on('click', '.dashboard-config-cancel', function () {
            flipConfiguration(this.closest('[data-instance-id]'), false);
        });
        $(container).on('keydown', '.dashboard-widget-config', function (event) {
            if (event.key === 'Escape') {
                event.preventDefault();
                flipConfiguration(this.closest('[data-instance-id]'), false);
            }
        });
        $(container).on('submit', '.dashboard-widget-config', function (event) {
            event.preventDefault();

            if (previewing) return;

            const $form = $(this);

            if (!$form.valid()) {
                focusConfigurationError(this);
                return;
            }

            const el = this.closest('[data-instance-id]');
            const widget = widgetsById.get(el.dataset.instanceId);
            const before = capture();
            const data = $form.serialize() + '&' + $.param({
                apply: true,
                parameters: $(el).data('dashboardParameters'),
                instanceId: widget.id,
                changesJson: serializeLayout(layout),
                layer: config.layer
            });
            previewing = true;
            updateControls();
            $.ajax({
                url: this.dataset.configurationUrl,
                type: 'POST',
                data
            }).done(response => {
                if (typeof response === 'string') {
                    setConfigurationContent(this, response);
                    focusConfigurationError(this);
                    return;
                }

                Smartstore.Admin.Dashboard.updateWidget(el, response.html);
                widget.settings = response.settings;
                remember(before);
                flipConfiguration(el, false);
            }).fail(() => {
                displayNotification(window.Res.Dashboard.invalid, 'error');
            }).always(() => {
                previewing = false;
                updateControls();
            });
        });
        $(form).on('submit', event => { event.preventDefault(); save(); });
        $('#dashboard-cancel').on('click', () => {
            if (!dirty || previewing || leaving) return;

            guard(() => navigate(config.cancelUrl));
        });
        $('#dashboard-reset').on('click', () => {
            confirm2({
                message: window.Res.Dashboard.resetConfirm,
                icon: { type: 'warning' },
                callback: accepted => {
                    if (!accepted) return;

                    submit(null, 'reset');
                }
            });
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
