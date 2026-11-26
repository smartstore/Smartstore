/*
  Fileman - web based file manager. Ready to use with CKEditor, TinyMCE.
  Can be easily integrated with any other WYSIWYG editor or CMS.

  Copyright (C) 2013, Lyubomir Arsov. All rights reserved.
*/

$(function () {
	$(document).on('dblclick', '.file-item', function (e) {
		e.stopPropagation();
		e.preventDefault();
		selectFile(this);
		setFile();
	});

	$('#pnlFileList').on('contextmenu', '.file-item', function (e) {
		e.stopPropagation();
		e.preventDefault();
		closeMenus('dir');
		selectFile(this);
		FilemanUI.hidePreviews();
		FilemanUI.showContextMenu('#menuFile', e);

		return false;
	});

	$(document).on('click', '.file-item', function (e) {
		e.stopPropagation();
		e.preventDefault();

		selectFile(this);
	});


});

function File(filePath, fileSize, modTime, w, h, mime) {
	this.fullPath = filePath;
	this.mime = mime;
	this.type = FilemanUtils.GetFileType(filePath, mime);
	this.icon = FilemanIconHints[this.type];
	this.name = FilemanUtils.GetFilename(filePath);
	this.displayName = FilemanUtils.GetDisplayName(filePath);
	this.ext = FilemanUtils.GetFileExt(filePath);
	this.path = FilemanUtils.GetPath(filePath);
	this.image = filePath;
	this.size = fileSize || 0;
	this.time = modTime;
	this.width = w || 0;
	this.height = h || 0;
	this.thumb = this.type === 'image' ? filePath : null;
	this.GenerateHtml = function () {
		var attrs = [
			'data-mime="' + this.mime + '"',
			'data-path="' + this.fullPath + '"',
			'data-time="' + this.time + '"',
			'data-w="' + this.width + '"',
			'data-h="' + this.height + '"',
			'data-size="' + this.size + '"'
		];
		var html = [
			'<li class="file-item' + (this.IsImage() ? ' file-image loaded' : '') + '" ' + attrs.join(' ') + '>',
			'<div class="file-preview">' + (this.thumb ? '<img class="thumb" src="' + this.thumb + '">' : '') + '<sm-icon area="admin" class="file-icon icon-fw" name="' + this.icon.name + '" style="color:' + this.icon.color + '"></sm-icon></div>',
			'<span class="time">' + FilemanUtils.FormatDate(new Date(this.time * 1000)) + '</span>',
			'<span class="name"></span>',
			'<span class="size">' + FilemanUtils.FormatFileSize(this.size) + '</span>',
			'</li>'
		].join("");

		return $(html).find('.name').text(this.displayName).end().prop('outerHTML');
	};
	this.GetElement = function () {
		return $('li[data-path="' + this.fullPath + '"]');
	};
	this.IsImage = function () {
		return this.type === 'image';
	};
	this.Delete = function (fn) {
		if (!FilemanConfig.DELETEFILE) {
			alert(t('E_ActionDisabled'));
			return;
		}
		var deleteUrl = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.DELETEFILE), 'f', this.fullPath);
		var item = this;
		$.ajax({
			url: deleteUrl,
			type: 'POST',
			data: {
				f: this.fullPath
			},
			dataType: 'json',
			//async: false,
			success: function (data) {
				if (data.res.toLowerCase() === 'ok') {
					$('li[data-path="' + item.fullPath + '"]').remove();
					var d = Directory.Parse(item.path);
					if (d) {
						d.files--;
						d.Update();
						d.SetStatusBar();
					}
					fn?.apply(this);
				} else {
					alert(data.msg);
				}
			},
			error: function (data) {
				alert(t('E_LoadingAjax') + ' ' + deleteUrl);
			}
		});
	};
	this.Rename = function (newName, fn) {
		if (!FilemanConfig.RENAMEFILE) {
			alert(t('E_ActionDisabled'));
			return false;
		}
		if (!newName)
			return false;
		var url = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.RENAMEFILE), 'f', this.fullPath);
		url = FilemanUtils.AddParam(url, 'n', newName);
		var item = this;
		var ret = false;
		$.ajax({
			url: url,
			type: 'POST',
			data: {
				f: this.fullPath,
				n: newName
			},
			dataType: 'json',
			success: function (data) {
				if (data.res.toLowerCase() === 'ok') {
					var newPath = FilemanUtils.MakePath(this.path, newName);
					var fileType = FilemanUtils.GetFileType(newName);
					var icon = FilemanIconHints[fileType];
					var li = $('li[data-path="' + item.fullPath + '"]');

					li.toggleClass('file-image', fileType === 'image');
					$('.file-icon', li).attr('name', icon.name).css('color', icon.color);
					$('.name', li).text(newName);
					$('li[data-path="' + newPath + '"]').attr('data-path', newPath);
					ret = true;

					fn();
				}
				if (data.msg)
					alert(data.msg);
			},
			error: function (data) {
				alert(t('E_LoadingAjax') + ' ' + url);
			}
		});
		return ret;
	};
	this.Copy = function (newPath, fn) {
		if (!FilemanConfig.COPYFILE) {
			alert(t('E_ActionDisabled'));
			return;
		}
		var url = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.COPYFILE), 'f', this.fullPath);
		url = FilemanUtils.AddParam(url, 'n', newPath);
		var item = this;
		var ret = false;
		$.ajax({
			url: url,
			type: 'POST',
			data: {
				f: this.fullPath,
				n: newPath
			},
			dataType: 'json',
			success: function (data) {
				if (data.res.toLowerCase() === 'ok') {
					var d = Directory.Parse(newPath);
					if (d) {
						d.files++;
						d.Update();
						d.SetStatusBar();
						if (!$("#pnlDirList").data("indeterm")) {
							d.ListFiles(true);
						}
					}
					fn?.apply(this);
					ret = true;
				}
				if (data.msg)
					alert(data.msg);
			},
			error: function (data) {
				alert(t('E_LoadingAjax') + ' ' + url);
			}
		});
		return ret;
	};
	this.Move = function (newPath, fn) {
		if (!FilemanConfig.MOVEFILE) {
			alert(t('E_ActionDisabled'));
			return;
		}
		newFullPath = FilemanUtils.MakePath(newPath, this.name);
		var url = FilemanUtils.AddParam(FilemanUtils.GetRootPath(FilemanConfig.MOVEFILE), 'f', this.fullPath);
		url = FilemanUtils.AddParam(url, 'n', newFullPath);
		var item = this;
		var ret = false;
		$.ajax({
			url: url,
			type: 'POST',
			data: {
				f: this.fullPath,
				n: newFullPath
			},
			dataType: 'json',
			success: function (data) {
				if (data.res.toLowerCase() === 'ok') {
					$('li[data-path="' + item.fullPath + '"]').remove();
					var d = Directory.Parse(item.path);
					if (d) {
						d.files--;
						d.Update();
						d.SetStatusBar();
						d = Directory.Parse(newPath);
						d.files++;
						d.Update();
					}
					fn?.apply(this);
					ret = true;
				}
				if (data.msg)
					alert(data.msg);
			},
			error: function (data) {
				alert(t('E_LoadingAjax') + ' ' + url);
			}
		});
		return ret;
	};
}

File.Parse = function (path) {
	var ret = false;
	var li = $('#pnlFileList').find('li[data-path="' + path + '"]');
	if (li.length > 0)
		ret = new File(li.data('path'), li.data('size'), li.data('time'), li.data('w'), li.data('h'));

	return ret;
};