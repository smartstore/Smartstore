using Smartstore.Collections;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a premission tree.
    /// </summary>
    public partial class PermissionTree
    {
        public PermissionTree(
            TreeNode<IPermissionNode> permissions,
            Dictionary<string, string> displayNames = null,
            int? languageId = null)
        {
            Guard.NotNull(permissions);

            Permissions = permissions;
            DisplayNames = displayNames;
            LanguageId = languageId;
        }

        /// <summary>
        /// Gets all permissions structured as a tree.
        /// </summary>
        public TreeNode<IPermissionNode> Permissions { get; init; }

        /// <summary>
        /// Gets the identifier of the language of the display names. <c>null</c> if no display names are provided.
        /// </summary>
        public int? LanguageId { get; init; }

        /// <summary>
        /// Gets the display names of permissions.
        /// The key is the string resource name and the value the localized display name of the permission.
        /// </summary>
        public IReadOnlyDictionary<string, string> DisplayNames { get; init; }

        /// <summary>
        /// Gets the localized display name for a permission node.
        /// </summary>
        /// <param name="node">Permission node.</param>
        /// <returns>Display name.</returns>
        public string GetDisplayName(TreeNode<IPermissionNode> node)
        {
            if (node == null || DisplayNames == null)
            {
                return null;
            }

            var systemName = node.Value.SystemName;
            if (string.IsNullOrEmpty(systemName))
            {
                return null;
            }

            var token = systemName.Substring(systemName.LastIndexOf('.') + 1).ToLower();
            var displayName = PermissionService.GetDisplayName(token, DisplayNames);

            return displayName ?? token ?? systemName;
        }
    }
}
