namespace Smartstore.Core.Security;

public static partial class Permissions
{
    public static partial class Dashboard
    {
        /// <summary>
        /// Allows viewing dashboards and choosing a personal view.
        /// </summary>
        public const string Read = "dashboard.read";
        /// <summary>
        /// Allows editing and resetting the current user's customization.
        /// </summary>
        public const string Personalize = "dashboard.personalize";
        /// <summary>
        /// Allows editing and resetting the shared customization.
        /// </summary>
        public const string ManageGlobal = "dashboard.manageglobal";
    }
}
