using System.IO;
using System.Net.Http;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
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
            builder.EntitySet<NewsletterSubscription>("NewsletterSubscriptions");
            builder.EntitySet<OrderItem>("OrderItems");
            builder.EntitySet<OrderNote>("OrderNotes");
            builder.EntitySet<Order>("Orders");

            builder.EntitySet<PriceLabel>("PriceLabels");
            builder.EntitySet<Product>("Products");
            builder.EntitySet<RewardPointsHistory>("RewardPointsHistory");
            builder.EntitySet<Shipment>("Shipments");

            BuildDeliveryTimes(builder);
            BuildMediaFiles(builder);
            BuildMediaFolders(builder);
            BuildNewsletterSubscriptions(builder);
            BuildOrderItems(builder);
            BuildOrders(builder);

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
            var config = infoSet.EntityType.Collection;

            config.Action(nameof(MediaFilesController.GetFileByPath))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .Parameter<string>("path")
                .Required();

            config.Function(nameof(MediaFilesController.GetFilesByIds))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .CollectionParameter<int>("ids")
                .Required();

            config.Function(nameof(MediaFilesController.DownloadFile))
                .Returns<StreamContent>()
                .Parameter<int>("id")
                .Required();

            config.Action(nameof(MediaFilesController.SearchFiles))
                .ReturnsFromEntitySet<FileItemInfo>(setName)
                .Parameter<MediaSearchQuery>("query")
                .Optional();

            config.Action(nameof(MediaFilesController.CountFiles))
                .Returns<int>()
                .Parameter<MediaSearchQuery>("query")
                .Optional();

            config.Action(nameof(MediaFilesController.CountFilesGrouped))
                .Returns<MediaCountResult>()
                .Parameter<MediaFilesFilter>("filter")
                .Optional();

            config.Action(nameof(MediaFilesController.FileExists))
                .Returns<bool>()
                .Parameter<string>("path")
                .Required();

            config.Action(nameof(MediaFilesController.CheckUniqueFileName))
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

            var saveFile = config
                .Action(nameof(MediaFilesController.SaveFile))
                .ReturnsFromEntitySet<FileItemInfo>(setName);

            saveFile.Parameter<IFormFile>("file")
                .Required();
            saveFile.Parameter<string>("path")
                .Optional();
            saveFile.Parameter<bool>("isTransient")
                .HasDefaultValue(bool.TrueString)
                .Optional();
            saveFile.Parameter<DuplicateFileHandling>("duplicateFileHandling")
                .Optional();
        }

        private static void BuildMediaFolders(ODataModelBuilder builder)
        {
            const string setName = "MediaFolders";

            var folderSet = builder.EntitySet<MediaFolder>("MediaFolderEntities");
            var infoSet = builder.EntitySet<FolderNodeInfo>(setName);
            var config = infoSet.EntityType.Collection;

            config.Action(nameof(MediaFoldersController.FolderExists))
                .Returns<bool>()
                .Parameter<string>("path")
                .Required();

            config.Action(nameof(MediaFoldersController.CheckUniqueFolderName))
                .Returns<CheckUniquenessResult>()
                .Parameter<string>("path")
                .Required();

            config.Function(nameof(MediaFoldersController.GetRootNode))
                .ReturnsFromEntitySet<FolderNodeInfo>(setName);

            config.Action(nameof(MediaFoldersController.GetNodeByPath))
                .ReturnsFromEntitySet<FolderNodeInfo>(setName)
                .Parameter<string>("path")
                .Required();

            config.Action(nameof(MediaFoldersController.CreateFolder))
                .ReturnsFromEntitySet<FolderNodeInfo>(setName)
                .Parameter<string>("path")
                .Required();

            var moveFolder = config
                .Action(nameof(MediaFoldersController.MoveFolder))
                .ReturnsFromEntitySet<FolderNodeInfo>(setName);

            moveFolder.Parameter<string>("path")
                .Required();
            moveFolder.Parameter<string>("destinationPath")
                .Required();

            var copyFolder = config
                .Action(nameof(MediaFoldersController.CopyFolder))
                .Returns<MediaFolderOperationResult>();

            copyFolder.Parameter<string>("path")
                .Required();
            copyFolder.Parameter<string>("destinationPath")
                .Required();
            copyFolder.Parameter<DuplicateEntryHandling>("duplicateEntryHandling")
                .Optional();

            var deleteFolder = config
                .Action(nameof(MediaFoldersController.DeleteFolder))
                .Returns<MediaFolderDeleteResult>();

            deleteFolder.Parameter<string>("path")
                .Required();
            deleteFolder.Parameter<FileHandling>("fileHandling")
                .Optional();
        }

        private static void BuildNewsletterSubscriptions(ODataModelBuilder builder)
        {
            const string setName = "NewsletterSubscriptions";
            var set = builder.EntitySet<NewsletterSubscription>(setName);

            set.EntityType
                .Action(nameof(NewsletterSubscriptionsController.Subscribe))
                .ReturnsFromEntitySet<NewsletterSubscription>(setName);

            set.EntityType
                .Action(nameof(NewsletterSubscriptionsController.Unsubscribe))
                .ReturnsFromEntitySet<NewsletterSubscription>(setName);
        }

        private static void BuildOrderItems(ODataModelBuilder builder)
        {
            const string setName = "OrderItems";
            var set = builder.EntitySet<OrderItem>(setName);

            set.EntityType.Collection.Function(nameof(OrderItemsController.GetShipmentInfo))
                .Returns<OrderItemShipmentInfo>()
                .Parameter<int>("id")
                .Required();
        }

        private static void BuildOrders(ODataModelBuilder builder)
        {
            const string setName = "Orders";
            var set = builder.EntitySet<Order>(setName);
            var config = set.EntityType.Collection;

            config.Function(nameof(OrdersController.GetShipmentInfo))
                .Returns<OrderShipmentInfo>()
                .Parameter<int>("id")
                .Required();

            config.Function(nameof(OrdersController.DownloadPdf))
                .Returns<StreamContent>()
                .Parameter<int>("id")
                .Required();

            set.EntityType
                .Action(nameof(OrdersController.PaymentPending))
                .ReturnsFromEntitySet<Order>(setName);

            set.EntityType
                .Action(nameof(OrdersController.PaymentPaid))
                .ReturnsFromEntitySet<Order>(setName)
                .Parameter<string>("paymentMethodName")
                .Optional();

            set.EntityType
                .Action(nameof(OrdersController.PaymentRefund))
                .ReturnsFromEntitySet<Order>(setName)
                .Parameter<bool>("online")
                .Required();
        }
    }
}