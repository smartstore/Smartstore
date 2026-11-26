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

    let preview;
    let previewFrame = 0;
    let pointerX = 0;
    let pointerY = 0;

    function hidePreviews() {
        cancelAnimationFrame(previewFrame);
        previewFrame = 0;
        if (preview) preview.hide();
    }

    function disposePreviews() {
        hidePreviews();
        $('#pnlFileList').off('.filemanPreview');
    }

    function positionPreview() {
        previewFrame = 0;
        const bounds = preview[0].getBoundingClientRect();
        const width = document.documentElement.clientWidth;
        const height = document.documentElement.clientHeight;
        const gap = 16;
        const edge = 8;
        let x = pointerX + gap;
        let y = pointerY + gap;

        if (x + bounds.width > width - edge) x = pointerX - bounds.width - gap;
        if (y + bounds.height > height - edge) y = pointerY - bounds.height - gap;
        preview.css({
            left: Math.max(edge, Math.min(x, width - bounds.width - edge)),
            top: Math.max(edge, Math.min(y, height - bounds.height - edge))
        });
    }

    function movePreview(event) {
        pointerX = event.clientX;
        pointerY = event.clientY;
        if (!previewFrame) previewFrame = requestAnimationFrame(positionPreview);
    }

    function bindPreviews() {
        disposePreviews();
        if (!preview) {
            preview = $('<div class="popover fileman-preview shadow-sm" role="tooltip"><div class="popover-body"></div></div>')
                .hide().appendTo(document.body);
            document.addEventListener('scroll', hidePreviews, true);
            $(window).on('resize.filemanPreview blur.filemanPreview', hidePreviews);
        }

        $('#pnlFileList')
            .on('mouseenter.filemanPreview', '.file-item', function (event) {
                if (draggedItem || $('.modal.show').length) return;
                const content = tooltipContent.call(this);
                if (!content) return;

                preview.find('.popover-body').html(content);
                preview.find('img').on('load', function () {
                    if (preview.is(':visible')) positionPreview();
                });
                pointerX = event.clientX;
                pointerY = event.clientY;
                preview.show();
                positionPreview();
            })
            .on('mousemove.filemanPreview', '.file-item', function (event) {
                if (preview.is(':visible')) movePreview(event);
            })
            .on('mouseleave.filemanPreview mousedown.filemanPreview', '.file-item', hidePreviews);
    }

    function showContextMenu(selector, event) {
        hidePreviews();
        const menu = $(selector);
        const edge = 8;
        const width = document.documentElement.clientWidth;
        const height = document.documentElement.clientHeight;
        menu.css({
            position: 'fixed',
            left: 0,
            top: 0,
            margin: 0,
            minWidth: 0,
            maxWidth: Math.max(0, width - edge * 2),
            maxHeight: Math.max(0, height - edge * 2),
            overflow: 'auto'
        }).show();

        const bounds = menu[0].getBoundingClientRect();
        menu.css({
            left: Math.max(edge, Math.min(event.clientX, width - bounds.width - edge)),
            top: Math.max(edge, Math.min(event.clientY, height - bounds.height - edge))
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

    return { showDialog, showContextMenu, hidePreviews, disposePreviews, bindPreviews, initDragAndDrop, canDrop };
})(jQuery);
