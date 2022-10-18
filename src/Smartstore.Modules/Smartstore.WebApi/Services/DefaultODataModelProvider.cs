using System.IO;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Engine;
using Smartstore.Web.Api.Controllers.OData;

namespace Smartstore.Web.Api
{
    internal class DefaultODataModelProvider : ODataModelProviderBase
    {
        public override void Build(ODataModelBuilder builder, int version)
        {
            builder.EntitySet<Address>("Addresses");
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Country>("Countries");
            builder.EntitySet<Currency>("Currencies");
            builder.EntitySet<CustomerRoleMapping>("CustomerRoleMappings");
            builder.EntitySet<CustomerRole>("CustomerRoles");
            builder.EntitySet<Customer>("Customers");
            DeliveryTimesController.Init(builder);
            builder.EntitySet<Discount>("Discounts");
            builder.EntitySet<Download>("Downloads");
            builder.EntitySet<GenericAttribute>("GenericAttributes");
            builder.EntitySet<Language>("Languages");
            builder.EntitySet<LocalizedProperty>("LocalizedProperties");
            builder.EntitySet<Manufacturer>("Manufacturers");
            builder.EntitySet<MeasureDimension>("MeasureDimensions");

            builder.EntitySet<MediaFile>("MediaFiles");
            builder.EntitySet<StateProvince>("StateProvinces");
        }

        public override Stream GetXmlCommentsStream(IApplicationContext appContext)
            => GetModuleXmlCommentsStream(appContext, Module.SystemName);
    }
}
