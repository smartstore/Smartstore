namespace Smartstore.Data.Hooks
{
    /// <summary>
    /// Specifies importance of a hook.
    /// </summary>
    public enum HookImportance
    {
        // Hook can be ignored during long running processes.
        Normal,

        // Hook is important and should also run during long running processes.
        Important,

        // Hook instance should run in any case. Hooks that are required even during installation (e.g. AuditHook) should be essential.
        Essential
    }

    /// <summary>
    /// Indicates that a hook instance should run in any case, even when hooking has been turned off.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ImportantAttribute : Attribute
    {
        public ImportantAttribute()
        {
        }

        public ImportantAttribute(HookImportance importance)
        {
            Importance = importance;
        }

        /// <summary>
        /// Gets the importance of a hook class. Defaults to <see cref="HookImportance.Important"/>.
        /// </summary>
        public HookImportance Importance { get; } = HookImportance.Important;
    }
}
