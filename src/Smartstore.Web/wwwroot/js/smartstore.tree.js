/*
*  Project: Smartstore tree.
*  Author: Marcus Gesing, SmartStore AG.
*/
; (function ($, window, document, undefined) {

    var methods = {
        init: function (options) {
            options = $.extend({}, $.tree.defaults, options);

            return this.each(function () {
                var root = $(this);

                root.data('tree-options', options);

                if (!loadData(root, null, function (data) {
                    initialize(root, options, data);
                })) {
                    initialize(root, options, null);
                }
            });
        },

        expandAll: function () {
            return this.each(function () {
                expandAll(this);
            });
        },
    };

    $.fn.tree = function (method) {
        return main.apply(this, arguments);
    };

    $.tree = function () {
        return main.apply($('.tree:first'), arguments);
    };

    $.tree.defaults = {
        expanded: false,        // true: initially expand tree.
        showLines: false,       // true: show helper lines.
        readOnly: false,        // true: no state changes allowed (checkbox disabled).
        nodeState: '',          // 'on-off': adds state checkboxes for 'on' (green), 'off' (red), 'inherit' (muted green\red).
        dragAndDrop: false,     // true: drag & drop enabled.
        showNumChildren: true,
        showNumChildrenDeep: false,
        defaultIconClass: null,
        defaultIconUrl: null,
        expandedClass: 'fas fa-chevron-down',
        collapsedClass: 'fas fa-chevron-right',
        leafClass: 'tree-leaf',
        stateTitles: ['', '', '', '']
    };


    function main(method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }

        if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }

        EventBroker.publish("message", { title: 'Tree method "' + method + '" does not exist', type: "error" });
        return null;
    }

    function initialize(root, opt, data) {
        // Add node HTML.
        addNodeHtml(root, opt, data);

        // Set root item class.
        root.find('ul:first > .tree-node').each(function () {
            $(this).addClass('root-node');
        });

        // Initially expand or collapse nodes.
        root.find('.tree-noleaf').each(function () {
            expandNode($(this), opt.expanded, opt, false);
        });

        // Add state checkbox HTML.
        if (opt.nodeState === 'on-off') {
            root.find('.tree-label').each(function (i, el) {
                var label = $(this);
                var node = label.closest('.tree-node');
                var value = parseInt(node.data('state-value')) || 0;
                var stateId = node.data('state-id');
                var html = '';
                var stateClass = 'tree-state';
                var stateTitle = '';

                if (value === 2) {
                    stateClass += ' on';
                    stateTitle = opt.stateTitles[0];
                }
                else if (value === 1 || node.hasClass('root-node')) {
                    value = 1;
                    stateClass += ' off';
                    stateTitle = opt.stateTitles[1];
                }

                if (!opt.readOnly) {
                    stateClass += ' tree-state-active';
                    label.attr('for', stateId);

                    html += '<input class="tree-state-checkbox" type="checkbox" name="' + stateId + '" id="' + stateId + '" value="' + value + '"' + (value === 2 ? ' checked="checked"' : '') + ' />';
                    html += '<input type="hidden" name="' + stateId + '" value="' + (value === 0 ? 0 : 1) + '" />';
                }
                html += '<span class="' + stateClass + '" title="' + stateTitle + '"></span>';

                label.prepend(html);
            });

            if (!opt.readOnly) {
                // Set indeterminate property.
                //root.find('input[type=checkbox][value=0]').prop('indeterminate', true);

                // Set inherited state.
                root.find('ul:first > .tree-node').each(function () {
                    setInheritedState($(this), 0, opt);
                });
            }
        }

        // Expander click handler.
        root.on('click', '.tree-expander', function () {
            var node = $(this).closest('.tree-node');

            if (node.find('ul').length) {
                expandNode(node, node.hasClass('tree-collapsed'), opt, true);
            }
            else {
                loadData(root, node, function (data) {
                    addNodeHtml(node, opt, data);
                    expandNode(node, true, opt, true);
                });
            }
        });

        // State click handler.
        root.on('click', 'input[type=checkbox]', function () {
            var el = $(this);
            var node = el.closest('.tree-node');

            if (opt.nodeState === 'on-off') {
                var hIn = el.next();
                var state = el.siblings('.tree-state:first');
                var inheritedState = 0;
                var val = parseInt(el.val());

                state.removeClass('on off in-on in-off');

                if (val === 2) {
                    // Checked > unchecked.
                    el.prop({ checked: false, indeterminate: false, value: 1 });
                    hIn.val(1);
                    state.addClass('off').attr('title', opt.stateTitles[1]);
                    inheritedState = 1;
                }
                else if (val === 0 || node.hasClass('root-node')) {
                    // Indeterminate > checked.
                    // Root item cannot have an inherited state.
                    el.prop({ checked: true, indeterminate: false, value: 2 });
                    hIn.val(1);
                    state.addClass('on').attr('title', opt.stateTitles[0]);
                    inheritedState = 2;
                }
                else {
                    // Unchecked > indeterminate.
                    el.prop({ checked: false, indeterminate: true, value: 0 });
                    hIn.val(0);
                    inheritedState = getInheritedState(node);
                }

                // Update nodes with inherited state.
                setInheritedState(node, inheritedState, opt);
            }
        });
    }

    function addNodeHtml(context, opt, data) {
        var isRoot = context.hasClass('tree');

        context.find('li').each(function () {
            var li = $(this);
            var childList = li.find('ul');
            var dataLoaded = childList.length;
            var nodeData = data?.nodes?.find(x => x.Id == li.data('id'))?.Value;

            var numChildren = dataLoaded
                ? childList.find('li').length
                : parseInt(nodeData?.NumChildren ?? '0');

            var numChildrenDeep = parseInt(nodeData?.NumChildrenDeep ?? '0');

            var name = nodeData?.Name || li.data('name');
            var title = nodeData?.Title ? window.htmlEncode(nodeData.Title) : li.data('title');
            var nodeUrl = nodeData?.Url || li.data('url');
            var badgeText = nodeData?.BadgeText || li.data('badge-text');
            var iconClass = nodeData?.IconClass || li.data('icon-class') || opt.defaultIconClass;
            var iconUrl = nodeData?.IconUrl || li.data('icon-url') || opt.defaultIconUrl;
            var published = nodeData ? nodeData.Published : toBool(li.data('published'), true);
            var innerClass = published ? '' : ' tree-unpublished';
            var textClass = numChildren == 0 ? 'tree-leaf-text' : 'tree-noleaf-text';
            var labelClass = !nodeUrl && !opt.readOnly && opt.nodeState === 'on-off' ? ' tree-label-active' : '';
            var labelHtml = '';
            var html = '';

            if (nodeUrl) {
                var target = nodeData?.UrlTarget || li.data('url-target');
                // Why does string.format() sometimes not work with URLs?
                labelHtml = '<a href="' + nodeUrl + '"';
                if (target) {
                    labelHtml += ' target="' + target + '"';
                }
                if (title) {
                    labelHtml += ' title="' + title + '"';
                }
                labelHtml += '>' + name + '</a>';
            }
            else if (title) {
                labelHtml = '<span title="{0}">{1}</span>'.format(title, name);
            }
            else {
                labelHtml = name;
            }

            if (numChildren > 0 && opt.showNumChildren) {
                labelHtml += ' ({0})'.format(numChildren);
            }
            else if (numChildrenDeep > 0 && opt.showNumChildrenDeep) {
                labelHtml += ' ({0})'.format(numChildrenDeep);
            }

            if (badgeText) {
                var badgeStyle = nodeData?.BadgeStyle || li.data('badge-style') || 'badge-secondary';
                labelHtml += ' <span class="badge {0}">{1}</span>'.format(badgeStyle, badgeText);
            }

            if (numChildren > 0) {
                html += '<span class="tree-expander-container tree-expander"><i class="{0}"></i></span>'.format(opt.collapsedClass);
            }
            else {
                html += '<span class="tree-expander-container"></span>';
            }

            if (iconClass) {
                html += '<span class="tree-icon"><i class="{0}"></i></span>'.format(iconClass);
            }
            else if (iconUrl) {
                html += '<span class="tree-icon"><img src="{0}" /></span>'.format(iconUrl);
            }

            html += '<label class="tree-label{0}"><span class="{1}">{2}</span></label>'.format(labelClass, textClass, labelHtml);

            li.addClass('tree-node ' + (numChildren == 0 ? opt.leafClass : 'tree-noleaf'))
                .prepend('<div class="tree-inner{0}">{1}</div>'.format(innerClass, html));

            li.closest('ul').addClass('tree-list');
        });

        if (opt.showLines) {
            context.find(isRoot ? 'ul:first ul' : 'ul')
                .addClass('tree-hline')
                .prepend('<span class="tree-vline"></span>');
        }
    }

    function expandAll(context) {
        var self = $(context);
        var opt = self.data('tree-options') || $.tree.defaults;
        var expand = !(opt.expanded || false);

        self.find('.tree-noleaf').each(function () {
            expandNode($(this), expand, opt, false);
        });

        opt.expanded = expand;
    }

    function expandNode(node, expand, opt, slide) {
        if (expand) {
            node.removeClass('tree-collapsed').addClass('tree-expanded');

            if (slide) {
                node.children('ul').slideDown(300);
            }
            else {
                node.children('ul').show();
            }
        }
        else {           
            if (slide) {
                node.children('ul').slideUp(300);
            }
            else {
                node.children('ul').hide();
            }

            node.removeClass('tree-expanded').addClass('tree-collapsed');
        }

        node.find('.tree-inner:first .tree-expander').html('<i class="' + (expand ? opt.expandedClass : opt.collapsedClass) + '"></i>');
    }

    function setInheritedState(node, inheritedState, opt) {
        if (!node) return;

        var childState = inheritedState;
        var val = parseInt(node.find('> .tree-inner input[type=checkbox]').val()) || 0;

        if (val > 0) {
            // Is directly on.
            childState = val;
        }
        else {
            // Is not directly on.
            node.find('.tree-state:first')
                .removeClass('in-on in-off on off')
                .addClass(inheritedState === 2 ? 'in-on' : 'in-off')
                .attr('title', opt.stateTitles[inheritedState === 2 ? 2 : 3]);
        }

        node.find('> ul > .tree-node').each(function () {
            setInheritedState($(this), childState, opt);
        });
    }

    function getInheritedState(node) {
        var result = 0;

        if (node) {
            node.parents('.tree-node').each(function () {
                result = parseInt($(this).find('> .tree-inner input[type=checkbox]').val()) || 0;
                if (result > 0) {
                    return false;
                }
            });
        }

        return result;
    }

    function loadData(root, node, callback) {
        var url = root.data('url');
        if (!url) {
            // We assume that all data already loaded.
            return false;
        }

        var parentId = parseInt(node?.data('id')) || 0;
        var expander = node?.find('.tree-expander');

        $.ajax({
            type: 'GET',
            url: url,
            global: node ? null : this._global,
            dataType: 'json',
            cache: false,
            timeout: 5000,
            data: { parentId: parentId },
            beforeSend: function () {
                if (expander) {
                    expander.find('i').hide();
                    expander.prepend(window.createCircularSpinner(12, true));
                }
            },
            success: function (data) {
                var items = '';
                data.nodes.forEach(x => items += '<li data-id="' + x.Id + '"></li>');

                (node ?? root).append('<ul>' + items + '</ul>');

                callback(data);
            },
            complete: function () {
                if (expander) {
                    expander.find('.spinner').remove();
                    expander.find('i').show();
                }
            }
        });

        return true;
    }

})(jQuery, this, document);