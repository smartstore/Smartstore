(function ($) {

    $.fn.editableList = function () {
        return this.each(function () {
            const $editor = $(this);
            if ($editor.data("editableList"))
                return;

            const $items = $editor.children(".editable-list-items");
            const $empty = $editor.children(".editable-list-empty");
            const $add = $editor.children(".editable-list-add");
            const template = $editor.children("template")[0];
            // Read the configured disabled state from the inert template, unaffected by shop overrides.
            const inputDisabled = template.content.querySelector(".editable-list-value").disabled;
            const name = $editor.attr("data-field-name");
            const id = $editor.attr("data-field-id");
            const label = $editor.attr("data-field-label");
            const minItems = Number($editor.attr("data-min-items"));
            const maxItems = Number($editor.attr("data-max-items"));
            const readOnly = $editor.attr("data-read-only") === "true";
            const sortable = $editor.attr("data-sortable") === "true";
            const $override = $editor.closest(".multi-store-setting-group").find(".multi-store-override-option").first();
            const sorter = sortable ? $items.sortable({
                draggable: ".editable-list-item:not(.editable-list-removing)",
                handle: ".editable-list-handle",
                ghostClass: "sortable-ghost",
                animation: 150
            }).sortable("widget") : null;

            function getRows() {
                return $items.children(":not(.editable-list-removing)");
            }

            function isDisabled() {
                return inputDisabled || readOnly || ($override.length > 0 && !$override.prop("checked"));
            }

            // Keep array binding indices, labels and available actions in sync with the current rows.
            function update() {
                const $rows = getRows();
                const disabled = isDisabled();
                const storeDisabled = $override.length > 0 && !$override.prop("checked");
                const dragDisabled = disabled || $rows.length < 2;

                $empty.toggleClass("d-none", $rows.length > 0);

                $rows.each((index, row) => {
                    const $row = $(row);
                    const $input = $row.find(".editable-list-value");
                    $input.attr({
                        name: `${name}[${index}]`,
                        id: index === 0 ? id : `${id}_${index}`,
                        "aria-label": `${label} ${index + 1}`
                    });
                    // Read-only values remain selectable; only disabled or inherited values are excluded from posting.
                    $input.prop("disabled", inputDisabled || storeDisabled);
                    $row.find(".editable-list-remove").prop("disabled", disabled || $rows.length <= minItems);
                    $row.find(".editable-list-handle").prop("disabled", dragDisabled)
                        .css("cursor", dragDisabled ? "default" : "move");
                });

                $add.prop("disabled", disabled || $rows.length >= maxItems);

                sorter?.option("disabled", disabled);
            }

            // Commit the unbound add field. Enter keeps input focus; blur must not move focus back.
            function addItem(focus) {
                if (isDisabled() || getRows().length >= maxItems) {
                    return;
                }

                const value = $add.val().trim();
                if (!value) {
                    return;
                }

                const $duplicate = getRows().find(".editable-list-value")
                    .filter((_, input) => input.value.trim().toLowerCase() === value.toLowerCase()).first();

                if ($duplicate.length) {
                    $add.val("");
                    if (focus) {
                        $duplicate.trigger("focus");
                    }
                    return;
                }

                $items.append(template.content.cloneNode(true));
                const $input = $items.children().last().find(".editable-list-value").val(value);

                $add.val("");
                update();

                if (focus) {
                    ($add.prop("disabled") ? $input : $add).trigger("focus");
                }

                $editor.trigger("change");
            }

            // Enter adds an entry instead of submitting the form, but must not interrupt IME composition.
            $add.on("keydown", (e) => {
                if (e.key !== "Enter" || e.originalEvent?.isComposing) {
                    return;
                }
                e.preventDefault();
                addItem(true);
            }).on("blur", () => addItem(false));

            // Also commit pending text when submission occurs without leaving the add field.
            $editor.closest("form").on("submit", () => addItem(false));

            $items.on("click", ".editable-list-remove", (e) => {
                if (isDisabled()) {
                    return;
                }

                const $row = $(e.currentTarget).closest(".editable-list-item");
                const $rows = getRows();

                if ($row.hasClass("editable-list-removing") || $rows.length <= minItems) {
                    return;
                }

                const index = $rows.index($row);
                // Exclude the row from posting and limit checks immediately, while its collapse animation finishes.
                $row.addClass("editable-list-removing")
                    .find(":input")
                    .prop("disabled", true)
                    .removeAttr("name id");

                $row.slideUp(150, () => $row.remove());
                update();

                const $next = getRows().eq(Math.min(index, $rows.length - 2));
                ($next.length ? $next.find(".editable-list-value") : $add).trigger("focus");
                $editor.trigger("change");
            });

            $items.on("sort", () => {
                update();
                $editor.trigger("change");
            }).on("keydown", ".editable-list-handle", (e) => {
                if (isDisabled() || !sorter || (e.key !== "ArrowUp" && e.key !== "ArrowDown")) {
                    return;
                }
                e.preventDefault();

                const $row = $(e.currentTarget).closest(".editable-list-item");
                const $neighbor = e.key === "ArrowUp"
                    ? $row.prevAll(":not(.editable-list-removing)").first()
                    : $row.nextAll(":not(.editable-list-removing)").first();

                if (!$neighbor.length) {
                    return;
                }

                // Use the same movement animation for keyboard reordering as for drag-and-drop.
                sorter.captureAnimationState();

                if (e.key === "ArrowUp") {
                    $neighbor.before($row);
                } else {
                    $neighbor.after($row);
                }

                update();
                sorter.animateAll();
                $(e.currentTarget).trigger("focus");
                $editor.trigger("change");
            });

            if ($override.length) {
                $override.on("change", update);
                // The bulk override toggle does not raise change events on the individual checkboxes.
                $("#override-settings").on("change", function () {
                    $override.prop("checked", this.checked);
                    update();
                });
            }

            // Create the minimum number of fields before calculating the initial editor state.
            for (let i = $items.children().length; i < minItems; i++) {
                $items.append(template.content.cloneNode(true));
            }

            $editor.data("editableList", true);
            update();
        });
    };
})(jQuery);