using Smartstore.Core.Catalog.Attributes;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering.Choices
{
    public abstract class ChoiceModel : EntityModelBase
    {
        public AttributeControlType AttributeControlType { get; set; }

        public string Alias { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TextPrompt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsRequired { get; set; }
        public bool IsDisabled { get; set; }

        public string CustomData { get; set; }

        /// <summary>
        /// Allowed file extensions for customer uploaded files
        /// </summary>
        public List<string> AllowedFileExtensions { get; set; } = [];

        /// <summary>
        /// Selected value for textboxes
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// Selected date value for datepicker
        /// </summary>
        public DateTime? SelectedDate { get; set; }
        public string UploadedFileGuid { get; set; }
        public string UploadedFileName { get; set; }

        public virtual List<ChoiceItemModel> Values { get; set; } = [];

        public abstract string BuildControlId();

        public virtual string GetLabel()
            => TextPrompt.NullEmpty() ?? Name;

        public virtual string GetDescription()
        {
            var containsImg = !Description.IsEmpty() && Description.Contains("<img");

            var desc = Description.RemoveHtml();
            if (containsImg || (desc.HasValue() && !desc.Trim().EqualsNoCase(GetLabel())))
            {
                return Description;
            }

            return null;
        }

        public virtual string GetFileUploadUrl(IUrlHelper url)
            => null;
    }
}
