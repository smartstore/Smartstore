namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Specifies the alias name of an <see cref="Enum"/> type
    /// to be used in translation resource keys instead of the enum's type name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public sealed class EnumAliasNameAttribute : Attribute
    {
        public EnumAliasNameAttribute(string name)
        {
            Guard.NotEmpty(name, nameof(name));
            Name = name;
        }

        public string Name { get; }
    }
}
