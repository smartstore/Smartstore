using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Products
{
    [TestFixture]
    public class ProductExtensionTests
    {
        [Test]
        public void Can_parse_allowed_quantities()
        {
            var product = new Product()
            {
                AllowedQuantities = "1, 5,4,10,sdf"
            };

            var result = product.ParseAllowedQuantities();
            result.Length.ShouldEqual(4);
            result[0].ShouldEqual(1);
            result[1].ShouldEqual(4);
            result[2].ShouldEqual(5);
            result[3].ShouldEqual(10);
        }

        [Test]
        public void Can_parse_required_product_ids()
        {
            var product = new Product
            {
                RequiredProductIds = "1, 4,7 ,a,"
            };

            var ids = product.ParseRequiredProductIds();
            ids.Length.ShouldEqual(3);
            ids[0].ShouldEqual(1);
            ids[1].ShouldEqual(4);
            ids[2].ShouldEqual(7);
        }
    }
}