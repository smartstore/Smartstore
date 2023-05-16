using NUnit.Framework;
using Smartstore.Core.Catalog.Search;

namespace Smartstore.Core.Tests.Catalog.Search
{
    [TestFixture]
    public class CatalogSearchQueryTests
    {
        [TestCase(new[] { "name" }, "ateg")]
        [TestCase(new[] { "name", "shortdescription" }, "organic")]
        public void LinqSearch_can_get_default_term(string[] fields, string term)
        {
            var query = new CatalogSearchQuery(fields, term);

            Assert.That(query.DefaultTerm, Is.EqualTo(term));
        }

        [Test]
        public void LinqSearch_can_change_default_term()
        {
            var query = new CatalogSearchQuery(new[] { "name", "shortdescription" }, "organic")
            {
                DefaultTerm = "ateg"
            };

            Assert.That(query.DefaultTerm, Is.EqualTo("ateg"));
        }
    }
}
