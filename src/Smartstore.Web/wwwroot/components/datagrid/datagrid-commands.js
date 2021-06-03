Vue.component("sm-datagrid-commands", {
    template: `
        <div class="dg-cell dg-commands p-0">
            <div v-if="!editing.active || row != editing.row" class="d-flex w-100 h-100 align-items-center justify-content-center dropdown">
                <a href="#" class="dg-commands-toggle dropdown-toggle no-chevron btn btn-secondary btn-flat btn-icon btn-sm" data-toggle="dropdown" data-boundary="window">
                    <i class="fa fa-ellipsis-h"></i>
                </a>
                <div class="dg-commands-dropdown dropdown-menu dropdown-menu-right">
                    <a href="#" class="dropdown-item" @click="$parent.activateEdit(row)">Bearbeiten</a>
                    <a href="#" class="dropdown-item">Löschen</a>
                </div>
            </div>

            <div v-if="editing.active && row == editing.row" class="dg-row-edit-commands btn-group-vertical">
                <button @click="$parent.saveChanges()" class="btn btn-primary btn-sm btn-flat rounded-0" type="button" title="Änderungen speichern">
                    <i class="fa fa-check"></i>
                </button>
                <button @click="$parent.cancelEdit()" class="btn btn-secondary btn-sm btn-flat rounded-0" type="button" title="Abbrechen">
                    <i class="fa fa-times"></i>
                </button>
            </div>
        </div>
    `,

    props: {
        row: Object,
        editing: Object
    }
});