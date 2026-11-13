// Badge expressions and the shared admin badge editor.
(function ($) {
    'use strict';

    const colors = ['primary', 'secondary', 'success', 'info', 'warning', 'danger', 'light', 'dark'];
    const modifiers = ['subtle', 'outline', 'ring', 'pill'];

    function parse(expression) {
        const tokens = new Set((expression || '').trim().toLowerCase().split(/\s+/).filter(Boolean));
        if (!tokens.size) return { color: null, modifiers: [] };

        const selected = modifiers.filter(x => tokens.has(x));
        return {
            color: colors.find(x => tokens.has(x)) || 'secondary',
            modifiers: tokens.has('outline') ? selected.filter(x => x !== 'subtle') : selected
        };
    }

    function serialize(state) {
        return state.color ? [state.color, ...modifiers.filter(x => state.modifiers.includes(x))].join(' ') : '';
    }

    const expression = {
        parse,
        normalize: value => serialize(parse(value)),
        toClassName: value => ['badge', ...expression.normalize(value).split(' ').filter(Boolean).map(x => 'badge-' + x)].join(' ')
    };
    Smartstore.BadgeExpression = expression;

    $.fn.badgeStyles = function () {
        return this.each(function () {
            const select = $(this);
            if (select.data('badgeStyles')) return;

            const template = document.getElementById(this.id + '-tools');
            const badgeText = template.dataset.badgeText;
            // The hidden model field posts the complete expression.
            const expressionField = document.getElementById(select.attr('data-expression-field'));
            const colorOf = item => colors.includes(String(item?.id).toLowerCase())
                ? String(item.id).toLowerCase()
                : String(item?.text || '').toLowerCase();
            let initial = expressionField?.value;
            if (initial != null && /^[0-7]$/.test(initial)) {
                // Older price settings may contain the numeric enum value.
                initial = ['secondary', 'primary', 'success', 'info', 'warning', 'danger', 'light', 'dark'][Number(initial)];
            }
            let state = parse(initial ?? (select.val() ? colorOf({ id: select.val(), text: select.find(':selected').text() }) : ''));
            state.color ??= 'secondary';
            if (expressionField) {
                selectColor();
                expressionField.value = serialize(state);
            }
            let toolbar;

            function syncTools() {
                if (!toolbar) return;
                toolbar.find('[data-modifier]').each(function () {
                    const active = state.modifiers.includes(this.dataset.modifier);
                    $(this).toggleClass('active', active).attr('aria-pressed', String(active))
                        .prop('disabled', !state.color);
                });
            }

            function selectColor() {
                const option = [...select[0].options].find(x => colorOf({ id: x.value, text: x.text }) === state.color);
                select.val(option?.value || '');
            }

            function refresh() {
                if (expressionField) {
                    $(expressionField).val(serialize(state)).trigger('change');
                }
                select.trigger('change.select2');
                syncTools();
                select.trigger('badge:change', [serialize(state)]);
            }

            function format(item) {
                if (!item.id) return item.text;
                const value = serialize({ color: colorOf(item), modifiers: state.modifiers });
                return $('<span class="select2-option w-100 align-items-center"></span>').text(item.text)
                    .append($('<span></span>').addClass(expression.toClassName(value) + ' ml-auto').text(badgeText));
            }

            select.selectWrapper({
                selectionToolbar: template,
                allowClear: false,
                templateResult: format,
                templateSelection: format
            });
            toolbar = select.data('select2').selection.$toolbar;
            toolbar.on('click.badgeStyles', '[data-modifier]', function () {
                const modifier = this.dataset.modifier;
                if (state.modifiers.includes(modifier)) {
                    state.modifiers = state.modifiers.filter(x => x !== modifier);
                } else {
                    // A deliberate selection replaces incompatible presentation modifiers.
                    const conflicts = modifier === 'outline' ? ['subtle']
                        : modifier === 'subtle' ? ['outline'] : [];
                    state.modifiers = state.modifiers.filter(x => !conflicts.includes(x));
                    state.modifiers.push(modifier);
                }
                refresh();
            });
            select.on('change.badgeStyles', function () {
                const item = select.select2('data')[0];
                state.color = item?.id ? parse(colorOf(item)).color : null;
                if (!state.color) state.modifiers = [];
                refresh();
            });

            select.data('badgeStyles', {
                getExpression: () => serialize(state),
                setExpression(value) {
                    state = parse(value);
                    state.color ??= 'secondary';
                    selectColor();
                    refresh();
                }
            });
            syncTools();
        });
    };
})(jQuery);
