const DATAGRID_CELL_MIN_WIDTH = 60;

// https://dev.to/loilo92/an-approach-to-vuejs-template-variables-5aik
// TODO: (core) Move Vue.pass component to a central location.
Vue.component("pass", {
    render() {
        return this.$scopedSlots.default(this.$attrs);
    }
});

Vue.component("sm-data-grid", {
    template: `
        <div class="datagrid">
            <div v-if="paging.enabled && (paging.position === 'top' || paging.position === 'both')" class="dg-pager-wrapper border-bottom">
                <sm-data-grid-pager :command="command" :rows="rows" :total="total" :max-pages-to-display="10"></sm-data-grid-pager>
            </div>
            <div class="dg-table-wrapper">
                <table ref="table"
                    :class="getTableClass()"
                    :style="getTableStyles()">
                    <thead v-if="!options.hideHeader" class="dg-head">
                        <tr>
                            <th v-if="allowRowSelection" class="dg-col-pinned dg-col-pinned-left" @click="onSelectAllRows($event)">
                                <div class="dg-cell dg-cell-header dg-cell-selector">
                                    <span class="dg-cell-value">
                                        <input type="checkbox" class="dg-cell-selector-checkbox" ref="masterSelector" />
                                    </span>
                                </div>
                            </th>            
                
                            <th v-for="(column, columnIndex) in columns"
                                :data-member="column.member"
                                :data-index="columnIndex"
                                ref="column">
                                <div class="dg-cell dg-cell-header" 
                                    :style="getCellStyles(column, true)" 
                                    :class="{ 'dg-sortable': sorting.enabled && column.sortable }"
                                    @click="onSort($event, column)">
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
                        <tr v-for="(row, rowIndex) in rows" :key="row[options.keyMemberName]" :class="{ active: isRowSelected(row) }">
                             <td v-if="allowRowSelection" class="dg-col-pinned dg-col-pinned-left" @click="onSelectRow($event, row)">
                                <div class="dg-cell dg-cell-selector">
                                    <span class="dg-cell-value">
                                        <input type="checkbox" class="dg-cell-selector-checkbox" :checked="isRowSelected(row)" />
                                    </span>
                                </div>
                            </td>             

                            <td v-for="(column, columnIndex) in columns"
                                :data-index="columnIndex"
                                :key="row[options.keyMemberName] + '-' + columnIndex">
                                <div class="dg-cell" :class="getCellClass(column)" :style="getCellStyles(column, false)">
                                    <slot :name="'display-' + column.member.toLowerCase()" v-bind="{ row, rowIndex, column, columnIndex, value: row[column.member] }">
                                        <span class="dg-cell-value">
                                            <template v-if="column.type === 'boolean'">
                                                <i class="fa fa-fw" :class="'icon-active-' + row[column.member]"></i>
                                            </template>
                                            <template v-else>
                                                {{ renderCellValue(row[column.member], column, row) }}
                                            </template>
                                        </span>
                                    </slot>
                                </div>
                            </td>
                            <td>
                                <div class="dg-cell dg-cell-spacer"></div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-if="paging.enabled && (paging.position === 'bottom' || paging.position === 'both')" class="dg-pager-wrapper border-top">
                <sm-data-grid-pager :command="command" :rows="rows" :total="total" :max-pages-to-display="10"></sm-data-grid-pager>
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
            default: { enabled: false, pageIndex: 1, pageSize: 25, position: "bottom" }
        },

        sorting: {
            type: Object,
            required: false,
            default: { enabled: false, descriptors: [] }
        },
    },

    created: function () {
        this.command = this.buildCommand();
    },

    mounted: function () {
        this.read();
    },

    data: function () {
        return {
            command: {},
            rows: [],
            total: 0,
            aggregates: [],
            selectedRows: {},
            isLoading: false
        }
    },

    computed: {
        allowRowSelection() {
            return this.options.allowRowSelection && this.rows && this.rows.length > 0;
        },

        selectedRowsCount() {
            return Object.values(this.selectedRows).length;
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
        }
    },

    methods: {
        buildCommand() {
            var p = this.paging;
            var s = this.sorting;
            var command = {
                page: p.pageIndex,
                pageSize: p.pageSize,
                sorting: s.descriptors
            };

            return command;
        },

        getTableClass() {
            const cssClass = {
                'dg-table': true,
                'dg-striped': this.options.striped,
                'dg-hover': this.options.hover
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

        getCellStyles(column, isHeader) {
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

        getCellClass(column) {
            const cssClass = {
                'dg-cell-wrap': !column.nowrap
            };

            return cssClass;
        },

        getGridTemplateColumns() {
            var hasFraction = false;
            var result = this.columns
                .map(c => {
                    let w = c.visible ? c.width : "0";
                    if (c.visible && !c.width) {
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
            //result += " " + (hasFraction ? "0" : "auto");
            result.push(hasFraction ? "0" : "auto");

            return result.join(' ');
        },

        renderCellValue(value, column, row) {
            var t = column.type;

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

        read() {
            if (this.isLoading)
                return;

            var self = this;
            self.isLoading = true;

            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const command = $.extend(true, { __RequestVerificationToken: input.value }, this.command);

            $.ajax({
                url: this.dataSource.read,
                type: 'POST',
                cache: false,
                dataType: 'json',
                data: command,
                success(result) {
                    self.rows = result.rows !== undefined ? result.rows : result;
                    self.total = result.total || self.rows.length;
                    self.aggregates = result.aggregates !== undefined ? result.aggregates : [];
                    self.$emit("data-bound", self.rows);
                },
                complete() {
                    self.isLoading = false;
                    self.setMasterSelectorState(self.getMasterSelectorState());
                }
            });

            //fetch(this.dataSource.read, {
            //    method: 'POST',
            //    cache: 'no-cache',
            //    headers: {
            //        'Accept': 'application/json',
            //        'Content-Type': 'application/json'
            //    },
            //    body: JSON.stringify(body)
            //})
            //.then(r => r.json())
            //.then(data => { this.rows = data; });
        },

        isSortedAsc(column) {
            var sort = this.getSortDescriptor(column);
            return sort && !sort.descending;
        },

        isSortedDesc(column) {
            var sort = this.getSortDescriptor(column);
            return sort && sort.descending;
        },

        getSortDescriptor(column) {
            return this.sorting.enabled
                ? this.command.sorting.find(x => x.member === column.entityMember || x.member === column.member)
                : null;
        },


        getMasterSelectorState() {
            var numSelected = this.selectedRowsInCurrentPage.length;

            return {
                checked: numSelected === this.rows.length,
                indeterminate: numSelected > 0 && numSelected < this.rows.length
            };
        },

        setMasterSelectorState(state) {
            var chk = this.$refs.masterSelector;
            if (!chk || !state) return;

            chk.checked = state.checked;
            chk.indeterminate = state.indeterminate;
        },

        isRowSelected(row) {
            return this.selectedRows[row[this.options.keyMemberName]];
        },

        onSelectAllRows(e) {
            var state = this.getMasterSelectorState();
            this.rows.forEach(x => {
                this.onSelectRow(e, x, state.indeterminate);
            });
        },

        onSelectRow(e, row, select) {
            if (!row)
                return;

            var key = row[this.options.keyMemberName];
            var selectedRow = this.selectedRows[key];
            if (selectedRow && !select) {
                this.$delete(this.selectedRows, key);
                this.$emit('row-selected', this.selectedRows, row, false);
            }
            else if (!selectedRow) {
                this.$set(this.selectedRows, key, row);
                this.$emit('row-selected', this.selectedRows, row, true);
            }
        },

        onSort(e, column) {
            if (!this.sorting.enabled || !column.sortable || this.isLoading)
                return;

            var descriptor = this.getSortDescriptor(column);
            var multiMode = this.sorting.allowMultiSort && e.ctrlKey;

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

            var self = this;

            requestAnimationFrame(() => {
                const pageX = e.pageX;
                const diffX = pageX - self.resizeX;
                const width = self.resizeHeader.offsetWidth + diffX;

                self.resizeX = pageX;

                if (width < DATAGRID_CELL_MIN_WIDTH) {
                    return;
                }

                self.resizeColumn.width = width + 'px';
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
        }
    }
});