/*
  RoxyFileman - web based file manager. Ready to use with CKEditor, TinyMCE. 
  Can be easily integrated with any other WYSIWYG editor or CMS.

  Copyright (C) 2013, RoxyFileman.com - Lyubomir Arsov. All rights reserved.
  For licensing, see LICENSE.txt or http://RoxyFileman.com/license

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <http://www.gnu.org/licenses/>.

  Contact: Lyubomir Arsov, liubo (at) web-lobby.com
*/
$.ajaxSetup({
	cache: false
});

// The embedded page does not load the full admin bundle or its AJAX interceptor.
$.ajaxPrefilter(function (options, originalOptions, xhr) {
	if (!options.crossDomain && options.type.toUpperCase() === 'POST') {
		xhr.setRequestHeader('X-XSRF-Token', $('#frmUpload input[name="__RequestVerificationToken"]').val());
	}
});

function selectFile(item) {
	$('#pnlFileList li').removeClass('selected');
	$(item).addClass('selected');
	var html = FilemanUtils.GetFilename($(item).attr('data-path'));
	html += ' (' + t('Size') + ': ' + FilemanUtils.FormatFileSize($(item).attr('data-size'));
	if ($(item).attr('data-w') > 0)
		html += ', ' + t('Dimensions') + ':' + $(item).attr('data-w') + 'x' + $(item).attr('data-h');
	html += ')';
	$('#pnlStatus').html(html);
}

function getLastDir() {
	return FilemanUtils.GetCookie('roxyld');
}

function setLastDir(path) {
	FilemanUtils.SetCookie('roxyld', path, 10);
}

function selectDir(item, indeterm) {
	var d = Directory.Parse($(item).parent('li').attr('data-path'));
	if (d) {
		d.Select(null, indeterm);
	}
}

function addDir() {
	var f = getSelectedDir();
	if (!f)
		return;
	$('#txtDirName').val('');

	var dialogButtons = {};
	dialogButtons[t('CreateDir')] = function () {
		var newName = $('#txtDirName').val().trim();
		if (!newName) {
			alert(t('E_MissingDirName'));
			return;
		}
		f.Create(newName, () => {
			$('#pnlDirName').modal('hide');
		});
	};
	dialogButtons[t('Cancel')] = function () {
		$('#pnlDirName').modal('hide');
	};
	FilemanUI.showDialog('#pnlDirName', {
		title: t('T_CreateDir'),
		buttons: dialogButtons
	});
}
var uploadFileList = new Array();

function showUploadList(files) {
	var filesPane = $('#uploadFilesList');
	filesPane.html('');
	clearFileField();
	for (i = 0; i < files.length; i++) {
		var f = files[i];
		var html = [
			'<div class="fileUpload">',
			'<div class="fileName">' + f.name + ' (' + FilemanUtils.FormatFileSize(f.size) + ')',
			'<span class="progressPercent"></span>',
			'<div class="uploadProgress"><div class="stripes"></div></div>',
			'</div>',
			'<a class="removeUpload" onclick="removeUpload(' + i + ')"></a>',
			'</div>'
		].join('');
		filesPane.append(html);
	}
	if (files.length > 0)
		$('#btnUpload').prop('disabled', false);
	else
		$('#btnUpload').prop('disabled', true);
}

function listUploadFiles(files) {
	if (!window.FileList) {
		$('#btnUpload').prop('disabled', false);
	} else if (files.length > 0) {
		uploadFileList = new Array();
		addUploadFiles(files);
	}
}

function addUploadFiles(files) {
	for (i = 0; i < files.length; i++)
		uploadFileList.push(files[i]);
	showUploadList(uploadFileList);
}

function removeUpload(i) {
	var el = findUploadElement(i);
	el.remove();
	try {
		uploadFileList.splice(i, 1);
		showUploadList(uploadFileList);
	} catch (ex) {
		//alert(ex); 
	}
}

function findUploadElement(i) {
	return $('#uploadFilesList .fileUpload:eq(' + (i) + ')');
}

