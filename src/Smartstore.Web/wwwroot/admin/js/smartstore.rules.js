Smartstore.Admin.Rules = (function () {
    return {
        onRuleValueChanged: function (dirty) {
            if (dirty === undefined) {
                dirty = true;
            }
            var ctx = $('#ruleset-root');
            ctx.find('.ruleset-save:first').prop('disabled', !dirty);
            ctx.data('dirty', dirty);
        }
    };
})();

(function ($, window, document, undefined) {

    var root = $('#ruleset-root');
    
    function enableRuleValueControl(el) {
        var rule = el.closest('.rule');
        var ruleId = rule.data('rule-id');
        var op = rule.find('.rule-operator').data('value');
        var inputElements = rule.find(':input[name="rule-value-' + ruleId + '"], :input[name^="rule-value-' + ruleId + '-"]');

        switch (op) {
            case 'IsEmpty':
            case 'IsNotEmpty':
            case 'IsNotNull':
            case 'IsNull':
                inputElements.prop('disabled', true);
                break;
            default:
                inputElements.prop('disabled', false);
                break;
        }
    }

    function appendToRuleSetBody(ruleSet, html) {
        var target = ruleSet.find('.ruleset-body').first();
        if (target.length === 0) {
            target = $('<div class="ruleset-body"></div>').appendTo(ruleSet);
        }
        target.append(html);
        enableRuleValueControl(target.find('.rule:last'));
        $('#excute-result').addClass('hide');
        applyCommonPlugins(target);
    }

    function getRuleData() {
        var data = [];

        root.find('.rule').each(function () {
            var rule = $(this);
            var ruleId = rule.data('rule-id');
            var op = rule.find('.rule-operator').data('value');
            var multipleNamePrefix = 'rule-value-' + ruleId + '-';
            var multipleInputElements = rule.find(':input[name^="' + multipleNamePrefix + '"]');
            var value = '';

            if (multipleInputElements.length > 0) {
                var valueObj = {};
                multipleInputElements.each(function () {
                    var el = $(this);
                    var val = el.val();
                    var name = el.attr('name') || '';

                    if (!_.isEmpty(name)) {
                        valueObj[name.replace(multipleNamePrefix, '')] = Array.isArray(val) ? val.join(',') : val;
                    }
                });

                value = JSON.stringify(valueObj);
            }
            else {
                var val = rule.find(':input[name="rule-value-' + ruleId + '"]').val();
                value = Array.isArray(val) ? val.join(',') : val;
            }

            data.push({ ruleId: ruleId, op: op, value: value });
        });

        return data;
    }

    //function showRuleError(ruleId, error) {
    //    var rule = root.find('[data-rule-id=' + ruleId + ']');
    //    var errorContainer = rule.find('.r-rule-error');
    //    var hasError = !_.isEmpty(error);

    //    errorContainer.toggleClass('hide', !hasError);
    //    errorContainer.find('.field-validation-error').text(error || '');

    //    rule.find('.btn-rule-operator')
    //        .toggleClass('btn-info', !hasError)
    //        .toggleClass('btn-danger', hasError);
    //}


    // Initialize.
    root.find('.rule').each(function () {
        var rule = $(this);
        enableRuleValueControl(rule);

        if (rule.data('has-error')) {
            rule.find(':input[name^="rule-value-"]').addClass('input-validation-error');
        }
    });

    $(document).ready(function () {
        Smartstore.Admin.Rules.onRuleValueChanged(false);
    });

    // Save rule set.
    $(document).on('click', 'button[name="save"], .save-rule-data', function () {
        var rawRuleData = root.data('dirty')
            ? JSON.stringify(getRuleData())
            : '';

        $('#RawRuleData').val(rawRuleData);

        var event = jQuery.Event('saveRuleSetData');
        event.save = true;
        event.button = this;
        event.rawRuleData = rawRuleData;

        $(document).trigger(event);

        return event.save;
    });


    // Add group.
    $(document).on('click', '.r-add-group', function (e) {
        var parentSet = $(this).closest('.ruleset');

        $.ajax({
            cache: false,
            type: 'POST',
            url: root.data('url-addgroup'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleSetId: parentSet.data('ruleset-id')
            },
            success: function (html) {
                appendToRuleSetBody(parentSet, html);
                parentSet.find('.ruleset:last select.r-add-rule').selectWrapper();
            }
        });

        return false;
    });

    // Delete group.
    $(document).on('click', '.r-delete-group', function () {
        var parentSet = $(this).closest('.ruleset');

        $.ajax({
            cache: false,
            type: 'POST',
            url: root.data('url-deletegroup'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleId: parentSet.data('refrule-id')
            },
            success: function (result) {
                if (result.Success) {
                    parentSet.remove();
                    $('#excute-result').addClass('hide');
                }
            }
        });

        return false;
    });

    // Change rule set operator.
    $(document).on('click', '.ruleset-operator .dropdown-item:not(.disabled)', function (e) {
        e.stopPropagation();
        e.preventDefault();

        var item = $(this);
        var operator = item.closest('.ruleset-operator');
        var op = item.data('value');

        $.ajax({
            cache: false,
            type: 'POST',
            url: root.data('url-changeoperator'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleSetId: item.closest('.ruleset').data('ruleset-id'),
                op
            },
            success: function (result) {
                if (result.Success) {
                    operator.find('input[name=LogicalOperator]').val(op);
                    operator.find('.logical-operator-chooser').removeClass('show');
                    operator.find('.ruleset-op-one').toggleClass('hide', op == 'And').toggleClass('d-flex', op != 'And');
                    operator.find('.ruleset-op-all').toggleClass('hide', op != 'And').toggleClass('d-flex', op == 'And');
                }
            }
        });

        return false;
    });

    // Change rule operator.
    $(document).on('click', 'div.rule-operator .dropdown-item', function () {
        var item = $(this);
        var operator = item.closest('.rule-operator');
        operator.data("value", item.data("value"));
        operator.find(".btn")
            .html('<span class="text-truncate">' + item.text() + '</span>')
            .attr('title', item.text());
        enableRuleValueControl(item);
        Smartstore.Admin.Rules.onRuleValueChanged();
    });

    // Change state of save rules button.
    $(document).on('change', ':input[name^="rule-value-"]', function () {
        Smartstore.Admin.Rules.onRuleValueChanged();
    });

    $(document).on('change.datetimepicker', '.datepicker-rule-value', function () {
        Smartstore.Admin.Rules.onRuleValueChanged();
    });

    // Save rules.
    $(document).on('click', 'button.ruleset-save', function () {
        var ruleData = getRuleData();

        $.ajax({
            cache: false,
            url: root.data('url-updaterules'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleData
            },
            type: 'POST',
            success: function (result) {
                if (result.Success) {
                    location.reload();
                }
                else if (!_.isEmpty(result.Message)) {
                    displayNotification(result.Message, 'error');
                }
            }
        });

        return false;
    });

    // Add rule.
    $(document).on('change', '.r-add-rule', function () {
        var select = $(this);
        var ruleType = select.val();
        if (!ruleType)
            return;

        var parentSet = select.closest('.ruleset');

        $.ajax({
            cache: false,
            type: 'POST',
            url: root.data('url-addrule'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleSetId: parentSet.data('ruleset-id'),
                ruleType
            },
            success: function (html) {
                appendToRuleSetBody(parentSet, html);
                select.val('').trigger('change');
            }
        });

        return false;
    });

    // Delete rule.
    $(document).on('click', '.r-delete-rule', function () {
        var rule = $(this).closest('.rule');

        $.ajax({
            cache: false,
            type: 'POST',
            url: root.data('url-deleterule'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleId: rule.data('rule-id')
            },
            success: function (result) {
                if (result.Success) {
                    rule.remove();
                    $('#excute-result').addClass('hide');
                }
            }
        });

        return false;
    });

    // Execute rule.
    $(document).on('click', '#execute-rules', function () {
        var ruleSet = $(".ruleset-root > .ruleset")

        $.ajax({
            cache: false,
            type: 'POST',
            url: $(this).attr('href'),
            data: {
                scope: root.data('scope'),
                entityId: root.data('entity-id'),
                ruleSetId: ruleSet.data('ruleset-id')
            },
            success: function (result) {
                $('#excute-result')
                    .html(result.Message)
                    .removeClass('hide alert-warning alert-danger')
                    .addClass(result.Success ? 'alert-warning' : 'alert-danger');
            }
        });

        return false;
    });

    // Ruleset hover
    var hoveredRuleset;
    root.on('mousemove', function (e) {
        var ruleset = $(e.target).closest('.ruleset');
        if (ruleset && ruleset.get(0) !== hoveredRuleset) {
            root.find('.ruleset').removeClass('hover');
            ruleset.addClass('hover');
            hoveredRuleset = ruleset.get(0);
        }
    });
    root.on('mouseleave', function (e) {
        root.find('.ruleset').removeClass('hover');
        hoveredRuleset = null;
    });

    //$(document).on('mouseenter', '.ruleset', function () {
    //    root.find('.ruleset').removeClass('hover');
    //    $(this).addClass('hover');
    //});
    //$(document).on('mouseleave', '.ruleset', function (e) {
    //    $(this).removeClass('hover');
    //    var target = $(e.target).closest('.ruleset');
    //    if (target.length) {
    //        target.addClass('.hover');
    //    }
    //    console.log(e.currentTarget, e.relatedTarget);
    //});

})(jQuery, window, document);