;

var summernote_global_config;
var summernote_image_upload_url;

(function () {
    var dom = $.summernote.dom;
    var originalDomHtml = dom.html;

    var beautifyOpts = {
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

    dom.html = function ($node, isNewlineOnBlock) {
        var markup = dom.value($node);
        if (isNewlineOnBlock) {
            markup = window.html_beautify(markup, beautifyOpts);
        }
        return markup;
    };

	$.extend(true, $.summernote.lang, {
		'en-US': {
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
			}
		}
	});

	summernote_global_config = {
		disableDragAndDrop: false,
		dialogsInBody: true,
		container: 'body',
		dialogsFade: true,
		height: 300,
        prettifyHtml: true,
		onCreateLink: function (url) {
			// Prevents that summernote prepends "http://" to our links (WTF!!!)
			var c = url[0];
			if (c === "/" || c === "~" || c === "\\" || c === "." || c === "#") {
				return url;
			}

			if (/^[A-Za-z][A-Za-z0-9+-.]*\:[\/\/]?/.test(url)) {
				// starts with a valid protocol
				return url;
			}

			// if url doesn't match an URL schema, set http:// as default
			return "http://" + url;
		},
        callbacks: {
            onFocus: function () {
                $(this).next().addClass('focus');
            },
            onBlur: function () {
                $(this).next().removeClass('focus');
            },
			onImageUpload: function (files) {
				sendFile(files[0], this);
			},
            onBlurCodeview: function (code, e) {
				// Summernote does not update WYSIWYG content on codable blur,
				// only when switched back to editor
                $(this).val(code);
            }
		},
		toolbar: [
			['text', ['bold', 'italic', 'underline', 'strikethrough', 'clear', 'cleaner']],
            //['font', ['forecolor', 'backcolor']],
            //['font', ['fontname', 'color', 'fontsize']],
			['para', ['style', 'cssclass', 'ul', 'ol', 'paragraph']],
			['insert', ['link', 'media',  'table', 'hr', 'video']],
			['view', ['fullscreen', 'codeview', 'help']]
		],
		popover: {
			image: [
				['custom', ['imageAttributes', 'link', 'unlinkImage', 'imageShapes']],
				['imagesize', ['imageSize100', 'imageSize50', 'imageSize25']],
				//['float', ['floatLeft', 'floatRight', 'floatNone']],
				['float', ['bsFloatLeft', 'bsFloatRight', 'bsFloatNone']],
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
            'blockquote'
        ],
		icons: {
			'align': 'fa fa-align-left',
			'alignCenter': 'fa fa-align-center',
			'alignJustify': 'fa fa-align-justify',
			'alignLeft': 'fa fa-align-left',
			'alignRight': 'fa fa-align-right',
            //'rowBelow': 'note-icon-row-below',
            'rowBelow': '<svg viewBox="0 0 14 14"><path d="m 1e-4,5.3181 c 0,0.2901 0.1688,0.5229 0.3789,0.5229 l 13.242,0 c 0.21,0 0.3789,-0.2328 0.3789,-0.5229 l 0,-4.7928 C 13.9999,0.2353 13.831,1e-4 13.621,1e-4 l -13.242,0 C 0.1689,1e-4 1e-4,0.2353 1e-4,0.5253 l 0,4.7928 z m 1.1582,-0.687 0,-3.4211 3.2168,0 0,3.4211 -3.2168,0 z m 4.2852,0 0,-3.4211 3.2148,0 0,3.4211 -3.2148,0 z m 4.2832,0 0,-3.4211 3.2148,0 0,3.4211 -3.2148,0 z m -6.7266,5.337 q 0,-0.264 0.1915,-0.4658 L 3.5798,9.1141 q 0.1966,-0.1965 0.4709,-0.1965 0.2795,0 0.4658,0.1965 l 1.5217,1.5164 0,-3.6436 q 0,-0.2691 0.194,-0.4372 0.1941,-0.1683 0.4684,-0.1683 l 0.6625,0 q 0.2743,0 0.4684,0.1683 0.1941,0.1681 0.1941,0.4372 l 0,3.6436 1.5216,-1.5164 q 0.1863,-0.1965 0.4658,-0.1965 0.2795,0 0.4658,0.1965 l 0.3882,0.3882 q 0.1966,0.1967 0.1966,0.4658 0,0.2742 -0.1966,0.471 l -3.3693,3.3692 q -0.1812,0.1916 -0.4658,0.1916 -0.2795,0 -0.471,-0.1916 L 3.1916,10.4391 Q 3.0001,10.2372 3.0001,9.9681 Z"/></svg>',
            //'colBefore': 'note-icon-col-before',
            'colBefore': '<svg viewBox="0 0 14 14"><path d="M 8.6819,1e-4 C 8.3918,1e-4 8.159,0.1689 8.159,0.379 l 0,13.242 c 0,0.21 0.2328,0.3789 0.5229,0.3789 l 4.7928,0 c 0.29,0 0.5252,-0.1689 0.5252,-0.3789 l 0,-13.242 c 0,-0.2101 -0.2352,-0.3789 -0.5252,-0.3789 l -4.7928,0 z m 0.687,1.1582 3.4211,0 0,3.2168 -3.4211,0 0,-3.2168 z m 0,4.2852 3.4211,0 0,3.2148 -3.4211,0 0,-3.2148 z m 0,4.2832 3.4211,0 0,3.2148 -3.4211,0 0,-3.2148 z M 4.0319,3.0001 q 0.264,0 0.4658,0.1915 l 0.3882,0.3882 q 0.1965,0.1966 0.1965,0.4709 0,0.2795 -0.1965,0.4658 l -1.5164,1.5217 3.6436,0 q 0.2691,0 0.4372,0.194 0.1683,0.1941 0.1683,0.4684 l 0,0.6625 q 0,0.2743 -0.1683,0.4684 -0.1681,0.1941 -0.4372,0.1941 l -3.6436,0 1.5164,1.5216 q 0.1965,0.1863 0.1965,0.4658 0,0.2795 -0.1965,0.4658 L 4.4977,10.867 q -0.1967,0.1966 -0.4658,0.1966 -0.2742,0 -0.471,-0.1966 L 0.1917,7.4977 Q 1e-4,7.3165 1e-4,7.0319 1e-4,6.7524 0.1917,6.5609 L 3.5609,3.1916 Q 3.7628,3.0001 4.0319,3.0001 Z"/></svg>',
            //'colAfter': 'note-icon-col-after',
            'colAfter': '<svg viewBox="0 0 14 14"><path d="m 5.3165569,14.0019 c 0.2900213,0 0.5227581,-0.1688 0.5227581,-0.3789 l 0,-13.2422 c 0,-0.21 -0.2327368,-0.3789 -0.5227581,-0.3789 l -4.7914999,0 C 0.235136,0.0019 0,0.1708 0,0.3808 L 0,13.623 c 0,0.2101 0.235136,0.3789 0.525057,0.3789 l 4.7914999,0 z m -0.6868135,-1.1582 -3.4201714,0 0,-3.2168 3.4201714,0 0,3.2168 z m 0,-4.2852 -3.4201714,0 0,-3.2148 3.4201714,0 0,3.2148 z m 0,-4.2832 -3.4201714,0 0,-3.2148 3.4201714,0 0,3.2148 z m 5.3395506,6.7583 q -0.2639283,0 -0.4656736,-0.1915 L 9.1155258,10.4539 Q 8.9190791,10.2573 8.9190791,9.983 q 0,-0.2795 0.1964467,-0.4658 l 1.5159882,-1.5217 -3.642611,0 q -0.269027,0 -0.4370814,-0.194 Q 6.3835673,7.6074 6.3835673,7.3331 l 0,-0.6625 q 0,-0.2743 0.1682543,-0.4684 Q 6.719876,6.0081 6.988903,6.0081 l 3.642611,0 L 9.1155258,4.4865 Q 8.9190791,4.3002 8.9190791,4.0207 q 0,-0.2795 0.1964467,-0.4658 L 9.5036204,3.1667 Q 9.7002671,2.9701 9.969294,2.9701 q 0.274126,0 0.470872,0.1966 L 13.808452,6.536 Q 14,6.7172 14,7.0018 q 0,0.2795 -0.191548,0.471 l -3.368286,3.3693 q -0.201845,0.1915 -0.470872,0.1915 z"/></svg>',
            //'rowAbove': 'note-icon-row-above',
            'rowAbove': '<svg viewBox="0 0 14 14" role="img"><path d="m 13.9999,8.6819 c 0,-0.2901 -0.1688,-0.5229 -0.3789,-0.5229 l -13.242,0 c -0.21,0 -0.3789,0.2328 -0.3789,0.5229 l 0,4.7928 c 0,0.29 0.1689,0.5252 0.3789,0.5252 l 13.242,0 c 0.2101,0 0.3789,-0.2352 0.3789,-0.5252 l 0,-4.7928 z m -1.1582,0.687 0,3.4211 -3.2168,0 0,-3.4211 3.2168,0 z m -4.2852,0 0,3.4211 -3.2148,0 0,-3.4211 3.2148,0 z m -4.2832,0 0,3.4211 -3.2148,0 0,-3.4211 3.2148,0 z m 6.7266,-5.337 q 0,0.264 -0.1915,0.4658 l -0.3882,0.3882 q -0.1966,0.1965 -0.4709,0.1965 -0.2795,0 -0.4658,-0.1965 l -1.5217,-1.5164 0,3.6436 q 0,0.2691 -0.194,0.4372 -0.1941,0.1683 -0.4684,0.1683 l -0.6625,0 Q 6.3626,7.6186 6.1685,7.4503 5.9744,7.2822 5.9744,7.0131 l 0,-3.6436 -1.5216,1.5164 Q 4.2665,5.0824 3.987,5.0824 3.7075,5.0824 3.5212,4.8859 L 3.133,4.4977 Q 2.9364,4.301 2.9364,4.0319 q 0,-0.2742 0.1966,-0.471 L 6.5023,0.1917 Q 6.6835,1e-4 6.9681,1e-4 q 0.2795,0 0.471,0.1916 l 3.3693,3.3692 q 0.1915,0.2019 0.1915,0.471 z"/></svg>',
            //'rowRemove': 'note-icon-row-remove',
            'rowRemove': '<svg viewBox="0 0 14 14"><path d="m 13.9999,8.68029 c 0,-0.29019 -0.1688,-0.52307 -0.3789,-0.52307 l -13.242,0 c -0.21,0 -0.3789,0.23288 -0.3789,0.52307 l 0,4.79434 C 1e-4,13.76472 0.169,14 0.379,14 l 13.242,0 c 0.2101,0 0.3789,-0.23528 0.3789,-0.52537 l 0,-4.79434 z m -1.1582,0.68722 0,3.4222 -3.2168,0 0,-3.4222 3.2168,0 z m -4.2852,0 0,3.4222 -3.2148,0 0,-3.4222 3.2148,0 z m -4.2832,0 0,3.4222 -3.2148,0 0,-3.4222 3.2148,0 z M 4.6999,7.59994 q -0.2558,0 -0.4348,-0.17911 L 3.3953,6.5508 Q 3.2162,6.37168 3.2162,6.11579 q 0,-0.2559 0.1791,-0.43501 L 5.2755,3.79997 3.3953,1.91917 Q 3.2162,1.74005 3.2162,1.48416 q 0,-0.2559 0.1791,-0.43501 L 4.2651,0.17912 Q 4.4441,0 4.6999,0 4.9557,0 5.1348,0.17912 L 7.015,2.05992 8.8952,0.17912 Q 9.0742,0 9.33,0 9.5858,0 9.7649,0.17912 l 0.8698,0.87003 q 0.179,0.17911 0.179,0.43501 0,0.25589 -0.179,0.43501 l -1.8802,1.8808 1.8802,1.88081 q 0.179,0.17911 0.179,0.43501 0,0.25589 -0.179,0.43501 L 9.7649,7.42083 Q 9.5858,7.59994 9.33,7.59994 q -0.2558,0 -0.4348,-0.17911 L 7.015,5.54002 5.1348,7.42083 Q 4.9557,7.59994 4.6999,7.59994 Z"/></svg>',
            //'colRemove': 'note-icon-col-remove',
            'colRemove': '<svg viewBox="0 0 14 14"><path d="m 5.31971,13.9999 c 0.29019,0 0.52307,-0.1688 0.52307,-0.3789 l 0,-13.242 C 5.84278,0.169 5.6099,1e-4 5.31971,1e-4 l -4.79434,0 C 0.23528,1e-4 0,0.169 0,0.379 l 0,13.242 c 0,0.2101 0.23528,0.3789 0.52537,0.3789 l 4.79434,0 z m -0.68722,-1.1582 -3.4222,0 0,-3.2168 3.4222,0 0,3.2168 z m 0,-4.2852 -3.4222,0 0,-3.2148 3.4222,0 0,3.2148 z m 0,-4.2832 -3.4222,0 0,-3.2148 3.4222,0 0,3.2148 z m 1.76757,0.4266 q 0,-0.2558 0.17911,-0.4348 L 7.4492,3.3953 q 0.17912,-0.1791 0.43501,-0.1791 0.2559,0 0.43501,0.1791 l 1.88081,1.8802 1.8808,-1.8802 q 0.17912,-0.1791 0.43501,-0.1791 0.2559,0 0.43501,0.1791 l 0.87003,0.8698 Q 14,4.4441 14,4.6999 14,4.9557 13.82088,5.1348 l -1.8808,1.8802 1.8808,1.8802 Q 14,9.0742 14,9.33 14,9.5858 13.82088,9.7649 l -0.87003,0.8698 q -0.17911,0.179 -0.43501,0.179 -0.25589,0 -0.43501,-0.179 l -1.8808,-1.8802 -1.88081,1.8802 q -0.17911,0.179 -0.43501,0.179 -0.25589,0 -0.43501,-0.179 L 6.57917,9.7649 Q 6.40006,9.5858 6.40006,9.33 q 0,-0.2558 0.17911,-0.4348 L 8.45998,7.015 6.57917,5.1348 Q 6.40006,4.9557 6.40006,4.6999 Z"/></svg>',
			'indent': 'fa fa-indent',
			'outdent': 'fa fa-outdent',
            'arrowsAlt': 'fa fa-expand',
            'bold': 'fa fa-bold',
			'caret': 'fa fa-caret-down',
			'circle': 'far fa-circle',
			'close': 'fa fa-times',
			'code': 'fa fa-code',
            'eraser': 'fa fa-eraser',
            'font': 'fa fa-font',
			//'frame': 'far fa-window-maximize',
			'italic': 'fa fa-italic',
			'link': 'fa fa-link',
			'unlink': 'fa fa-unlink',
            'magic': 'fa fa-magic',
			'menuCheck': 'fa fa-check',
            'minus': 'fa fa-minus',
			'orderedlist': 'fa fa-list-ol',
			'pencil': 'fa fa-pencil',
			'picture': 'far fa-image',
			'question': 'fa fa-question',
			'redo': 'fa fa-redo',
			'square': 'far fa-square',
			'strikethrough': 'fa fa-strikethrough',
			'subscript': 'fa fa-subscript',
			'superscript': 'fa fa-superscript',
			'table': 'fa fa-table',
			'textHeight': 'fa fa-text-height',
			'trash': 'fa fa-trash',
			'underline': 'fa fa-underline',
			'undo': 'fa fa-undo',
			'unorderedlist': 'fa fa-list-ul',
            'video': 'fa fa-video'
		},
		codemirror: {
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
			extraKeys: {
				"'.'": CodeMirror.hint.completeAfter,
				"'<'": CodeMirror.hint.completeAfter,
				"'/'": CodeMirror.hint.completeIfAfterLt,
				"' '": CodeMirror.hint.completeIfAfterSpace,
				"'='": CodeMirror.hint.completeIfInTag,
				"Ctrl-Space": "autocomplete",
				"F11": function (cm) { cm.setOption("fullScreen", !cm.getOption("fullScreen")); },
				"Esc": function (cm) { if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false); }
			},
			hintOptions: {
				closeCharacters: /[\s()\[\]{};:>,.|%]/,
				completeSingle: false
			}
		},
		imageAttributes: {
            icon: '<i class="fa fa-pencil"/>',
			removeEmpty: true, // true = remove attributes | false = leave empty if present
			disableUpload: true // true = don't display Upload Options | Display Upload Options
		}
	};

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

	// Custom events
	$(function () {
		// Editor toggling
		$(document).on('click', '.note-editor-preview', function (e) {
			var div = $(this);
			var textarea = $(div.data("target"));
			var lang = div.data("lang");

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
})();