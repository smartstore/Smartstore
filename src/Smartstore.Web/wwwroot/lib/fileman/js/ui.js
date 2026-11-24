/* Bootstrap and native browser interactions for the embedded file manager. */
const FilemanUI = (function ($) {
    let draggedItem = null;

    function showDialog(selector, options) {
        const modal = $(selector);
        const opener = document.activeElement;
        const footer = modal.find('.modal-footer').empty();
        modal.find('.modal-title').text(options.title);

        Object.entries(options.buttons).forEach(([label, definition], index) => {
            const action = typeof definition === 'function' ? { click: definition } : definition;
            $('<button type="button">')
                .addClass('btn ' + (index === 0 ? 'btn-primary' : 'btn-secondary'))
                .attr('id', action.id || null)
                .text(action.text || label)
                .prop('disabled', !!action.disabled)
                .on('click', function () { action.click.call(modal[0]); })
                .appendTo(footer);
        });

        modal.off('.filemanDialog')
            .on('keydown.filemanDialog', function (event) {
                if (event.key === 'Enter' && $(event.target).is('input:not([type="file"])')) {
                    event.preventDefault();
                    event.stopPropagation();
                    footer.find('button').first().filter(':enabled').trigger('click');
                }
            })
            .one('shown.bs.modal.filemanDialog', function () {
                modal.find('input:visible:enabled').first().trigger('focus');
                if (options.onShown) options.onShown();
            })
            .one('hidden.bs.modal.filemanDialog', function () {
                if (opener && opener.isConnected) opener.focus();
            });

        hidePreviews();
        closeMenus();
        modal.modal({ show: true, backdrop: true, keyboard: true });
    }

    function hidePreviews() {
        $('#pnlFileList .file-item').each(function () {
            if ($(this).data('bs.popover')) $(this).popover('hide');
        });
    }

    function disposePreviews() {
        $('#pnlFileList .file-item').each(function () {
            if ($(this).data('bs.popover')) $(this).popover('dispose');
        });
    }

    function bindPreviews() {
        disposePreviews();
        $('#pnlFileList .file-item').popover({
            container: 'body',
            boundary: 'viewport',
            placement: 'auto',
            trigger: 'hover',
            html: true,
            animation: false,
            delay: { show: $('#hdViewType').val() === 'thumb' ? 50 : 500, hide: 100 },
            template: '<div class="popover fileman-preview" role="tooltip"><div class="arrow"></div><div class="popover-body"></div></div>',
            content: function () {
                return draggedItem || $('.modal.show').length ? '' : tooltipContent.call(this);
            }
        });
    }

    function canDrop(source, target) {
        if (!source || !target) return false;
        const sourcePath = source.path.replace(/\/$/, '');
        const targetPath = target.replace(/\/$/, '');
        const parentPath = sourcePath.substring(0, sourcePath.lastIndexOf('/'));
        return targetPath !== sourcePath && targetPath !== parentPath
            && (source.type !== 'directory' || !targetPath.startsWith(sourcePath + '/'));
    }

    function clearDrag() {
        draggedItem = null;
        $('#pnlDirList .dragover, .fileman-dragging').removeClass('dragover fileman-dragging');
    }

    function initDragAndDrop() {
        $(document).on('dragstart.fileman', '#pnlFileList .file-item, #pnlDirList .dir-item', function (event) {
            const item = $(this);
            const directory = item.hasClass('dir-item');
            const row = directory ? item.parent('li') : item;
            if ((directory && (!FilemanConfig.MOVEDIR || !row.parents('li').length))
                || (!directory && !FilemanConfig.MOVEFILE)) {
                event.preventDefault();
                return;
            }

            event.stopPropagation();
            hidePreviews();
            closeMenus();
            if (!directory) selectFile(this);
            draggedItem = { type: directory ? 'directory' : 'file', path: row.attr('data-path') };
            item.addClass('fileman-dragging');
            const transfer = event.originalEvent.dataTransfer;
            transfer.effectAllowed = 'move';
            transfer.setData('text/plain', draggedItem.path);
            transfer.setDragImage(this, 10, 10);
        });

        $('#pnlDirList').on('dragover.fileman', '.dir-item', function (event) {
            if (!draggedItem) return;
            event.preventDefault();
            event.stopPropagation();
            const target = $(this).parent('li').attr('data-path');
            const allowed = canDrop(draggedItem, target);
            event.originalEvent.dataTransfer.dropEffect = allowed ? 'move' : 'none';
            $('#pnlDirList .dragover').removeClass('dragover');
            $(this).toggleClass('dragover', allowed);

            const pane = this.closest('.scrollPane');
            if (pane) {
                const bounds = pane.getBoundingClientRect();
                const y = event.originalEvent.clientY;
                if (y < bounds.top + 32) pane.scrollTop -= 20;
                else if (y > bounds.bottom - 32) pane.scrollTop += 20;
            }
        }).on('dragleave.fileman', '.dir-item', function (event) {
            if (!this.contains(event.relatedTarget)) $(this).removeClass('dragover');
        }).on('drop.fileman', '.dir-item', function (event) {
            if (!draggedItem) return;
            event.preventDefault();
            event.stopPropagation();
            const source = draggedItem;
            const target = $(this).parent('li').attr('data-path');
            clearDrag();
            if (canDrop(source, target)) {
                const item = source.type === 'directory' ? Directory.Parse(source.path) : new File(source.path);
                if (item) item.Move(target);
            }
        });

        $(document).on('dragend.fileman', clearDrag);
        $(window).on('blur.fileman', function () {
            hidePreviews();
        });
    }

    return { showDialog, hidePreviews, disposePreviews, bindPreviews, initDragAndDrop, canDrop };
})(jQuery);
