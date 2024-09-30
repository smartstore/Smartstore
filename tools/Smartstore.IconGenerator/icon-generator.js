/* 
    MC Review:
    -----------------------------------------------
	
    TODO: (mw) Implement decent responsiveness for upper #controls bar.

    TODO: (mw) Study the review commits and comply to conventions and quality level in future.
    TODO: (mw) CSS needs better (predictable) structure. Use more class names.
    TODO: (mw) Always work with CSS variables.
    TODO: (mw) Make proper ES6 modules and actually LOAD them as modules. Your scripts are NOT modules. Ask ChatGPT!
    TODO: (mw) The HTML file should not contain any inline scripts. Just initialization stuff.
*/

/*
	TODO: (mw) Add Prettified export.
	
	QUESTION: (mc) Export using tabs or spaces? RE: Tabs.
	QUESTION: (mc) Export as text in a textarea, as download file, or offer both? RE: Offer both!
*/

export class IconGenerator {
	constructor() {
        this.reset();
    }
	
	/**
	* Resets the icon set.
	*/
	reset() {
		this.iconSet = {
			// Contains each icon.
			icons: [],
			// Contains each icon ID.
			dictionary: [],
			// Contains the SVG code used to reference each symbol.
			svg: ''
		};
	}

    tryRestoreFile(fileInput, fileType) {
        const blobKey = 'svg_' + fileType + '_blob';
        const nameKey = 'svg_' + fileType + '_name';

        const savedFile = localStorage.getItem(blobKey);
        const savedFileName = localStorage.getItem(nameKey);
        if (!savedFile || !savedFileName) {
            return false;
        }

        // Extract metadata and content from the saved file.
        const metadata = savedFile.split(',');
		const base64Content = metadata[1];
        const mimeString = metadata[0].split(':')[1].split(';')[0];

        // Decode Base64.
        const binaryString = atob(base64Content);
        const uint8Array = Uint8Array.from(binaryString, c => c.charCodeAt(0));

        // Create blob and file from the Uint8Array.
        const blob = new Blob([uint8Array], { type: mimeString });
        const file = new File([blob], savedFileName, { type: mimeString });

        // Insert the file into the file input element
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        fileInput.files = dataTransfer.files;

        return true;
    }

	/**
	* Adds a new file to the icon set.
	* @param fileType Specifies the type of file to load. 0 = latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
	*/
	async addFile(file, fileType) {
		const readFile = new Promise((resolve, _reject) => {
			const reader = new FileReader();

            reader.onload = (e) => {
                const blobKey = 'svg_' + fileType + '_blob';
                const nameKey = 'svg_' + fileType + '_name';

                this.parseXML(e.target.result, fileType);

                // Save in localStorage
                const base64String = utf8ToBase64(e.target.result);
                const dataUrl = `data:${file.type};base64,${base64String}`;
                localStorage.setItem(blobKey, dataUrl);
                localStorage.setItem(nameKey, file.name);

				resolve();
			};

			reader.readAsText(file);
		});
		
		return readFile;
	}

	/**
	* Parses an icon set with a given file type.
	* @param fileType Specifies the type of file to load. latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
	*/
	parseXML(xml, fileType) {
		const parser = new DOMParser();
		const xmlDoc = parser.parseFromString(xml, "text/xml");
		const symbols = xmlDoc.children[0].children;
		const mySet = this.iconSet;
		
		for (const symbol of symbols) {
			let drawCode = '';
			let viewBox = symbol.getAttribute('viewBox');
			
			// Retrieve svg code.
			for (const part of symbol.children) {
				drawCode += "\n\t\t" + part.outerHTML;
			}
			
			let iconIndex = mySet.dictionary.indexOf(symbol.id);
			
			if (iconIndex !== -1) {
				let icon = mySet.icons[iconIndex];
				icon.isNew = false;
				
				if (fileType == 2) {
					icon.isUsed = true;
				}
			}
			else {
				let symbolCode = '\t<symbol viewBox="' + viewBox + '" id="' + symbol.id + '">' + drawCode + '\n\t</symbol>';
				
				mySet.icons.push({
					viewBox: viewBox,
					id: symbol.id,
					code: drawCode,
					symbol: symbolCode,
                    svg: '<svg xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">' + drawCode + '</svg>',
                    isNew: true,
                    isUsed: false
				});
				
				mySet.dictionary.push(symbol.id);
				
				mySet.svgCode += symbolCode;
			}
		}
	}
	
	/**
	* Renders the icon set by adding all used symbols to the reference SVG and inserting each into the given container.
	* @param iconSetContainer The container element to which all displayed icons are added.
	*/
	renderIconSet(iconSetContainer){
		document.getElementById('symbol_reference').innerHTML = this.iconSet.svg;
		
		let allIcons = '';
		
		for (const icon of this.iconSet.icons) {
			let iconClasses = (icon.isNew ? ' new' : '') +
				(icon.isUsed ? ' selected' : '');
			if (iconClasses.length > 0) {
				iconClasses += ' badge';
			}
			
			allIcons += '<div class="icon' + iconClasses + '" name="' + icon.id + '"><div class="symbol">' + icon.svg + '</div><span>' + icon.id + '</span></div>';
		}
		
		iconSetContainer.innerHTML = allIcons;
	}

    /**
    * Exports the selected icons as an SVG file.
    * @param asDownload Specifies whether the export should be returned as a string or downloaded.
    */
    exportIconSet(asDownload) {
        let exportContent = '<svg\n\txmlns="http://www.w3.org/2000/svg"\n\txmlns:xlink="http://www.w3.org/1999/xlink">';

        // Get all selected icons and collect their name attributes.
        const selectedIcons = document.querySelectorAll('.icon.selected');
        for (const icon of selectedIcons) {
            let iconName = icon.getAttribute('name');
            let iconIndex = this.iconSet.dictionary.indexOf(iconName);

            // TODO: (mw) Pretty print the SVG code.
            exportContent += '\n' + this.iconSet.icons[iconIndex].symbol;
        }

        exportContent += '\n</svg>';

        if (!asDownload) {
            // TODO: (mw) Add exportContent to a textarea for copy-pasting.
        }
        else {
            const exportFile = new Blob([exportContent], { type: 'image/svg+xml' });
            const exportUrl = URL.createObjectURL(exportFile);

            const downloadLink = document.createElement('a');
            downloadLink.href = exportUrl;

            downloadLink.download = 'icon_set.svg';
            downloadLink.click();
        }
    }
}

function utf8ToBase64(str) {
    // Text in UTF-8 Byte-Array umwandeln
    const utf8Bytes = new TextEncoder().encode(str);

    // Byte-Array in Base64 umwandeln
    let binary = '';
    utf8Bytes.forEach((byte) => {
        binary += String.fromCharCode(byte);
    });

    return btoa(binary);
}