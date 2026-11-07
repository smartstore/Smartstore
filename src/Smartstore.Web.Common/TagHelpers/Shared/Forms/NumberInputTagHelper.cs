using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.IconKit;
using Smartstore.Web.Models;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared;

public enum NumberInputStyle
{
    /// <summary>
    /// Up/Down chevrons on right, number left-aligned, postfix right-aligned.
    /// </summary>
    Default,

    /// <summary>
    /// Minus on left, Plus on right, number centered, postfix centered below number.
    /// </summary>
    Centered
}

/// <summary>Edits scalar numbers and Amount values with optional unit selection.</summary>
[HtmlTargetElement("input", Attributes = "[type=number]")]
public class NumberInputTagHelper : SmartTagHelper
{
    const string ForAttributeName = "asp-for";
    const string ValueAttributeName = "value";
    const string UnitsAttributeName = "sm-units";
    const string ControlSizeAttributeName = "sm-control-size";
    const string RequiredAttributeName = "sm-required";
    const string PlaintextAttributeName = "sm-plaintext";
    const string PrefixAttributeName = "sm-prefix";
    const string PostfixAttributeName = "sm-postfix";
    const string DecimalsAttributeName = "sm-decimals";
    const string StyleAttributeName = "sm-numberinput-style";
    const string GroupClassAttributeName = "sm-numberinput-group-class";
    const string NativeAttributeName = "sm-native";

    private readonly ILocalizationService _localizationService;
    private readonly IHtmlGenerator _generator;
    private readonly IIconRenderer _icons;

    public NumberInputTagHelper(ILocalizationService localizationService)
        : this(localizationService, null, null)
    {
    }

    /// <summary>Creates a number editor using the MVC and icon renderers.</summary>
    [ActivatorUtilitiesConstructor]
    public NumberInputTagHelper(ILocalizationService localizationService, IHtmlGenerator generator, IIconRenderer icons)
    {
        _localizationService = localizationService;
        _generator = generator;
        _icons = icons;
    }

    private IHtmlGenerator Generator => _generator ?? HtmlGenerator;

    // Must come before FormControlTagHelper because of sizing issues.
    public override int Order => 0;

    /// <summary>
    /// The text which will be displayed before the number input as a plain addon.
    /// </summary>
    [HtmlAttributeName(PrefixAttributeName)]
    public string Prefix { get; set; }

    /// <summary>
    /// The text which will be displayed inside the number input as a post fix.
    /// </summary>
    /// <remarks>
    /// Retained for compatibility; use Units for new code. A nonempty Units collection takes precedence.
    /// Otherwise, Postfix is passed through as a single static unit. This member is not marked obsolete.
    /// </remarks>
    [HtmlAttributeName(PostfixAttributeName)]
    public string Postfix { get; set; }

    /// <summary>
    /// Number of decimal digits.
    /// </summary>
    [HtmlAttributeName(DecimalsAttributeName)]
    public uint Decimals { get; set; }

    /// <summary>
    /// Style of numberinput control.
    /// </summary>
    [HtmlAttributeName(StyleAttributeName)]
    public NumberInputStyle Style { get; set; }

    /// <summary>
    /// CSS class of parent group tag.
    /// </summary>
    [HtmlAttributeName(GroupClassAttributeName)]
    public string GroupClass { get; set; }

    /// <summary>
    /// Suppresses processing of the <see cref="NumberInputTagHelper"/> and renders the control browser native.
    /// </summary>
    [HtmlAttributeName(NativeAttributeName)]
    public bool Native { get; set; }

