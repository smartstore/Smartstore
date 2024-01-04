using System;
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
    public class PermissionServiceTests : ServiceTestBase
    {
        private IPermissionService _permissionService;
        private ILocalizationService _localizationService;
        private IWorkContext _workContext;
        private ICacheManager _cacheManager;

        private readonly CustomerRole _rAdmin = new() { Id = 10, Active = true, SystemName = "Administrators", Name = "Administrators" };
        private readonly CustomerRole _rModerator = new() { Id = 20, Active = true, SystemName = "Moderators", Name = "Moderators" };
        private readonly CustomerRole _rGuest = new() { Id = 30, Active = true, SystemName = "Guests", Name = "Guests" };

        private readonly Customer _cAdmin = new() { Id = 10, Username = "Admin" };
        private readonly Customer _cModerator = new() { Id = 20, Username = "Moderator" };
        private readonly Customer _cGuest = new() { Id = 30, Username = "Guest" };

        [OneTimeSetUp]
        public virtual void Setup()
        {
            _cacheManager = NullCache.Instance;

            var localizationServiceMock = new Mock<ILocalizationService>();
            _localizationService = localizationServiceMock.Object;

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;

            AddEntities();

            _permissionService = new PermissionService(
                DbContext,
                new Lazy<IWorkContext>(() => _workContext),
                _localizationService,
                _cacheManager);
        }

        [Test]
        public async Task Permission_allow()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer.read", true);

            var result = _permissionService.Authorize("catalog.manufacturer", _cModerator);
            Assert.That(result, Is.False);

            result = _permissionService.Authorize("catalog.manufacturer.read", _cModerator);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Permission_deny()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer.write", false);

            var result = _permissionService.Authorize("catalog.manufacturer.write", _cModerator);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task Permission_allow_by_parent()
        {
            await CheckTreeNodeAsync(_rAdmin, "catalog.category", true);

            var result = _permissionService.Authorize("catalog.category.write", _cAdmin);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Permission_deny_by_parent()
        {
            await CheckTreeNodeAsync(_rGuest, "catalog.category", false);

            var result = _permissionService.Authorize("catalog.category.read", _cGuest);
            Assert.That(result, Is.False);

            result = _permissionService.Authorize("catalog.manufacturer.write", _cGuest);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task Permission_findAsync()
        {
            await CheckTreeNodeAsync(_rModerator, "catalog.manufacturer", false);

            var result = _permissionService.Authorize("catalog.manufacturer", _cModerator, true);
            Assert.That(result, Is.True);

            result = _permissionService.Authorize("catalog.product", _cModerator);
            Assert.That(result, Is.False);
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
            var node = tree.Permissions.SelectNode(x => x.Value.SystemName == permissionSystemName);

            Assert.That(node, Is.Not.Null, $"Cannot select node by id '{permissionSystemName}'.");
            Assert.That(node.Value.Allow, Is.Not.Null, "The selected node must not be 'null'.");

            if (allow)
            {
                Assert.That(node.Value.Allow.Value, Is.True, "The value of the selected node must be 'true'.");
            }
            else
            {
                Assert.That(node.Value.Allow.Value, Is.False, "The value of the selected node must be 'false'.");
            }
        }
    }
}