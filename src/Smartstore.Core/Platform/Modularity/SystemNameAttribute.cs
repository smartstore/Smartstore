namespace Smartstore.Engine.Modularity
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemNameAttribute : Attribute
    {
        public SystemNameAttribute(string name)
        {
            Guard.NotEmpty(name, nameof(name));
            Name = name;
        }

        public string Name { get; set; }
    }
}