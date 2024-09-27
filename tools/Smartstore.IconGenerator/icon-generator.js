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
	constructor(){
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
			// Contains the svg code used to reference each symbol.
			svg: '',
		};
	}
	
	/**
	* Adds a new file to the icon set.
	* @param fileType Specifies the type of file to load. 0 = latest icon set (remote), 1 = latest icon set (local), 2 = current subset.
	*/
	async addFile(file, fileType){
		let self = this;
		const readFile = new Promise((resolve, reject) => {
			const reader = new FileReader();

			reader.onload = function(e) {
				self.parseXML(e.target.result, fileType);
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