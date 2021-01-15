using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Security
{
    // TODO: (mg) (core) Implement PermissionService
    public partial class PermissionService : IPermissionService
    {
        public bool Authorize(string permissionSystemName)
        {
            return true;
        }

        public bool Authorize(string permissionSystemName, Customer customer)
        {
            return true;
        }

        public Task<bool> AuthorizeAsync(string permissionSystemName)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AuthorizeAsync(string permissionSystemName, Customer customer)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AuthorizeByAliasAsync(string permissionSystemName)
        {
            return Task.FromResult(true);
        }

        public Task<bool> FindAuthorizationAsync(string permissionSystemName)
        {
            return Task.FromResult(true);
        }

        public Task<bool> FindAuthorizationAsync(string permissionSystemName, Customer customer)
        {
            return Task.FromResult(true);
        }

        public Task<Dictionary<string, string>> GetAllSystemNamesAsync()
        {
            return Task.FromResult(new Dictionary<string, string>());
        }

        public string GetDiplayName(string permissionSystemName)
        {
            return permissionSystemName;
        }

        public Task<TreeNode<IPermissionNode>> GetPermissionTreeAsync(CustomerRole role, bool addDisplayNames = false)
        {
            return Task.FromResult(new TreeNode<IPermissionNode>(new PermissionNode()));
        }

        public Task<TreeNode<IPermissionNode>> GetPermissionTreeAsync(Customer customer, bool addDisplayNames = false)
        {
            return Task.FromResult(new TreeNode<IPermissionNode>(new PermissionNode()));
        }

        public string GetUnauthorizedMessage(string permissionSystemName)
        {
            return permissionSystemName;
        }

        public void InstallPermissions(IPermissionProvider[] permissionProviders, bool removeUnusedPermissions = false)
        {
            // ...
        }
    }
}