function updateUploadProgress(e, i) {
	var el = findUploadElement(i);
	var percent = 99;
	if (e.lengthComputable) {
		percent = Math.floor((e.loaded / e.total) * 100);
	}
	if (percent > 99)
		percent = 99;
	el.find('.uploadProgress').css('width', percent + '%');
	el.find('.progressPercent').html(' - ' + percent + '%');
}

function uploadComplete(e, i) {
	uploadFinished(e, i, 'ok');
}

function uploadError(e, i) {
	setUploadError(i);
	uploadFinished(e, i, 'error');
}

function setUploadError(i) {
	var el = findUploadElement(i);
	el.find('.uploadProgress').css('width', '100%').addClass('uploadError').removeClass('uploadComplete');
	el.find('.progressPercent').html(' - <span class="error">' + t('E_UploadingFile') + '</span>');
}

function setUploadSuccess(i) {
	var el = findUploadElement(i);
	el.find('.uploadProgress').css('width', '100%').removeClass('uploadError').addClass('uploadComplete');
	el.find('.progressPercent').html(' - 100%');
}

function uploadCanceled(e, i) {
	uploadFinished(e, i, 'error');
}

function uploadFinished(e, i, res) {
	var el = findUploadElement(i);
	var httpRes = null;
	try {
		httpRes = JSON.parse(e.target.responseText);
	} catch (ex) { /**/ }

	if ((httpRes && httpRes.res === 'error') || res !== 'ok') {
		res = 'error';
		setUploadError(i);
	} else {
		res = 'ok';
		setUploadSuccess(i);
	}

	el.attr('data-ulpoad', res);
	checkUploadResult();
}

function checkUploadResult() {
	var all = $('#uploadFilesList .fileUpload').length;
	var completed = $('#uploadFilesList .fileUpload[data-ulpoad]').length;
	var success = $('#uploadFilesList .fileUpload[data-ulpoad="ok"]').length;
	if (completed === all) {
		//$('#uploadResult').html(success + ' files uploaded; '+(all - success)+' failed');
		uploadFileList = new Array();
		var d = Directory.Parse($('#hdDir').val());
		d.ListFiles(true);
		$('#btnUpload').prop('disabled', true);
	}
}

function fileUpload(f, i) {
	var http = new XMLHttpRequest();
	var fData = new FormData();
	var el = findUploadElement(i);
	el.find('.removeUpload').remove();
	fData.append("action", 'upload');
	fData.append("method", 'ajax');
	fData.append("d", $('#hdDir').val());
	fData.append("files[]", f);
	fData.append("__RequestVerificationToken", $('#frmUpload input[name="__RequestVerificationToken"]').val());
	http.upload.addEventListener("progress", function (e) {
		updateUploadProgress(e, i);
	}, false);
	http.addEventListener("load", function (e) {
		uploadComplete(e, i);
	}, false);
	http.addEventListener("error", function (e) {
		uploadError(e, i);
	}, false);
	http.addEventListener("abort", function (e) {
		uploadCanceled(e, i);
	}, false);
	http.open("POST", FilemanUtils.GetRootPath(FilemanConfig.UPLOAD), true);
	http.setRequestHeader("Accept", "*/*");
	http.send(fData);
}

function dropFiles(e, append) {
	if (e && e.dataTransfer && e.dataTransfer.files.length > 0) {
		if (!$('#dlgAddFile').hasClass('show')) addFile();
		if (append)
			addUploadFiles(e.dataTransfer.files);
		else
			listUploadFiles(e.dataTransfer.files);
	}
}

function clearFileField(selector) {
	if (!selector)
		selector = '#fileUploads';
	try {
		$(selector).val('');
		$(selector).val(null);
	} catch (ex) { /**/ }
}

function addFileClick() {
	$('#uploadResult').html('');
	showUploadList(new Array());
	addFile();
}

