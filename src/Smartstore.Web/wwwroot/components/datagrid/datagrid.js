Vue.component("sm-data-grid", {
    template: `
        <div class="h3">Hello World {{ columns.length }}</div>
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

    },
    data: function () {
        return {
            test: "Hallo Welt"
        }
    },
    computed: {
        computeTest() {
            return "test";
        }
    }
});