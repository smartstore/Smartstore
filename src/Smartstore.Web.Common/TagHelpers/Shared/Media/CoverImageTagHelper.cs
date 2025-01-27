using System.Text.Encodings.Web;
using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Renders an image that is aligned inside its content box.
    /// </summary>
    [OutputElementHint("figure")]
    [HtmlTargetElement("cover-image", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class CoverImageTagHelper : ImageTagHelper
    {
        const string PositionAttributeName = "sm-position";
        const string EditUrlAttributeName = "sm-edit-url";
        const string IconPositionBottomAttributeName = "sm-icon-bottom";

        /// <summary>
        /// Specifies the position of the image inside its content box. Default = "center".
        /// See "object-position" CSS for valid values.
        /// </summary>
        [HtmlAttributeName(PositionAttributeName)]
        public string Position { get; set; } = "center";

        /// <summary>
        /// Specifies the URL that will be used to save the updated image position.
        /// </summary>
        [HtmlAttributeName(EditUrlAttributeName)]
        public string EditUrl { get; set; }

        /// <summary>
        /// Specifies whether the icon for dropdown is placed on the bottom. Default = false.
        /// </summary>
        [HtmlAttributeName(IconPositionBottomAttributeName)]
        public bool IconPositionBottom { get; set; }

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
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
            output.AppendCssClass("img-fluid horizontal-image media-edit-object");

            if (Position.HasValue())
            {
                output.AddCssStyle("object-position", Position);
            }

            // Image container.
            var figure = new TagBuilder("figure");
            figure.AddCssClass("cover-image-container");

            if (customer.IsAdmin())
            {
                var dropdown = CreateDropdown();
                figure.InnerHtml.AppendHtml(dropdown);
            }

            // Order: First the image then the dropdown.
            output.WrapElementWith(InnerHtmlPosition.Append, figure);
        }

        protected virtual TagBuilder CreateDropdown()
        {
            var icon = (TagBuilder)HtmlHelper.BootstrapIcon("arrows-move", htmlAttributes: new Dictionary<string, object>
            {
                ["class"] = "dropdown-icon bi-fw bi"
            });

            var btnLink = new TagBuilder("a");
            btnLink.Attributes["href"] = "javascript:;";
            btnLink.Attributes["class"] = "btn btn-clear-dark btn-no-border btn-sm btn-icon rounded-circle no-chevron dropdown-toggle cover-image-dropdown";
            btnLink.Attributes["title"] = T("Admin.Media.Editing.Align");
            btnLink.Attributes["data-toggle"] = "dropdown";
            btnLink.Attributes["data-placement"] = "top";
            btnLink.InnerHtml.AppendHtml(icon);

            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu dropdown-menu-right";
            dropdownUl.InnerHtml.AppendHtml(CreateDropdownItem("top", "Admin.Media.Editing.AlignTop", "align-top"));
            dropdownUl.InnerHtml.AppendHtml(CreateDropdownItem("center", "Admin.Media.Editing.AlignMiddle", "align-middle"));
            dropdownUl.InnerHtml.AppendHtml(CreateDropdownItem("bottom", "Admin.Media.Editing.AlignBottom", "align-bottom"));

            if (IconPositionBottom)
            {
                btnLink.AddCssStyle("bottom", 0);
            }

            var rootDiv = new TagBuilder("div");
            rootDiv.Attributes["class"] = "d-flex cover-image-dropdown-root media-edit-root";
            rootDiv.Attributes["data-media-edit-url"] = EditUrl;
            rootDiv.InnerHtml.AppendHtml(btnLink);
            rootDiv.InnerHtml.AppendHtml(dropdownUl);

            return rootDiv;
        }

        protected virtual TagBuilder CreateDropdownItem(string position, string resourceKey, string iconName = null)
        {
            var model = new MediaEditModel
            {
                Commands =
                [
                    new() { Command = "object-position", Value = position }
                ]
            };

            var a = new TagBuilder("a");
            a.Attributes["href"] = "#";
            a.Attributes["class"] = "dropdown-item media-edit-command";
            a.Attributes["title"] = T(resourceKey + ".Hint").Value;
            a.Attributes["data-media-edit"] = model.ToJson();
            // TODO: disable command of applied position

            if (iconName.HasValue())
            {
                a.InnerHtml.AppendHtml(HtmlHelper.BootstrapIcon(iconName, htmlAttributes: new { @class = "bi-fw" }));
            }

            a.InnerHtml.AppendHtml(T(resourceKey).Value);

            var li = new TagBuilder("li");
            li.InnerHtml.AppendHtml(a);
            return li;
        }
    }
}
