using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Tests.Content.Menus
{
    [TestFixture]
    public class MenuQueryExtensionsTests : ServiceTestBase
    {
        #region Test data

        private readonly static string _menuEntityName = NamedEntity.GetEntityName<MenuEntity>();
        private readonly static string _menuItemEntityName = NamedEntity.GetEntityName<MenuItemEntity>();

        private readonly List<MenuEntity> _menus = new()
        {
            new MenuEntity { Id = 1, SystemName = "Menu1", Title = "Menu 1", DisplayOrder = 1, Published = true },
            new MenuEntity { Id = 2, SystemName = "Menu2", Title = "Menu 2", DisplayOrder = 2, Published = false },
            new MenuEntity { Id = 3, SystemName = "Menu3", Title = "Menu 3", DisplayOrder = 3, Published = true, LimitedToStores = true },
            new MenuEntity { Id = 4, SystemName = "Menu4", Title = "Menu 4", DisplayOrder = 4, Published = true, LimitedToStores = true, SubjectToAcl = true },
            new MenuEntity { Id = 5, SystemName = "Menu5", Title = "Menu 5", DisplayOrder = 5, Published = true, SubjectToAcl = true },
        };

        private readonly List<MenuItemEntity> _menuItems = new()
        {
            new MenuItemEntity { Id = 1, MenuId = 1, DisplayOrder = 1, Published = true },
            new MenuItemEntity { Id = 2, MenuId = 1, DisplayOrder = 2, Published = false },
            new MenuItemEntity { Id = 3, MenuId = 2, DisplayOrder = 1, Published = true },
            new MenuItemEntity { Id = 4, MenuId = 3, DisplayOrder = 1, Published = true, LimitedToStores = true },
            new MenuItemEntity { Id = 5, MenuId = 3, DisplayOrder = 2, Published = true },
            new MenuItemEntity { Id = 6, MenuId = 4, DisplayOrder = 1, Published = true, LimitedToStores = true },
            new MenuItemEntity { Id = 7, MenuId = 4, DisplayOrder = 2, Published = true }
        };

        private readonly List<StoreMapping> _storeMappings = new()
        {
            new StoreMapping { EntityId = 3, EntityName = _menuEntityName, StoreId = 1 },
            new StoreMapping { EntityId = 4, EntityName = _menuEntityName, StoreId = 1 },
            new StoreMapping { EntityId = 4, EntityName = _menuEntityName, StoreId = 2 },
            new StoreMapping { EntityId = 4, EntityName = _menuEntityName, StoreId = 3 },
            new StoreMapping { EntityId = 9, EntityName = _menuEntityName, StoreId = 2 },

            new StoreMapping { EntityId = 4, EntityName = _menuItemEntityName, StoreId = 2 },
            new StoreMapping { EntityId = 6, EntityName = _menuItemEntityName, StoreId = 2 },
        };

        private readonly List<AclRecord> _aclRecords = new()
        {
            new AclRecord { EntityId = 4, EntityName = _menuEntityName, CustomerRoleId = 1 },
            new AclRecord { EntityId = 4, EntityName = _menuEntityName, CustomerRoleId = 2 },
            new AclRecord { EntityId = 5, EntityName = _menuEntityName, CustomerRoleId = 1 },
        };

        #endregion

        [OneTimeSetUp]
        public new async Task SetUp()
        {
            await DbContext.Menus.AddRangeAsync(_menus);
            await DbContext.MenuItems.AddRangeAsync(_menuItems);
            await DbContext.StoreMappings.AddRangeAsync(_storeMappings);
            await DbContext.AclRecords.AddRangeAsync(_aclRecords);

            await DbContext.SaveChangesAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            DbContext.AclRecords.RemoveRange(_aclRecords);
            DbContext.StoreMappings.RemoveRange(_storeMappings);
            DbContext.MenuItems.RemoveRange(_menuItems);
            DbContext.Menus.RemoveRange(_menus);

            await DbContext.SaveChangesAsync();
        }

        [Test]
        public async Task Can_query_menu_by_systemname()
        {
            var menus1 = await DbContext.Menus
                .ApplyStandardFilter("Menu1")
                .ApplySorting()
                .ToListAsync();

            Assert.That(menus1, Has.Count.EqualTo(1));
            Assert.That(menus1[0].Id, Is.EqualTo(1));

            var menus2 = await DbContext.Menus
                .ApplyStandardFilter("Menu2")
                .ApplySorting()
                .ToListAsync();

            Assert.That(menus2, Is.Empty);
        }

        [Test]
        public async Task Can_query_store_restricted_menu_items()
        {
            var items1 = await DbContext.Menus
                .ApplyStandardFilter("Menu4", null, 1)
                .ApplyMenuItemFilter(1)
                .ToListAsync();

            Assert.That(items1, Has.Count.EqualTo(1));
            Assert.That(items1[0].Id, Is.EqualTo(7));

            var items2 = await DbContext.Menus
                .ApplyStandardFilter("Menu4", null, 2)
                .ApplyMenuItemFilter(2)
                .ToListAsync();

            Assert.That(items2, Has.Count.EqualTo(2));
            Assert.That(string.Join(",", items2.Select(x => x.Id)), Is.EqualTo("6,7"));

            var items3 = await DbContext.Menus
                .ApplyStandardFilter("Menu4", null, 9)
                .ApplyMenuItemFilter(9)
                .ToListAsync();

            Assert.That(items3, Is.Empty);
        }

        [Test]
        public async Task Can_query_acl_restricted_menu_items()
        {
            var items1 = await DbContext.Menus
                .ApplyStandardFilter(null, null, 0, new[] { 1 })
                .ApplyMenuItemFilter(0, new[] { 1 })
                .ToListAsync();

            Assert.That(items1, Has.Count.EqualTo(5));

            var items2 = await DbContext.Menus
                .ApplyStandardFilter("Menu4", null, 0, new[] { 2 })
                .ApplyMenuItemFilter(0, new[] { 2 })
                .ToListAsync();

            Assert.That(items2.Count, Is.EqualTo(2));
            Assert.That(string.Join(",", items2.Select(x => x.Id)), Is.EqualTo("6,7"));

            var items3 = await DbContext.Menus
                .ApplyStandardFilter("Menu4", null, 0, [3, 4])
                .ApplyMenuItemFilter(0, [3, 4])
                .ToListAsync();

            Assert.That(items3, Is.Empty);
        }

        [Test]
        public async Task Can_query_store_and_acl_restricted_menu_items()
        {
            var items = await DbContext.Menus
                .ApplyStandardFilter(null, null, 2, new[] { 2 })
                .ApplyMenuItemFilter(2, new[] { 2 })
                .ToListAsync();

            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(string.Join(",", items.Select(x => x.Id)), Is.EqualTo("1,6,7"));
        }
    }
}
