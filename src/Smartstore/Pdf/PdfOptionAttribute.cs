using System;

namespace Smartstore.Pdf
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PdfOptionAttribute : Attribute
    {
        public PdfOptionAttribute(string name)
        {
            Guard.NotEmpty(name, nameof(name));

            Name = name;
        }

        public string Name { get; }
    }
}
