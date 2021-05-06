Vue.component("sm-data-grid-pager", {
    template: `
        <div class="dg-pager bg-light d-flex" style="border-bottom: 1px solid #dee2e6">
            <a href="#" class="dg-page dg-page--refresh px-3" @click="refresh"><i class="fa fa-sync-alt"></i></a>

            <a href="#" class="dg-page dg-page--first" @click="firstPage" :class="{ 'dg-page--disabled': !hasPrevPage }"><i class="fa fa-angle-double-left"></i></a>
            <a href="#" class="dg-page dg-page--prev" @click="prevPage" :class="{ 'dg-page--disabled': !hasPrevPage }"><i class="fa fa-angle-left"></i></a>
            <a href="#" class="dg-page dg-page--next" @click="nextPage" :class="{ 'dg-page--disabled': !hasNextPage }"><i class="fa fa-angle-right"></i></a>
            <a href="#" class="dg-page dg-page--last" @click="lastPage" :class="{ 'dg-page--disabled': !hasNextPage }"><i class="fa fa-angle-double-right"></i></a>

            <span class="text-muted px-2 py-2 pl-4">{{ currentPageIndex }} / {{ totalPages }}</span>
            <span class="px-2 py-2 ml-auto text-muted">Anzeigen der Elemente {{ firstItemIndex.toLocaleString() }} - {{ lastItemIndex.toLocaleString() }} von {{ total.toLocaleString() }}</span>
        </div>
    `,
    props: {
        command: Object,
        rows: Array,
        total: Number
    },
    created: function () {
    },
    mounted: function () {
    },
    data: function () {
        return {
        }
    },
    computed: {
        currentPageIndex() {
            return this.command.page;
        },

        currentPageSize() {
            return this.command.pageSize;
        },

        totalPages() {
            if (this.currentPageSize === 0)
                return 0;

            var total = this.total / this.currentPageSize;
            if (this.total % this.currentPageSize > 0)
                total++;

            return Math.floor(total);
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
            return ((this.currentPageIndex - 1) * this.currentPageSize) + 1;
        },

        lastItemIndex() {
            return Math.min(this.total, (((this.currentPageIndex - 1) * this.currentPageSize) + this.currentPageSize));
        },
    },
    methods: {
        refresh() {
            this.$parent.read();
        },
        firstPage() {
            this.command.page = 1;
        },
        prevPage() {
            this.command.page -= 1;
        },
        nextPage() {
            this.command.page += 1;
        },
        lastPage() {
            this.command.page = this.totalPages;
        },
    }
});