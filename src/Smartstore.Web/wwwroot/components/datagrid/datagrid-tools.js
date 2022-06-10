Vue.component("sm-datagrid-tools", {
    template: `
        <div class="dg-tools dropdown text-align-center border-left pl-1 ml-1">
            <a href="#" class="dg-tools-toggle btn btn-light btn-flat btn-icon btn-sm dropdown-toggle no-chevron" data-toggle="dropdown" data-boundary="window">
                <i class="fa fa-cog"></i>
            </a>
            <div class="dg-tools-dropdown dropdown-menu dropdown-menu-right" v-on:click="$event.stopPropagation()">
                <div class="dg-tools-group px-3 pt-1">
                    <label class="d-flex align-items-center justify-content-between switcher-sm m-0">
                        <span>{{ T.vborders }}</span>
                        <label class="switch">
                            <input type="checkbox" v-model="options.vborders">
                            <span class="switch-toggle"></span>
                        </label>
                    </label>
                    <label class="d-flex align-items-center justify-content-between switcher-sm m-0">
                        <span>{{ T.hborders }}</span>
                        <label class="switch">
                            <input type="checkbox" v-model="options.hborders">
                            <span class="switch-toggle"></span>
                        </label>
                    </label>
                    <label class="d-flex align-items-center justify-content-between switcher-sm m-0">
                        <span>{{ T.striped }}</span>
                        <label class="switch">
                            <input type="checkbox" v-model="options.striped">
                            <span class="switch-toggle"></span>
                        </label>
                    </label>
                    <label class="d-flex align-items-center justify-content-between switcher-sm m-0">
                        <span>{{ T.hover }}</span>
                        <label class="switch">
                            <input type="checkbox" v-model="options.hover">
                            <span class="switch-toggle"></span>
                        </label>
                    </label>
                    <label v-if="paging.enabled" class="d-flex align-items-center justify-content-between m-0">
                        <span>{{ T.pagerPos }}</span>
                        <select class="form-control form-control-sm noskin w-auto px-1" v-model="paging.position">
                            <option value="top">{{ T.pagerTop }}</option>
                            <option value="bottom">{{ T.pagerBottom }}</option>
                            <option value="both">{{ T.pagerBoth }}</option>
                        </select>
                    </label>
                    <button type="button" class="btn btn-sm btn-block btn-secondary mt-2" @click="$parent.$parent.resetState()">
                        {{ T.resetState }}
                    </button>
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