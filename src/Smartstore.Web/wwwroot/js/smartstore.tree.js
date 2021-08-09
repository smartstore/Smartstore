/*
*  Project: Smartstore tree.
*  Author: Marcus Gesing, SmartStore AG.
*/
(function ($, window, document, undefined) {

    // TODO: (mg) (core) For optimal dragdrop visual feedback style the hover effect how it was before (background for icon + label). 
    // For this you need to move the icon to a new parent element and put some padding around the content.

    $.fn.tree = function (method) {
        return main.apply(this, arguments);
    };

    $.tree = function () {
        return main.apply($('.tree:first'), arguments);
    };

    $.tree.defaults = {
        stateType: null,        // 'checkbox': adds simple checkboxes to check\uncheck items.
                                // 'on-off': adds checkboxes for 'on'(green), 'off'(red), 'inherit'(muted green\red).
        selectMode: null,       // 'single': single selection, only one node is selected at any time.
                                // 'multiple': multiple selection, several nodes can be selected.
        url: null,              // URL to load tree items on demand.
        expanded: false,        // true: initially expand tree.
        highlightNodes: true,   // true: highlight items on mouse hover.
        showLines: false,       // true: show helper lines.
        readOnly: false,        // true: no state changes allowed (checkbox disabled).
        dragAndDrop: false,     // true: drag & drop enabled.
        showNumChildren: true,
        showNumChildrenDeep: false,
        defaultCollapsedIconClass: null,
        defaultExpandededIconClass: null,
        defaultCollapsedIconUrl: null,
        defaultExpandededIconUrl: null,
        expandedClass: 'fas fa-chevron-down',
        collapsedClass: 'fas fa-chevron-right',
        leafClass: 'tree-leaf',
        stateTitles: ['', '', '', ''],
        disabledTitle: null
    };

    var methods = {
        init: function (options) {
            options = $.extend({}, $.tree.defaults, options);

            return this.each(function () {
                var root = $(this);

                root.data('tree-options', options);

                if (!loadData(root, null, options, function (data) {
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

        checkedNodeKeys: function (getStateKeys) {
            var root = $(this);
            var opt = root.data('tree-options');

            if (opt.stateType === 'checkbox') {
                var keys = _.map(root.find('.tree-state-checkbox:checked'), function (x) {
                    return getStateKeys ? $(x).attr('name') : $(x).closest('li').data('id');
                });

                return keys;
            }

            return null;
        },

        selectedNodeKeys: function () {
            var keys = _.map($(this).find('.tree-selected'), function (x) {
                return $(x).closest('li').data('id');
            });

            return keys;
        }
    };


    function main(method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }

        if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }

        EventBroker.publish('message', { title: 'Tree method "' + method + '" does not exist', type: 'error' });
        return null;
    }

    function initialize(root, opt, data) {
        addNodeHtml(root, opt, data);

        if (opt.highlightNodes) {
            root.addClass('tree-highlight');
        }

        // Set root item class.
        root.find('ul:first > .tree-node').each(function () {
            $(this).addClass('root-node');
        });

        // Initially expand or collapse nodes.
        root.find('.tree-noleaf').each(function () {
            expandNode($(this), opt.expanded, opt, false);
        });

        // Set inherited state.
        if (opt.stateType === 'on-off' && !opt.readOnly) {
            // Set indeterminate property.
            //root.find('input[type=checkbox][value=0]').prop('indeterminate', true);

            root.find('ul:first > .tree-node').each(function () {
                setInheritedState($(this), 0, opt);
            });
        }

        // Expander clicked.
        root.on('click', '.tree-expander', function () {
            var node = $(this).closest('.tree-node');

            if (node.find('ul').length) {
                expandNode(node, node.hasClass('tree-collapsed'), opt, true);
            }
            else {
                loadData(root, node, opt, function (data) {
                    addNodeHtml(node, opt, data);
                    expandNode(node, true, opt, true);
                });
            }
        });

        // State checkbox clicked.
        root.on('click', 'input[type=checkbox]', function () {
            var el = $(this);
            var node = el.closest('.tree-node');

            if (opt.stateType === 'on-off') {
                var hIn = el.next();
                var state = el.siblings('.tree-state-onoff:first');
                var inheritedState = 0;
                var currentValue = parseInt(el.val());
                var value;

                state.removeClass('on off in-on in-off');

                if (currentValue === 2) {
                    // Checked > unchecked.
                    value = 1;
                    el.prop({ checked: false, indeterminate: false, value });
                    hIn.val(1);
                    state.addClass('off').attr('title', opt.stateTitles[1]);
                    inheritedState = 1;
                }
                else if (currentValue === 0 || node.hasClass('root-node')) {
                    // Indeterminate > checked.
                    // Root item cannot have an inherited state.
                    value = 2;
                    el.prop({ checked: true, indeterminate: false, value });
                    hIn.val(1);
                    state.addClass('on').attr('title', opt.stateTitles[0]);
                    inheritedState = 2;
                }
                else {
                    // Unchecked > indeterminate.
                    value = 0;
                    el.prop({ checked: false, indeterminate: true, value });
                    hIn.val(0);
                    inheritedState = getInheritedState(node);
                }

                // Update nodes with inherited state.
                setInheritedState(node, inheritedState, opt);

                EventBroker.publishSync('tree.checked', { node, value });
            }
            else if (opt.stateType === 'checkbox') {
                EventBroker.publishSync('tree.checked', { node });
            }
        });

        // Selectable node clicked.
        root.on('click', '.tree-selectable', function (e) {
            e.stopPropagation();
            e.preventDefault();
            var self = $(this);
            var node = self.closest('.tree-node');

            if (opt.selectMode === 'single') {
                root.find('.tree-selected').removeClass('tree-selected');
                self.addClass('tree-selected');
            }
            else if (opt.selectMode === 'multiple') {
                self.toggleClass('tree-selected');
            }

            EventBroker.publishSync('tree.selected', { node });
            return false;
        });

        if (opt.dragAndDrop) {
            initializeDragAndDrop(root, opt);
        }

        EventBroker.publishSync('tree.initialized', { root });
    }

    function addNodeHtml(context, opt, data) {
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
            var iconClass = nodeData?.IconClass || li.data('icon-class') || opt.defaultCollapsedIconClass;
            var iconUrl = nodeData?.IconUrl || li.data('icon-url') || opt.defaultCollapsedIconUrl;
            var dimmed = nodeData ? nodeData.Dimmed : toBool(li.data('dimmed'), false);
            var enabled = nodeData ? nodeData.Enabled : toBool(li.data('enabled'), true);
            var textClass = numChildren == 0 ? 'tree-leaf-text' : 'tree-noleaf-text';
            var contentClass = `tree-node-content${dimmed ? ' tree-dim' : ''}${enabled ? '' : ' tree-disabled'}`;
            var nodeClass = `tree-node ${numChildren == 0 ? opt.leafClass : 'tree-noleaf'}`;
            var labelHtml = '';
            var html = '';

            if (!nodeUrl) {
                contentClass += `${opt.selectMode && enabled ? ' tree-selectable' : ''}${!opt.readOnly && opt.stateType === 'on-off' ? ' tree-pointer' : ''}`;
            }

            if (nodeUrl) {
                var target = nodeData?.UrlTarget || li.data('url-target');
                labelHtml = `<a class="tree-link tree-name" href="${nodeUrl}"${target ? ` target="${target}"` : ''}${title ? ` title="${title}"` : ''}>${name}</a>`;
            }
            else {
                labelHtml = `<span class="tree-name"${title ? ` title="${title}"` : ''}>${name}</span>`;
            }

            if (numChildren > 0 && opt.showNumChildren) {
                labelHtml += ` (${numChildren})`;
            }
            else if (numChildrenDeep > 0 && opt.showNumChildrenDeep) {
                labelHtml += ` (${numChildrenDeep})`;
            }

            if (badgeText) {
                var badgeStyle = nodeData?.BadgeStyle || li.data('badge-style') || 'badge-secondary';
                labelHtml += ` <span class="badge ${badgeStyle}">${badgeText}</span>`;
            }

            if (numChildren > 0) {
                html += `<span class="tree-expander-container tree-expander"><i class="${opt.collapsedClass}"></i></span>`;
            }
            else {
                html += '<span class="tree-expander-container"></span>';
            }

            html += `<label class="tree-label"${!enabled && opt.disabledTitle ? ` title="${opt.disabledTitle}"` : ''}><span class="${contentClass}">`;

            if (iconClass) {
                html += `<span class="tree-icon"><i class="${iconClass}"></i></span>`;
            }
            else if (iconUrl) {
                html += `<span class="tree-icon"><img src="${iconUrl}" /></span>`;
            }

            html += `<span class="${textClass}">${labelHtml}</span>`;
            html += '</span></label>';

            li.addClass(nodeClass).prepend(`<div class="tree-inner">${html}</div>`);
            li.closest('ul').addClass('tree-list');
        });

        if (opt.showLines) {
            context.find(context.hasClass('tree') ? 'ul:first ul' : 'ul')
                .addClass('tree-hline')
                .prepend('<span class="tree-vline"></span>');
        }

        if (opt.stateType) {
            addStateCheckboxes(context, opt, data);
        }
    }

    function addStateCheckboxes(context, opt, data) {
        context.find('.tree-label').each(function () {
            var label = $(this);

            if (label.find('> input[type=checkbox]').length) {
                return;
            }

            var node = label.closest('.tree-node');
            var nodeId = node.data('id');
            var nodeData = data?.nodes?.find(x => x.Id == nodeId)?.Value;
            var stateId = nodeData?.StateId || node.data('state-id');
            var value = nodeData?.StateValue || node.data('state-value');
            var html = '';

            if (opt.stateType === 'on-off') {
                value = parseInt(value) || 0;
                var stateClass = 'tree-state-onoff';
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
                    stateClass += ' tree-state-onoff-active';
                    label.attr('for', stateId);

                    html += `<input class="tree-state-onoff-checkbox" type="checkbox" name="${stateId}" id="${stateId}" value="${value}"${value === 2 ? ' checked="checked"' : ''} />`;
                    html += `<input type="hidden" name="${stateId}" value="${value === 0 ? 0 : 1}" />`;
                }
                html += `<span class="${stateClass}" title="${stateTitle}"></span>`;
            }
            else if (opt.stateType === 'checkbox') {
                var enabled = nodeData ? nodeData.Enabled : toBool(node.data('enabled'), true);
                var checked = nodeData ? nodeData.Checked : toBool(node.data('checked'), false);

                html += `<input class="tree-state-checkbox" type="checkbox"${checked ? ' checked="checked"' : ''}${enabled ? '' : ' disabled="disabled"'}`;
                html += `${stateId ? ` name="${stateId}" id="${stateId}"` : ''}${value ? ` value="${value}"` : ''} />`;
            }

            label.prepend(html);
        });
    }

    function expandAll(root) {
        var self = $(root);
        var opt = self.data('tree-options') || $.tree.defaults;
        var expand = !(opt.expanded || false);

        self.find('.tree-noleaf').each(function () {
            expandNode($(this), expand, opt, false);
        });

        opt.expanded = expand;
    }

    function expandNode(node, expand, opt, slide) {
        var childNodes = node.children('ul');
        var nodeInner = node.find('.tree-inner:first');

        if (expand) {
            // Expand.
            node.removeClass('tree-collapsed').addClass('tree-expanded');

            if (slide) {
                childNodes.hide().slideDown(200);
            }
            else {
                childNodes.show();
            }

            toggleIcons();
            EventBroker.publishSync('tree.expanded', { node });
        }
        else {
            // Collapse.
            node.removeClass('tree-expanded').addClass('tree-collapsed');

            if (slide) {
                childNodes.slideUp(200, function () {
                    childNodes.hide();
                    toggleIcons();
                    EventBroker.publishSync('tree.collapsed', { node });
                });
            }
            else {
                childNodes.hide();
                toggleIcons();
                EventBroker.publishSync('tree.collapsed', { node });
            }
        }

        function toggleIcons() {
            // Toggle node icon.
            if (opt.defaultCollapsedIconClass && opt.defaultExpandededIconClass) {
                nodeInner.find('.tree-icon i').attr('class', expand ? opt.defaultExpandededIconClass : opt.defaultCollapsedIconClass);
            }
            else if (opt.defaultCollapsedIconUrl && opt.defaultExpandededIconUrl) {
                nodeInner.find('.tree-icon img').attr('src', expand ? opt.defaultExpandededIconUrl : opt.defaultCollapsedIconUrl);
            }

            // Toggle expander icon.
            nodeInner.find('.tree-expander').html(`<i class="${expand ? opt.expandedClass : opt.collapsedClass}"></i>`);
        }
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
            node.find('.tree-state-onoff:first')
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

    function loadData(root, node, opt, callback) {
        var url = opt.url || root.data('url');
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

                EventBroker.publishSync('tree.loading', { node });
            },
            success: function (data) {
                var items = '';
                data.nodes.forEach(x => items += `<li data-id="${x.Id}"></li>`);

                (node ?? root).append(`<ul>${items}</ul>`);

                callback(data);
                EventBroker.publishSync('tree.loaded', { node });
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

    // Drag & drop
    function initializeDragAndDrop(root, opt) {
        var d = opt._drag = {
            active: false,
            scrollTimer: null,
            scrollEdgeSize: 50, // How close to the edge the scrolling should start.
            scrollMaxStep: 20,  // Make the icremental scroll changes more "intense" the closer that the user gets the viewport edge.
            indicator: $('<div class="tree-drop-indicator"></div>').appendTo(document.body).get(0),
            ghost: $('<div class="tree-ghost badge badge-light"><i></i><span class="ml-1"></span></div>').appendTo(document.body).get(0)
        };

        root.on('dragstart', '.tree-node-content', function (e) {
            var node = $(this).closest('.tree-node');

            startDragging(true);
            d.sourceId = node.data('id');
            d.ghost.getElementsByTagName('span')[0].innerText = $(this).find('.tree-name').text();
            d.ghostIcon = d.ghost.getElementsByTagName('i')[0];

            root.find('.tree-node-content').addClass('droppable');
            node.find('.tree-node-content').removeClass('droppable');

            e.originalEvent.dataTransfer.effectAllowed = 'move';
            e.originalEvent.dataTransfer.setDragImage(this, -99999, -99999);
            e.originalEvent.dataTransfer.setData('text/plain', d.sourceId);

            console.log('dragstart ' + d.ghost.getElementsByTagName('span')[0].innerText);
        });

        root.on('dragend', '.tree-node-content', function () {
            startDragging(false);
        });

        root.on('dragenter', '.droppable', function (e) {
            e.preventDefault();

            if (d.active) {
                d.nodeContent = this;

                var nodeId = $(this).closest('.tree-node').data('id');
                if (nodeId !== d.targetId) {
                    d.targetId = nodeId;
                    d.targetRect = this.getBoundingClientRect();
                    d.position = '';

                    var scrollY = window.scrollY;
                    d.beforeY = d.targetRect.top + scrollY + 8;
                    d.afterY = d.targetRect.bottom + scrollY - 8;
                    d.indicatorBeforeTop = d.targetRect.top + scrollY;
                    d.indicatorAfterBottom = d.targetRect.bottom + scrollY;

                    d.indicator.style.left = (d.targetRect.left + 10) + 'px';

                    root.find('.droppable-highlight').removeClass('droppable-highlight');
                }
            }
        });

        root.on('dragleave', '.droppable', function (e) {
            if (d.active && this === d.nodeContent) {
                e.preventDefault();
                d.indicator.style.display = 'none';
                d.ghost.style.display = 'none';
                root.find('.droppable-highlight').removeClass('droppable-highlight');
            }
        });

        root.on('dragover', '.droppable', function (e) {
            e.preventDefault();  // Allow dropping.

            if (d.active) {
                // Indicate.
                if (e.pageY < d.beforeY) {
                    d.position = 'before';
                    d.indicator.style.display = 'block';
                    d.indicator.style.top = d.indicatorBeforeTop + 'px';
                    d.ghostIcon.setAttribute('class', 'fas fa-fw fa-long-arrow-alt-right');
                    d.nodeContent.classList.remove('droppable-highlight');
                }
                else if (e.pageY > d.afterY) {
                    d.position = 'after';
                    d.indicator.style.display = 'block';
                    d.indicator.style.top = d.indicatorAfterBottom + 'px';
                    d.ghostIcon.setAttribute('class', 'fas fa-fw fa-long-arrow-alt-right');
                    d.nodeContent.classList.remove('droppable-highlight');
                }
                else if (e.pageY >= d.beforeY && e.pageY <= d.afterY) {
                    d.position = 'over';
                    d.indicator.style.display = 'none';
                    d.ghostIcon.setAttribute('class', 'fas fa-fw fa-plus');
                    d.nodeContent.classList.add('droppable-highlight');
                }
                else {
                    d.position = '';
                    d.indicator.style.display = 'none';
                    d.ghost.style.display = 'none';
                    d.nodeContent.classList.remove('droppable-highlight');
                }

                // Show ghost tooltip.
                if (d.position.length > 0) {
                    d.ghost.style.display = 'block';
                    d.ghost.style.top = (e.pageY + 14) + 'px';
                    d.ghost.style.left = (e.pageX + 18) + 'px';
                }
            }
        });

        root.on('drop', '.droppable', function (e) {
            e.preventDefault();

            var sourceName = root.find(`.tree-node[data-id=${d.sourceId}]`).find('.tree-inner:first .tree-name').text();
            var targetName = $(this).closest('.tree-node').find('.tree-inner:first .tree-name').text();
            console.log(`drop ${sourceName} ${d.position} ${targetName}`);

            startDragging(false);
        });


        function startDragging(active) {
            d.active = active;
            d.position = '';
            d.sourceId = 0;
            d.targetId = 0;
            d.targetRect = null;
            d.nodeContent = null;
            d.ghostIcon = null;

            if (active) {
                root.removeClass('tree-highlight');
            }
            else {
                root.find('.tree-node-content').removeClass('droppable droppable-highlight');
                d.indicator.style.display = 'none';
                d.ghost.style.display = 'none';

                if (opt.highlightNodes) {
                    root.addClass('tree-highlight');
                }
            }
        }
    }

})(jQuery, this, document);