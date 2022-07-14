Vue.component("sm-datagrid-pager", {
    template: `
        <div class="dg-pager d-flex flex-nowrap align-items-center">
            <div class="dg-page-refresh-wrapper">
                <a href="#" class="dg-page dg-page-refresh btn btn-light btn-sm" @click.prevent="refresh">
                    <i class="fa fa-sync-alt" :class="{ 'fa-spin text-success': $parent.isBusy }"></i>
                </a>
            </div>
            
            <template v-if="paging.enabled">
                <a href="#" class="dg-page dg-page-arrow btn btn-light btn-sm" @click.prevent="pageTo(1)" :class="{ disabled: !hasPrevPage }"><i class="fa fa-angle-double-left"></i></a>
                <a href="#" class="dg-page dg-page-arrow btn btn-light btn-sm" @click.prevent="pageTo(currentPageIndex - 1)" :class="{ disabled: !hasPrevPage }"><i class="fa fa-angle-left"></i></a>
            
                <a v-for="item in pageItems" href="#" @click.prevent="pageTo(item.page)" class="dg-page dg-page-number btn btn-light py-1 btn-sm d-none d-md-inline" :class="{ active: item.active }">
                    {{ item.label || item.page }}
                </a>
            
                <a href="#" class="dg-page dg-page-arrow btn btn-light btn-sm" @click.prevent="pageTo(currentPageIndex + 1)" :class="{ disabled: !hasNextPage }"><i class="fa fa-angle-right"></i></a>
                <a href="#" class="dg-page dg-page-arrow btn btn-light btn-sm" @click.prevent="pageTo(totalPages)" :class="{ disabled: !hasNextPage }"><i class="fa fa-angle-double-right"></i></a>
            </template>

            <div class="ml-auto d-flex">
                <div class="d-flex align-items-center">
                    <span v-if="paging.enabled && paging.showInfo" class="dg-page text-muted text-truncate d-none d-md-inline pl-2">
                        <span class="d-none d-lg-inline">{{ T.displayingItems.format(firstItemIndex.toLocaleString(), lastItemIndex.toLocaleString(), total.toLocaleString()) }}</span>
                        <span class="d-inline d-lg-none">{{ T.displayingItemsShort.format(firstItemIndex.toLocaleString(), lastItemIndex.toLocaleString(), total.toLocaleString()) }}</span>
                    </span>
                    <div v-if="paging.enabled && paging.showSizeChooser && paging.availableSizes?.length" class="dropdown d-flex align-items-center border-left pl-1 ml-2">
                        <a href="#" v-html="T.xPerPage.format(command.pageSize)" class="dg-page dg-page-size-chooser btn btn-light btn-sm dropdown-toggle text-truncate px-2" data-toggle="dropdown">
                        </a>
                        <div class="dropdown-menu">
                            <a v-for="size in paging.availableSizes" href="#" class="dropdown-item" @click.prevent="setPageSize(size)">{{ size }}</a>
                        </div>
                    </div>
                </div>

                <sm-datagrid-tools v-bind="{ options, columns, paging }"></sm-datagrid-tools>
            </div>
        </div>
    `,

    props: {
        options: Object,
        paging: Object,
        command: Object,
        columns: Array,
        rows: Array,
        total: Number,
        maxPagesToDisplay: { type: Number, required: false,  default: 10 },
    },

    created() {
        this.T = window.Res.DataGrid;
    },

    computed: {
        currentPageIndex() {
            return this.command.page;
        },

        currentPageSize() {
            return this.command.pageSize;
        },

        totalPages() {
            return this.$parent.totalPages;
        },

        hasPrevPage() {
            return this.currentPageIndex > 1;
        },

        hasNextPage() {
            return this.currentPageIndex < this.totalPages;
        },

        isFirstPage() {
            return this.currentPageIndex <= 1;
        },

        isLastPage() {
            return this.currentPageIndex >= this.totalPages;
        },

        firstItemIndex() {
            return Math.min(this.lastItemIndex, ((this.currentPageIndex - 1) * this.currentPageSize) + 1);
        },

        lastItemIndex() {
            return Math.min(this.total, (((this.currentPageIndex - 1) * this.currentPageSize) + this.currentPageSize));
        },

        pageItems() {
            var currentIndex = this.currentPageIndex;
            var totalPages = this.totalPages;
            var maxPages = this.maxPagesToDisplay;
            var start = 1;

            if (currentIndex > maxPages) {
                var v = currentIndex % maxPages;
                start = v === 0 ? currentIndex - maxPages + 1 : currentIndex - v + 1;
            }

            var p = start + maxPages - 1;
            p = Math.min(p, totalPages);

            var items = [];

            if (start > 1) {
                items.push({ page: start - 1, label: '...' });
            }

            for (var i = start; i <= p; i++) {
                items.push({ page: i, label: i.toString(), active: i === currentIndex });
            }

            if (p < totalPages) {
                items.push({ page: p + 1, label: '...' });
            }

            return items;
        }
    },

    methods: {
        refresh() {
            this.$parent.read();
        },

        pageTo(pageIndex) {
            if (pageIndex > 0 && pageIndex <= this.totalPages && !this.$parent.isBusy) {
                this.paging.pageIndex = pageIndex;
            }
        },

        setPageSize(size) {
            if (!this.$parent.isBusy) {
                if (size > this.command.pageSize) {
                    this.paging.pageIndex = 1;
                }
                this.paging.pageSize = size;
            }
        }
    }
});