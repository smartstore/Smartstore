// #region Script enhancements (move later)

// TODO: (core) Move the extended String.prototype.format to smartstore.system.js once bundling is ready

String.prototype.format = function () { 
    function getType(o) {
        const t = typeof o;

        if (t === "number" || t === "boolean") {
            return t;
        }
        if (o instanceof Date) {
            return "date";
        }
        if (isNaN(o) && !isNaN(Date.parse(o))) {
            return "date";
        }
        return null;
    }

    let g = Smartstore.globalization;
    let s = this, args = arguments;

    for (var i = 0, len = args.length; i < len; i++) {
        let rg = new RegExp("\\{" + i + "(:([^\\}]+))?\\}", "gm");
        let arg = args[i];
        let type = getType(arg), formatter;

        if (g) {
            if (type === "number") {
                formatter = g.formatNumber;
            }
            else if (type === "date") {
                formatter = g.formatDate;
            }

            if (formatter) {
                let match = rg.exec(s);
                if (match) {
                    arg = formatter(arg, match[2]);
                }
            }
        }

        s = s.replace(rg, function () {
            return arg;
        });
    }

    return s;
};

Smartstore.globalization.formatDate = function (value, format) {
    let g = Smartstore.globalization;
    format = format || g.culture.dateTimeFormat.patterns.G;

    if (format.length === 1) {
        // Maybe pattern shortcut, like "G", "F", "d" etc.
        let patterns = g.culture.dateTimeFormat.patterns;
        format = patterns[format] || format;
    }

    let momentFormat = g.convertDatePatternToMomentFormat(format);
    return moment(value).format(momentFormat);
};