function addFile() {
	$('#uploadResult').html('');
	clearFileField();
	var dialogButtons = {};
	dialogButtons[t('Upload')] = {
		id: 'btnUpload',
		text: t('Upload'),
		disabled: true,
		click: function () {
			if (!$('#fileUploads').val() && (!uploadFileList || uploadFileList.length === 0))
				alert(t('E_SelectFiles'));
			else {
				if (!FilemanConfig.UPLOAD) {
					alert(t('E_ActionDisabled'));
					//$('#dlgAddFile').modal('hide');
				} else {
					if (window.FormData && window.XMLHttpRequest && window.FileList && uploadFileList && uploadFileList.length > 0) {
						for (i = 0; i < uploadFileList.length; i++) {
							fileUpload(uploadFileList[i], i);
						}
					} else {
						document.forms['addfile'].action = FilemanUtils.GetRootPath(FilemanConfig.UPLOAD);
						document.forms['addfile'].submit();
					}
				}
			}
		}
	};

	dialogButtons[t('Cancel')] = function () {
		$('#dlgAddFile').modal('hide');
	};
	FilemanUI.showDialog('#dlgAddFile', {
		title: t('T_AddFile'),
		buttons: dialogButtons
	});
}

function fileUploaded(res) {
	var d;
	if (res.res === 'ok' && res.msg) {
		$('#dlgAddFile').modal('hide');
		d = Directory.Parse($('#hdDir').val());
		d.ListFiles(true);
		alert(res.msg);
	} else if (res.res === 'ok') {
		$('#dlgAddFile').modal('hide');
		d = Directory.Parse($('#hdDir').val());
		d.ListFiles(true);
	} else
		alert(res.msg);
}

function renameDir() {
	var f = getSelectedDir();
	if (!f)
		return;
	if ($('[data-path="' + f.fullPath + '"]').parents('li').length < 1) {
		alert(t('E_CannotRenameRoot'));
		return;
	}
	$('#txtDirName').val(f.name);

	var dialogButtons = {};
	dialogButtons[t('RenameDir')] = function () {
		var newName = $('#txtDirName').val().trim();
		if (!newName) {
			alert(t('E_MissingDirName'));
			return;
		}
		f.Rename(newName, () => {
			$('#pnlDirName').modal('hide');
		});
			
	};
	dialogButtons[t('Cancel')] = function () {
		$('#pnlDirName').modal('hide');
	};

	FilemanUI.showDialog('#pnlDirName', {
		title: t('T_RenameDir'),
		buttons: dialogButtons,
		onShown: () => FilemanUtils.SelectText('txtDirName', 0, f.name.length)
	});
}

function renameFile() {
	var f = getSelectedFile();
	if (!f)
		return;
	$('#txtFileName').val(f.name);

	var dialogButtons = {};
	dialogButtons[t('RenameFile')] = function () {
		var newName = $('#txtFileName').val().trim();
		if (!newName)
			alert('Missing file name');
		else {
			f.Rename(newName, () => {
				$('li[data-path="' + f.fullPath + '"] .name').text(newName);
				$('li[data-path="' + f.fullPath + '"]').attr('data-path', FilemanUtils.MakePath(f.path, newName));
				$('#pnlRenameFile').modal('hide');
			});
		}
	};
	dialogButtons[t('Cancel')] = function () {
		$('#pnlRenameFile').modal('hide');
	};

	FilemanUI.showDialog('#pnlRenameFile', {
		title: t('T_RenameFile'),
		buttons: dialogButtons,
		onShown: () => FilemanUtils.SelectText('txtFileName', 0, f.name.lastIndexOf('.') > 0 ? f.name.lastIndexOf('.') : f.name.length)
	});
}

function getSelectedFile() {
	var ret = null;
	if ($('#pnlFileList .selected').length > 0) {
		var el = $('#pnlFileList .selected');
		ret = new File(el.attr('data-path'));
	}
		
	return ret;
}

function getSelectedDir() {
	var ret = null;
	var indeterm = $('#pnlDirList').data('indeterm');

	if (indeterm) {
		ret = Directory.Parse(indeterm.closest('li').attr('data-path'));
	}
	else if ($('#pnlDirList .selected')) {
		ret = Directory.Parse($('#pnlDirList .selected').closest('li').attr('data-path'));
	}

	return ret;
}

function deleteDir(path) {
	var d = null;
	if (path)
		d = Directory.Parse(path);
	else
		d = getSelectedDir();

	if (d && confirm(t('Q_DeleteFolder'))) {
		d.Delete();
	}
}

