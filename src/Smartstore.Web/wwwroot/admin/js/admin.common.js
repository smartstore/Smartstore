var currentModalId = "";
function closeModalWindow() {
    var modal = $('#' + currentModalId).data('modal');
    if (modal)
        modal.hide();
    return false;
}
function openModalWindow(modalId) {
    currentModalId = modalId;
    $('#' + modalId).modal('show');
}

// Global Admin namespace
Smartstore.Admin = {
    modelTrees: {},
    checkboxCheck: function (obj, checked) {
        if (checked)
            $(obj).attr('checked', 'checked');
        else
            $(obj).removeAttr('checked');
    },
    checkAllOverriddenStoreValue: function (obj) {
        $('.multi-store-override-option').each(function (i, el) {
            Smartstore.Admin.checkboxCheck(el, obj.checked);
            Smartstore.Admin.checkOverriddenStoreValue(el);
        });
    },
    checkOverriddenStoreValue: function (el) {
        var checkbox = $(el);
        var parentSelector = checkbox.data('parent-selector'),
            parent = parentSelector ? $(parentSelector) : checkbox.closest('.multi-store-setting-group').find('> .multi-store-setting-control'),
            checked = checkbox.is(':checked');

        parent.find('input:not([type=hidden]), select, textarea').each(function (i, el) {
            var input = $(el);
            var tbox = input.data('tTextBox');

            if (tbox) {
                checked ? tbox.enable() : tbox.disable();
            }
            else {
                checked ? input.removeAttr('disabled') : input.attr('disabled', true);
            }
        });
    },
    movePluginActionButtons: function () {
        // Move plugin specific action buttons (like 'Save') to top header section
        var pluginActions = $('.plugin-config-container .plugin-actions');
        if (pluginActions) {
            // Action buttons do exist: prepend them to header
            pluginActions.detach().prependTo('.section-header .options').on('click', 'button[type=submit]', function (e) {
                // On SubmitButtonClick, post the form programmatically, as the button is not a child of the form anymore...
                var form = $('.plugin-config-container form').first();
                if (form) {
                    // ...but first add a hidden input to the form with button's name and value to mimic button click WITHIN the form.
                    var btn = $(e.currentTarget);
                    form.prepend($('<input type="hidden" name="' + btn.attr('name') + '" value="' + btn.attr('value') + '" />'));
                    form.trigger('submit');
                }
            });
        }
    },
    syncAmountPostfix: function () {
        // INFO: (mw) Think THOROUGHLY about var and method naming. Take some time!
        // INFO: (mw) DRY: don't touch a million files for such simple UI helpers. Always try to find a central approach.

        // Switch between the currency code and the percent sign in the postfix 
        // of the amount control.
        let usePercentCheckbox = $('#AdditionalFeePercentage, .sync-postfix-percent');
        let amountPostfix = $('#AdditionalFee, .sync-postfix-amount').parent().find('.numberinput-postfix').first();

        if (amountPostfix.length) {
            let currencyCode = amountPostfix.text();

            // Update the postfix text depending on the checkbox state.
            function updateAmountPostfix() {
                if (usePercentCheckbox.is(':checked')) {
                    amountPostfix.text('%');
                } else {
                    amountPostfix.text(currencyCode);
                }
            }

            // Make sure the postfix matches the setting on page init.
            updateAmountPostfix();

            // Sync postfix on checkbox change.
            usePercentCheckbox.on('change', updateAmountPostfix);
        }
    },
    togglePanel: function (el /* the toggler */, animate) {
        var ctl = $(el),
            show = ctl.is(':checked'),
            reverse = ctl.data('toggler-reverse');

        if (reverse) show = !show;

        $(ctl.data('toggler-for')).each(function (i, cel) {
            var pnl = $(cel),
                isTableGroup = pnl.is('tbody'),
                reversePanel = pnl.data('panel-reverse'),
                showPanel = show;

            if (reversePanel) showPanel = !showPanel;

            pnl.addClass('collapsible');
            if (isTableGroup) pnl.addClass('collapsible-group');

            if (!pnl.data('bs.collapse')) {
                // Existing views don't provide Bootstrap's initial collapse state. Establish it
                // before creating the instance and prevent the constructor from toggling it.
                pnl
                    .addClass('collapse')
                    .toggleClass('show expanded', showPanel)
                    .collapse({ toggle: false });
            }

            if (!animate || isTableGroup) {
                // Initial setup must not animate. Bootstrap's height transition also cannot
                // reliably animate a table row group, but its collapse state still works.
                pnl
                    .removeClass('collapsing')
                    .addClass('collapse')
                    .toggleClass('show expanded', showPanel)
                    .css('height', '');
            }
            else {
                pnl
                    .toggleClass('expanded', showPanel)
                    .collapse(showPanel ? 'show' : 'hide');
            }
        });
    },
    getThemeColorVars: function () {
        const frameRoot = $(window.document.documentElement);
        let themeVars = frameRoot.data('themeVars');
        if (themeVars) {
            return themeVars;
        }

        const styles = getComputedStyle(frameRoot.get(0));
        const colors = styles.getPropertyValue("--varnames");

        // Make array
        let arr = colors.split(",");

        themeVars = {};
        $.each(arr, (_i, val) => {
            val = val.trim();
            themeVars[val] = styles.getPropertyValue(val);
        });

        frameRoot.data('themeVars', themeVars);
        return themeVars;
    },
    TaskWatcher: (function () {
        var interval;

        return {
            startWatching: function (opts) {
                function poll() {
                    $.ajax({
                        cache: false,
                        type: 'POST',
                        global: false,
                        url: opts.pollUrl,
                        //dataType: 'json',
                        success: function (data) {
                            data = data || [];
                            var runningElements = [];
                            $.each(data, function (i, task) {
                                var el = $(opts.elementsSelector + '[data-task-id=' + task.id + ']');
                                if (el.length) {
                                    runningElements.push(el[0]);
                                    if (el.data('running') && el.text()) {
                                        // already running
                                        el.find('.text').text(task.message || opts.defaultProgressMessage);
                                        el.find('.percent').text(task.percent >> 0 ? task.percent + ' %' : "");
                                    }
                                    else {
                                        // new task
                                        var row1 = $('<div class="hint position-relative d-flex justify-content-between flex-nowrap"></div>').appendTo(el);
                                        row1.append($('<div class="text text-truncate">' + (task.message || opts.defaultProgressMessage) + '</div>'));
                                        row1.append($('<div class="percent">' + (task.percent >> 0 ? task.percent + ' %' : "") + '</div>'));
                                        var row2 = $('<div class="loading-bar mt-2"></div>').appendTo(el);
                                        el.attr('data-running', 'true').data('running', true);
                                        if (_.isFunction(opts.onTaskStarted)) {
                                            opts.onTaskStarted(task, el);
                                        }
                                        el.removeClass('hide');
                                    }
                                }
                            });

                            // remove runningElements for finished tasks (the ones currently running but are not in 'runningElements'
                            var currentlyRunningElements = opts.context.find(opts.elementsSelector + '[data-running=true]');
                            $.each(currentlyRunningElements, function (i, el) {
                                var shouldRun = _.find(runningElements, function (val) { return val === el; });
                                if (!shouldRun) {
                                    // restore element to it's init state
                                    var jel = $(el);
                                    jel.addClass('hide').html('').attr('data-running', 'false').data('running', false);
                                    if (_.isFunction(opts.onTaskCompleted)) {
                                        opts.onTaskCompleted(jel.data('task-id'), jel);
                                    }
                                }
                            });
                        },
                        error: function (xhr, ajaxOptions, thrownError) {
                            window.clearInterval(interval);
                            if (thrownError) {
                                console.error(thrownError);
                            }
                        }
                    });
                }
                window.setTimeout(poll, 50);
                interval = window.setInterval(poll, 2500);
            }
        }
    })()
};
