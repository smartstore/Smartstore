using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Tests.Catalog.Brands
{
    [TestFixture]
    public class ManufacturerQueryExtensionsTests : ServiceTest
    {
        [OneTimeSetUp]
        public new async Task SetUp()
        {
            DbContext.AddRange(
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
                });

            DbContext.StoreMappings.AddRange(
                new StoreMapping { EntityId = 3, EntityName = nameof(Manufacturer), StoreId = 1 },
                new StoreMapping { EntityId = 4, EntityName = nameof(Manufacturer), StoreId = 1 },
                new StoreMapping { EntityId = 4, EntityName = nameof(Manufacturer), StoreId = 2 },
                new StoreMapping { EntityId = 4, EntityName = nameof(Manufacturer), StoreId = 3 },
                new StoreMapping { EntityId = 9, EntityName = nameof(Manufacturer), StoreId = 2 },
                new StoreMapping { EntityId = 4, EntityName = nameof(Product), StoreId = 2 });

            DbContext.AclRecords.AddRange(
                new AclRecord { EntityId = 4, EntityName = nameof(Manufacturer), CustomerRoleId = 1 },
                new AclRecord { EntityId = 4, EntityName = nameof(Manufacturer), CustomerRoleId = 2 },
                new AclRecord { EntityId = 5, EntityName = nameof(Manufacturer), CustomerRoleId = 3 });

            await DbContext.SaveChangesAsync();
        }

        [Test]
        public async Task Can_query_published_manufacturers()
        {
            var manufacturers = await DbContext.Manufacturers
                .ApplyStandardFilter()
                .ToListAsync();

            Assert.AreEqual(4, manufacturers.Count);
            Assert.AreEqual(string.Join(",", manufacturers.Select(x => x.Id)), "1,3,4,5");
        }

        [Test]
        public async Task Can_query_store_restricted_manufacturers()
        {
            // INFO: do not run due to baked-in GetDbContext.
            //var manufacturers = await DbContext.Manufacturers
            //    .ApplyStandardFilter(false, null, 2)
            //    .ToListAsync();

            var storeId = 2;
            var entityName = nameof(Manufacturer);
            var query = DbContext.Manufacturers.Where(x => x.Published);

            // Old query
            //query =
            //    from x in query
            //    join m in DbContext.StoreMappings on new { id = x.Id, name = entityName } equals new { id = m.EntityId, name = m.EntityName } into xm
            //    from sc in xm.DefaultIfEmpty()
            //    where !x.LimitedToStores || storeId == sc.StoreId
            //    select x;

            var subQuery = DbContext.StoreMappings
                .Where(x => x.EntityName == entityName && x.StoreId == storeId)
                .Select(x => x.EntityId);

            query = query.Where(x => !x.LimitedToStores || subQuery.Contains(x.Id));

            query = query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var manufacturers = await query.ToListAsync();

            Assert.AreEqual(3, manufacturers.Count);
            Assert.AreEqual(string.Join(",", manufacturers.Select(x => x.Id)), "1,4,5");
        }

        [Test]
        public async Task Can_query_acl_restricted_manufacturers()
        {
            var customerRoleIds = new[] { 1, 2 };
            var entityName = nameof(Manufacturer);
            var query = DbContext.Manufacturers.Where(x => x.Published);

            // Old query (provides duplicates if entity is mapped to multiple roles): 1,3,4,4
            //query =
            //    from m in query
            //    join a in DbContext.AclRecords on new { m1 = m.Id, m2 = entityName } equals new { m1 = a.EntityId, m2 = a.EntityName } into ma
            //    from a in ma.DefaultIfEmpty()
            //    where !m.SubjectToAcl || customerRoleIds.Contains(a.CustomerRoleId)
            //    select m;

            var subQuery = DbContext.AclRecords
                .Where(x => x.EntityName == entityName && customerRoleIds.Contains(x.CustomerRoleId))
                .Select(x => x.EntityId);

            query = query.Where(x => !x.SubjectToAcl || subQuery.Contains(x.Id));

            query = query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var manufacturers = await query.ToListAsync();

            Assert.AreEqual(3, manufacturers.Count);
            Assert.AreEqual(string.Join(",", manufacturers.Select(x => x.Id)), "1,3,4");
        }

        [Test]
        public async Task Can_query_store_and_acl_restricted_manufacturers()
        {
            var storeId = 2;
            var customerRoleIds = new[] { 1, 2 };
            var entityName = nameof(Manufacturer);
            var query = DbContext.Manufacturers.Where(x => x.Published);

            var subQuery1 = DbContext.StoreMappings
                .Where(x => x.EntityName == entityName && x.StoreId == storeId)
                .Select(x => x.EntityId);

            query = query.Where(x => !x.LimitedToStores || subQuery1.Contains(x.Id));

            var subQuery2 = DbContext.AclRecords
                .Where(x => x.EntityName == entityName && customerRoleIds.Contains(x.CustomerRoleId))
                .Select(x => x.EntityId);

            query = query.Where(x => !x.SubjectToAcl || subQuery2.Contains(x.Id));

            query = query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var manufacturers = await query.ToListAsync();

            Assert.AreEqual(2, manufacturers.Count);
            Assert.AreEqual(string.Join(",", manufacturers.Select(x => x.Id)), "1,4");
        }
    }
}
