Vue.component("sm-data-grid", {
    template: `
        <table class="datagrid" 
            ref="table"
            :style="getTableStyle()">
            <thead class="datagrid-head">
                <tr>
                    <th v-for="(column, columnIndex) in columns"
                        :data-member="column.member"
                        ref="column">
                        <div class="datagrid-cell datagrid-cell-header" :style="getCellStyle(column, true)">
                            {{ column.title }}
                        </div>
                        <div class="datagrid-resize-handle" v-on:mousedown="onStartResize($event, column, columnIndex)"></div>
                    </th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="(row, rowIndex) in rows" :key="row.Id">
                    <td v-for="(column, columnIndex) in columns"
                        ref="cell"
                        :key="row.Id + '-' + columnIndex">
                        <div class="datagrid-cell" :style="getCellStyle(column, false)">
                            <slot :name="'display-' + column.member.toLowerCase()" v-bind="{ row, rowIndex, column, columnIndex, value: row[column.member] }">
                                {{ row[column.member] }}
                            </slot>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
    `,
    props: {
        dataSource: {
            type: Object,
            required: true
        },

        columns: {
            type: Array,
            required: true
        },

        command: {
            type: Object,
            required: false
        },

        vborders: {
            type: Boolean,
            required: false,
            default: true
        },

        hborders: {
            type: Boolean,
            required: false
        }
    },
    created: function () {

    },
    mounted: function () {
        this.read();
    },
    data: function () {
        return {
            rows: [],
            total: 0,
            aggregates: []
        }
    },
    computed: {
        computeTest() {
            return "test";
        }
    },
    methods: {
        getTableStyle() {
            const style = { 'grid-template-columns': this.generateGridTemplateColumns() };

            style['row-gap'] = this.vborders ? "1px" : "0";
            style['column-gap'] = this.hborders ? "1px" : "0";

            return style;
        },

        getCellStyle(column, isHeader) {
            const style = {};

            if (column.align && !isHeader) {
                style.alignItems = column.align;
            }

            if (column.justify) {
                style.justifyContent = column.justify;
            }

            return style;
        },

        formatNumber(input) {
            return input;
        },

        formatCurrency(input) {
            return input;
        },

        read() {
            var self = this;

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

        generateGridTemplateColumns() {
            return this.columns
                .map(x => x.width || "minmax(120px, 1fr)")
                .join(' ');
        },

        onStartResize(e, column, columnIndex) {
            this.isResizing = true;
            this.resizeIndex = columnIndex;
            this.resizeColumn = column;
            this.resizeHeader = e.target.parentNode;
            this.resizeX = e.pageX;

            window.addEventListener('mousemove', this.onResize, false);
            window.addEventListener('mouseup', this.onStopResize, false);

            this.$refs.table.classList.add('datagrid--resizing');
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

                if (width < 80) {
                    return;
                }

                self.resizeColumn.width = width + 'px';
            });
        }, 20, true),

        onStopResize(e) {
            window.removeEventListener('mousemove', this.onResize);
            window.removeEventListener('mouseup', this.onStopResize);

            this.$refs.table.classList.remove('datagrid--resizing');

            this.resizeIndex = null;
            this.resizeColumn = null;
            this.resizeHeader = null;
            this.resizeX = null;
            this.isResizing = false;
        }
    }
});