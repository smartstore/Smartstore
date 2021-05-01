Vue.component("sm-data-grid", {
    template: `
        <table class="datagrid" 
            ref="table"
            :style="{ gridTemplateColumns: generateGridTemplateColumns() }">
            <thead>
                <tr>
                    <th v-for="(column, columnIndex) in columns"
                        :data-field="column.field"
                        ref="column">
                        <span>{{ column.name }}</span>
                        <span class="datagrid-resize-handle" v-on:mousedown="onStartResize($event, column, columnIndex)"></span>
                    </th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="(row, rowIndex) in rows" :key="row.Id">
                    <td v-for="(column, columnIndex) in columns" 
                        :key="row.Id + '-' + columnIndex">
                        <span>{{ row[column.field] }}</span>
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
        }
    },
    created: function () {

    },
    mounted: function () {
        //this._table = this.$root.$el.getElementsByClassName('datagrid')[0];
        //console.log(this.$root.$el);
        this.read();
    },
    data: function () {
        return {
            rows: []
        }
    },
    computed: {
        computeTest() {
            return "test";
        }
    },
    methods: {
        read() {
            const input = document.querySelector('input[name=__RequestVerificationToken]');
            const body = { __RequestVerificationToken: input.value };
            console.log(body);
            fetch(this.dataSource.read, {
                method: 'POST',
                cache: 'no-cache',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(body)
            })
            .then(r => r.json())
            .then(data => { this.rows = data; });
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