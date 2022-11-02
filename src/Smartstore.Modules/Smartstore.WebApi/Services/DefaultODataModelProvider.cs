using System.IO;
using System.Net.Http;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Engine;
using Smartstore.Web.Api.Controllers.OData;
using Smartstore.Web.Api.Models;
using Smartstore.Web.Api.Models.Media;

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

            builder.EntitySet<PriceLabel>("PriceLabels");

            BuildDeliveryTimes(builder);
            BuildMediaFiles(builder);
            BuildMediaFolders(builder);

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
            const string setName = "MediaFiles";

            var fileSet = builder.EntitySet<MediaFile>("MediaFileEntities");
            var infoSet = builder.EntitySet<FileItemInfo>(setName);

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.GetFileByPath))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .Parameter<string>("path")
                .Required();

            infoSet.EntityType.Collection
                .Function(nameof(MediaFilesController.GetFilesByIds))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .CollectionParameter<int>("ids")
                .Required();

            infoSet.EntityType.Collection
                .Function(nameof(MediaFilesController.Download))
                .Returns<StreamContent>()
                .Parameter<int>("id")
                .Required();

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.SearchFiles))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .Parameter<MediaSearchQuery>("query")
                .Optional();

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.CountFiles))
                .Returns<int>()
                .Parameter<MediaSearchQuery>("query")
                .Optional();

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.CountFilesGrouped))
                .Returns<MediaCountResult>()
                .Parameter<MediaFilesFilter>("filter")
                .Optional();

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.FileExists))
                .Returns<bool>()
                .Parameter<string>("path")
                .Required();

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.CheckUniqueFileName))
                .Returns<CheckUniquenessResult>()
                .Parameter<string>("path")
                .Required();

            var moveFile = infoSet.EntityType
                .Action(nameof(MediaFilesController.MoveFile))
                .ReturnsFromEntitySet<FileItemInfo>(setName);

            moveFile.Parameter<string>("destinationFileName")
                .Required();
            moveFile.Parameter<DuplicateFileHandling>("duplicateFileHandling")
                .Optional();

            var copyFile = infoSet.EntityType
                .Action(nameof(MediaFilesController.CopyFile))
                .Returns<MediaFileOperationResult>();

            copyFile.Parameter<string>("destinationFileName")
                .Required();
            copyFile.Parameter<DuplicateFileHandling>("duplicateFileHandling")
                .Optional();

            var deleteFile = infoSet.EntityType
                .Action(nameof(MediaFilesController.DeleteFile));

            deleteFile.Parameter<bool>("permanent")
                .Required();
            deleteFile.Parameter<bool>("force")
                .HasDefaultValue(bool.FalseString);

            infoSet.EntityType.Collection
                .Action(nameof(MediaFilesController.SaveFile))
                .ReturnsFromEntitySet<FileItemInfo>(setName);
        }

        private static void BuildMediaFolders(ODataModelBuilder builder)
        {
            const string setName = "MediaFolders";

            var folderSet = builder.EntitySet<MediaFolder>("MediaFolderEntities");
            var infoSet = builder.EntitySet<FolderNodeInfo>(setName);

            // ...
        }
    }
}
