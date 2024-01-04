using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Search;

namespace Smartstore.Core.Tests.Catalog.Search
{
    [TestFixture]
    public class CatalogSearchQueryTests
    {
        [TestCase(new[] { "name" }, "ateg")]
        [TestCase(new[] { "name", "shortdescription" }, "organic")]
        public void SearchQuery_can_get_default_term(string[] fields, string term)
        {
            var query = new CatalogSearchQuery(fields, term);

            Assert.That(query.DefaultTerm, Is.EqualTo(term));
        }

        [Test]
        public void SearchQuery_can_change_default_term()
        {
            var query = new CatalogSearchQuery(new[] { "name", "shortdescription" }, "organic")
            {
                DefaultTerm = "ateg"
            };

            Assert.That(query.DefaultTerm, Is.EqualTo("ateg"));

            var termFilter = query.Filters.OfType<ICombinedSearchFilter>().FirstOrDefault(x => x.FieldName == "searchterm");
            Assert.That(termFilter, Is.Not.Null);

            var allTermsChanged = termFilter.Filters
                .Select(x => (IAttributeSearchFilter)x)
                .All(x => (string)x.Term == "ateg");
            Assert.That(allTermsChanged, Is.True);
        }
    }
}
