/* 
    MC Review:
    -----------------------------------------------
    TODO: (mw) Make the selected state more "shiny" with more vibrant colors, outline, border etc.
	QUESTION: (mc) Too shiny?
	
    TODO: (mw) Implement decent responsiveness.

*/

/*
	TODO: (mw) Add Prettified export.
	TODO: (mw) Style controls: file inputs and buttons. Move Filters to an aside?
	
	QUESTION: (mc) Export using tabs or spaces?
	QUESTION: (mc) Export as text in a textarea, as download file, or offer both?
*/

class IconGenerator {
	constructor() {
        this.reset();
    }
	
	/**
	* Resets the icon set.
	*/
	reset(){
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
        if (!savedFile && !savedFileName) {
            return false;
        }

        // Wiederherstellen der Datei als Blob
        const base64Content = savedFile.split(',')[1];
        const mimeString = savedFile.split(',')[0].split(':')[1].split(';')[0];

        // Base64 dekodieren
        const binaryString = atob(base64Content);
        const len = binaryString.length;
        const uint8Array = new Uint8Array(len);

        // In eine Uint8Array umwandeln
        for (let i = 0; i < len; i++) {
            uint8Array[i] = binaryString.charCodeAt(i);
        }

        // Aus Uint8Array einen Blob erstellen
        const blob = new Blob([uint8Array], { type: mimeString });

        // Erstelle ein File-Objekt aus dem Blob
        const file = new File([blob], savedFileName, { type: mimeString });

        // Insert a new file into the file input element
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        fileInput.files = dataTransfer.files;

        return true;
    }

	/**
	* Adds a new file to the icon set.
	* @param fileType Specifies the type of file to load. 0 = latest full icon set (remote), 1 = current distributed full icon set (local), 2 = current distributed subset.
	*/
	async addFile(file, fileType){
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
	* @param fileType Specifies the type of file to load. 0 = latest icon set (remote), 1 = latest icon set (local), 2 = current subset.
	*/
	parseXML(xml, fileType){
		const parser = new DOMParser();
		const xmlDoc = parser.parseFromString(xml, "text/xml");
		const symbols = xmlDoc.children[0].children;
		const mySet = this.iconSet;
		
		for (const symbol of symbols){
			let drawCode = '';
			let viewBox = symbol.getAttribute('viewBox');
			let symbolCode = '<symbol viewBox="' + viewBox + '" id="' + symbol.id + '">' + drawCode + '</symbol>';
			
			// Retrieve svg code.
			for (const part of symbol.children){
				drawCode += "\n\t" + part.outerHTML;
			}
			
			let iconIndex = mySet.dictionary.indexOf(symbol.id);
			
			if (iconIndex !== -1){
				let icon = mySet.icons[iconIndex];
				icon.isNew = false;
				
				if (icon.code.trim() !== drawCode.trim()){
					icon.isModified = true;
				}
				
				if (fileType == 2){
					icon.isUsed = true;
				}
			}
			else{
				mySet.icons.push({
					viewBox: viewBox,
					id: symbol.id,
					code: drawCode,
					symbol: symbolCode,
					svg: '<svg xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">' + drawCode + '</svg>',
					isNew: true,
					isModified: false,
					isUsed: false, // Icon is used in the subset file, as in 'actively used'.
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
		
		for (const icon of this.iconSet.icons){
			let iconClasses = (icon.isNew ? ' new' : '') +
				(icon.isUsed ? ' selected subset' : '') +
				(icon.isModified ? ' modified' : '');
			if(iconClasses.length > 0){
				iconClasses += ' badge';
			}
			
			allIcons += '<div class="icon' + iconClasses + '" name="' + icon.id + '"><div class="symbol">' + icon.svg + '</div><span>' + icon.id + '</span></div>';
		}
		
		iconSetContainer.innerHTML = allIcons;
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