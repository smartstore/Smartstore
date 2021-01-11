namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductVariantAttributeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether this variant attribute should have values.
        /// </summary>
        /// <param name="productVariantAttribute">Product variant attribute.</param>
        /// <returns>A value indicating whether this variant attribute should have values.</returns>
        public static bool ShouldHaveValues(this ProductVariantAttribute productVariantAttribute)
        {
            if (productVariantAttribute == null)
            {
                return false;
            }

            return productVariantAttribute.AttributeControlType switch
            {
                AttributeControlType.TextBox or 
                AttributeControlType.MultilineTextbox or 
                AttributeControlType.Datepicker or 
                AttributeControlType.FileUpload => false,
                _ => true,  // All other attribute control types support values.
            };
        }
    }
}
