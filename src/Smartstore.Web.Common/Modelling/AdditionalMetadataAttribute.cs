using System;

namespace Smartstore.Web.Modelling
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class AdditionalMetadataAttribute : Attribute
    {
        public AdditionalMetadataAttribute(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the attribute.
        /// </summary>
        public object Value { get; set; }
    }
}
