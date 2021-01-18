namespace Smartstore.Core.Security
{
    /// <summary>
    /// Provides system names of standard permissions.
    /// Usage: [Permission(PermissionSystemNames.Customer.Read)]
    /// </summary>
    public static partial class Permissions
    {
        // TODO: (mg) (core) Implement/Finalize "Permissions" static class

        public static class System
        {
            public const string Self = "system";
            public const string AccessBackend = "system.accessbackend";
            public const string AccessShop = "system.accessshop";
        }
    }
}
