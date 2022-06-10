namespace Smartstore.Web.Modelling
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LocalizedDisplayAttribute : Attribute
    {
        public LocalizedDisplayAttribute(string nameKey)
        {
            Name = nameKey;
        }

        public LocalizedDisplayAttribute(string nameKey, string descriptionKey)
        {
            Name = nameKey;
            Description = descriptionKey;
        }

        /// <summary>
        /// Gets or sets the resource key for UI display name used as field labels.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resource key for UI description used as tooltip or control hint.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the resource key for UI prompt mostly used as input placeholder.
        /// </summary>
        public string Prompt { get; set; }
    }
}