    /// <summary>Gets or sets the scalar or Amount expression. Its ID also remains the numeric input's label target.</summary>
    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; }

    /// <summary>Gets or sets an explicit unbound value. When asp-for is supplied, MVC determines the value instead.</summary>
    [HtmlAttributeName(ValueAttributeName)]
    public object Value { get; set; }

    /// <summary>Gets or sets allowed unit identifiers and labels. Nonempty units take precedence over Postfix.</summary>
    [HtmlAttributeName(UnitsAttributeName)]
    public IEnumerable<AmountUnit> Units { get; set; }

    /// <summary>Gets or sets the control size.</summary>
    [HtmlAttributeName(ControlSizeAttributeName)]
    public ControlSize ControlSize { get; set; } = ControlSize.Medium;

    protected override string GenerateTagId(TagHelperContext context) => null;

    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        For ??= context.AllAttributes[ForAttributeName]?.Value as ModelExpression;
        var bound = For != null;
        var isAmount = bound && typeof(Amount).IsAssignableFrom(For.Metadata.ModelType);
        var valueExplorer = isAmount ? For.ModelExplorer.GetExplorerForProperty(nameof(Amount.Value)) : For?.ModelExplorer;
        var amount = Value as Amount;
        if (Native && !isAmount && amount == null)
        {
            return;
        }

        var units = Units?.ToArray() ?? [];
        var htmlPostfix = units.Length == 0 && Postfix.HasValue();
        if (htmlPostfix)
        {
            units = [new AmountUnit(Postfix)];
        }
        if (units.Any(x => x == null || string.IsNullOrEmpty(x.Value))
            || units.Select(x => x.Value).Distinct(StringComparer.Ordinal).Count() != units.Length)
        {
            throw new ArgumentException("Units must have unique, nonempty identifiers.", nameof(Units));
        }

        var value = bound ? valueExplorer.Model : amount?.Value ?? Value;
        var valueType = valueExplorer?.Metadata.UnderlyingOrModelType ?? value?.GetType() ?? typeof(decimal);
        if ((!bound || isAmount) && (valueType.IsEnum || Type.GetTypeCode(valueType) is not
            (TypeCode.Decimal or TypeCode.Double or TypeCode.Single or TypeCode.Byte or TypeCode.SByte
            or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64)))
        {
            throw new ArgumentException("The value must be a number or Amount.", bound ? nameof(For) : nameof(Value));
        }

        if (bound && !isAmount && units.Length > 1)
        {
            throw new ArgumentException("Selectable units require an Amount property.", nameof(For));
        }

        var valueName = isAmount ? ModelNames.CreatePropertyModelName(For.Name, nameof(Amount.Value)) : For?.Name;
        var fullName = bound ? ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(valueName) : null;
        var state = default(ModelStateEntry);
        if (bound)
        {
            ViewContext.ViewData.ModelState.TryGetValue(fullName, out state);
        }
        var attemptedValue = state?.AttemptedValue;
        var invalidNumber = attemptedValue != null && attemptedValue.Length > 0
            && !TryNormalizeNumber(attemptedValue, out _);
        var enhanced = !Native && !invalidNumber;

        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in output.Attributes)
        {
            if (!attribute.Name.StartsWith("sm-", StringComparison.OrdinalIgnoreCase)
                && !attribute.Name.StartsWith("asp-", StringComparison.OrdinalIgnoreCase)
                && attribute.Name != "type"
                && (!bound || attribute.Name is not ("name" or ValueAttributeName))
                && !(attribute.Value is false && attribute.Name is "disabled" or "readonly" or "required"))
            {
                attributes[attribute.Name] = attribute.Value;
            }
        }

        attributes["type"] = invalidNumber ? "text" : "number";
        if (invalidNumber)
        {
            attributes.TryAdd("inputmode", "decimal");
        }
        attributes.TryAdd("id", TagBuilder.CreateSanitizedId(
            bound ? ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name) : "numberinput-" + context.UniqueId,
            Generator.IdAttributeDotReplacement));
        if (valueExplorer?.Metadata.IsNullableValueType == true)
        {
            attributes.TryAdd("placeholder", _localizationService.GetResource("Common.Empty", logIfNotFound: false, returnEmptyIfNotFound: true));
        }

        var input = bound
            ? Generator.GenerateTextBox(ViewContext, valueExplorer, valueName, valueExplorer.Model, null, attributes)
            : new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
        if (!bound)
        {
            input.MergeAttributes(attributes);
        }
        if (attemptedValue != null && TryNormalizeNumber(attemptedValue, out var normalized))
        {
            input.Attributes[ValueAttributeName] = normalized;
        }
        else if (attemptedValue == null)
        {
            input.Attributes[ValueAttributeName] = Convert.ToString(bound && !isAmount ? value.Convert<decimal?>() : value, CultureInfo.InvariantCulture);
        }

        input.AddCssClass("form-control");
        input.Attributes["data-numberinput-value"] = string.Empty;
        input.Attributes.TryAdd("data-decimals", Decimals.ToString(CultureInfo.InvariantCulture));
        var required = context.AllAttributes[RequiredAttributeName]?.Value.Convert<bool?>() ?? valueExplorer?.Metadata.IsRequired;

        if (required == true)
        {
            input.Attributes["required"] = "required";
            input.Attributes["aria-required"] = "true";
        }

        if (state?.Errors.Count > 0)
        {
            input.Attributes["aria-invalid"] = "true";
        }

        var disabled = input.Attributes.ContainsKey("disabled");
        var locked = disabled || input.Attributes.ContainsKey("readonly");
        var defaultStyle = Style == NumberInputStyle.Default || !enhanced;
        var group = Element("div", "edit-control " + GroupClass);

        group.Attributes["data-numberinput"] = string.Empty;
        group.Attributes["data-editor"] = "number";
        group.AddCssClass(enhanced ? "numberinput-group" : "numberinput-native");
        group.AddCssClass("numberinput-" + (defaultStyle ? "default" : "centered"));

        var plaintext = context.AllAttributes[PlaintextAttributeName]?.Value.Convert<bool>() == true;
        var size = ControlSize == ControlSize.Small ? "sm" : ControlSize == ControlSize.Large ? "lg" : null;
        if (size == null && !context.AllAttributes.ContainsName(ControlSizeAttributeName)
            && ViewContext.ViewData.TryGetValue("size", out var inheritedSize) && inheritedSize is string sizeText
            && sizeText is "sm" or "lg")
        {
            size = sizeText;
        }

        using (var inputClasses = input.GetClassList())
        {
            if (size == null && !context.AllAttributes.ContainsName(ControlSizeAttributeName))
            {
                size = inputClasses.Contains("form-control-sm") ? "sm" : inputClasses.Contains("form-control-lg") ? "lg" : null;
            }
            plaintext |= inputClasses.Contains("form-control-plaintext");
            inputClasses.Remove("form-control-sm", "form-control-lg");
            if (plaintext)
            {
                inputClasses.Remove("form-control");
                inputClasses.Add("form-control-plaintext");
                group.AddCssClass("numberinput-plaintext");
            }
        }
        if (size != null)
        {
            group.AddCssClass("numberinput-group-" + size);
        }

        context.Items["IsNumberInput"] = true;

        if (isAmount)
        {
            // Replace MVC's invariant marker for the root expression with one for Amount.Value.
            output.PostElement.Clear();
        }

        var needsInvariant = bound && !invalidNumber && (isAmount || output.PostElement.IsEmptyOrWhiteSpace);

        if (enhanced)
        {
            input.AddCssClass("numberinput");
            input.Attributes.TryAdd("data-editor", "numberinput");
            output.PostElement.AppendHtml(Element("span", "numberinput-formatted"));
        }

        // Keep the actual input in the Razor pipeline for FormControlTagHelper and other helpers.
        output.Attributes.Clear();
        foreach (var attribute in input.Attributes)
        {
            output.Attributes.SetAttribute(attribute.Key, attribute.Value);
        }

        output.TagName = "input";
        if (defaultStyle)
        {
            output.WrapElementWith(Element("div", "numberinput-number"));
            if (!string.IsNullOrEmpty(Prefix))
            {
                group.InnerHtml.AppendHtml(CreatePrefix(input.Attributes["id"]));
            }
        }

        if (needsInvariant)
        {
            var invariant = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            invariant.Attributes["type"] = "hidden";
            invariant.Attributes["name"] = "__Invariant";
            invariant.Attributes[ValueAttributeName] = fullName;
            CopyFormAssociation(input, invariant);
            output.PostElement.AppendHtml(invariant);
        }

        var unitInput = default(TagBuilder);
        if (isAmount)
        {
            var unitExplorer = For.ModelExplorer.GetExplorerForProperty(nameof(Amount.Unit));
            unitInput = Generator.GenerateHidden(ViewContext, unitExplorer, ModelNames.CreatePropertyModelName(For.Name, nameof(Amount.Unit)),
                unitExplorer.Model ?? units.FirstOrDefault()?.Value, false,
                new Dictionary<string, object> { ["data-numberinput-unit"] = string.Empty });
            CopyFormAssociation(input, unitInput);
        }
        else if (!bound && units.Length > 1)
        {
            unitInput = new TagBuilder("input") { TagRenderMode = TagRenderMode.SelfClosing };
            unitInput.Attributes["type"] = "hidden";
            unitInput.Attributes["data-numberinput-unit"] = string.Empty;
            unitInput.Attributes[ValueAttributeName] = amount?.Unit ?? units[0].Value;
            CopyFormAssociation(input, unitInput);
        }

        if (unitInput != null)
        {
            output.PostElement.AppendHtml(unitInput);
        }

        if (units.Length > 0)
        {
            group.AddCssClass("has-postfix");
            var selected = unitInput?.Attributes.GetValueOrDefault(ValueAttributeName) ?? units[0].Value;
            output.PostElement.AppendHtml(CreatePostfix(units, selected, input.Attributes["id"], locked, htmlPostfix));
        }

        if (enhanced)
        {
            output.PostElement.AppendHtml(await CreateSteppersAsync(defaultStyle, locked));
        }

        output.WrapElementWith(group);

        if (!defaultStyle && !string.IsNullOrEmpty(Prefix))
        {
            var prefixGroup = Element("div", "numberinput-prefix-group");
            if (size != null)
            {
                prefixGroup.AddCssClass("numberinput-group-" + size);
            }
            prefixGroup.InnerHtml.AppendHtml(CreatePrefix(input.Attributes["id"]));
            output.WrapElementWith(prefixGroup);
        }
    }

    private TagBuilder CreatePrefix(string inputId)
    {
        var prepend = Element("div", "input-group-prepend input-addon-plain");
        var label = Element(string.IsNullOrEmpty(inputId) ? "span" : "label", "input-group-text");
        if (!string.IsNullOrEmpty(inputId))
        {
            label.Attributes["for"] = inputId;
        }
        label.InnerHtml.Append(Prefix);
        prepend.InnerHtml.AppendHtml(label);
        return prepend;
    }

    private TagBuilder CreatePostfix(AmountUnit[] units, string selected, string inputId, bool locked, bool htmlPostfix)
    {
        var postfix = Element(units.Length > 1 ? "div" : "span", "numberinput-postfix numberinput-unit-selector");
        if (units.Length == 1)
        {
            if (htmlPostfix)
            {
                // Legacy Postfix callers may already supply HTML-encoded content.
                postfix.InnerHtml.AppendHtml(units[0].Text);
            }
            else
            {
                postfix.InnerHtml.Append(units[0].Text);
            }
            return postfix;
        }

        var label = Element("span", "numberinput-unit-label");
        label.InnerHtml.Append(units.FirstOrDefault(x => x.Value == selected)?.Text ?? selected);
        postfix.AddCssClass("dropdown");
        var button = Element("button", "btn btn-xs btn-plain fwn dropdown-toggle numberinput-unit-toggle");
        button.Attributes["type"] = "button";
        button.Attributes["id"] = inputId + "_unit_toggle";
        button.Attributes["data-toggle"] = "dropdown";
        button.Attributes["aria-haspopup"] = "true";
        button.Attributes["aria-expanded"] = "false";
        if (locked)
        {
            button.Attributes["disabled"] = "disabled";
        }
        button.InnerHtml.AppendHtml(label);
        postfix.InnerHtml.AppendHtml(button);

        var menu = Element("div", "dropdown-menu dropdown-menu-right");
        menu.Attributes["aria-labelledby"] = button.Attributes["id"];
        foreach (var unit in units)
        {
            var item = Element("button", "dropdown-item");
            item.Attributes["type"] = "button";
            item.Attributes["data-numberinput-unit-option"] = unit.Value;
            item.Attributes["aria-pressed"] = unit.Value == selected ? "true" : "false";
            if (unit.Value == selected)
            {
                item.AddCssClass("active");
            }
            if (locked)
            {
                item.Attributes["disabled"] = "disabled";
            }
            item.InnerHtml.Append(unit.Text);
            menu.InnerHtml.AppendHtml(item);
        }
        postfix.InnerHtml.AppendHtml(menu);
        return postfix;
    }

    private async Task<IHtmlContent> CreateSteppersAsync(bool stacked, bool locked)
    {
        var content = new HtmlContentBuilder();
        var container = stacked ? Element("div", "numberinput-stepper-stack") : null;
        foreach (var direction in stacked ? new[] { "up", "down" } : new[] { "down", "up" })
        {
            var iconName = Style == NumberInputStyle.Centered ? (direction == "down" ? "minus" : "plus") : "chevron-" + direction;
            var stepper = Element(stacked ? "button" : "a", "numberinput-stepper numberinput-" + direction);
            if (stacked)
            {
                stepper.AddCssClass("btn btn-plain");
                stepper.Attributes["type"] = "button";
                if (locked)
                {
                    stepper.Attributes["disabled"] = "disabled";
                }
            }
            else
            {
                stepper.Attributes["href"] = "javascript:;";
            }
            stepper.Attributes["tabindex"] = "-1";
            stepper.Attributes["aria-hidden"] = "true";
            var icon = _icons == null
                ? await HtmlHelper.IconAsync(iconName)
                : await _icons.RenderAsync(IconExpression.Parse(iconName));
            if (icon != null)
            {
                stepper.InnerHtml.AppendHtml(icon);
            }
            if (stacked)
            {
                container.InnerHtml.AppendHtml(stepper);
            }
            else
            {
                content.AppendHtml(stepper);
            }
        }
        return stacked ? container : content;
    }

    private static TagBuilder Element(string tag, string css)
    {
        var element = new TagBuilder(tag);
        element.AddCssClass(css);
        return element;
    }

    private static void CopyFormAssociation(TagBuilder input, TagBuilder hidden)
    {
        foreach (var name in new[] { "disabled", "form" })
        {
            if (input.Attributes.TryGetValue(name, out var value))
            {
                hidden.Attributes[name] = value;
            }
        }
    }

    private static bool TryNormalizeNumber(string text, out string normalized)
    {
        var styles = NumberStyles.Float;

        if (decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(text, styles | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
        {
            normalized = value.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if ((double.TryParse(text, styles, CultureInfo.InvariantCulture, out var doubleValue)
            || double.TryParse(text, styles | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out doubleValue)) && double.IsFinite(doubleValue))
        {
            normalized = doubleValue.ToString("R", CultureInfo.InvariantCulture);
            return true;
        }

        normalized = text;
        return false;
    }
}
