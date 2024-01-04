using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Tests.Catalog.Brands
{
    [TestFixture]
    public class ManufacturerQueryExtensionsTests : ServiceTestBase
    {
        #region Test data

        private readonly static string _manufacturerEntityName = NamedEntity.GetEntityName<Manufacturer>();
        private readonly static string _productEntityName = NamedEntity.GetEntityName<Product>();

        private readonly List<Manufacturer> _manufacturers = new()
        {
            new Manufacturer
            {
                Id = 1,
                Name = "Brand 1",
                Published = true,
                DisplayOrder = 1
            },
            new Manufacturer
            {
                Id = 2,
                Name = "Brand 2",
                Published = false,
                DisplayOrder = 2
            },
            new Manufacturer
            {
                Id = 3,
                Name = "Brand 3",
                Published = true,
                DisplayOrder = 3,
                LimitedToStores = true
            },
            new Manufacturer
            {
                Id = 4,
                Name = "Brand 4",
                Published = true,
                DisplayOrder = 4,
                LimitedToStores = true,
                SubjectToAcl = true
            },
            new Manufacturer
            {
                Id = 5,
                Name = "Brand 5",
                Published = true,
                DisplayOrder = 5,
                LimitedToStores = false,
                SubjectToAcl = true
            }
        };

        private readonly List<StoreMapping> _storeMappings = new()
        {
            new StoreMapping { EntityId = 3, EntityName = _manufacturerEntityName, StoreId = 1 },
            new StoreMapping { EntityId = 4, EntityName = _manufacturerEntityName, StoreId = 1 },
            new StoreMapping { EntityId = 4, EntityName = _manufacturerEntityName, StoreId = 2 },
            new StoreMapping { EntityId = 4, EntityName = _manufacturerEntityName, StoreId = 3 },
            new StoreMapping { EntityId = 9, EntityName = _manufacturerEntityName, StoreId = 2 },
            new StoreMapping { EntityId = 4, EntityName = _productEntityName, StoreId = 2 }
        };

        private readonly List<AclRecord> _aclRecords = new()
        {
            new AclRecord { EntityId = 4, EntityName = _manufacturerEntityName, CustomerRoleId = 1 },
            new AclRecord { EntityId = 4, EntityName = _manufacturerEntityName, CustomerRoleId = 2 },
            new AclRecord { EntityId = 5, EntityName = _manufacturerEntityName, CustomerRoleId = 3 }
        };

        #endregion

        [OneTimeSetUp]
        public new async Task SetUp()
        {
            await DbContext.Manufacturers.AddRangeAsync(_manufacturers);
            await DbContext.StoreMappings.AddRangeAsync(_storeMappings);
            await DbContext.AclRecords.AddRangeAsync(_aclRecords);

            await DbContext.SaveChangesAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            DbContext.AclRecords.RemoveRange(_aclRecords);
            DbContext.StoreMappings.RemoveRange(_storeMappings);
            DbContext.Manufacturers.RemoveRange(_manufacturers);

            await DbContext.SaveChangesAsync();
        }

        [Test]
        public async Task Can_query_published_manufacturers()
        {
            var manufacturers = await DbContext.Manufacturers
                .ApplyStandardFilter()
                .ToListAsync();

            Assert.That(manufacturers.Count, Is.EqualTo(4));
            Assert.That(string.Join(",", manufacturers.Select(x => x.Id)), Is.EqualTo("1,3,4,5"));
        }

        [Test]
        public async Task Can_query_store_restricted_manufacturers()
        {
            // Old query:
            //query =
            //    from x in query
            //    join m in DbContext.StoreMappings on new { id = x.Id, name = entityName } equals new { id = m.EntityId, name = m.EntityName } into xm
            //    from sc in xm.DefaultIfEmpty()
            //    where !x.LimitedToStores || storeId == sc.StoreId
            //    select x;

            var manufacturers = await DbContext.Manufacturers
                .ApplyStandardFilter(false, null, 2)
                .ToListAsync();

            Assert.That(manufacturers.Count, Is.EqualTo(3));
            Assert.That(string.Join(",", manufacturers.Select(x => x.Id)), Is.EqualTo("1,4,5"));
        }

        [Test]
        public async Task Can_query_acl_restricted_manufacturers()
        {
            // Old query. Provides duplicates if entity is mapped to multiple roles: 1,3,4,4
            //query =
            //    from m in query
            //    join a in DbContext.AclRecords on new { m1 = m.Id, m2 = entityName } equals new { m1 = a.EntityId, m2 = a.EntityName } into ma
            //    from a in ma.DefaultIfEmpty()
            //    where !m.SubjectToAcl || customerRoleIds.Contains(a.CustomerRoleId)
            //    select m;

            var manufacturers = await DbContext.Manufacturers
                .ApplyStandardFilter(false, new[] { 1, 2 }, 0)
                .ToListAsync();

            Assert.That(manufacturers.Count, Is.EqualTo(3));
            Assert.That(string.Join(",", manufacturers.Select(x => x.Id)), Is.EqualTo("1,3,4"));
        }

        [Test]
        public async Task Can_query_store_and_acl_restricted_manufacturers()
        {
            var manufacturers = await DbContext.Manufacturers
                .ApplyStandardFilter(false, new[] { 1, 2 }, 2)
                .ToListAsync();

            Assert.That(manufacturers.Count, Is.EqualTo(2));
            Assert.That(string.Join(",", manufacturers.Select(x => x.Id)), Is.EqualTo("1,4"));
        }
    }
}
