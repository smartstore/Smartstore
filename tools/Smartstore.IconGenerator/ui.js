class DialogBox {
    showExport(svgCode) {
        document.getElementById('dialogExport').innerHTML = svgCode;
        document.getElementById('dialogBox').classList.remove('hidden');
    }

    hide() {
        document.getElementById('dialogBox').classList.add('hidden');
    }
}

const dialogBox = new DialogBox();

// The UI object handles the UI logic of the icon generator.
const iconGeneratorUI = (function () {
    return {
        /**
        * Apply the filter logic to the icon display.
        */
        applyFilter: function () {
            const searchTerm = iconGeneratorUI.textFilter.value;
            const onlyNew = iconGeneratorUI.newFilter.checked;
            const onlyUsed = iconGeneratorUI.usedFilter.checked;

            // Filter by search term.
            if (searchTerm.length > 0 || onlyNew || onlyUsed) {
                const icons = document.querySelectorAll('.icon');

                for (const icon of icons) {
                    let iconName = icon.getAttribute('name');

                    if (iconName.includes(searchTerm) &&
                        (!onlyNew || (onlyNew && icon.classList.contains('new'))) &&
                        (!onlyUsed || (onlyUsed && icon.classList.contains('selected')))
                    ) {
                        icon.classList.remove('hide');
                    }
                    else {
                        icon.classList.add('hide');
                    }
                }
            }
            else {
                const hiddenIcons = document.querySelectorAll('.icon.hide');

                for (const icon of hiddenIcons) {
                    icon.classList.remove('hide');
                }
            }
        },

        /**
         * Show the export dialog.
         */
        showExport: function () {
            iconGeneratorUI.generator.exportIconSet();
            dialogBox.showExport(iconGeneratorUI.generator.lastExport);
        },

        /**
         * Try to restore and render the files from the local storage.
         */
        tryRestoreFiles: function () {
            const iGenerator = iconGeneratorUI.generator;

            iGenerator.tryRestoreFile(iconGeneratorUI.localInput, 1);
            iGenerator.tryRestoreFile(iconGeneratorUI.subsetInput, 2);
            if (iGenerator.tryRestoreFile(iconGeneratorUI.remoteInput, 0)) {
                iconGeneratorUI.addFilesAndRender();
            }
        },

        /**
        * This function makes sure all files are loaded in sequence.
        */
        addFilesAndRender: async function () {
            const remoteFile = iconGeneratorUI.remoteInput.files[0];
            const localFile = iconGeneratorUI.localInput.files[0];
            const subsetFile = iconGeneratorUI.subsetInput.files[0];

            if (!remoteFile) {
                return;
            }

            const iGenerator = iconGeneratorUI.generator;
            iGenerator.reset();

            await iGenerator.addFile(remoteFile, 0);

            if (localFile) {
                await iGenerator.addFile(localFile, 1);
            }
            if (subsetFile) {
                await iGenerator.addFile(subsetFile, 2);
            }

            iGenerator.renderIconSet(iconGeneratorUI.iconContainer);

            // Update the file count for remotefile, localfile and subsetfile.
            document.querySelector('label[for="' + iconGeneratorUI.remoteInput.id + '"]').textContent =
                'Remote (' + iGenerator.fileTypeCount[0] + ' icon' + (iGenerator.fileTypeCount[0] == 1 ? '' : 's') + ')';
            document.querySelector('label[for="' + iconGeneratorUI.localInput.id + '"]').textContent =
                'Local (' + iGenerator.fileTypeCount[1] + ' icon' + (iGenerator.fileTypeCount[1] == 1 ? '' : 's') + ')';
            document.querySelector('label[for="' + iconGeneratorUI.subsetInput.id + '"]').textContent =
                'Subset (' + iGenerator.fileTypeCount[2] + ' icon' + (iGenerator.fileTypeCount[2] == 1 ? '' : 's') + ')';

            // Make sure the export button is enabled/disabled correctly.
            iconGeneratorUI.iconContainer.dispatchEvent(new Event('click'));

            // Enable export button if at least one icon is selected.
            iconGeneratorUI.exportButton.disabled = !document.querySelector('.icon.selected');
        },

        /**
         * Add event listeners to the controls.
         */
        initUI: function () {
            // Apply filter on keyup and checkbox change.
            iconGeneratorUI.textFilter.addEventListener('keyup', iconGeneratorUI.applyFilter);
            iconGeneratorUI.newFilter.addEventListener('change', iconGeneratorUI.applyFilter);
            iconGeneratorUI.usedFilter.addEventListener('change', iconGeneratorUI.applyFilter);

            // Add read and export on buttons.
            iconGeneratorUI.readFilesButton.addEventListener('click', iconGeneratorUI.addFilesAndRender);
            iconGeneratorUI.exportButton.addEventListener('click', iconGeneratorUI.showExport);

            // Enable the 'Read files' button, when a remote file is selected.
            iconGeneratorUI.remoteInput.addEventListener('change', (e) => {
                iconGeneratorUI.readFilesButton.disabled = !e.target.files[0];
            });

            // Select and deselect icons, and enable/disable export button.
            iconGeneratorUI.iconContainer.addEventListener('click', (e) => {
                if (e.target.matches('.icon-name')) {
                    var range = document.createRange();
                    var selection = window.getSelection();

                    range.selectNodeContents(e.target);  // Select the contents of the clicked element
                    selection.removeAllRanges();     // Clear any existing selections
                    selection.addRange(range);       // Add the new range (selected element)
                }
                else {
                    const symbol = e.target.closest('.symbol');
                    if (symbol != null) {
                        // Toggle selection
                        symbol.parentElement.classList.toggle('selected');

                        // Enable export button if at least one icon is selected.
                        iconGeneratorUI.exportButton.disabled = !document.querySelector('.icon.selected');
                    }
                }
            });

            // Download the export file in the dialog box.
            document.getElementById('dialogBox').addEventListener('click', (e) => {
                if (e.target.id === 'download-export') {
                    iconGeneratorUI.generator.downloadExport();
                }
            });

            // Make sure the disabled state of the 'Read files' button is not set, when the last used file is cached.
            iconGeneratorUI.remoteInput.dispatchEvent(new Event('change'));
        },

        init: function () {

        }
    };
})();

// Initialize the UI after the DOM has loaded.
document.addEventListener('DOMContentLoaded', () => {
    iconGeneratorUI.generator = new IconGenerator();

    // File input elements.
    iconGeneratorUI.remoteInput = document.getElementById('file_remote');
    iconGeneratorUI.localInput = document.getElementById('file_local');
    iconGeneratorUI.subsetInput = document.getElementById('file_subset');

    // Filter input elements.
    iconGeneratorUI.textFilter = document.getElementById('filter_text');
    iconGeneratorUI.newFilter = document.getElementById('filter_new');
    iconGeneratorUI.usedFilter = document.getElementById('filter_used');

    // Icon container and buttons.
    iconGeneratorUI.iconContainer = document.getElementById('iconContainer');
    iconGeneratorUI.readFilesButton = document.getElementById('read_files');
    iconGeneratorUI.exportButton = document.getElementById('export_subset');

    iconGeneratorUI.tryRestoreFiles();
    iconGeneratorUI.initUI();
});