Smartstore.globalization.formatNumber = function (value, format) {
    function truncate(value) {
        if (isNaN(value)) {
            return NaN;
        }
        return Math[value < 0 ? "ceil" : "floor"](value);
    }

    function zeroPad(str, count, left) {
        var l;
        for (l = str.length; l < count; l += 1) {
            str = (left ? ("0" + str) : (str + "0"));
        }
        return str;
    };

    function expandNumber (number, precision, formatInfo) {
        var groupSizes = formatInfo.groupSizes,
            curSize = groupSizes[0],
            curGroupIndex = 1,
            factor = Math.pow(10, precision),
            rounded = Math.round(number * factor) / factor;

        if (!isFinite(rounded)) {
            rounded = number;
        }
        number = rounded;

        var numberString = number + "",
            right = "",
            split = numberString.split(/e/i),
            exponent = split.length > 1 ? parseInt(split[1], 10) : 0;
        numberString = split[0];
        split = numberString.split(".");
        numberString = split[0];
        right = split.length > 1 ? split[1] : "";

        if (exponent > 0) {
            right = zeroPad(right, exponent, false);
            numberString += right.slice(0, exponent);
            right = right.substr(exponent);
        }
        else if (exponent < 0) {
            exponent = -exponent;
            numberString = zeroPad(numberString, exponent + 1, true);
            right = numberString.slice(-exponent, numberString.length) + right;
            numberString = numberString.slice(0, -exponent);
        }

        if (precision > 0) {
            right = formatInfo["."] +
                ((right.length > precision) ? right.slice(0, precision) : zeroPad(right, precision));
        }
        else {
            right = "";
        }

        var stringIndex = numberString.length - 1,
            sep = formatInfo[","],
            ret = "";

        while (stringIndex >= 0) {
            if (curSize === 0 || curSize > stringIndex) {
                return numberString.slice(0, stringIndex + 1) + (ret.length ? (sep + ret + right) : right);
            }
            ret = numberString.slice(stringIndex - curSize + 1, stringIndex + 1) + (ret.length ? (sep + ret) : "");

            stringIndex -= curSize;

            if (curGroupIndex < groupSizes.length) {
                curSize = groupSizes[curGroupIndex];
                curGroupIndex++;
            }
        }

        return numberString.slice(0, stringIndex + 1) + sep + ret + right;
    };

    var g = Smartstore.globalization;
    var culture = g.culture;

    if (!isFinite(value)) {
        if (value === Infinity) {
            return culture.numberFormat.positiveInfinity;
        }
        if (value === -Infinity) {
            return culture.numberFormat.negativeInfinity;
        }
        return culture.numberFormat.NaN;
    }
    if (!format || format === "i") {
        return culture.name.length ? value.toLocaleString() : value.toString();
    }
    format = format || "D";

    var nf = culture.numberFormat,
        number = Math.abs(value),
        precision = -1,
        pattern;
    if (format.length > 1) precision = parseInt(format.slice(1), 10);

    var current = format.charAt(0).toUpperCase(),
        formatInfo,
        patterns;

    if (current == "N" || current == "D") {
        formatInfo = nf;
        patterns = g.patterns.numeric;
    }

    if (current == "D") {
        pattern = "n";
        number = truncate(number);
        if (precision !== -1) {
            number = zeroPad("" + number, precision, true);
        }
        if (value < 0) number = "-" + number;
    }
    else if (current == "C") {
        formatInfo = nf.currency;
        patterns = g.patterns.currency;
    }
    else if (current == "P") {
        formatInfo = nf.percent;
        patterns = g.patterns.percent;
    }

    if (!formatInfo || !patterns) {
        throw "Bad number format specifier: " + current;
    }

    if (!pattern) {
        if (value < 0 && patterns.negative) {
            pattern = patterns.negative[formatInfo.pattern[0]];
        }
        else if (value >= 0 && patterns.positive) {
            pattern = patterns.positive[formatInfo.pattern[1]];
        }
    }

    pattern = pattern || "n";

    if (current != "D") {
        if (precision === -1) precision = formatInfo.decimals;
        number = expandNumber(number * (current === "P" ? 100 : 1), precision, formatInfo);
    }

    var patternParts = /n|\$|-|%/g,
        ret = "";
    for (; ;) {
        var index = patternParts.lastIndex,
            ar = patternParts.exec(pattern);

        ret += pattern.slice(index, ar ? ar.index : pattern.length);

        if (!ar) {
            break;
        }

        switch (ar[0]) {
            case "n":
                ret += number;
                break;
            case "$":
                ret += nf.currency.symbol;
                break;
            case "-":
                // don't make 0 negative
                if (/[1-9]/.test(number)) {
                    ret += nf["-"];
                }
                break;
            case "%":
                ret += nf.percent.symbol;
                break;
        }
    }

    return ret;
};

// #endregion

const DATAGRID_CELL_MIN_WIDTH = 60;

const DATAGRID_VALIDATION_SETTINGS = {
    ignore: ":hidden, .dg-cell-selector-checkbox",
    errorPlacement: function (error, input) {
        input.closest('.dg-cell-edit').append(error.addClass("input-validation shadow shadow-danger shadow-sm"));
    },
    success: function (error, input) {
        error.remove();
    }
};

// https://dev.to/loilo92/an-approach-to-vuejs-template-variables-5aik
// TODO: (core) Move Vue.pass component to a central location.
Vue.component("pass", {
    render() {
        return this.$scopedSlots.default(this.$attrs);
    }
});