function deleteFile() {
	var f = getSelectedFile();
	if (f && confirm(t('Q_DeleteFile'))) {
		f.Delete();
	}
}

function previewFile() {
	var f = getSelectedFile();
	if (f) {
		window.open(f.fullPath);
	}
}

function downloadFile() {
	var f = getSelectedFile();
	if (f && FilemanConfig.DOWNLOAD) {
		var url = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.DOWNLOAD), 'f', f.fullPath);
		window.frames['frmUploadFile'].location.href = url;
	} else if (!FilemanConfig.DOWNLOAD)
		alert(t('E_ActionDisabled'));
}

function downloadDir() {
	var d = getSelectedDir();
	if (d && FilemanConfig.DOWNLOADDIR) {
		var url = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.DOWNLOADDIR), 'd', d.fullPath);
		window.frames['frmUploadFile'].location.href = url;
	} else if (!FilemanConfig.DOWNLOAD)
		alert(t('E_ActionDisabled'));
}

function closeMenus(el) {
	if (!el || el === 'dir') {
		var indeterm = $('#pnlDirList').data('indeterm');
		if (indeterm) {
			$('#pnlDirList').data('indeterm', null);
			$('#pnlDirList .indeterm').removeClass('indeterm');
		}
		$('#menuDir').hide();
	}
	if (!el || el === 'file') {
		$('#menuFile').hide();
	}
}

function selectFirst() {
	var item = $('#pnlDirList li:first').children('div').first();
	if (item.length > 0)
		selectDir(item);
	else
		window.setTimeout('selectFirst()', 300);
}

function tooltipContent() {
	if ($('#menuFile').is(':visible')) return '';
	var f = File.Parse($(this).attr('data-path'));
	if (!f) return '';
	var content = $('<div>');
	var image = f.IsImage();
	var thumbnailView = $('#hdViewType').val() === 'thumb';
	if (image && !thumbnailView) {
		var url = f.fullPath;
		if (FilemanConfig.GENERATETHUMB) {
			url = FilemanUtils.AddParam(url, 'w', FilemanConfig.PREVIEW_THUMB_WIDTH);
			url = FilemanUtils.AddParam(url, 'h', FilemanConfig.PREVIEW_THUMB_HEIGHT);
		}
		$('<div class="text-center mb-2">').append($('<img class="imgPreview" alt="">').attr('src', url)).appendTo(content);
	}
	$('<div>').text(image && !thumbnailView ? f.name : f.fullPath).appendTo(content);
	var details = t('Size') + ': ' + FilemanUtils.FormatFileSize(f.size);
	if (image) details += ' ' + t('Dimensions') + ': ' + f.width + 'x' + f.height;
	$('<span class="filesize">').text(details).appendTo(content);
	return content.html();
}

function filterFiles() {
	var str = $('#txtSearch').val();
	$('#pnlSearchNoFiles').hide();
	if ($('#pnlFileList li').length === 0)
		return;
	if (!str) {
		$('#pnlFileList li').show();
		return;
	}
	var i = 0;
	$('#pnlFileList li').each(function () {
		var name = $(this).children('.name').text();
		if (name.toLowerCase().indexOf(str.toLowerCase()) > -1) {
			i++;
			$(this).show();
		} else {
			$(this).removeClass('selected');
			$(this).hide();
		}
	});
	if (i === 0)
		$('#pnlSearchNoFiles').show();
}

function sortFiles() {
	var d = getSelectedDir();
	if (!d)
		return;
	d.ListFiles();
	filterFiles();
	switchView($('#hdViewType').val());
}

