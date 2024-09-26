/* 
    MC Review:
    -----------------------------------------------
    TODO: (mw) Ask for 3 files to compare. Don't hardcode the remote paths. We should be able to compare ANY 3 SVG icon sets.
    TODO: (mh) Icon boxes must be equal width & height.
    TODO: (mh) Make icons slightly larger. See https://icons.getbootstrap.com/
    TODO: (mh) Place labels below icon boxes.
    TODO: (mh) Use hand cursor on box hover.
    TODO: (mh) Make the script an ES6 file.
    TODO: (mh) Make the selected state more "shiny" with more vibrant colors, outline, border etc.
    TODO: (mh) Implement decent responsiveness.

*/

/*
	TODO: (mw) Add documentation.
	TODO: (mw) Put code into module or IIFE for cleaner handling.
	TODO: (mw) Check whether the code is cleaner and still efficient using jQuery.
	TODO: (mw) Add Prettified export.
	TODO: (mw) If used further, save results of github files in localstorage and update on request.
	TODO: (mw) Rename booleans on objects.
	TODO: (mw) Alert user on 404 (fetchCode).
	
	QUESTION: (mc) Are the three file inputs for 'generic' icon sets necessary, or will this tool only be used to query the main branch of bootstrap and Smartstore's main branch icon sets?
        RE: We need 3 file inputs to make this tool more generic. We should be able to compare any 3 SVG icon sets.

*/

const iconSet = {
	// Contains each icon.
	icons: [],
	// Contains each icon ID.
	dictionary: [],
	// Contains the svg code used to reference each symbol.
	svg: '',
};

async function fetchCode (path){
	return await fetch(path)
		.then(function (response){
			switch (response.status) {
				case 200:
					return response.text();
				case 404:
					throw response;
			}
		})
		.then(function (source) {
			return source;
		})
		.catch(function (response){
			console.error(response.statusText);
			return null;
		});
}

/**
* Parses an icon set with a given file type
* @param fileType Specifies the type of file to load. 0 = latest icon set (remote), 1 = latest icon set (local), 2 = current subset.
*/
function parseXML(xml, fileType){
	const parser = new DOMParser();
	const doc = parser.parseFromString(xml, "text/xml");
	const symbols = doc.children[0].children
	
	for (const symbol of symbols){
		let drawCode = '';
		let viewBox = symbol.getAttribute('viewBox');
		let symbolCode = '<symbol viewBox="' + viewBox + '" id="' + symbol.id + '">' + drawCode + '</symbol>';
		
		// Retrieve svg code.
		for (const part of symbol.children){
			drawCode += "\n\t" + part.outerHTML;
		}
		
		let iconIndex = iconSet.dictionary.indexOf(symbol.id);
		
		if (iconIndex !== -1){
			let icon = iconSet.icons[iconIndex];
			icon.new = false;
			
			if (icon.code.trim() !== drawCode.trim()){
				icon.modified = true;
			}
			
			if (fileType == 2){
				icon.used = true;
			}
		}
		else{
			iconSet.icons.push({
				viewBox: viewBox,
				id: symbol.id,
				code: drawCode,
				symbol: symbolCode,
				svg: '<svg xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 16 16">' + drawCode + '</svg>',
				'new': true,
				modified: false,
				used: false,
			});
			
			iconSet.dictionary.push(symbol.id);
			
			iconSet.svgCode += symbolCode;
		}
	}
}

/**
* Adds a new file to the icon set.
* @param fileType Specifies the type of file to load. 0 = latest icon set (remote), 1 = latest icon set (local), 2 = current subset.
*/
async function addFile(fileName, fileType){
	let code = await fetchCode(fileName);
	
	if (code == null){
		return;
	}
	
	parseXML(code, fileType);
}

function renderIconSet(){
	const svg = document.createElement('svg');
	svg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
	svg.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
	svg.innerHTML = iconSet.svg;
	document.body.appendChild(svg);
	
	const iconSetContainer = document.getElementById('iconContainer');
	let allIcons = '';
	
	for (const icon of iconSet.icons){
		let iconClasses = (icon.new ? ' new' : '') +
			(icon.used ? ' selected subset' : '') +
			(icon.modified ? ' modified' : '');
		if(iconClasses.length > 0){
			iconClasses += ' badge';
		}
		
		allIcons += '<div class="icon' + iconClasses + '" name="' + icon.id + '">' + icon.svg + '<span>' + icon.id + '</span></div>';
	}
	
	iconSetContainer.innerHTML = allIcons;
	
	iconSetContainer.addEventListener('click', (e) => {
		const icon = e.target.closest('.icon');
	
		if (icon != null){
			// Toggle selection
			icon.classList.toggle('selected');
		}
	});
}

async function addFilesAndRender(remoteFile, localFile, subsetFile){ // Make sure everything is loaded in sequence.
	await addFile(remoteFile, 0);
	await addFile(localFile, 1);
	await addFile(subsetFile, 2);
	
	renderIconSet();
}

function applyFilter(){
	const searchTerm = document.querySelector('#controls input[name=filter]').value;
	const onlyNew = document.getElementById('filter_new').checked;
	const onlyModified = document.getElementById('filter_modified').checked;
	const onlyUsed = document.getElementById('filter_used').checked;
	const onlySelected = document.getElementById('filter_selected').checked;
	
	// Filter by search term.
	if (searchTerm.length > 0 || onlyNew || onlyModified || onlyUsed || onlySelected){
		const icons = document.querySelectorAll('.icon');
		
		for (const icon of icons){
			let iconName = icon.getAttribute('name');
			
			if (iconName.includes(searchTerm) &&
				(!onlyNew || (onlyNew && icon.classList.contains('new'))) &&
				(!onlyModified || (onlyModified && icon.classList.contains('modified'))) &&
				(!onlyUsed || (onlyUsed && icon.classList.contains('subset'))) &&
				(!onlySelected || (onlySelected && icon.classList.contains('selected')))
			){
				icon.classList.remove('hide');
			}
			else{
				icon.classList.add('hide');
			}
		}
	}
	else{
		const hiddenIcons = document.querySelectorAll('.icon.hide');
		
		for (const icon of hiddenIcons){
			icon.classList.remove('hide');
		}
	}
}

let github_RemoteFile = 'https://raw.githubusercontent.com/twbs/icons/refs/heads/main/bootstrap-icons.svg';
let github_LocalFile = 'https://raw.githubusercontent.com/smartstore/Smartstore/refs/heads/main/src/Smartstore.Web/wwwroot/lib/bi/bootstrap-icons-all.svg';
let github_SubsetFile = 'https://raw.githubusercontent.com/smartstore/Smartstore/refs/heads/main/src/Smartstore.Web/wwwroot/lib/bi/bootstrap-icons.svg';
addFilesAndRender(github_RemoteFile, github_LocalFile, github_SubsetFile);

document.querySelector('#controls input[name=filter]').addEventListener('keyup', applyFilter);
document.getElementById('filter_new').addEventListener('change', applyFilter);
document.getElementById('filter_modified').addEventListener('change', applyFilter);
document.getElementById('filter_used').addEventListener('change', applyFilter);
document.getElementById('filter_selected').addEventListener('change', applyFilter);