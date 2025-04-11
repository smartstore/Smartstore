(function ($, window, document, undefined) {
    $(function () {
        // Images
        $(document).on('click', '.ai-provider-tool .ai-image-composer', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");
            if (tool.length === 0) {
                return;
            }

            // Set the title used for the modal dialog title. 
            // For direct openers the title is set else we take the text of the dropdown item.
            let title = this.getAttribute("title") || el.html();
            
            let params = {
                targetProperty: tool.data('target-property'),
                entityName: tool.data('entity-name'),
                type: tool.data('entity-type'),
                modalTitle: title,
                format: tool.data('format'),
                mediaFolder: tool.data('media-folder')
            };

            openDialog(tool, params, true);
        });

        // Text creation
        $(document).on('click', '.ai-text-composer', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");
            if (tool.length === 0) {
                return;
            }

            const cmd = el.data('command');
            const isSummernoteInlineEditing = el.closest(".html-editor-root").length !== 0;
            
            let isRichText = tool.data('is-rich-text') || (cmd === "create-new" && isSummernoteInlineEditing);

            let params = {
                entityName: tool.data('entity-name'),
                Type: tool.data('entity-type'),
                targetProperty: tool.data('target-property'),
                charLimit: tool.data('char-limit'),
                // INFO: This is the optimization command of the clicked item.
                optimizationCommand: el.data('command'),
                // INFO: This is important for change style and tone items. We must know how to change the present text. 
                // For command "change-style" e.g.professional, casual, friendly, etc.
                changeParameter: cmd === 'change-style' || cmd === 'change-tone' ? el.text() : '',
                displayWordLimit: tool.data('display-word-limit'),
                displayStyle: tool.data('display-style'),
                displayTone: tool.data('display-tone'),
                selectedElementType: tool.data('range-is-on')
            };

            if (tool.closest(".note-dropdown-menu").length) {
                params.origin = "summernote";
            }

            if (!isRichText) {
                Object.assign(params, {
                    // TODO: (mh) (ai) Is this still needed? Originally it was used to supress optimization options in the dialog (e.g. For SEO-Meta-Properties).
                    // Seems like it isn't used anymore.
                    displayOptimizationOptions: tool.data('display-optimization-options')
                });
            }
            else {
                Object.assign(params, {
                    entityId: tool.data('entity-id'),
                    displayTocOptions: tool.data('display-toc-options'),
                    displayLinkOptions: tool.data('display-link-options'),
                    displayImageOptions: tool.data('display-image-options'),
                    displayLayoutOptions: tool.data('display-layout-options')
                });

                var richTextUrl = tool.data("rich-text-modal-url");
                if (richTextUrl) {
                    tool.data('modal-url', richTextUrl);
                }
            }

            openDialog(tool, params, isRichText);
        });

        // Translation
        $(document).on('click', '.ai-provider-tool .ai-translator', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");
            if (tool.length === 0) {
                return;
            }

            let params = {
                targetProperty: tool.data('target-property'),
                targetPropertyName: tool.data('target-property-name'),
                LocalizedEditorName: tool.data('localized-editor-name'),
                ModalTitle: tool.data('modal-title'),
                EntityId: tool.data('entity-id'),
                EntityName: tool.data('entity-type')
            };

            openDialog(tool, params, false);
        });

        // Suggestion
        $(document).on('click', '.ai-provider-tool .ai-suggestion', function (e) {
            e.preventDefault();

            let el = $(this);
            let tool = el.closest(".ai-provider-tool");
            if (tool.length === 0) {
                return;
            }

            let params = {
                targetProperty: tool.data('target-property'),
                type: tool.data('entity-type'),
                mandatoryEntityFields: tool.data('mandatory-entity-fields'),
                charLimit: tool.data('char-limit')
            };

            openDialog(tool, params, false);
        });

        const checkScrollbar = (element) => {
            element.closest('.ai-dialog-opener-root')?.style?.setProperty('--scrollbar-width', (element.offsetWidth - element.clientWidth) + 'px');
        };

        const resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                checkScrollbar(entry.target);
            }
        });

        // Set a class to apply margin if the dialog opener contains a textarea with scrollbar.
        $('.ai-dialog-opener-root').each(function () {
            const root = $(this);
            const localeEditor = root.parent();

            if (localeEditor.hasClass('locale-editor')) {
                // Removing translator menu items that have no according input element in the localized editor.
                root.find('.ai-translator-menu .ai-provider-tool').each(function () {
                    const tool = $(this);
                    const propName = tool.data('target-property');
                    if (!_.isEmpty(propName) && localeEditor.find('#' + propName).length == 0) {
                        tool[0].remove();
                    }
                });
                return;
            }

            let textarea = root.find('> textarea');
            if (textarea.length) {
                resizeObserver.observe(textarea[0]);
            }

            let summernote = root.find('.note-editor-preview');
            if (summernote.length) {
                resizeObserver.observe(summernote[0]);
            }
        });
    });

    function openDialog(opener, params, large) {
        openPopup({
            url: getDialogUrl(opener.data('modal-url'), params),
            large: large,
            flex: true,
            backdrop: 'static',
            scrollable: false
        });
    }

    function getDialogUrl(baseUrl, params) {
        let queryString = _.map(params, (value, key) => {
            return encodeURIComponent(key) + "=" + encodeURIComponent(value);
        }).join("&");

        return baseUrl + (baseUrl.includes('?') ? '&' : '?') + queryString;
    }
})(jQuery, this, document);