function switchView(t) {
	if (t === $('#hdViewType').val())
		return;
	if (!t)
		t = $('#hdViewType').val();

	var pnlFileList = $('#pnlFileList');

	$('.btn-view').removeClass('active');

	if (t === 'thumb') {
		pnlFileList.addClass('thumbView');
		if ($('#dynStyle').length === 0) {
			$('head').append('<style id="dynStyle" />');
			var rules = 'ul#pnlFileList.thumbView li{width:' + (parseInt(FilemanConfig.THUMBS_VIEW_WIDTH) + 22) + 'px;}';
			rules += 'ul#pnlFileList.thumbView li{height:' + (parseInt(FilemanConfig.THUMBS_VIEW_HEIGHT) + 42) + 'px;}';
			rules += 'ul#pnlFileList.thumbView .file-preview{width:' + FilemanConfig.THUMBS_VIEW_WIDTH + 'px;}';
			rules += 'ul#pnlFileList.thumbView .file-preview{height:' + FilemanConfig.THUMBS_VIEW_HEIGHT + 'px;}';
			$('#dynStyle').html(rules);
		}
		$('li', pnlFileList).each(function () {
			var isImage = FilemanUtils.IsImage($(this).attr('data-path'));
			var imgUrl = $(this).attr('data-path');
			if (FilemanConfig.GENERATETHUMB && isImage) {
				// Let the SMNET MediaController do the image resizing per ImageProcessor
				imgUrl = FilemanUtils.AddParam(imgUrl, 'w', FilemanConfig.THUMBS_VIEW_WIDTH);
				imgUrl = FilemanUtils.AddParam(imgUrl, 'h', FilemanConfig.THUMBS_VIEW_HEIGHT);
			}
			$(this).find('> .file-preview > img').attr('src', imgUrl);
		});
		$('#btnThumbView').addClass('active');
	}
	else {
		pnlFileList.removeClass('thumbView');
		$('#btnListView').addClass('active');
	}
	$('#hdViewType').val(t);
	FilemanUI.bindPreviews();
	FilemanUtils.SetCookie('roxyview', t, 10);
}
var clipBoard = null;

function Clipboard(a, obj) {
	this.action = a;
	this.obj = obj;
}

function cutDir() {
	var d = getSelectedDir();
	if (d) {
		setClipboard('cut', d);
		d.GetElement().addClass('disabled');
	}
}

function copyDir() {
	var d = getSelectedDir();
	if (d) {
		setClipboard('copy', d);
	}
}

function cutFile() {
	var f = getSelectedFile();
	if (f) {
		setClipboard('cut', f);
		f.GetElement().addClass('disabled');
	}
}

function copyFile() {
	var f = getSelectedFile();
	if (f) {
		setClipboard('copy', f);
	}
}

function pasteToFiles(e, el) {
	if ($(el).hasClass('disabled')) {
		e.stopPropagation();
		return false;
	}
	var d = getSelectedDir();
	if (!d)
		d = Directory.Parse($('#pnlDirList li:first').children('div').first());
	if (d && clipBoard && clipBoard.obj) {
		if (clipBoard.action === 'copy')
			clipBoard.obj.Copy(d.fullPath);
		else {
			clipBoard.obj.Move(d.fullPath, () => {
				clearClipboard();
			});
		}
	}
	return true;
}

function pasteToDirs(e, el) {
	if ($(el).hasClass('disabled')) {
		e.stopPropagation();
		return false;
	}
	var d = getSelectedDir();
	if (!d)
		d = Directory.Parse($('#pnlDirList li:first').children('div').first());
	if (clipBoard && d) {
		if (clipBoard.action === 'copy') {
			clipBoard.obj.Copy(d.fullPath);
		}	
		else {
			clipBoard.obj.Move(d.fullPath, () => {
				clearClipboard();
				if (!$("#pnlDirList").data("indeterm")) {
					d.ListFiles(true);
				}
			});
		}
	} else
		alert('error');
	return true;
}

function clearClipboard() {
	$('#pnlDirList li').removeClass('disabled');
	$('#pnlFileList li').removeClass('disabled');
	clipBoard = null;
	$('.paste').addClass('disabled');
}

function setClipboard(a, obj) {
	clearClipboard();
	if (obj) {
		clipBoard = new Clipboard(a, obj);
		$('.paste').removeClass('disabled');
	}
}

function ResizeLists() {
	var tmp = $(window).innerHeight() - $('#fileActions .actions').outerHeight() - $('.bottomLine').outerHeight();
	$('.scrollPane').css('height', tmp);
}

