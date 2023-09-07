Vue.component("sm-datagrid-tools", {
    template: `
        <div class="dg-tools dropdown text-align-center border-left pl-1 ml-1">
            <a href="#" class="dg-tools-toggle btn btn-light btn-flat btn-icon btn-sm dropdown-toggle no-chevron" data-toggle="dropdown" data-boundary="window">
                <i class="fa fa-cog"></i>
            </a>
            <div class="dg-tools-dropdown dropdown-menu dropdown-menu-right" v-on:click="$event.stopPropagation()">
                <div class="dg-tools-group px-3 pt-1">
                    <label class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.vborders }}</span>
                        <div class="form-check form-check-warning form-switch m-0">
                            <input type="checkbox" class="form-check-input m-0" v-model="options.vborders">
                        </div>
                    </label>
                    <label class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.hborders }}</span>
                        <div class="form-check form-check-warning form-switch m-0">
                            <input type="checkbox" class="form-check-input m-0" v-model="options.hborders">
                        </div>
                    </label>
                    <label class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.striped }}</span>
                        <div class="form-check form-check-warning form-switch m-0">
                            <input type="checkbox" class="form-check-input m-0" v-model="options.striped">
                        </div>
                    </label>
                    <label class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.hover }}</span>
                        <div class="form-check form-check-warning form-switch m-0">
                            <input type="checkbox" class="form-check-input m-0" v-model="options.hover">
                        </div>
                    </label>
                    <label v-if="paging.enabled" class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.pagerPos }}</span>
                        <select class="form-control form-control-sm noskin w-auto" v-model="paging.position">
                            <option value="top">{{ T.pagerTop }}</option>
                            <option value="bottom">{{ T.pagerBottom }}</option>
                            <option value="both">{{ T.pagerBoth }}</option>
                        </select>
                    </label>
                    <div class="row xs-gutters">
                        <div class="col">
                            <button type="button" class="btn btn-sm btn-block btn-secondary mt-2" @click="$parent.$parent.resetState()">
                                <span>{{ T.resetState }}</span>
                            </button>
                        </div>
                        <div class="col">
                            <button type="button" class="btn btn-sm btn-block btn-secondary text-truncate mt-2" @click="$parent.$parent.autoSizeAllColumns()">
                                <i class="fa fa-arrows-left-right-to-line"></i>
                                <span>{{ T.fitColumns }}</span>
                            </button>
                        </div>
                    </div>
                </div>
                <div class="dropdown-divider"></div>
                <div class="dg-tools-group dg-tools-columns px-3 pb-1">
                    <div v-for="(column, columnIndex) in columns" class="dg-column-toggle form-check my-1">
                        <input class="form-check-input" type="checkbox" v-model="column.visible" :id="'dg-column-toggle-' + columnIndex" :disabled="column.hideable ? null : true">
                        <label class="form-check-label d-block text-truncate" :for="'dg-column-toggle-' + columnIndex">{{ column.name }}</label>
                    </div>
                </div>
            </div>
        </div>
    `,

    props: {
        options: Object,
        paging: Object,
        columns: Array
    },

    created() {
        this.T = window.Res.DataGrid;
    }
});