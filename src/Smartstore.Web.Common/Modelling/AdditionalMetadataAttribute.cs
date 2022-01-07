namespace Smartstore.Web.Modelling
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AdditionalMetadataAttribute : Attribute
    {
        public AdditionalMetadataAttribute(string name, object value)
        {
            Name = Guard.NotNull(name, nameof(name));
            Value = value;
        }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the value of the attribute.
        /// </summary>
        public object Value { get; }
    }
}
