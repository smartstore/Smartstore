(function ($) {

    $.fn.stringList = function () {
        return this.each(function () {
            const $editor = $(this);
            if ($editor.data("stringList")) return;

            const $items = $editor.children(".string-list-items");
            const $empty = $editor.children(".string-list-empty");
            const $add = $editor.children(".string-list-add");
            const template = $editor.children("template")[0];
            const inputDisabled = template.content.querySelector(".string-list-value").disabled;
            const name = $editor.attr("data-field-name");
            const id = $editor.attr("data-field-id");
            const label = $editor.attr("data-field-label");
            const minItems = Number($editor.attr("data-min-items"));
            const maxItems = Number($editor.attr("data-max-items"));
            const readOnly = $editor.attr("data-read-only") === "true";
            const $override = $editor.closest(".multi-store-setting-group").find(".multi-store-override-option").first();

            function isDisabled() {
                return inputDisabled || readOnly || ($override.length > 0 && !$override.prop("checked"));
            }

            function update() {
                const $rows = $items.children();
                const disabled = isDisabled();
                const storeDisabled = $override.length > 0 && !$override.prop("checked");
                $empty.toggleClass("d-none", $rows.length > 0);
                $rows.each((index, row) => {
                    const $row = $(row);
                    const $input = $row.find(".string-list-value");
                    $input.attr({
                        name: `${name}[${index}]`,
                        id: index === 0 ? id : `${id}_${index}`,
                        "aria-label": `${label} ${index + 1}`
                    });
                    $input.prop("disabled", inputDisabled || storeDisabled);
                    $row.find(".string-list-remove").prop("disabled", disabled || $rows.length <= minItems);
                    $row.find(".string-list-up").prop("disabled", disabled || index === 0);
                    $row.find(".string-list-down").prop("disabled", disabled || index === $rows.length - 1);
                });
                $add.prop("disabled", disabled || $rows.length >= maxItems);
            }

            $add.on("click", () => {
                if (isDisabled() || $items.children().length >= maxItems) return;
                $items.append(template.content.cloneNode(true));
                update();
                $items.children().last().find(".string-list-value").trigger("focus");
                $editor.trigger("change");
            });

            $items.on("click", ".string-list-remove, .string-list-up, .string-list-down", (e) => {
                if (isDisabled()) return;
                const $button = $(e.currentTarget);
                const $row = $button.closest(".string-list-item");
                let $focus = $button;
                if ($button.hasClass("string-list-remove")) {
                    if ($items.children().length <= minItems) return;
                    const index = $row.index();
                    $row.remove();
                    const $next = $items.children().eq(Math.min(index, $items.children().length - 1));
                    $focus = $next.length ? $next.find(".string-list-value") : $add;
                }
                else if ($button.hasClass("string-list-up")) {
                    $row.prev().before($row);
                }
                else {
                    $row.next().after($row);
                }
                update();
                $focus.trigger("focus");
                $editor.trigger("change");
            });

            if ($override.length) {
                $override.on("change", update);
                $("#override-settings").on("change", function () {
                    $override.prop("checked", this.checked);
                    update();
                });
            }

            for (let i = $items.children().length; i < minItems; i++) {
                $items.append(template.content.cloneNode(true));
            }

            $editor.data("stringList", true);
            update();
        });
    };

})(jQuery);
