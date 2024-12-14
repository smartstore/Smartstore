class IconGenerator {
	constructor() {
        this.reset();
    }
	
	/**
	* Resets the icon set.
	*/
	reset() {
		this.iconSet = {
			// Contains each icon.
            icons: {},
			// Contains the SVG code used to reference each symbol.
			svg: ''
        };

        this.fileTypeCount = [0, 0, 0];
	}

    /**
     * Tries to restore a file from the local storage.
     * @param {HTMLInputElement} fileInput The file input element to which the file should be restored.
     * @param {number} fileType The type of file to restore. 0 = latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
     * @returns {boolean} True if the file was restored successfully, false otherwise.
     */
    tryRestoreFile(fileInput, fileType) {
        const codeKey = 'svg_' + fileType + '_code';
        const nameKey = 'svg_' + fileType + '_name';

        const savedCode = localStorage.getItem(codeKey);
        const savedFileName = localStorage.getItem(nameKey);
        if (!savedCode || !savedFileName) {
            return false;
        }

        const mimeString = 'image/svg+xml';

        // Create blob and file from the code.
        const blob = new Blob([savedCode], { type: mimeString });
        const file = new File([blob], savedFileName, { type: mimeString });

        // Insert the file into the file input element
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        fileInput.files = dataTransfer.files;

        return true;
    }

	/**
	* Adds a new file to the icon set.
    * @param [HTMLInputElement] file The file input element from which the file should be loaded.
	* @param fileType Specifies the type of file to load. 0 = latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
	*/
	async addFile(file, fileType) {
		const readFile = new Promise((resolve, _reject) => {
			const reader = new FileReader();

            reader.onload = (e) => {
                const codeKey = 'svg_' + fileType + '_code';
                const nameKey = 'svg_' + fileType + '_name';

                this.parseXML(e.target.result, fileType);

                // Save in localStorage
                localStorage.setItem(codeKey, e.target.result);
                localStorage.setItem(nameKey, file.name);

				resolve();
			};

			reader.readAsText(file);
		});
		
		return readFile;
	}

	/**
	* Parses an icon set with a given file type.
    * @param xml The XML content to parse.
	* @param fileType Specifies the type of file to load. latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
	*/
	parseXML(xml, fileType) {
		const parser = new DOMParser();
		const xmlDoc = parser.parseFromString(xml, "text/xml");
		const symbols = xmlDoc.children[0].children;
        const mySet = this.iconSet;

        // Reset the count of the current file type.
        this.fileTypeCount[fileType] = 0;
		
		for (const symbol of symbols) {
			let drawCode = '';
            let viewBox = symbol.getAttribute('viewBox');
            let id = symbol.id;

            // Increase the count of the current file type.
            this.fileTypeCount[fileType]++;
			
			// Retrieve svg code without the namespace.
            for (const part of symbol.children) {
				drawCode += "\n\t\t" + part.outerHTML.replaceAll(/\s+xmlns="http:\/\/www.w3.org\/2000\/svg"/g, '');
			}
			
            // Check if the icon already exists in the set.
            let icon = mySet.icons[id];
            if (icon) {
                // Update the usage bit to indicate that the icon is used in the current file type.
                icon.usageBit |= (fileType == 1 ? 2 : 0) | (fileType == 2 ? 4 : 0);
			}
			else {
				let symbolCode = '\t<symbol viewBox="' + viewBox + '" id="' + id + '">' + drawCode + '\n\t</symbol>';

                mySet.icons[id] = {
					viewBox: viewBox,
					id: id,
					symbol: symbolCode,
                    svg: '<svg xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16"><use xlink:href="#' + id + '" /></svg>',
                    // 0 = not used, 1 = new, 2 = only in local, 4 = only in subset
                    usageBit: 0 | (fileType == 0 ? 1 : 0) | (fileType == 1 ? 2 : 0) | (fileType == 2 ? 4 : 0),
                };
				
				mySet.svgSymbolCode += symbolCode;
			}
		}
	}
	
	/**
	* Renders the icon set by adding all used symbols to the reference SVG and inserting each into the given container.
	* @param iconSetContainer The container element to which all displayed icons are added.
	*/
	renderIconSet(iconSetContainer){
        document.getElementById('symbol_reference').innerHTML = this.iconSet.svgSymbolCode;
		
		let allIcons = '';

        for (const iconId in this.iconSet.icons) {
            let icon = this.iconSet.icons[iconId];
            let iconClasses =
                // Add a badge for new icons (only in the remote file).
                (icon.usageBit == 1 ? ' new' : '') +
                // Add a badge for the selected icons (used in the subset file).
                ((icon.usageBit & 4) == 4 ? ' selected' : '') +
                // Add a badge for icons not included in the subset file.
                (icon.usageBit == 2 ? ' only-local' : '') +
                // Add a badge for icons not included in the local file.
                (icon.usageBit == 5 ? ' not-local' : '');
			if (iconClasses.length > 0) {
				iconClasses += ' badge';
			}
			
            allIcons += '<div class="icon' + iconClasses + '" name="' + icon.id + '"><div class="symbol">' + icon.svg + '</div><span class="icon-name">' + icon.id + '</span></div>';
		}
		
		iconSetContainer.innerHTML = allIcons;
	}

    /**
    * Prepares the selected icons as an SVG file.
    */
    exportIconSet() {
        let exportContent = '<svg\n\txmlns="http://www.w3.org/2000/svg"\n\txmlns:xlink="http://www.w3.org/1999/xlink">';

        // Get all selected icons and collect their name attributes.
        const selectedIcons = document.querySelectorAll('.icon.selected');
        for (const icon of selectedIcons) {
            let iconName = icon.getAttribute('name');
            exportContent += '\n' + this.iconSet.icons[iconName].symbol;
        }

        exportContent += '\n</svg>';

        this.lastExport = exportContent;
    }

    /**
     * Downloads the last export as an SVG file.
     */
    downloadExport() {
        if(!this.lastExport) {
            return;
        }

        const exportFile = new Blob([this.lastExport], { type: 'image/svg+xml' });
        const exportUrl = URL.createObjectURL(exportFile);

        const downloadLink = document.createElement('a');
        downloadLink.href = exportUrl;

        downloadLink.download = 'icon_set.svg';
        downloadLink.click();
    }
}