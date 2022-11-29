using System.IO;
using System.Net.Http;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Engine;
using Smartstore.Web.Api.Controllers.OData;
using Smartstore.Web.Api.Models;
using Smartstore.Web.Api.Models.Catalog;
using Smartstore.Web.Api.Models.Checkout;
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
            builder.EntitySet<OrderNote>("OrderNotes");
            builder.EntitySet<PriceLabel>("PriceLabels");
            builder.EntitySet<ProductAttributeOption>("ProductAttributeOptions");
            builder.EntitySet<ProductAttributeOptionsSet>("ProductAttributeOptionsSets");
            builder.EntitySet<ProductAttribute>("ProductAttributes");
            builder.EntitySet<ProductBundleItem>("ProductBundleItems");
            builder.EntitySet<ProductCategory>("ProductCategories");
            builder.EntitySet<ProductManufacturer>("ProductManufacturers");
            builder.EntitySet<ProductMediaFile>("ProductMediaFiles");

            builder.EntitySet<ProductSpecificationAttribute>("ProductSpecificationAttributes");
            builder.EntitySet<ProductVariantAttribute>("ProductVariantAttributes");
            builder.EntitySet<ProductVariantAttributeValue>("ProductVariantAttributeValues");
            builder.EntitySet<ProductVariantAttributeCombination>("ProductVariantAttributeCombinations");
            builder.EntitySet<ProductTag>("ProductTags");
            builder.EntitySet<QuantityUnit>("QuantityUnits");
            builder.EntitySet<RewardPointsHistory>("RewardPointsHistory");
            // TODO: (mg) (core) add actions to "Shipments": SetAsShipped, SetAsDelivered, DownloadPdfPackagingSlips.
            builder.EntitySet<Shipment>("Shipments");
            builder.EntitySet<TierPrice>("TierPrices");

            // INFO: functions specified directly on the ODataModelBuilder (instead of entity type or collection)
            // are called unbound functions (like static operations on the service).

            BuildDeliveryTimes(builder);
            BuildMediaFiles(builder);
            BuildMediaFolders(builder);
            BuildNewsletterSubscriptions(builder);
            BuildOrderItems(builder);
            BuildOrders(builder);
            BuildPaymentMethods(builder);
            BuildProducts(builder);

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
                .Parameter<int>("Id")
                .Required();
        }

        private static void BuildMediaFiles(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<MediaFile>("MediaFiles");
            var config = set.EntityType.Collection;

            //var infoSet = builder.EntitySet<FileItemInfo>("FileItemInfos");
            //infoSet.EntityType.HasKey(x => x.Id);
            //infoSet.EntityType.HasRequired(x => x.File).AutomaticallyExpand(true);
            //infoSet.HasRequiredBinding(x => x.File, set);

            config.Action(nameof(MediaFilesController.GetFileByPath))
                .Returns<FileItemInfo>()
                .Parameter<string>("path")
                .Required();

            config.Function(nameof(MediaFilesController.GetFilesByIds))
                .ReturnsCollection<FileItemInfo>()
                .CollectionParameter<int>("ids")
                .Required();

            config.Function(nameof(MediaFilesController.DownloadFile))
                .Returns<StreamContent>()
                .Parameter<int>("id")
                .Required();

            config.Action(nameof(MediaFilesController.SearchFiles))
                .ReturnsCollection<FileItemInfo>()
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

            var moveFile = set.EntityType
                .Action(nameof(MediaFilesController.MoveFile))
                .Returns<FileItemInfo>();
            moveFile.Parameter<string>("destinationFileName")
                .Required();
            moveFile.Parameter<DuplicateFileHandling>("duplicateFileHandling")
                .Optional();

            var copyFile = set.EntityType
                .Action(nameof(MediaFilesController.CopyFile))
                .Returns<MediaFileOperationResult>();
            copyFile.Parameter<string>("destinationFileName")
                .Required();
            copyFile.Parameter<DuplicateFileHandling>("duplicateFileHandling")
                .Optional();

            var deleteFile = set.EntityType
                .Action(nameof(MediaFilesController.DeleteFile));
            deleteFile.Parameter<bool>("permanent")
                .Required();
            deleteFile.Parameter<bool>("force")
                .HasDefaultValue(bool.FalseString)
                .Optional();

            var saveFile = config
                .Action(nameof(MediaFilesController.SaveFile))
                .Returns<FileItemInfo>();
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
            var set = builder.EntitySet<MediaFolder>("MediaFolders");
            var config = set.EntityType.Collection;

            config.Action(nameof(MediaFoldersController.FolderExists))
                .Returns<bool>()
                .Parameter<string>("path")
                .Required();

            config.Action(nameof(MediaFoldersController.CheckUniqueFolderName))
                .Returns<CheckUniquenessResult>()
                .Parameter<string>("path")
                .Required();

            config.Function(nameof(MediaFoldersController.GetRootNode))
                .Returns<FolderNodeInfo>();

            config.Action(nameof(MediaFoldersController.GetNodeByPath))
                .Returns<FolderNodeInfo>()
                .Parameter<string>("path")
                .Required();

            config.Action(nameof(MediaFoldersController.CreateFolder))
                .Returns<FolderNodeInfo>()
                .Parameter<string>("path")
                .Required();

            var moveFolder = config.Action(nameof(MediaFoldersController.MoveFolder))
                .Returns<FolderNodeInfo>();
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
            var set = builder.EntitySet<NewsletterSubscription>("NewsletterSubscriptions");

            set.EntityType
                .Action(nameof(NewsletterSubscriptionsController.Subscribe))
                .ReturnsFromEntitySet(set);

            set.EntityType
                .Action(nameof(NewsletterSubscriptionsController.Unsubscribe))
                .ReturnsFromEntitySet(set);
        }

        private static void BuildOrderItems(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<OrderItem>("OrderItems");

            set.EntityType.Collection.Function(nameof(OrderItemsController.GetShipmentInfo))
                .Returns<OrderItemShipmentInfo>()
                .Parameter<int>("id")
                .Required();
        }

        private static void BuildOrders(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<Order>("Orders");
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
                .ReturnsFromEntitySet(set);

            set.EntityType
                .Action(nameof(OrdersController.PaymentPaid))
                .ReturnsFromEntitySet(set)
                .Parameter<string>("paymentMethodName")
                .Optional();

            set.EntityType
                .Action(nameof(OrdersController.PaymentRefund))
                .ReturnsFromEntitySet(set)
                .Parameter<bool>("online")
                .Required();

            set.EntityType
                .Action(nameof(OrdersController.Cancel))
                .ReturnsFromEntitySet(set)
                .Parameter<bool>("notifyCustomer")
                .HasDefaultValue(bool.TrueString)
                .Optional();

            set.EntityType
                .Action(nameof(OrdersController.CompleteOrder))
                .ReturnsFromEntitySet(set);

            set.EntityType
                .Action(nameof(OrdersController.ReOrder))
                .ReturnsFromEntitySet(set);

            var addShipment = set.EntityType
                .Action(nameof(OrdersController.AddShipment))
                .ReturnsFromEntitySet<Shipment>("Shipments");

            addShipment.Parameter<string>("trackingNumber")
                .Optional();
            addShipment.Parameter<string>("trackingUrl")
                .Optional();
            addShipment.Parameter<bool>("isShipped")
                .HasDefaultValue(bool.FalseString)
                .Optional();
            addShipment.Parameter<bool>("notifyCustomer")
                .HasDefaultValue(bool.TrueString)
                .Optional();
        }

        private static void BuildPaymentMethods(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<PaymentMethod>("PaymentMethods");

            var getAllPaymentMethods = set.EntityType.Collection.Function(nameof(PaymentMethodsController.GetAllPaymentMethods))
                .Returns<string[]>();

            getAllPaymentMethods.Parameter<bool>("active")
                .Required();
            getAllPaymentMethods.Parameter<int>("storeId")
                .Optional();
        }

        private static void BuildProducts(ODataModelBuilder builder)
        {
            var set = builder.EntitySet<Product>("Products");
            var config = set.EntityType.Collection;

            var search = config.Action(nameof(ProductsController.Search))
                .ReturnsFromEntitySet(set);
            search.Parameter<string>("q").Required();
            search.Parameter<int>("i").Optional();
            search.Parameter<int>("s").Optional();
            search.Parameter<ProductSortingEnum>("o").Optional();
            search.Parameter<string>("c").Optional();
            search.Parameter<string>("m").Optional();
            search.Parameter<string>("d").Optional();
            search.Parameter<string>("p").Optional();
            search.Parameter<double>("r").Optional();
            search.Parameter<bool>("a").Optional();
            search.Parameter<bool>("n").Optional();

            var calcPrice = set.EntityType
                .Action(nameof(ProductsController.CalculatePrice))
                .Returns<CalculatedProductPrice>();
            calcPrice.Parameter<bool>("forListing")
                .HasDefaultValue(bool.FalseString)
                .Optional();
            calcPrice.Parameter<int>("quantity")
                .HasDefaultValue("1")
                .Optional();
            calcPrice.Parameter<int>("customerId")
                .HasDefaultValue("0")
                .Optional();
            calcPrice.Parameter<int>("targetCurrencyId")
                .HasDefaultValue("0")
                .Optional();

            set.EntityType
                .Action(nameof(ProductsController.CreateAttributeCombinations))
                .ReturnsCollectionFromEntitySet<ProductVariantAttributeCombination>("ProductVariantAttributeCombinations");

            var manageAttributes = set.EntityType
                .Action(nameof(ProductsController.ManageAttributes))
                .ReturnsCollectionFromEntitySet<ProductVariantAttribute>("ProductVariantAttributes");
            manageAttributes.CollectionParameter<ManagedProductAttribute>("attributes")
                .Required();
            manageAttributes.Parameter<bool>("synchronize")
                .HasDefaultValue(bool.FalseString)
                .Optional();
        }
    }
}