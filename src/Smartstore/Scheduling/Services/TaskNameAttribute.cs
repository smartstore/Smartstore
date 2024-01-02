namespace Smartstore.Scheduling
{
    /// <summary>
    /// Specifies the name of an <see cref="ITask"/> implementation class.
    /// The task will be registered with specified name in DI instead of the type's name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TaskNameAttribute : Attribute
    {
        public TaskNameAttribute(string name)
        {
            Guard.NotEmpty(name);
            Name = name;
        }

        public string Name { get; }
    }
}
