using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class CategoryProductModel : EntityModelBase
    {
        public int CategoryId { get; set; }
        public int ProductId { get; set; }
        public string EditUrl { get; set; }

        [LocalizedDisplay("Admin.Catalog.Categories.Products.Fields.Product")]
        public string ProductName { get; set; }

        [LocalizedDisplay("*Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("*ProductType")]
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Admin.Catalog.Categories.Products.Fields.IsFeaturedProduct")]
        public bool IsFeaturedProduct { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Rules.AddedByRule")]
        public bool IsSystemMapping { get; set; }
    }

    public class CategoryProductMapper : IMapper<ProductCategory, CategoryProductModel>
    {
        private readonly ICommonServices _services;
        private readonly IUrlHelper _urlHelper;

        public CategoryProductMapper(ICommonServices services, IUrlHelper urlHelper)
        {
            _services = services;
            _urlHelper = urlHelper;
        }

        public async Task MapAsync(ProductCategory from, CategoryProductModel to, dynamic parameters = null)
        {
            await _services.DbContext.LoadReferenceAsync(from, x => x.Product);

            MiniMapper.Map(from, to);

            var product = from.Product;
            if (product != null)
            {
                to.ProductName = product.GetLocalized(x => x.Name);
                to.Sku = product.Sku;
                to.ProductTypeName = product.GetProductTypeLabel(_services.Localization);
                to.ProductTypeLabelHint = product.ProductTypeLabelHint;
                to.Published = product.Published;

                to.EditUrl = _urlHelper.Action("Edit", "Product", new { id = from.Id, area = "Admin" });
            }
        }
    }
}
