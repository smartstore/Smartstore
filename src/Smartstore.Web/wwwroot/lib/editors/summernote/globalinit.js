let summernote_global_config;
let summernote_image_upload_url;

(function () {
    const beautifyOpts = {
        indent_size: 2,
        indent_with_tabs: true,
        indent_char: " ",
        max_preserve_newlines: "2",
        preserve_newlines: true,
        keep_array_indentation: false,
        break_chained_methods: false,
        indent_scripts: "normal",
        brace_style: "collapse",
        space_before_conditional: true,
        unescape_strings: false,
        jslint_happy: false,
        end_with_newline: false,
        wrap_line_length: "140",
        indent_inner_html: true,
        comma_first: false,
        e4x: false,
        indent_empty_lines: false
    };

    summernote_global_config = {
        disableDragAndDrop: false,
        dialogsInBody: true,
        container: 'body',
        dialogsFade: true,
        height: 300,
        prettifyHtml: true,
        popatmouse: true,
        hideArrow: false,
        recordEveryKeystroke: false,
        followingToolbar: true,
        // TODO: Turn on spellCheck again
        spellCheck: false,
        colorButton: {
            foreColor: '#424242',
            backColor: '#CEE7F7',
        },
        callbacks: {
            onBlurCodeview(code, e) {
                // Summernote does not update WYSIWYG content on codable blur,
                // only when switched back to editor
                $(this).val(code);
            },
            onFileBrowse(e, mediaType, deferred) {
                Smartstore.media.openFileManager({
                    el: e.target,
                    type: mediaType,
                    backdrop: false,
                    onSelect: (files) => {
                        if (!files.length) {
                            deferred.reject();
                        }
                        else {
                            deferred.resolve(files[0].url);
                        }
                    }
                });
            },
            onImageUpload(files) {
                if (summernote_image_upload_url) {
                    sendFile(files[0], this);
                }
            },
            onSanitizeHtml(html, opts) {
                if (opts.prettify && window.html_beautify) {
                    return window.html_beautify(html, beautifyOpts);
                }
                return html;
            }
        },
        icons: {
            'ai': ai_icon_svg || 'fa fa-wand-magic-sparkles',
        },
        toolbar: [
            ['edit', ['undo', 'redo']],
            ['text', ['bold', 'italic', 'underline', 'color', 'moreFontStyles']],
            //['color', ['forecolor', 'backcolor']],
            //['font', ['fontname', 'x-color', 'fontsize']],
            ['para', ['ai', 'style', 'cssclass', 'ul', 'ol', 'paragraph', 'clear', 'cleaner']],
            ['insert', ['link', 'image', 'video', 'table', 'hr']],
            //['ai', ['ai']],
            ['view', ['codeview', 'fullscreen', 'help']]
        ],
        popover: {
            image: [
                ['custom', ['imageAttributes', 'link', 'unlink', 'imageShapes']],
                ['imagesize', ['resizeFull', 'resizeHalf', 'resizeQuarter', 'resizeNone']],
                ['float', ['floatLeft', 'floatRight', 'floatNone']],
                ['remove', ['removeMedia']]
            ],
            link: [
                ['link', ['linkDialogShow', 'unlink']]
            ],
            table: [
                ['add', ['addRowDown', 'addRowUp', 'addColLeft', 'addColRight']],
                ['delete', ['deleteRow', 'deleteCol', 'deleteTable']],
                ['custom', ['tableStyles']]
            ],
             air: [
               ['color', ['color']],
               ['font', ['bold', 'underline', 'clear']],
               ['para', ['ul', 'paragraph']],
               ['table', ['table']],
               ['insert', ['link', 'picture']]
             ]
        },
        styleTags: [
            'p',
            'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'pre',
            { title: 'Blockquote', tag: 'blockquote', className: 'blockquote', value: 'blockquote' }
        ],
        imageAttributes: {
            icon: '<i class="fa fa-pencil"/>',
            removeEmpty: true, // true = remove attributes | false = leave empty if present
            disableUpload: true // true = don't display Upload Options | Display Upload Options
        }
    };

    if (CodeMirror?.hint) {
        summernote_global_config.codemirror = {
            mode: "htmlmixed",
            theme: "eclipse",
            lineNumbers: true,
            lineWrapping: false,
            tabSize: 2,
            indentWithTabs: true,
            smartIndent: true,
            matchTags: true,
            matchBrackets: true,
            autoCloseTags: true,
            autoCloseBrackets: true,
            styleActiveLine: true,
            hintOptions: {
                closeCharacters: /[\s()\[\]{};:>,.|%]/,
                completeSingle: false
            },
            extraKeys: {
                "'.'": CodeMirror.hint.completeAfter,
                "'<'": CodeMirror.hint.completeAfter,
                "'/'": CodeMirror.hint.completeIfAfterLt,
                "' '": CodeMirror.hint.completeIfAfterSpace,
                "'='": CodeMirror.hint.completeIfInTag,
                "Ctrl-Space": "autocomplete",
                "F11": function (cm) { cm.setOption("fullScreen", !cm.getOption("fullScreen")); },
                "Esc": function (cm) { if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false); }
            }
        };
    }

    function sendFile(file, editor, welEditable) {
        data = new FormData();
        data.append("file", file);
        data.append("a", "UPLOAD");
        data.append("d", "file");
        data.append("ext", true);

        $.ajax({
            data: data,
            type: "POST",
            url: summernote_image_upload_url,
            cache: false,
            contentType: false,
            processData: false,
            success: function (result) {
                if (result.Success) {
                    $(editor).summernote('insertImage', result.Url);
                }
                else {
                    EventBroker.publish("message", {
                        title: 'Image upload error',
                        text: result.Message,
                        type: 'error',
                        hide: false
                    });
                }
            }
        });
    }
})();

// Initialize summernote
$(function () {
    // Extend base language
    $.extend(true, $.summernote.lang, {
        'en-US': {
            common: {
                ok: 'OK',
                cancel: 'Cancel'
            },
            font: {
                code: 'Code'
            },
            attrs: {
                cssClass: 'CSS Class',
                cssStyle: 'CSS Style',
                rel: 'Rel',
            },
            link: {
                browse: 'Browse'
            },
            image: {
                imageProps: 'Image Attributes'
            },
            imageShapes: {
                tooltip: 'Shape',
                tooltipShapeOptions: ['Responsive', 'Border', 'Rounded', 'Circle', 'Thumbnail', 'Shadow (small)', 'Shadow (medium)', 'Shadow (large)']
            },
            tableStyles: {
                tooltip: "Table style",
                stylesExclusive: ["Basic", "Bordered"],
                stylesInclusive: ["Striped", "Condensed", "Hoverable"]
            }
        }
    });

    // Custom events
    // Editor toggling
    $(document).on('click', '.note-editor-preview', function (e) {
        let div = $(this);
        let textarea = $(div.data("target"));
        let lang = div.data("lang");
        let root = div.parent();

        if (root.parent().is('.ai-provider-tool')) {
            // Remove button and dropdown menu from DOM
            root.nextAll().remove();
        }

        // Remove preview element
        div.remove();
        textarea
            .removeClass('d-none')
            .summernote($.extend(true, {}, summernote_global_config, { lang: lang, focus: true }));
    });

    // Fix "CodeMirror too wide" issue
    $(document).on('click', '.note-toolbar .btn-codeview', function (e) {
        var wrapper = $(this).closest('.adminData');
        if (wrapper.length) {
            wrapper.css('overflow-x', $(this).is('.active') ? 'auto' : '');
        }
    });
});