function removeDisabledActions() {
	if (FilemanConfig.CREATEDIR === '') {
		$('#mnuCreateDir').next().remove();
		$('#mnuCreateDir').remove();
		$('#btnAddDir').remove();
	}
	if (FilemanConfig.DELETEDIR === '') {
		$('#mnuDeleteDir').prev().remove();
		$('#mnuDeleteDir').remove();
		$('#btnDeleteDir').remove();
	}
	if (FilemanConfig.MOVEDIR === '') {
		$('#mnuDirCut').next().remove();
		$('#mnuDirCut').remove();
	}
	if (FilemanConfig.COPYDIR === '') {
		$('#mnuDirCopy').next().remove();
		$('#mnuDirCopy').remove();
	}
	if (FilemanConfig.COPYDIR === '' && FilemanConfig.MOVEDIR === '') {
		$('#mnuDirPaste').next().remove();
		$('#mnuDirPaste').remove();
	}
	if (FilemanConfig.RENAMEDIR === '') {
		$('#mnuRenameDir').next().remove();
		$('#mnuRenameDir').remove();
		$('#btnRenameDir').remove();
	}
	if (FilemanConfig.UPLOAD === '') {
		$('#btnAddFile').remove();
	}
	if (FilemanConfig.DOWNLOAD === '') {
		$('#mnuDownload').next().remove();
		$('#mnuDownload').remove();
	}
	if (FilemanConfig.DOWNLOADDIR === '') {
		$('#mnuDownloadDir').next().remove();
		$('#mnuDownloadDir').remove();
	}
	if (FilemanConfig.DELETEFILE === '') {
		$('#mnuDeleteFile').prev().remove();
		$('#mnuDeleteFile').remove();
		$('#btnDeleteFile').remove();
	}
	if (FilemanConfig.MOVEFILE === '') {
		$('#mnuFileCut').next().remove();
		$('#mnuFileCut').remove();
	}
	if (FilemanConfig.COPYFILE === '') {
		$('#mnuFileCopy').next().remove();
		$('#mnuFileCopy').remove();
	}
	if (FilemanConfig.COPYFILE === '' && FilemanConfig.MOVEFILE === '') {
		$('#mnuFilePaste').next().remove();
		$('#mnuFilePaste').remove();
	}
	if (FilemanConfig.RENAMEFILE === '') {
		$('#mnuRenameFile').next().remove();
		$('#mnuRenameFile').remove();
		$('#btnRenameFile').remove();
	}
}

function getPreselectedFile() {
	var filePath = FilemanUtils.GetUrlParam('selected');
	if (!filePath) {
		var win;
		switch (getFilemanIntegration()) {
			case 'ckeditor':
				try {
					var dialog = window.opener.CKEDITOR.dialog.getCurrent();
					filePath = dialog.getValueOf('info', (dialog.getName() === 'link' ? 'url' : 'txtUrl'));
				} catch (ex) { /**/ }
				break;
			case 'tinymce3':
				try {
					win = tinyMCEPopup.getWindowArg("window");
					filePath = win.document.getElementById(tinyMCEPopup.getWindowArg("input")).value;
					if (filePath.indexOf('..') === 0)
						filePath = filePath.substr(2);
				} catch (ex) { /**/ }
				break;
			case 'tinymce4':
				try {
					win = (window.opener ? window.opener : window.parent);
					filePath = win.document.getElementById(FilemanUtils.GetUrlParam('input')).value;
					if (filePath.indexOf('..') === 0)
						filePath = filePath.substr(2);
				} catch (ex) { /**/ }
				break;
			default:
				filePath = GetSelectedValue();
				break;
		}
	}
	if (FilemanConfig.RETURN_URL_PREFIX) {
		var prefix = FilemanConfig.RETURN_URL_PREFIX;
		if (filePath.indexOf(prefix) === 0) {
			if (prefix.substr(-1) === '/')
				prefix = prefix.substr(0, prefix.length - 1);
			filePath = filePath.substr(prefix.length);
		}
	}

	return filePath;
}

