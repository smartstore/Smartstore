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

/**
* Apply the filter logic to the icon display.
*/
function applyFilter() {
    const searchTerm = textFilter.value;
    const onlyNew = newFilter.checked;
    const onlyUsed = usedFilter.checked;
	
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
}

/**
 * Show the export dialog.
 */
function showExport() {
    iconGenerator.exportIconSet();
    dialogBox.showExport(iconGenerator.lastExport);
}

/**
 * Try to restore and render the files from the local storage.
 */
function tryRestoreFiles() {
    iconGenerator.tryRestoreFile(localInput, 1);
    iconGenerator.tryRestoreFile(subsetInput, 2);
    if (iconGenerator.tryRestoreFile(remoteInput, 0)) {
        addFilesAndRender();
    }
}

/**
* This function makes sure all files are loaded in sequence.
*/
async function addFilesAndRender() {
    const remoteFile = remoteInput.files[0];
    const localFile = localInput.files[0];
    const subsetFile = subsetInput.files[0];

    if (!remoteFile) {
        return;
    }

    iconGenerator.reset();

    await iconGenerator.addFile(remoteFile, 0);

    if (localFile) {
        await iconGenerator.addFile(localFile, 1);
    }
    if (subsetFile) {
        await iconGenerator.addFile(subsetFile, 2);
    }

    iconGenerator.renderIconSet(iconContainer);

    // Make sure the export button is enabled/disabled correctly.
    iconContainer.dispatchEvent(new Event('click'));
}

/**
 * Add event listeners to the controls.
 */
function initUI() {
    // Apply filter on keyup and checkbox change.
    textFilter.addEventListener('keyup', applyFilter);
    newFilter.addEventListener('change', applyFilter);
    usedFilter.addEventListener('change', applyFilter);

    // Add read and export on buttons.
    readFilesButton.addEventListener('click', addFilesAndRender);
    exportButton.addEventListener('click', showExport);

    // Enable the 'Read files' button, when a remote file is selected.
    remoteInput.addEventListener('change', (e) => {
        readFilesButton.disabled = !e.target.files[0];
    });

    // Select and deselect icons, and enable/disable export button.
    iconContainer.addEventListener('click', (e) => {
        const icon = e.target.closest('.icon');

        if (icon != null) {
            // Toggle selection
            icon.classList.toggle('selected');
        }

        // Enable export button if at least one icon is selected.
        exportButton.disabled = !document.querySelector('.icon.selected');
    });

    // Download the export file in the dialog box.
    document.getElementById('dialogBox').addEventListener('click', (e) => {
        if (e.target.id === 'download-export') {
            iconGenerator.downloadExport();
        }
    });

    // Make sure the disabled state of the 'Read files' button is not set, when the last used file is cached.
    remoteInput.dispatchEvent(new Event('change'));
}