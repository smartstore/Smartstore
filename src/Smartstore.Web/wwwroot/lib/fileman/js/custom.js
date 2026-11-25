/*
  Fileman - web based file manager. Ready to use with CKEditor, TinyMCE.
  Can be easily integrated with any other WYSIWYG editor or CMS.

  Copyright (C) 2013, Lyubomir Arsov. All rights reserved.
*/
function FileSelected(file) {
    /**
     * file is an object containing following properties:
     * 
     * fullPath - path to the file - absolute from your site root
     * path - directory in which the file is located - absolute from your site root
     * size - size of the file in bytes
     * time - timestamp of last modification
     * name - file name
     * ext - file extension
     * width - if the file is image, this will be the width of the original image, 0 otherwise
     * height - if the file is image, this will be the height of the original image, 0 otherwise
     * 
     */

    var f = {
        url: file.fullPath,
        dir: file.path,
        path: file.path.trimEnd('/') + '/' + file.name,
        name: file.name,
        ext: file.ext,
        size: file.size,
        lastUpdated: file.time
    };

    window.postMessage([f], window.location.origin);
}

function GetSelectedValue() {
    /**
     * This function is called to retrieve selected value when custom integration is used.
     * Url parameter selected will override this value.
     */

	var p = (window.opener || window.parent);
	var fieldId = FilemanUtils.GetUrlParam('field');

	if (fieldId) {
		return p.window.jQuery('#' + fieldId).val();
	}
}