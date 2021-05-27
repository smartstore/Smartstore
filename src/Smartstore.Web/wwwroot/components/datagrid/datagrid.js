const DATAGRID_CELL_MIN_WIDTH = 60;

const DATAGRID_VALIDATION_SETTINGS = {
    ignore: ":hidden, .dg-cell-selector-checkbox",
    errorPlacement: function (error, input) {
        input.closest('.dg-cell-edit').append(error.addClass("input-validation shadow shadow-danger shadow-sm"));
    },
    success: function (error, input) {
        error.remove();
    },
    //invalidHandler: function () {
    //    console.log("invalidHandler", arguments);
    //},
    //showErrors: function () {
    //    console.log("showErrors", arguments);
    //},
    //highlight: function () {
    //    console.log("highlight", arguments);
    //},
    //unhighlight: function () {
    //    console.log("unhighlight", arguments);
    //}
};

// https://dev.to/loilo92/an-approach-to-vuejs-template-variables-5aik
// TODO: (core) Move Vue.pass component to a central location.
Vue.component("pass", {
    render() {
        return this.$scopedSlots.default(this.$attrs);
    }
});

Vue.component("sm-data-grid", {
    template: `
        <div class="datagrid" :style="{ maxHeight: options.maxHeight }" ref="grid">
            <slot name="toolbar" v-bind="{ 
                selectedRows, 
                selectedRowsCount,
                selectedRowKeys, 
                hasSelection,
                command,
                rows,
                editing,
                insertRow,
                saveChanges,
                cancelEdit,
                deleteSelected,
                resetState }">
            </slot>

            <div v-if="paging.enabled && (paging.position === 'top' || paging.position === 'both')" class="dg-pager-wrapper border-bottom">
                <sm-data-grid-pager :paging="paging" :command="command" :rows="rows" :total="total" :max-pages-to-display="10"></sm-data-grid-pager>
            </div>
            <component :is="options.allowEdit ? 'form' : 'div'" ref="tableWrapper" class="dg-table-wrapper">
                <table ref="table"
                    :class="getTableClass()"
                    :style="getTableStyles()">
                    <thead v-show="!options.hideHeader" class="dg-head" ref="tableHeader">
                        <tr ref="tableHeaderRow">
                            <th v-if="allowRowSelection" class="dg-col-pinned alpha">
                                <label class="dg-cell dg-cell-header dg-cell-selector">
                                    <span class="dg-cell-value">
                                        <input type="checkbox" class="dg-cell-selector-checkbox" ref="masterSelector" @change="onSelectAllRows($event)" />
                                    </span>
                                </label>
                            </th>            
                
                            <th v-for="(column, columnIndex) in columns" 
                                v-show="!column.hidden"
                                :data-member="column.member"
                                :data-index="columnIndex"
                                :draggable="options.allowColumnReordering && column.reorderable"
                                v-on:dragstart="onColumnDragStart"
                                v-on:dragenter.stop="onColumnDragEnter"
                                v-on:dragover.stop="onColumnDragOver"
                                v-on:dragleave.stop="onColumnDragLeave"
                                v-on:dragend="onColumnDragEnd"
                                v-on:drop="onColumnDrop"
                                ref="column">
                                <div class="dg-cell dg-cell-header" 
                                    :style="getCellStyles(null, column, true)" 
                                    :class="{ 'dg-sortable': sorting.enabled && column.sortable }"
                                    :title="column.title ? null : column.name"
                                    @click="onSort($event, column)">
                                    <i v-if="column.icon" class="dg-icon" :class="column.icon"></i>
                                    <span class="dg-cell-value">{{ column.title }}</span>
                                    <i v-if="isSortedAsc(column)" class="fa fa-fw fa-sm fa-arrow-up mx-1"></i>
                                    <i v-if="isSortedDesc(column)" class="fa fa-fw fa-sm fa-arrow-down mx-1"></i>
                                </div>
                                <div v-if="options.allowResize && column.resizable" 
                                    class="dg-resize-handle" 
                                    @mousedown.stop.prevent="onStartResize($event, column, columnIndex)"
                                    @dblclick.stop.prevent="autoSizeColumn($event, column, columnIndex)">
                                </div>
                            </th>
                            <th>
                                <div class="dg-cell dg-cell-header dg-cell-spacer">&nbsp;</div>
                            </th>
                        </tr>
                    </thead>
                    <tbody ref="tableBody">
                        <tr v-if="ready && rows.length === 0" class="dg-no-data">
                            <td class="text-muted text-center">
                                <div class="dg-cell justify-content-center">Keine Daten</div>
                            </td>
                        </tr>

                        <tr v-for="(row, rowIndex) in rows" :key="row[options.keyMemberName]" :class="{ 'active': isRowSelected(row), 'dg-edit-row': isInlineEditRow(row) }">
                             <td v-if="allowRowSelection" class="dg-col-pinned alpha">
                                <label class="dg-cell dg-cell-selector">
                                    <span v-if="!isInlineEditRow(row) || !editing.insertMode" class="dg-cell-value">
                                        <input type="checkbox" class="dg-cell-selector-checkbox" :checked="isRowSelected(row)" @change="onSelectRow($event, row)" />
                                    </span>
                                </label>
                            </td>             

                            <td v-for="(column, columnIndex) in columns" 
                                v-show="!column.hidden"
                                :data-index="columnIndex"
                                :key="row[options.keyMemberName] + '-' + columnIndex"
                                @dblclick="onCellDblClick($event, row)">
                                <div class="dg-cell" :class="getCellClass(row, column)" :style="getCellStyles(row, column, false)">
                                    <slot v-if="!isInlineEditCell(row, column)" :name="'display-' + column.member.toLowerCase()" v-bind="{ row, rowIndex, column, columnIndex, value: row[column.member] }">
                                        <span class="dg-cell-value">
                                            <template v-if="column.type === 'boolean'">
                                                <i class="fa fa-fw" :class="'icon-active-' + row[column.member]"></i>
                                            </template>
                                            <template v-else>
                                                {{ renderCellValue(row[column.member], column, row) }}
                                            </template>
                                        </span>
                                    </slot>
                                    <slot v-if="isInlineEditCell(row, column)" :name="'edit-' + column.member.toLowerCase()" v-bind="{ row, rowIndex, column, columnIndex, value: row[column.member] }">
                                    </slot>
                                </div>
                            </td>
                            <td>
                                <div class="dg-cell dg-cell-spacer"></div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </component>
            <div v-if="paging.enabled && (paging.position === 'bottom' || paging.position === 'both')" class="dg-pager-wrapper border-top">
                <sm-data-grid-pager :paging="paging" :command="command" :rows="rows" :total="total" :max-pages-to-display="10"></sm-data-grid-pager>
            </div>

            <div v-show="isBusy" class="dg-blocker"></div>
        </div>
    `,

    props: {
        options: {
            type: Object,
            required: false,
            default: {}
        },

        dataSource: {
            type: Object,
            required: true
        },

        columns: {
            type: Array,
            required: true
        },

        paging: {
            type: Object,
            required: false,
            default() { return { enabled: false, pageIndex: 1, pageSize: 25, position: "bottom" } }
        },

        sorting: {
            type: Object,
            required: false,
            default() { return { enabled: false, descriptors: [] } }
        },

        filtering: {
            type: Object,
            required: false,
            default() { return { enabled: false, descriptors: [] } }
        }
    },

    data: function () {
        return {
            command: {},
            rows: [],
            total: 0,
            aggregates: [],
            selectedRows: {},
            originalState: {},
            isBusy: false,
            ready: false,
            isScrollable: false,
            dragging: {},
            editing: {
                active: false,
                insertMode: false,
                row: {},
                tr: null,
                getEditors() {
                    if (!this.tr) return [];
                    return this.tr.querySelectorAll('.dg-cell-edit input, .dg-cell-edit textarea, .dg-cell-edit select')
                },
                updateEditors(editors) {
                    const r = this.row;
                    (editors || this.getEditors()).forEach(el => {
                        if (el.name) {
                            const v = r[el.name];
                            if (el.tagName.toLowerCase() === "input") {
                                if (el.type !== "hidden") {
                                    switch (el.type) {
                                        case "checkbox":
                                        case "radio":
                                            el.checked = v;
                                            break;
                                        default:
                                            // TODO: (core) datetimepicker will not update! Investigate!
                                            if (v !== undefined && v !== null)
                                                el.value = v;
                                    }
                                }
                            }
                            else {
                                if (v !== undefined && v!== null)
                                    el.value = v;
                            }
                        }
                    });
                },
                bindModel(editors) {
                    const r = this.row;
                    (editors || this.getEditors()).forEach(el => {
                        if (el.name) {
                            if (el.tagName.toLowerCase() === "input") {
                                if (el.type !== "hidden") {
                                    switch (el.type) {
                                        case "checkbox":
                                        case "radio":
                                            r[el.name] = el.checked;
                                            break;
                                        case "number":
                                        case "range":
                                            r[el.name] = parseFloat(el.value);
                                            break;
                                        case "date": // TODO: (core) Bind input[type=date]
                                        case "time": // TODO: (core) Bind input[type=time]
                                        case "datetime": // TODO: (core) Bind input[type=time]
                                        case "datetime-local": // TODO: (core) Bind input[type=time]
                                        default:
                                            r[el.name] = el.value;
                                    }
                                }
                            }
                            else {
                                r[el.name] = el.value;
                            }
                        }
                    });
                }
            }
        }
    },

    created() {
        const self = this;
        this.command = this.buildCommand();

        // Load user prefs
        this.originalState = this.getGridState();
        if (this.options.preserveState) {
            this.userPrefs = JSON.parse(localStorage.getItem('sm:grid:state:' + this.options.stateKey));
        }  

        this.$on('data-binding', command => {
            call('onDataBinding', command);
        });

        this.$on('data-bound', (command, rows) => {
            this.setMasterSelectorState(this.getMasterSelectorState());
            call('onDataBound', command, rows);
        });

        this.$on('row-selected', (selectedRows, row, selected) => {
            call('onRowSelected', selectedRows, row, selected);
        });

        function call(name) {
            if (_.isString(self.options[name])) {
                const args = Array.prototype.splice.call(arguments, 1);
                window[self.options[name]].apply(self, args);
            }
        }
    },

    mounted () {
        const self = this;

        // Handle sticky columuns on resize
        const resizeObserver = new ResizeObserver(entries => {
            const tableWrapper = entries[0].target;
            self.isScrollable = tableWrapper.offsetWidth < tableWrapper.scrollWidth;
        });
        resizeObserver.observe(this.$refs.tableWrapper);

        // Read data from server
        this.read();
    },

    updated() {
        this.initializeEditRow();
    },

    computed: {
        allowRowSelection() {
            return this.options.allowRowSelection && this.rows && this.rows.length > 0;
        },

        selectedRowsCount() {
            return Object.values(this.selectedRows).length;
        },

        hasSelection() {
            return Object.values(this.selectedRows).length > 0;
        },

        selectedRowKeys() {
            return Object.values(this.selectedRows).map(row =>
            {
                return row[this.options.keyMemberName];
            });
        },

        selectedRowsInCurrentPage() {
            if (this.selectedRowsCount === 0)
                return [];

            const selectedRows = this.selectedRows;
            return this.rows.filter(row => selectedRows[row[this.options.keyMemberName]] !== undefined);
        },

        hasEditableVisibleColumn() {
            return this.columns.some(this.isEditableVisibleColumn);
        },

        totalPages() {
            const pageSize = this.command.pageSize;
            if (pageSize === 0)
                return 0;

            let total = this.total / pageSize;
            if (this.total % pageSize > 0)
                total++;

            return Math.floor(total);
        },

        userPrefs: {
            get() {
                return this.getGridState();
            },
            set(value) {
                if (!value)
                    return;

                $.extend(this.options, value.options);
 
                value.columns.forEach((userColumn, userIndex) => {
                    const column = this.columns.find(x => x.member === userColumn.member);
                    if (column) {
                        column.width = userColumn.width;
                        column.hidden = userColumn.hidden;
                        this.changeColumnOrder(column, userIndex);
                    }
                });
            }
        }
    },

    watch: {
        command: {
            handler: function () {
                this.read();
            },
            deep: true
        },
        selectedRows() {
            this.setMasterSelectorState(this.getMasterSelectorState());
        },
        userPrefs(value) {
            if (this.options.preserveState) {
                var key = 'sm:grid:state:' + this.options.stateKey;
                if (value) {
                    localStorage.setItem(key, JSON.stringify(value));
                }
                else {
                    localStorage.removeItem(key);
                }
            }
        }
    },

    methods: {
        // #region Rendering

        getTableClass() {
            const cssClass = {
                'dg-table': true,
                'dg-striped': this.options.striped,
                'dg-hover': this.options.hover,
                'dg-scrollable': this.isScrollable,
                //'dg-condensed': this.options.condensed
            };

            return cssClass;
        },

        getTableStyles() {
            const style = { 'grid-template-columns': this.getGridTemplateColumns() };

            style['row-gap'] = this.options.vborders ? "1px" : "0";
            style['column-gap'] = this.options.hborders ? "1px" : "0";

            return style;
        },

        getCellStyles(row, column, isHeader) {
            const style = {};

            if (column.halign) {
                style.justifyContent = column.halign;
            }

            if (column.valign && !isHeader) {
                style.alignItems = column.valign;
            }

            //if (column.flow) {
            //    style.flexFlow = column.flow;
            //}

            return style;
        },

        getCellClass(row, column) {
            const cssClass = {
                'dg-cell-wrap': !column.nowrap,
                'dg-cell-edit': this.isInlineEditCell(row, column)
            };

            return cssClass;
        },

        getGridTemplateColumns() {
            let hasFraction = false;
            let result = this.columns
                .filter(c => !c.hidden)
                .map(c => {
                    let w = c.width;
                    if (_.isEmpty(w)) {
                        switch (c.type) {
                            case "int":
                            case "float":
                            case "boolean":
                            case "date":
                                w = "auto";
                                break;
                            default:
                                w = "minmax({0}px, 1fr)".format(DATAGRID_CELL_MIN_WIDTH);
                        }
                    }
                    if (!hasFraction) {
                        hasFraction = w === 'auto' || w.indexOf('fr') > -1;
                    }
                    return w;
                });

            if (this.allowRowSelection) {
                result.splice(0, 0, "48px");
            }

            // Spacer always 'auto' to fill remaining area
            result.push(hasFraction ? "0" : "auto");

            return result.join(' ');
        },

        getGridState() {
            return {
                options: {
                    vborders: this.options.vborders,
                    hborders: this.options.hborders,
                    striped: this.options.striped,
                    hover: this.options.hover
                },
                columns: this.columns.map(c => {
                    return { member: c.member, hidden: c.hidden, width: c.width };
                })
            };
        },

        renderCellValue(value, column, row) {
            const t = column.type;

            if (t === 'int') {
                return Smartstore.globalization.formatNumber(value);
            }
            else if (t === 'float') {
                return Smartstore.globalization.formatNumber(value, 'N2');
            }
            else if (t === 'date') {
                return moment(value).format(column.format || 'L LTS');
            }
            else if (column.format) {
                return value.format(column.format);
            }

            return value;
        },

        resetState() {
            // Reset options
            $.extend(this.options, this.originalState.options);

            // Reset columns
            this.originalState.columns.forEach((originalColumn, originalIndex) => {
                let column = this.columns.find(c => c.member === originalColumn.member);
                if (column) {
                    column.width = originalColumn.width;
                    column.hidden = originalColumn.hidden;
                    this.changeColumnOrder(column, originalIndex);
                }
            });

            this.userPrefs = null;
        },

        // #endregion

        // #region Commands

        buildCommand() {
            const p = this.paging;
            const s = this.sorting;
            const command = {
                page: p.pageIndex,
                pageSize: p.pageSize,
                sorting: s.descriptors
            };

            return command;
        },

        read(force) {
            if (!force && this.isBusy)
                return;

            const self = this;
            self.cancelEdit();
            self.isBusy = true;

            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const command = $.extend(true, { __RequestVerificationToken: input.value }, this.command);

            self.$emit("data-binding", command);
            
            $.ajax({
                url: this.dataSource.read,
                type: 'POST',
                cache: false,
                dataType: 'json',
                data: command,
                global: true,
                success(result) {
                    self.rows = result.rows !== undefined ? result.rows : result;
                    self.total = result.total || self.rows.length;

                    if (self.command.page > self.totalPages) {
                        // Fix "pageIndex > totalPages" by reloading
                        self.isBusy = false;
                        self.command.page = self.totalPages;
                    }
                    else {
                        self.aggregates = result.aggregates !== undefined ? result.aggregates : [];
                        self.$emit("data-bound", command, self.rows);
                        self.ready = true;
                        self.isBusy = false;
                    }
                },
                error() {
                    self.ready = true;
                    self.isBusy = false;
                }
            });
        },

        deleteSelected() {
            const numSelected = this.selectedRowsCount;

            if (this.isBusy || !numSelected || !this.dataSource.deleteSelected)
                return;

            const self = this;

            confirm2({
                message: "Sollen die gewählten {0} Datensätze wirklich unwiderruflich gelöscht werden?".format(numSelected),
                icon: { type: 'delete' },
                callback: accepted => {
                    if (!accepted)
                        return;

                    const selectedKeys = this.selectedRowKeys;
                    const input = document.querySelector('input[name=__RequestVerificationToken]');
                    const selection = $.extend(true, { __RequestVerificationToken: input.value }, { selectedKeys: selectedKeys });

                    self.$emit("deleting-rows", selectedKeys);

                    $.ajax({
                        url: this.dataSource.deleteSelected,
                        type: 'POST',
                        cache: false,
                        dataType: 'json',
                        data: selection,
                        global: true,
                        success(result) {
                            if (result.Success) {
                                self.selectedRows = {};
                                displayNotification("{0} Datensätze erfolgreich gelöscht.".format(result.Count || numSelected), "success");
                                self.$emit("deleted-rows", selectedKeys);
                                self.read();
                            }
                        }
                    });
                }
            });
        },

        // #endregion

        // #region Column reordering

        onColumnDragStart(e) {
            this.dragging.active = true;
            this.dragging.source = e.target;
            this.dragging.sourceIndex = parseInt(e.target.getAttribute("data-index"));
            this.dragging.sourceMember = e.target.getAttribute("data-member");
            this.dragging.sourceColumn = this.columns[this.dragging.sourceIndex];
        },

        onColumnDragEnter(e) {
            e.preventDefault();
            if (this.dragging.active) {
                this.dragging.lastEnter = e.target;
            }
        },

        onColumnDragOver(e) {
            if (this.dragging.active) {
                const th = $(e.target).closest('th').get(0);
                if (th === this.dragging.source) {
                    this.dragging.target = null;
                    e.dataTransfer.dropEffect = 'none';
                }
                else {
                    this.dragging.target = th;
                    e.preventDefault();
                    e.dataTransfer.dropEffect = "move";
                    //console.log("onColumnDragOver", th);
                }
            }
        },

        onColumnDragLeave(e) {
            //console.log("onColumnDragLeave", this.dragging);
        },

        onColumnDrop(e) {
            if (this.dragging.active && this.dragging.target) {
                const th = this.dragging.target;
                const index = parseInt(th.getAttribute("data-index"));

                //console.log("onColumnDrop", this.columns[this.dragging.sourceIndex], index);
                this.changeColumnOrder(this.dragging.sourceColumn, index);
            }
        },

        onColumnDragEnd(e) {
            this.dragging = {};
            //console.log("onColumnDragEnd", this.dragging);
        },

        changeColumnOrder(column, newIndex) {
            const curIndex = this.columns.indexOf(column);
            if (curIndex > -1 && curIndex !== newIndex) {
                this.columns.splice(newIndex, 0, this.columns.splice(curIndex, 1)[0]);
            }
        },

        // #endregion

        // #region Sorting

        isSortedAsc(column) {
            const sort = this.getSortDescriptor(column);
            return sort && !sort.descending;
        },

        isSortedDesc(column) {
            const sort = this.getSortDescriptor(column);
            return sort && sort.descending;
        },

        getSortDescriptor(column) {
            return this.sorting.enabled
                ? this.command.sorting.find(x => x.member === column.entityMember || x.member === column.member)
                : null;
        },

        onSort(e, column) {
            if (!this.sorting.enabled || !column.sortable || this.isBusy)
                return;

            let descriptor = this.getSortDescriptor(column);
            let multiMode = this.sorting.allowMultiSort && e.ctrlKey;

            if (descriptor) {
                if (this.sorting.allowUnsort && descriptor.descending) {
                    this.command.sorting = this.command.sorting.filter(x => x != descriptor);
                    descriptor = null;
                }
                else {
                    descriptor.descending = !descriptor.descending;
                }
            }
            else {
                descriptor = { member: column.entityMember || column.member, descending: false };
                this.command.sorting.push(descriptor);
            }

            if (descriptor && !multiMode) {
                this.command.sorting = this.command.sorting.filter(x => x === descriptor);
            }
        },

        // #endregion

        // #region Row selection

        getMasterSelectorState() {
            const numSelected = this.selectedRowsInCurrentPage.length;

            return {
                checked: numSelected === this.rows.length,
                indeterminate: numSelected > 0 && numSelected < this.rows.length
            };
        },

        setMasterSelectorState(state) {
            let chk = this.$refs.masterSelector;
            if (!chk || !state) return;

            chk.checked = state.checked;
            chk.indeterminate = state.indeterminate;
        },

        isRowSelected(row) {
            return this.selectedRows[row[this.options.keyMemberName]];
        },

        onSelectAllRows(e) {
            const state = this.getMasterSelectorState();
            this.rows.forEach(x => {
                this.onSelectRow(e, x, state.indeterminate);
            });
        },

        onSelectRow(e, row, select) {
            if (!row)
                return;

            const key = row[this.options.keyMemberName];
            const selectedRow = this.selectedRows[key];
            if (selectedRow && !select) {
                this.$delete(this.selectedRows, key);
                this.$emit('row-selected', this.selectedRows, row, false);
            }
            else if (!selectedRow) {
                this.$set(this.selectedRows, key, row);
                this.$emit('row-selected', this.selectedRows, row, true);
            }
        },

        // #endregion

        // #region Resizing

        onStartResize(e, column, columnIndex) {
            this.isResizing = true;
            this.resizeIndex = columnIndex;
            this.resizeColumn = column;
            this.resizeHeader = e.target.parentNode;
            this.resizeX = e.pageX;

            window.addEventListener('mousemove', this.onResize, false);
            window.addEventListener('mouseup', this.onStopResize, false);

            this.$refs.table.classList.add('dg-resizing');
        },

        onResize: _.throttle(function (e) {
            if (this.resizeX === null) {
                return;
            }

            const self = this;

            requestAnimationFrame(() => {
                const pageX = e.pageX;
                const diffX = pageX - self.resizeX;
                const width = self.resizeHeader.offsetWidth + diffX;

                self.resizeX = pageX;

                if (width < DATAGRID_CELL_MIN_WIDTH) {
                    return;
                }

                self.resizeColumn.width = width + 'px';
                self.updateRememberedColumnWidth(self.resizeColumn);
            });
        }, 20, true),

        onStopResize(e) {
            window.removeEventListener('mousemove', this.onResize);
            window.removeEventListener('mouseup', this.onStopResize);

            this.$refs.table.classList.remove('dg-resizing');

            this.resizeIndex = null;
            this.resizeColumn = null;
            this.resizeHeader = null;
            this.resizeX = null;
            this.isResizing = false;
        },

        autoSizeColumn(e, column, columnIndex) {
            var ctrl = e.ctrlKey;
            if (!ctrl) {
                column.width = 'max-content';
                this.updateRememberedColumnWidth(column);
            }
            else {
                this.columns.filter(c => c.resizable).forEach(c =>
                {
                    c.width = 'max-content';
                    this.updateRememberedColumnWidth(c);
                });
            }
        },

        // #endregion

        // #region Inline Edit or Insert

        isEditableVisibleColumn(column) {
            return !column.hidden && column.editable;
        },

        isInlineEditRow(row) {
            return this.editing.active && this.editing.row === row;
        },

        isInlineEditCell(row, column) {
            if (!this.editing.active || this.editing.row !== row) {
                return false;
            }

            return column.editable;
        },

        onCellDblClick(e, row) {
            const td = $(e.target).closest('td').get(0);
            const tr = $(td).closest('tr').get(0);
            this.activateEdit(row, tr, td);
        },

        insertRow() {
            if (!this.options.allowEdit || !this.dataSource.insert || !this.hasEditableVisibleColumn) {
                return;
            }

            this.cancelEdit();

            let row = this.options.defaultDataRow || {};
            this.rows.splice(0, 0, row);

            this.editing.active = true;
            this.editing.insertMode = true;
            this.editing.row = row;
            this.editing.initialized = false;

            this.rememberColumnWidths();
        },

        activateEdit(row, tr, td) {
            if (!this.options.allowEdit || !this.dataSource.update || !this.hasEditableVisibleColumn) {
                return;
            }

            this.cancelEdit();

            this.editing.active = true;
            this.editing.insertMode = false;
            this.editing.row = row;
            this.editing.tr = tr;
            this.editing.td = td;
            this.editing.initialized = false;

            this.rememberColumnWidths();
        },

        cancelEdit() {
            if (!this.editing.active) {
                return;
            }

            this.destroyValidator();

            if (this.rows.length && this.rows[0] === this.editing.row) {
                // Remove inserted row
                this.rows.splice(0, 1);
            }

            this.editing.active = false;
            this.editing.row = {};
            this.editing.tr = null;
            this.editing.td = null;
            this.editing.initialized = false;

            this.restoreColumnWidths();
        },

        initializeEditRow() {
            const editing = this.editing;
            if (!editing.active || editing.initialized) {
                return;
            }

            if (!editing.tr) {
                editing.tr = this.$refs.table.querySelector("tr.dg-edit-row");
            }

            editing.updateEditors();
            editing.initialized = true;

            this.$nextTick(() => {
                // Initialize editor plugins
                window.initializeEditControls(editing.tr);

                // Handle auto-focus
                var elFocus = $(editing.td || editing.tr).find('.dg-cell-edit :input:visible');
                if (elFocus.length === 0) {
                    elFocus = $(editing.tr).find('.dg-cell-edit :input:visible');
                }
                elFocus.first().focus();
            });
        },

        rememberColumnWidths() {
            const self = this;
            self._rememberedColWidths = {};

            $(this.$refs.tableBody).find("> tr:first > td[data-index]").each((i, td) => {
                const colIndex = parseInt($(td).data("index"));
                const column = self.columns[colIndex];

                self._rememberedColWidths[column.member] = column.width;

                const tdWidth = getComputedStyle(td).width;
                column.width = tdWidth;
            });
        },

        restoreColumnWidths() {
            let widths = this._rememberedColWidths;
            if (!widths) {
                return;
            }

            const self = this;
            self.columns.forEach(c => {
                if (widths[c.member] !== undefined) {
                    c.width = widths[c.member];
                }
            });

            this._rememberedColWidths = null;
        },

        updateRememberedColumnWidth(column) {
            if (this.editing.active && this._rememberedColWidths) {
                this._rememberedColWidths[column.member] = column.width;
            }
        },

        destroyValidator() {
            var validator = $(this.$refs.tableWrapper).data("validator");
            if (validator) {
                validator.hideErrors();
                validator.destroy();
            }
        },

        validateForm() {
            var form = $(this.$refs.tableWrapper);
            if (!form.is('form')) {
                return false;
            }

            form
                .removeData("validator")
                .removeData("unobtrusiveValidation")
                .off(".validate");

            $.validator.unobtrusive.parse(form);
            var validator = form.validate();
            $.extend(validator.settings, DATAGRID_VALIDATION_SETTINGS);

            return validator.form();
        },

        saveChanges() {
            if (this.isBusy || !this.editing.active) {
                return;
            }

            var url = this.editing.insertMode ? this.dataSource.insert : this.dataSource.update;
            if (!url) {
                return;
            }

            if (!this.validateForm()) {
                return;
            }

            const self = this;
            const editing = this.editing;

            editing.bindModel();

            self.isBusy = true;

            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const data = { __RequestVerificationToken: input.value, command: this.command, model: editing.row };

            self.$emit("saving-changes", editing.row);

            $.ajax({
                url: url,
                type: 'POST',
                cache: false,
                dataType: 'json',
                data: data,
                global: true,
                success(result) {
                    // TODO: (core) More stuff?
                    if (result.success) {
                        self.$emit("saved-changes", editing.row);
                        self.read(true);
                        self.cancelEdit();
                    }
                },
                error() {
                    self.isBusy = false;
                }
            });
        }

        // #endregion
    }
});