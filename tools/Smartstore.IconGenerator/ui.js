class DialogBox {
  show(message) {
    document.getElementById('dialogMessage').innerText = message;
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