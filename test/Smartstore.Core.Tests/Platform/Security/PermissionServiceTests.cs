using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;

namespace Smartstore.Core.Tests.Platform.Security
{
    [TestFixture]
    public class PermissionServiceTests : ServiceTest
    {
        private IPermissionService _permissionService;
        private ILocalizationService _localizationService;
        private IWorkContext _workContext;
        private ICacheManager _cacheManager;

        private readonly CustomerRole _rAdmin = new() { Id = 1, Active = true, SystemName = "Administrators", Name = "Administrators" };
        private readonly CustomerRole _rModerator = new() { Id = 2, Active = true, SystemName = "Moderators", Name = "Moderators" };
        private readonly CustomerRole _rGuest = new() { Id = 3, Active = true, SystemName = "Guests", Name = "Guests" };

        private readonly Customer _cAdmin = new() { Id = 1, Username = "Admin" };
        private readonly Customer _cModerator = new() { Id = 2, Username = "Moderator" };
        private readonly Customer _cGuest = new() { Id = 3, Username = "Guest" };

        [OneTimeSetUp]
        public virtual void Setup()
        {
            _cacheManager = NullCache.Instance;

            var localizationServiceWrapper = new Mock<ILocalizationService>();
            _localizationService = localizationServiceWrapper.Object;

            var workContextWrapper = new Mock<IWorkContext>();
            _workContext = workContextWrapper.Object;

            AddEntities();

            _permissionService = new PermissionService(
                DbContext,
                _workContext,
                _localizationService,
                _cacheManager);
        }

        [Test]
        public async Task Permission_allow()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer.read", true);

            var result = _permissionService.Authorize("catalog.manufacturer", _cModerator);
            Assert.IsFalse(result);

            result = _permissionService.Authorize("catalog.manufacturer.read", _cModerator);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task Permission_deny()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer.write", false);

            var result = _permissionService.Authorize("catalog.manufacturer.write", _cModerator);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Permission_allow_by_parent()
        {
            await CheckTreeNodeAsync(_rAdmin, "catalog.category", true);

            var result = _permissionService.Authorize("catalog.category.write", _cAdmin);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task Permission_deny_by_parent()
        {
            await CheckTreeNodeAsync(_rGuest, "catalog.category", false);

            var result = _permissionService.Authorize("catalog.category.read", _cGuest);
            Assert.IsFalse(result);

            result = _permissionService.Authorize("catalog.manufacturer.write", _cGuest);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Permission_findAsync()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer", false);

            var result = _permissionService.Authorize("catalog.manufacturer", _cModerator, true);
            Assert.IsTrue(result);

            result = _permissionService.Authorize("catalog.product", _cModerator);
            Assert.IsFalse(result);
        }

        private void AddEntities()
        {
            _cAdmin.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = _cAdmin.Id,
                CustomerRoleId = _rAdmin.Id,
                CustomerRole = _rAdmin
            });

            _cModerator.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = _cModerator.Id,
                CustomerRoleId = _rGuest.Id,
                CustomerRole = _rGuest
            });

            _cModerator.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = _cModerator.Id,
                CustomerRoleId = _rModerator.Id,
                CustomerRole = _rModerator
            });

            _cGuest.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = _cGuest.Id,
                CustomerRoleId = _rGuest.Id,
                CustomerRole = _rGuest
            });

            var pCatalog = new PermissionRecord { Id = 1, SystemName = "catalog" };

            var pManu = new PermissionRecord { Id = 10, SystemName = "catalog.manufacturer" };
            var pManuRead = new PermissionRecord { Id = 100, SystemName = "catalog.manufacturer.read" };
            var pManuWrite = new PermissionRecord { Id = 101, SystemName = "catalog.manufacturer.write" };

            var pCategory = new PermissionRecord { Id = 20, SystemName = "catalog.category" };
            var pCategoryRead = new PermissionRecord { Id = 200, SystemName = "catalog.category.read" };
            var pCategoryWrite = new PermissionRecord { Id = 201, SystemName = "catalog.category.write" };

            AddMapping(pManu, _rAdmin, true);
            AddMapping(pCategory, _rAdmin, true);

            AddMapping(pManu, _rModerator, false);
            AddMapping(pManuRead, _rModerator, true);
            AddMapping(pManuWrite, _rModerator, false);

            AddMapping(pCategory, _rGuest, false);

            var permissions = new List<PermissionRecord> { pCatalog, pManu, pManuRead, pManuWrite, pCategory, pCategoryRead, pCategoryWrite };

            DbContext.PermissionRecords.AddRange(permissions);
            DbContext.SaveChanges();
        }

        private void AddMapping(PermissionRecord permission, CustomerRole role, bool allow)
        {
            permission.PermissionRoleMappings.Add(new PermissionRoleMapping
            {
                CustomerRoleId = role.Id,
                CustomerRole = role,
                PermissionRecordId = permission.Id,
                PermissionRecord = permission,
                Allow = allow
            });
        }

        private async Task CheckTreeNodeAsync(CustomerRole role, string permissionSystemName, bool allow)
        {
            var tree = await _permissionService.GetPermissionTreeAsync(role);
            var node = tree.Permissions.SelectNode(x =>  x.Value.SystemName == permissionSystemName);

            Assert.NotNull(node, $"Cannot select node by id '{permissionSystemName}'.");
            Assert.NotNull(node.Value.Allow, "The selected node must not be 'null'.");

            if (allow)
            {
                Assert.IsTrue(node.Value.Allow.Value, "The value of the selected node must be 'true'.");
            }
            else
            {
                Assert.IsFalse(node.Value.Allow.Value, "The value of the selected node must be 'false'.");
            }
        }
    }
}