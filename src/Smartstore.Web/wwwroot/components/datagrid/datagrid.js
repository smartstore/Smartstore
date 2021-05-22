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
                edit,
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
                    <thead v-if="!options.hideHeader" class="dg-head">
                        <tr>
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
                                ref="column">
                                <div class="dg-cell dg-cell-header" 
                                    :style="getCellStyles(column, true)" 
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
                    <tbody>
                        <tr v-if="ready && rows.length === 0" class="dg-no-data">
                            <td class="text-muted text-center">
                                <div class="dg-cell justify-content-center">Keine Daten</div>
                            </td>
                        </tr>

                        <tr v-for="(row, rowIndex) in rows" :key="row[options.keyMemberName]" :class="{ 'active': isRowSelected(row), 'dg-edit-row': isInlineEditRow(row) }">
                             <td v-if="allowRowSelection" class="dg-col-pinned alpha">
                                <label class="dg-cell dg-cell-selector">
                                    <span class="dg-cell-value">
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
        },
    },

    data: function () {
        return {
            command: {},
            rows: [],
            total: 0,
            aggregates: [],
            selectedRows: {},
            isBusy: false,
            ready: false,
            isScrollable: false,
            edit: {
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
                                            el.value = v;
                                    }
                                }
                            }
                            else {
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
        this.originalState = {
            vborders: this.options.vborders,
            hborders: this.options.hborders,
            striped: this.options.striped,
            hover: this.options.hover,
            columns: this.columns.map((c, i) => {
                return { member: c.member, hidden: c.hidden, width: c.width, index: i };
            })
        };
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

        userPrefs: {
            get() {
                return {
                    vborders: this.options.vborders,
                    hborders: this.options.hborders,
                    striped: this.options.striped,
                    hover: this.options.hover,
                    columns: this.columns.map((c, i) => {
                        return { member: c.member, hidden: c.hidden, width: c.width, index: i };
                    })
                };
            },
            set(value) {
                if (!value)
                    return;

                this.options.vborders = value.vborders;
                this.options.hborders = value.hborders;
                this.options.striped = value.striped;
                this.options.hover = value.hover;
                console.log(value);
                var self = this;
                value.columns.forEach((c, i) => {
                    var column = self.columns.find(x => x.member === c.member);
                    if (column) {
                        column.width = c.width;
                        column.hidden = c.hidden;
                        // TODO: (core) Handle column index.
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
                localStorage.setItem('sm:grid:state:' + this.options.stateKey, JSON.stringify(value));
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
            localStorage.removeItem("sm:grid:state:" + this.options.stateKey);
            $.extend(this.options, this.originalState);
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

        read() {
            if (this.isBusy)
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
                    self.aggregates = result.aggregates !== undefined ? result.aggregates : [];
                    self.$emit("data-bound", command, self.rows);
                },
                complete() {
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
                                self.command.page = 1;
                            }
                        }
                    });
                }
            });
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
            column.width = 'max-content';
            this.updateRememberedColumnWidth(column);
        },

        // #endregion

        // #region Inline Edit or Insert

        isEditableVisibleColumn(column) {
            return !column.hidden && column.editable;
        },

        isInlineEditRow(row) {
            return this.edit.active && this.edit.row === row;
        },

        isInlineEditCell(row, column) {
            if (!this.edit.active || this.edit.row !== row) {
                return false;
            }

            return column.editable;
        },

        onCellDblClick(e, row) {
            const td = $(e.target).closest('td').get(0);
            const tr = $(td).closest('tr').get(0);
            this.activateEdit(row, tr, td);
        },

        activateEdit(row, tr, td) {
            if (!this.options.allowEdit || !this.dataSource.update || !this.hasEditableVisibleColumn) {
                return;
            }

            this.cancelEdit();

            this.edit.active = true;
            this.edit.row = row;
            this.edit.tr = tr;
            this.edit.td = td;
            this.edit.initialized = false;

            this.rememberColumnWidths();
        },

        cancelEdit() {
            if (!this.edit.active) {
                return;
            }

            this.destroyValidator();

            this.edit.active = false;
            this.edit.row = {};
            this.edit.tr = null;
            this.edit.td = null;
            this.edit.initialized = false;

            this.restoreColumnWidths();
        },

        initializeEditRow() {
            const edit = this.edit;
            if (!edit.active || edit.initialized) {
                return;
            }

            edit.updateEditors();
            edit.initialized = true;

            this.$nextTick(() => {
                // Initialize editor plugins
                window.initializeEditControls(edit.tr);

                // Handle auto-focus
                var elFocus = $(edit.td || edit.tr).find('.dg-cell-edit :input:visible');
                if (elFocus.length === 0) {
                    elFocus = $(edit.tr).find('.dg-cell-edit :input:visible').first();
                }
                elFocus.focus();
            });
        },

        rememberColumnWidths() {
            const self = this;
            self._rememberedColWidths = {};

            $(self.edit.tr).find("td[data-index]").each((i, td) => {
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
            if (this.edit.active && this._rememberedColWidths) {
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
            if (this.isBusy || !this.edit.active || !this.dataSource.update) {
                return;
            }

            if (!this.validateForm()) {
                return;
            }

            const self = this;
            const edit = this.edit;

            edit.bindModel();

            self.isBusy = true;

            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const data = { __RequestVerificationToken: input.value, command: this.command, model: edit.row };

            self.$emit("saving-changes", edit.row);

            $.ajax({
                url: this.dataSource.update,
                type: 'POST',
                cache: false,
                dataType: 'json',
                data: data,
                global: true,
                success(result) {
                    // TODO: (core) More stuff?
                    if (result.success) {
                        self.$emit("saved-changes", edit.row);
                        self.cancelEdit();
                    }
                },
                complete() {
                    self.isBusy = false;
                }
            });
        }

        // #endregion
    }
});