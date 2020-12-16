namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents an attribute control type.
    /// </summary>
    public enum AttributeControlType
    {
        /// <summary>
        /// Dropdown list.
        /// </summary>
        DropdownList = 1,

        /// <summary>
        /// Radio list.
        /// </summary>
        RadioList = 2,

        /// <summary>
        /// Checkboxes.
        /// </summary>
        Checkboxes = 3,

        /// <summary>
        /// Text box.
        /// </summary>
        TextBox = 4,

        /// <summary>
        /// Multiline textbox.
        /// </summary>
        MultilineTextbox = 10,

        /// <summary>
        /// Datepicker.
        /// </summary>
        Datepicker = 20,

        /// <summary>
        /// File upload control.
        /// </summary>
        FileUpload = 30,

        /// <summary>
        /// Boxes.
        /// </summary>
        Boxes = 40
    }

    /// <summary>
    /// Represents a value type for product attributes.
    /// </summary>
    public enum ProductVariantAttributeValueType
    {
        /// <summary>
        /// Simple attribute value.
        /// </summary>
        Simple = 0,

        /// <summary>
        /// Linked product attribute value.
        /// </summary>
        ProductLinkage = 10
    }
}
