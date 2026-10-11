using System.Text.Encodings.Web;
using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Utilities;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared;

/*
TODO: (mg) Image positioning (Review wip)
-----------------------------------------
- Only "object-fit: none" can be positioned with object-position
  RE: No. See this example https://www.sitepoint.com/using-css-object-fit-object-position-properties/#settingthepositionofimageswithobjectposition
- INSUFFICIENT pos args! Must support 9 options: left top, top, right top, left center, center, right center, left bottom, bottom, right bottom
  RE: Blog and news images always fills the entire width of its content box. An horizontal alignment would have no effect here. These images can only be aligned vertically.
- Pos UI: 9 buttons, 3 in a row, each button represents an anchor.
*/

/// <summary>
/// Renders an image that is aligned inside its content box.
/// </summary>
[OutputElementHint("figure")]
[HtmlTargetElement("cover-image", TagStructure = TagStructure.NormalOrSelfClosing)]
public class CoverImageTagHelper : ImageTagHelper
{
    const string PositionAttributeName = "sm-position";
    const string EditUrlAttributeName = "sm-edit-url";

    /// <summary>
    /// Specifies the position of the image inside its content box. Empty by default (image is centered).
    /// See "object-position" CSS for valid values.
    /// </summary>
    [HtmlAttributeName(PositionAttributeName)]
    public string ImagePosition { get; set; }

    /// <summary>
    /// Specifies the URL that will be used to save the updated image position.
    /// </summary>
    [HtmlAttributeName(EditUrlAttributeName)]
    public string EditUrl { get; set; }

    protected override async Task ProcessMediaAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Src.IsEmpty())
        {
            output.SuppressOutput();
            return;
        }

        var workContext = ViewContext.HttpContext.GetServiceScope().Resolve<IWorkContext>();
        var customer = workContext.CurrentCustomer;

        base.ProcessMedia(context, output);

        // Image.
        output.TagName = "img";
        output.RemoveClass("file-img", HtmlEncoder.Default);
        // Only images with horizontal orientation supported yet (100% width, fixed height).
        output.AppendCssClass("img-fluid horizontal-image media-edit-object");

        if (ImagePosition.HasValue())
        {
            output.AddCssStyle("object-position", ImagePosition);
        }

        // Image container.
        var figure = new TagBuilder("figure");
        figure.AddCssClass("cover-image-container");

        if (EditUrl.HasValue() && customer.IsAdmin())
        {
            var id = "media-edit-" + CommonHelper.GenerateRandomDigitCode(8);
            figure.Attributes["id"] = id;

            var widgetProvider = ViewContext.HttpContext.RequestServices.GetRequiredService<IWidgetProvider>();
            var dropdown = await CreateDropdownAsync(id);
            widgetProvider.RegisterHtml("admin_actions", dropdown);
        }

        var caption = Alt ?? Title;
        if (caption.HasValue())
        {
            var figCaption = new TagBuilder("figcaption");
            figCaption.Attributes["class"] = "sr-only";
            figCaption.InnerHtml.AppendHtml(caption);
            figure.InnerHtml.AppendHtml(figCaption);
        }

        // Order: First the image then the dropdown.
        output.WrapElementWith(InnerHtmlPosition.Append, figure);
    }

    protected virtual async Task<TagBuilder> CreateDropdownAsync(string id)
    {
        var toggle = new TagBuilder("a");
        toggle.Attributes["href"] = "javascript:;";
        toggle.Attributes["class"] = "btn btn-sm btn-secondary btn-icon rounded-circle no-chevron dropdown-toggle cover-image-dropdown";
        toggle.Attributes["title"] = T("Admin.Media.Editing.Align");
        toggle.Attributes["data-toggle"] = "dropdown";
        toggle.Attributes["data-placement"] = "top";

        var icon = await HtmlHelper.IconAsync("fa:arrow-down-up-across-line!/s");
        toggle.InnerHtml.AppendHtml(icon);

        var dropdownUl = new TagBuilder("ul");
        dropdownUl.Attributes["class"] = "dropdown-menu dropdown-menu-check dropdown-menu-right media-edit-dropdown";
        dropdownUl.InnerHtml.AppendHtml(await CreateDropdownItemAsync("center top", "Admin.Media.Editing.AlignTop", "arrow-up"));
        dropdownUl.InnerHtml.AppendHtml(await CreateDropdownItemAsync(string.Empty, "Admin.Media.Editing.AlignMiddle", "unfold"));
        dropdownUl.InnerHtml.AppendHtml(await CreateDropdownItemAsync("center bottom", "Admin.Media.Editing.AlignBottom", "arrow-down"));

        var rootDiv = new TagBuilder("div");
        rootDiv.Attributes["data-media-edit-url"] = EditUrl;
        rootDiv.Attributes["data-media-edit-id"] = id;
        rootDiv.Attributes["class"] = "cover-image-dropdown-root";
        rootDiv.InnerHtml.AppendHtml(toggle);
        rootDiv.InnerHtml.AppendHtml(dropdownUl);

        return rootDiv;
    }

    protected virtual async Task<TagBuilder> CreateDropdownItemAsync(string position, string resourceKey, string iconName = null)
    {
        var model = new MediaEditModel
        {
            Commands =
            [
                new() { Name = "object-position", Value = position }
            ]
        };

        var a = new TagBuilder("a");
        a.Attributes["href"] = "#";
        a.Attributes["class"] = "dropdown-item media-edit-command";
        a.Attributes["role"] = "button";
        a.Attributes["data-media-edit"] = model.ToJson();

        if (position.EqualsNoCase(ImagePosition))
        {
            a.AppendCssClass("checked");
            a.Attributes["aria-pressed"] = "true";
        }

        if (iconName.HasValue())
        {
            a.InnerHtml.AppendHtml(await HtmlHelper.IconAsync(iconName, options: new() { FixedWidth = true }));
            var label = new TagBuilder("span");
            label.InnerHtml.Append(T(resourceKey).Value);
            a.InnerHtml.AppendHtml(label);
        }
        else
        {
            a.InnerHtml.Append(T(resourceKey).Value);
        }

        var li = new TagBuilder("li");
        li.InnerHtml.AppendHtml(a);
        return li;
    }
}