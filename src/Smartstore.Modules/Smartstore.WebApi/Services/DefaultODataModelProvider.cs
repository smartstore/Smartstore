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
using Smartstore.Web.Api.Models.OData;
using Smartstore.Web.Api.Models.OData.Media;

namespace Smartstore.Web.Api
{
    internal class DefaultODataModelProvider : ODataModelProviderBase
    {
        public override void Build(ODataModelBuilder builder, int version)
        {
            builder.Namespace = string.Empty;

            builder.EntitySet<Address>("Addresses");
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Country>("Countries");
            builder.EntitySet<Currency>("Currencies");
            builder.EntitySet<CustomerRoleMapping>("CustomerRoleMappings");
            builder.EntitySet<CustomerRole>("CustomerRoles");
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Discount>("Discounts");
            builder.EntitySet<Download>("Downloads");
            builder.EntitySet<GenericAttribute>("GenericAttributes");
            builder.EntitySet<Language>("Languages");
            builder.EntitySet<LocalizedProperty>("LocalizedProperties");
            builder.EntitySet<Manufacturer>("Manufacturers");
            builder.EntitySet<MeasureDimension>("MeasureDimensions");
            builder.EntitySet<MeasureWeight>("MeasureWeights");

            BuildDeliveryTimes(builder);
            BuildMediaFiles(builder);

            builder.EntitySet<MediaFolder>("MediaFolderEntities");
            builder.EntitySet<StateProvince>("StateProvinces");
        }

        public override Stream GetXmlCommentsStream(IApplicationContext appContext)
            => GetModuleXmlCommentsStream(appContext, Module.SystemName);

        private static void BuildDeliveryTimes(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<DeliveryTime>("DeliveryTimes");

            set.EntityType.Collection
                .Function(nameof(DeliveryTimesController.GetDeliveryDate))
                .Returns<SimpleRange<DateTime?>>()
                .Parameter<int>("Id");
        }

        private static void BuildMediaFiles(ODataModelBuilder builder)
        {
            const string infoSetName = "MediaFiles";

            var fileSet = builder.EntitySet<MediaFile>("MediaFileEntities");
            var infoSet = builder.EntitySet<FileItemInfo>("MediaFiles");

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.GetFileByPath))
                .ReturnsFromEntitySet<FileItemInfo>(infoSetName)
                .Parameter<string>("Path");

            // Coming a lot more here...
        }
    }
}
