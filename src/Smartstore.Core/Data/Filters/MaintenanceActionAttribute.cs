namespace Smartstore.Core.Data
{
    /// <summary>
    /// Marker attribute for a (hidden) controller action that performs potentially long-running maintenance tasks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MaintenanceActionAttribute : Attribute
    {
    }
}
