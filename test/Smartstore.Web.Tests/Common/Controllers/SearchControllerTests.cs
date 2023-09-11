using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Smartstore.Core.Catalog.Search;
using Smartstore.Test.Common;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Search;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Models.Catalog;
using Smartstore.Collections;
using Moq;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Tests.Common.Controllers;

[TestFixture]
public class SearchControllerTests
{
    [Test]
    public async Task Term_Not_Found()
    {
        SearchSettings settings = new SearchSettings();
        settings.InstantSearchTermMinLength = 2;

        SearchController controller = new(null, null, null, null, settings, null, null, null);
        CatalogSearchQuery query = new CatalogSearchQuery();
        query.DefaultTerm = "a";
        var actual = await controller.GetSearchResultModel(query);

        SearchResultModel expected_result = new SearchResultModel(query);
        expected_result.SearchResult = new CatalogSearchResult(query);
        expected_result.TopProducts = ProductSummaryModel.Empty;
        expected_result.Error = "Search.SearchTermMinimumLengthIsNCharacters";

        //need to mock db for this assert
        Assert.AreEqual(expected_result.SearchResult.TotalHitsCount, actual.SearchResult.TotalHitsCount);
        Assert.AreEqual(expected_result.TopProducts, actual.TopProducts);
        Assert.AreEqual(expected_result.Error, actual.Error);
    }

    [Test]
    public async Task Search_Item()
    {
        SearchSettings settings = new SearchSettings();
        settings.InstantSearchTermMinLength = 2;

        SearchController controller = new(null, null, null, null, settings, null, null, null);
        CatalogSearchQuery query = new CatalogSearchQuery();
        query.DefaultTerm = "baseball";
        var actual = await controller.GetSearchResultModel(query);

        SearchResultModel expected_result = new SearchResultModel(query);
        expected_result.SearchResult = new CatalogSearchResult(query);

        var pagedMock = new Mock<IPagedList<Product>>();
        var pagedList = pagedMock.Object;

        pagedMock.Setup(x => x.FirstItemIndex == 0);
        

        expected_result.TopProducts = new ProductSummaryModel(pagedList);

        //need to mock db for this assert
        Assert.AreEqual(expected_result.TopProducts.Items.Count, actual.TopProducts.Items.Count);
    }
}