Vue.component("sm-datagrid", {
    template: `
        <div class="datagrid" 
            :style="{ maxHeight: options.maxHeight, '--dg-search-width': options.searchPanelWidth }" 
            :class="{ 'datagrid-has-search': hasSearchPanel }" 
            ref="grid">

            <div v-if="hasSearchPanel" class="dg-search d-flex flex-column" :class="{ show: options.showSearch }">
                <div class="dg-search-header d-flex py-3 mx-3">
                    <h6 class="m-0 text-muted">Filter</h6>
                </div>
                <form class="dg-search-body p-3 m-0">
                    <slot name="search" v-bind="{ command, rows, editing }"></slot>  
                </form>
            </div>        

            <div class="dg-grid">
                <slot name="toolbar" v-bind="{
                    selectedRows, 
                    selectedRowsCount,
                    selectedRowKeys, 
                    hasSelection,
                    hasSearchPanel,
                    numSearchFilters,
                    command,
                    rows,
                    editing,
                    insertRow,
                    saveChanges,
                    cancelEdit,
                    deleteSelectedRows,
                    resetState }">
                </slot>

                <div v-if="paging.position === 'top' || paging.position === 'both'" class="dg-pager-wrapper border-bottom">
                    <sm-datagrid-pager v-bind="{ options, columns, paging, command, rows, total }"></sm-datagrid-pager>
                </div>
                <component :is="options.allowEdit ? 'form' : 'div'" ref="tableWrapper" class="dg-table-wrapper">
                    <table ref="table"
                        :class="getTableClass()"
                        :style="getTableStyles()">
                        <thead v-show="!options.hideHeader" class="dg-thead" ref="tableHeader">
                            <tr ref="tableHeaderRow" class="dg-tr">
                                <th v-if="allowRowSelection || hasDetailView" class="dg-th dg-col-selector dg-col-pinned alpha">

                                    <label v-if="allowRowSelection" class="dg-cell dg-cell-header dg-cell-selector ml-auto">
                                        <span class="dg-cell-value">
                                            <input type="checkbox" class="dg-cell-selector-checkbox" ref="masterSelector" @change="onSelectAllRows($event)" />
                                        </span>
                                    </label>
                                </th>            

                                <th v-for="(column, columnIndex) in columns" 
                                    class="dg-th dg-th-column"
                                    v-show="column.visible"
                                    :data-member="column.member"
                                    :data-index="columnIndex"
                                    :draggable="options.allowColumnReordering && column.reorderable && !editing.active"
                                    v-on:dragstart="onColumnDragStart"
                                    v-on:dragenter.stop="onColumnDragEnter"
                                    v-on:dragover.stop="onColumnDragOver"
                                    v-on:dragend="onColumnDragEnd"
                                    v-on:drop="onColumnDrop"
                                    ref="column">
                                    <div class="dg-cell dg-cell-header" 
                                        :style="getCellStyles(null, column, true)" 
                                        :class="{ 'dg-sortable': sorting.enabled && column.sortable }"
                                        :title="column.title ? null : column.name"
                                        v-on:click="onSort($event, column)">
                                        <i v-if="column.icon" class="dg-icon" :class="column.icon"></i>
                                        <span v-if="column.title" class="dg-cell-value" :title="column.title">{{ column.title }}</span>
                                        <i v-if="isSortedAsc(column)" class="fa fa-fw fa-sm fa-arrow-up mx-1"></i>
                                        <i v-if="isSortedDesc(column)" class="fa fa-fw fa-sm fa-arrow-down mx-1"></i>
                                    </div>
                                    <div v-if="options.allowResize && column.resizable" 
                                        class="dg-resize-handle"
                                        v-on:mousedown.stop.prevent="onStartResize($event, column, columnIndex)"
                                        v-on:dblclick.stop.prevent="autoSizeColumn($event, column, columnIndex)">
                                    </div>
                                </th> 
                                <th class="dg-th dg-hborder-0">
                                    <div class="dg-cell dg-cell-header dg-cell-spacer">&nbsp;</div>
                                </th>
                                <th v-if="canEditRow || hasRowCommands" class="dg-th dg-col-pinned omega">&nbsp;</th> 
                            </tr>
                        </thead>
                        <tbody ref="tableBody" class="dg-tbody">
                            <tr v-if="ready && rows.length === 0" class="dg-tr dg-no-data">
                                <td class="dg-td text-muted text-center">
                                    <div class="dg-cell justify-content-center">Keine Daten</div>
                                </td>
                            </tr>                            
                            
                            <template v-for="(row, rowIndex) in rows">
                                <tr class="dg-tr" :class="getDataRowClass(row, rowIndex)" :data-key="row[options.keyMemberName]" :key="'row-' + row[options.keyMemberName]">

                                    <td v-if="allowRowSelection || hasDetailView" class="dg-td dg-col-selector dg-col-pinned alpha">
                                        <div v-if="hasDetailView" class="dg-cell dg-cell-detail-toggle" :class="{ 'expanded': getRowDetailState(row) === true }" @click="toggleDetailView(row)">
                                            <i class="fa fa-chevron-right fa-sm"></i>
                                        </div>
                                        <label v-if="allowRowSelection" class="dg-cell dg-cell-selector">
                                            <span v-if="!isInlineEditRow(row) || !editing.insertMode" class="dg-cell-value">
                                                <input type="checkbox" class="dg-cell-selector-checkbox" :checked="isRowSelected(row)" @change="onSelectRow($event, row)" />
                                            </span>
                                        </label>
                                    </td>

                                    <td v-for="(column, columnIndex) in columns"
                                        class="dg-td"
                                        :class="getDataCellClass(row[column.member], column, row)"
                                        v-show="column.visible"
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
                                    <td class="dg-td dg-hborder-0">
                                        <div class="dg-cell dg-cell-spacer"></div>
                                    </td>
                                    <td v-if="canEditRow || hasRowCommands" class="dg-td dg-col-pinned omega">
                                        <div class="dg-cell dg-commands p-0">
                                            <div v-if="hasRowCommands && (!editing.active || row != editing.row)" class="d-flex w-100 h-100 align-items-center justify-content-center dropdown">
                                                <a href="#" class="dg-commands-toggle dropdown-toggle no-chevron btn btn-secondary btn-flat btn-icon btn-sm" data-toggle="dropdown" data-boundary="window">
                                                    <i class="fa fa-ellipsis-h"></i>
                                                </a>
                                                <slot name="rowcommands" v-bind="{ row, activateEdit, deleteRows }"></slot> 
                                            </div>

                                            <div v-if="editing.active && row == editing.row" class="dg-row-edit-commands btn-group-vertical">
                                                <button @click="saveChanges()" class="btn btn-primary btn-sm btn-flat rounded-0" type="button" title="Änderungen speichern">
                                                    <i class="fa fa-check"></i>
                                                </button>
                                                <button @click="cancelEdit()" class="btn btn-secondary btn-sm btn-flat rounded-0" type="button" title="Abbrechen">
                                                    <i class="fa fa-times"></i>
                                                </button>
                                            </div>
                                        </div>

                                    </td>
                                </tr>

                                <tr v-if="hasDetailView && getRowDetailState(row) !== undefined" v-show="getRowDetailState(row) === true" class="dg-tr dg-tr-detail">
                                    <td class="dg-td dg-td-detail" style="grid-column: 1 / -1">
                                        <div class="dg-cell dg-cell-detail flex-column align-items-start">
                                            <slot name="detailview" v-bind="{ row }"></slot>  
                                        </div>
                                    </td>
                                </tr>
                            </template>
                        </tbody>
                    </table>
                </component>
                <div v-if="paging.position === 'bottom' || paging.position === 'both'" class="dg-pager-wrapper border-top">
                    <sm-datagrid-pager v-bind="{ options, columns, paging, command, rows, total }"></sm-datagrid-pager>
                </div>

                <div v-show="isBusy" class="dg-blocker"></div>
            </div>
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

    data() {
        return {
            rows: [],
            total: 0,
            aggregates: [],
            selectedRows: {},
            detailRows: {},
            originalState: {},
            isBusy: false,
            ready: false,
            isScrollable: false,
            numSearchFilters: 0,
            hasEditableVisibleColumn: false,
            hasRowCommands: false,
            hasDetailView: false,
            dragging: {
                active: false,
                targetRect: null,
                indicator: null,
                indicate(atStart) {
                    const rect = this.targetRect;
                    if (rect) {
                        if (!this.indicator) {
                            this.indicator = $('<div class="dg-drop-indicator"></div>').appendTo(document.body).get(0);
                        }

                        this.indicator.style.display = "block";
                        this.indicator.style.left = (atStart ? rect.left : rect.right) + "px";
                        this.indicator.style.top = rect.top + "px";
                        this.indicator.style.height = rect.height + "px";
                    }
                },
                hideIndicator() {
                    if (this.indicator) {
                        this.indicator.style.display = "none";
                    }
                },
                removeIndicator() {
                    if (this.indicator) {
                        this.indicator.remove();
                        this.indicator = null;
                    }
                },
                reset() {
                    this.removeIndicator();
                    Object.keys(this).forEach(key => {
                        if (!_.isFunction(this[key])) {
                            delete this[key];
                        }
                    });
                }
            },
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
                                            if (v !== undefined && v !== null) {
                                                el.value = v;
                                            }
                                                
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
                bindModel() {
                    if (!this.tr) return;
                    let r = this.row;
                    let form = $(this.tr).closest("form");
                    let model = form.serializeToJSON();

                    $.extend(r, model);
                }
            }
        }
    },

    created() {
        const self = this;

        // Load user prefs
        this.originalState = this.getGridState();
        if (this.options.preserveState) {
            var userPrefs = JSON.parse(localStorage.getItem('sm:grid:state:' + this.options.stateKey));
            this.userPrefs = userPrefs?.version === this.options.version ? userPrefs : null;
        }  

        this.$on('data-binding', command => {
            this._callHandler(this.options, 'onDataBinding', command);
        });

        this.$on('data-bound', (command, rows) => {
            this.setMasterSelectorState(this.getMasterSelectorState());
            this._callHandler(this.options, 'onDataBound', command, rows);
        });

        this.$on('row-selected', (selectedRows, row, selected) => {
            this._callHandler(this.options, 'onRowSelected', selectedRows, row, selected);
        });
    },

    mounted () {
        const self = this;

        // Put to data so we can access the component instance from outside
        $(this.$el.parentNode).data("datagrid", this);

        // Handle sticky columuns on resize
        const resizeObserver = new ResizeObserver(entries => {
            const tableWrapper = entries[0].target;
            self.isScrollable = tableWrapper.offsetWidth < tableWrapper.scrollWidth;
        });
        resizeObserver.observe(this.$refs.tableWrapper);

        // Bind search control events
        if (this.hasSearchPanel) {
            var search = $(this.$el).find(".dg-search-body");
            search.on("change", "select", this.read);
            search.on("change", "input[type='checkbox'], input[type='radio']", this.read);
            search.on("keydown focusout", "textarea, input", e => {
                if (e.target.type === 'checkbox' || e.target.type === 'radio') {
                    return;
                }
                if (e.type === "focusout" || (e.type === "keydown" && e.keyCode == 13)) {
                    e.preventDefault();
                    const prevValue = $(e.target).data("prev-value");
                    const handle = prevValue === undefined ? !!(e.target.value) : prevValue != e.target.value;
                    if (handle) {
                        $(e.target).data("prev-value", e.target.value);
                        self.read();
                    }
                }
            });
        }

        $(this.$el).on("show.bs.dropdown", function (e) {
            // Append the dropdown menu to body to prevent overflow clipping
            var menu = $(e.target).find('.dropdown-menu');
            $(e.target).data("dropdown-menu", menu);
            menu.detach().appendTo('body');
        });

        $(this.$el).on("hide.bs.dropdown", function (e) {
            // Put dropdown menu back to where it belongs
            var menu = $(e.target).data('dropdown-menu');
            menu.detach().appendTo(e.target);
        });

        this.hasEditableVisibleColumn = this.columns.some(this.isEditableVisibleColumn);
        this.hasRowCommands = !!(this.$scopedSlots.rowcommands);
        this.hasDetailView = !!(this.$scopedSlots.detailview);

        // Read data from server. Process initial read after a short delay, 
        // because something's wrong with numSearchFilters if we call immediately.
        window.setTimeout(() => { this.read(true, true); }, 50);
    },

    updated() {
        this.initializeEditRow();
    },

    computed: {
        command() {
            return {
                page: this.paging.pageIndex,
                pageSize: this.paging.pageSize,
                sorting: this.sorting.descriptors
            };
        },

        hasSearchPanel() {
            return !!(this.$scopedSlots.search);
        },

        canEditRow() {
            return this.options.allowEdit && this.hasEditableVisibleColumn && !!(this.dataSource.update);
        },

        canInsertRow() {
            return this.options.allowEdit && this.hasEditableVisibleColumn && !!(this.dataSource.insert);
        },

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
                $.extend(this.paging, value.paging);
 
                value.columns.forEach((userColumn, userIndex) => {
                    const column = this.columns.find(x => x.member === userColumn.member);
                    if (column) {
                        column.width = userColumn.width;
                        column.visible = userColumn.visible;
                        this.changeColumnOrder(column, userIndex);
                    }
                });
            }
        }
    },

    watch: {
        command: {
            handler: function(value) {
                this.read();
            },
            deep: true
        },
        columns: {
            handler() {
                this._debouncedWatchColumns();
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

        // #region Internal

        _debouncedWatchColumns: _.debounce(function () {
            this.hasEditableVisibleColumn = this.columns.some(this.isEditableVisibleColumn);
        }, 100, false),

        _callHandler(obj, handlerName) {
            if (obj && _.isString(obj[handlerName])) {
                const args = Array.prototype.splice.call(arguments, 2);
                return window[obj[handlerName]].apply(this, args);
            }
        },

        // #endregion

        // #region Class & Style binding

        getTableClass() {
            const cssClass = {
                'dg-table': true,
                'dg-striped': this.options.striped,
                'dg-hover': this.options.hover,
                'dg-hborders': this.options.hborders,
                'dg-vborders': this.options.vborders,
                'dg-has-detailview': this.hasDetailView,
                'dg-scrollable': this.isScrollable
            };

            return cssClass;
        },

        getTableStyles() {
            const style = { 'grid-template-columns': this.getGridTemplateColumns() };
            return style;
        },

        getDataRowClass(row, rowIndex) {
            const cssClass = {
                'active': this.isRowSelected(row),
                'even': (rowIndex + 1) % 2 === 0,
                'expanded': this.getRowDetailState(row) === true,
                'dg-edit-row': this.isInlineEditRow(row)
            };

            return $.extend(
                cssClass,
                this._callHandler(this.options, "onRowClass", row));
        },

        getDataCellClass(value, column, row) {
            const cssClass = $.extend(
                // First global handler...
                this._callHandler(this.options, "onCellClass", value, column, row),
                // ...then column specific handler
                this._callHandler(column, "onCellClass", value, column, row));

            return $.isEmptyObject(cssClass) ? null : cssClass;
        },

        getCellStyles(row, column, isHeader) {
            const style = {};

            if (column.halign) {
                style.justifyContent = column.halign;
            }

            if (column.valign && !isHeader) {
                style.alignItems = column.valign;
            }

            return style;
        },

        getCellClass(row, column) {
            const cssClass = {
                'dg-cell-wrap': column.wrap,
                'dg-cell-edit': this.isInlineEditCell(row, column)
            };

            return cssClass;
        },

        getGridTemplateColumns() {
            let hasFraction = false;
            let result = this.columns
                .filter(c => c.visible)
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

            if (this.allowRowSelection || this.hasDetailView) {
                result.splice(0, 0, "minmax(max-content, 48px)");
            }

            // Spacer always 'auto' to fill remaining area
            result.push(hasFraction ? "0" : "auto");

            // Last grid tools / row commands column
            if (this.canEditRow || this.hasRowCommands) {
                result.push("48px");
            }

            return result.join(' ');
        },

        // #endregion

        // #region Rendering

        getGridState() {
            return {
                version: this.options.version,
                options: {
                    vborders: this.options.vborders,
                    hborders: this.options.hborders,
                    striped: this.options.striped,
                    hover: this.options.hover
                },
                paging: {
                    pageSize: this.paging.pageSize,
                    position: this.paging.position
                },
                columns: this.columns.map(c => {
                    return { member: c.member, visible: c.visible, width: c.width };
                })
            };
        },

        renderCellValue(value, column, row) {
            const t = column.type, g = Smartstore.globalization;

            if (!value && column.nullable) {
                return value;
            }

            if (column.format) {
                return column.format.format(value);
            }
            else if (t === 'date') {
                return moment(value).format('L LTS');
            }
            else if (t === 'int') {
                return g.formatNumber(value);
            }
            else if (t === 'float') {
                return g.formatNumber(value, 'N2');
            }

            return value;
        },

        resetState() {
            // Reset options
            $.extend(this.options, this.originalState.options);

            // Reset paging
            $.extend(this.paging, this.originalState.paging);

            // Reset columns
            this.originalState.columns.forEach((originalColumn, originalIndex) => {
                let column = this.columns.find(c => c.member === originalColumn.member);
                if (column) {
                    column.width = originalColumn.width;
                    column.visible = originalColumn.visible;
                    this.changeColumnOrder(column, originalIndex);
                }
            });

            this.userPrefs = null;
        },

        // #endregion

        // #region Commands

        read(force, initial) {
            if (!force && this.isBusy)
                return;

            const self = this;
            self.cancelEdit();
            self.isBusy = true;

            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const command = $.extend(true, { __RequestVerificationToken: input.value }, this.command, {
                initialRequest: initial,
                gridId: this.options.stateKey,
                path: location.pathname + location.search
            });

            // Fix sort descriptors (member --> entityMember)
            if (command.sorting) {
                command.sorting = command.sorting.map(d => {
                    var c = this.columns.find(x => x.member === d.member);
                    return c?.entityMember ? { member: c.entityMember, descending: d.descending } : d;
                });
            }

            // Apply search filters to command
            this._applySearchFilters(command);

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
                    self.detailRows = {};

                    if (self.totalPages > 0 && self.command.page > self.totalPages) {
                        // Fix "pageIndex > totalPages" by reloading
                        self.isBusy = false;
                        self.paging.pageIndex = self.totalPages;
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

        deleteSelectedRows() {
            this.deleteRows(this.selectedRows);
        },

        deleteRows(rows) {
            let numRows, rowKeys;

            if (_.isArray(rows)) {
                numRows = rows.length;
                rowKeys = rows.map(r => r[this.options.keyMemberName]);
            }
            else if (rows.hasOwnProperty(this.options.keyMemberName)) {
                // Single row object
                numRows = 1;
                rowKeys = [rows[this.options.keyMemberName]];
            }
            else if ($.isPlainObject(rows)) {
                // Most probably array-like SelectedRows object
                const arr = Object.values(rows);
                numRows = arr.length;
                rowKeys = arr.map(r => r[this.options.keyMemberName]);
            }
            else {
                console.error("Wrong argument type 'rows' for 'deleteRows'.");
                return;
            }

            if (this.isBusy || !numRows || !this.dataSource.del)
                return;

            const self = this;
            const message = numRows === 1
                ? "Sollen der Datensatz wirklich unwiderruflich gelöscht werden?"
                : "Sollen die gewählten {0} Datensätze wirklich unwiderruflich gelöscht werden?".format(numRows);

            confirm2({
                message: message,
                icon: { type: 'delete' },
                callback: accepted => {
                    if (!accepted)
                        return;

                    const input = document.querySelector('input[name=__RequestVerificationToken]');
                    const selection = $.extend(true, { __RequestVerificationToken: input.value }, { selectedKeys: rowKeys });

                    self.$emit("deleting-rows", rowKeys);

                    $.ajax({
                        url: this.dataSource.del,
                        type: 'POST',
                        cache: false,
                        dataType: 'json',
                        data: selection,
                        global: true,
                        success(result) {
                            if (result.Success) {
                                self.selectedRows = {};
                                displayNotification("{0} Datensätze erfolgreich gelöscht.".format(result.Count || numRows), "success");
                                self.$emit("deleted-rows", rowKeys);
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
                this.dragging.lastEnter = e.target
            }
        },

        onColumnDragOver(e) {
            const d = this.dragging;
            if (d.active) {
                const th = $(e.target).closest('th.dg-th-column').get(0);
                if (th) {
                    e.preventDefault();
                    if (th !== d.target) {
                        d.target = th;
                        d.targetIndex = parseInt(th.getAttribute("data-index"));
                        d.targetMember = th.getAttribute("data-member");
                        d.targetColumn = this.columns[d.targetIndex];
                        d.targetRect = th.getBoundingClientRect();
                        e.dataTransfer.dropEffect = "move";
                    }

                    // TODO: (core) What about RTL?
                    let attemptedIndex = d.targetIndex;
                    let rect = d.targetRect;
                    let atStart = e.pageX < rect.left + (rect.width / 2);

                    if (atStart && attemptedIndex === (d.sourceIndex + 1)) {
                        atStart = false;
                    }

                    d.indicate(atStart);
                }
            }
        },

        onColumnDrop(e) {
            if (this.dragging.active && this.dragging.target) {
                const th = this.dragging.target;
                const index = parseInt(th.getAttribute("data-index"));
                this.changeColumnOrder(this.dragging.sourceColumn, index);
            }
        },

        onColumnDragEnd(e) {
            this.dragging.reset();
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
                ? this.sorting.descriptors.find(d => d.member === column.member)
                : null;
        },

        onSort(e, column) {
            if (!this.sorting.enabled || !column.sortable || this.isBusy)
                return;

            let descriptor = this.getSortDescriptor(column);
            let multiMode = this.sorting.allowMultiSort && e.ctrlKey;

            if (descriptor) {
                if (this.sorting.allowUnsort && descriptor.descending) {
                    this.sorting.descriptors = this.sorting.descriptors.filter(x => x != descriptor);
                    descriptor = null;
                }
                else {
                    descriptor.descending = !descriptor.descending;
                }
            }
            else {
                descriptor = { member: column.member, descending: false };
                this.sorting.descriptors.push(descriptor);
            }

            if (descriptor && !multiMode) {
                this.sorting.descriptors = this.sorting.descriptors.filter(x => x === descriptor);
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
            return column.visible && column.editable;
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
            if (!this.canInsertRow) {
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
            if (!this.canEditRow) {
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

            if (this.editing.insertMode && this.rows.length && this.rows[0] === this.editing.row) {
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
        },

        // #endregion

        // #region Search

        _applySearchFilters(command) {
            this.numSearchFilters = 0;
            if (this.hasSearchPanel) {
                const form = $(this.$el).find(".dg-search-body");
                const obj = form.serializeToJSON();
                this.numSearchFilters = Object.keys(obj)
                    .filter(key => {
                        const o = obj[key];
                        const el = form.find("[name='" + key + "']");
                        let defaultValue = el.data("default");
                        if (defaultValue === undefined) {
                            defaultValue = "";
                        }
                        if (_.isArray(o)) {
                            return o.length > 0 || (o.length === 1 && o[0]);
                        }
                        else if (el.is("input:checkbox")) {
                            return _.isBoolean(defaultValue) ? o != defaultValue : o === true;
                        }

                        return o !== defaultValue;

                    })
                    .length;

                $.extend(true, command, obj);
            }
        },

        // #endregion

        // #region Master/Details

        toggleDetailView(row) {
            const key = row[this.options.keyMemberName];
            const entry = this.detailRows[key];
            Vue.set(this.detailRows, key, entry === undefined ? true : !entry);
        },

        getRowDetailState(row) {
            return this.detailRows[row[this.options.keyMemberName]];
        }

        // #endregion
    }
});