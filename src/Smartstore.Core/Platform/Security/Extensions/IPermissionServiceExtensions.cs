using System.Runtime.CompilerServices;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore
{
    public static class IPermissionServiceExtensions
    {
        /// <summary>
        /// Checks whether given permission is granted.
        /// </summary>
        /// <param name="permission">Permission to check.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Authorize(this IPermissionService service, PermissionRecord permission)
            => service.Authorize(permission?.SystemName);

        /// <summary>
        /// Checks whether given permission is granted.
        /// </summary>
        /// <param name="permission">Permission to check.</param>
        /// <param name="customer">Customer.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Authorize(this IPermissionService service, PermissionRecord permission, Customer customer)
            => service.Authorize(permission?.SystemName, customer);

        /// <summary>
        /// Checks whether given permission is granted.
        /// </summary>
        /// <param name="permission">Permission to check.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> AuthorizeAsync(this IPermissionService service, PermissionRecord permission)
            => service.AuthorizeAsync(permission?.SystemName);

        /// <summary>
        /// Checks whether given permission is granted.
        /// </summary>
        /// <param name="permission">Permission to check.</param>
        /// <param name="customer">Customer.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> AuthorizeAsync(this IPermissionService service, PermissionRecord permission, Customer customer)
            => service.AuthorizeAsync(permission?.SystemName, customer);
    }
}