function initSelection(filePath) {
	var hasSelection = false,
		fileSelected = true;
	if (!filePath)
		filePath = getPreselectedFile();
	if (!filePath && FilemanUtils.ToBool(FilemanConfig.OPEN_LAST_DIR)) {
		filePath = getLastDir();
		fileSelected = false;
	}
	if (filePath) {
		var p = (fileSelected ? FilemanUtils.GetPath(filePath) : filePath);
		var d = tmp = Directory.Parse(p);
		do {
			if (tmp) {
				tmp.Expand(true);
				hasSelection = true;
			}
			tmp = Directory.Parse(tmp.path);
		} while (tmp);

		if (d) {
			d.Select(filePath);
			hasSelection = true;
		}
	}
	if (!hasSelection)
		selectFirst();
}

$(function () {
	FilemanUtils.LoadConfig(() => {
		var d = new Directory();
		d.LoadAll();
		$('#wraper').show();

		window.setTimeout('initSelection()', 100);

		FilemanUtils.Translate();
		FilemanUI.initDragAndDrop();
		$('body').on('click', function () {
			closeMenus();
		});

		var viewType = FilemanUtils.GetCookie('roxyview');
		if (!viewType)
			viewType = FilemanConfig.DEFAULTVIEW;
		if (viewType)
			switchView(viewType);

		ResizeLists();
		$(window).resize(ResizeLists);

		document.oncontextmenu = function () {
			//return false;
		};
		removeDisabledActions();
		$('#copyYear').html(new Date().getFullYear());
		if (FilemanConfig.UPLOAD && FilemanConfig.UPLOAD !== '') {
			var dropZone = document.getElementById('fileActions');
			dropZone.ondragover = function () {
				return false;
			};
			dropZone.ondragend = function () {
				return false;
			};
			dropZone.ondrop = function (e) {
				e.preventDefault();
				e.stopPropagation();
				dropFiles(e);
			};

			dropZone = document.getElementById('dlgAddFile');
			dropZone.ondragover = function () {
				return false;
			};
			dropZone.ondragend = function () {
				return false;
			};
			dropZone.ondrop = function (e) {
				e.preventDefault();
				e.stopPropagation();
				dropFiles(e, true);
			};
		}

		if (getFilemanIntegration() === 'tinymce3') {
			try {
				$('body').append('<script src="js/tiny_mce_popup.js"><\/script>');
			} catch (ex) { /**/ }
		}
	});
});

function getFilemanIntegration() {
	var integration = FilemanUtils.GetUrlParam('integration');
	if (!integration)
		integration = FilemanConfig.INTEGRATION;

	return integration.toLowerCase();
}

function setFile() {
	var f = getSelectedFile();
	if (!f) {
		alert(t('E_NoFileSelected'));
		return;
	}
	var insertPath = f.fullPath;
	if (FilemanConfig.RETURN_URL_PREFIX) {
		var prefix = FilemanConfig.RETURN_URL_PREFIX;
		if (prefix.substr(-1) === '/')
			prefix = prefix.substr(0, prefix.length - 1);
		insertPath = prefix + (insertPath.substr(0, 1) !== '/' ? '/' : '') + insertPath;
	}

	var win;
	switch (getFilemanIntegration()) {
		case 'ckeditor':
			window.opener.CKEDITOR.tools.callFunction(FilemanUtils.GetUrlParam('CKEditorFuncNum'), insertPath);
			self.close();
			break;
		case 'tinymce3':
			win = tinyMCEPopup.getWindowArg("window");
			win.document.getElementById(tinyMCEPopup.getWindowArg("input")).value = insertPath;
			if (typeof (win.ImageDialog) !== "undefined") {
				if (win.ImageDialog.getImageData)
					win.ImageDialog.getImageData();

				if (win.ImageDialog.showPreviewImage)
					win.ImageDialog.showPreviewImage(insertPath);
			}
			tinyMCEPopup.close();
			break;
		case 'tinymce4':
			win = (window.opener ? window.opener : window.parent);
			win.document.getElementById(FilemanUtils.GetUrlParam('input')).value = insertPath;
			if (typeof (win.ImageDialog) !== "undefined") {
				if (win.ImageDialog.getImageData)
					win.ImageDialog.getImageData();
				if (win.ImageDialog.showPreviewImage)
					win.ImageDialog.showPreviewImage(insertPath);
			}
			win.tinyMCE.activeEditor.windowManager.close();
			break;
		default:
			FileSelected(f);
			break;
	}
}