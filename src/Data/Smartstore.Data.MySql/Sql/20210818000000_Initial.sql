/*
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory_Core` (
    `MigrationId` varchar(95) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory_Core` PRIMARY KEY (`MigrationId`)
);
*/

START TRANSACTION;

CREATE TABLE `ActivityLogType` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SystemKeyword` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Enabled` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ActivityLogType` PRIMARY KEY (`Id`)
);

CREATE TABLE `Campaign` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Subject` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Body` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Campaign` PRIMARY KEY (`Id`)
);

CREATE TABLE `CategoryTemplate` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `ViewPath` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_CategoryTemplate` PRIMARY KEY (`Id`)
);

CREATE TABLE `CheckoutAttribute` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `IsActive` tinyint(1) NOT NULL,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `TextPrompt` longtext CHARACTER SET utf8mb4 NULL,
    `IsRequired` tinyint(1) NOT NULL,
    `ShippableProductRequired` tinyint(1) NOT NULL,
    `IsTaxExempt` tinyint(1) NOT NULL,
    `TaxCategoryId` int NOT NULL,
    `DisplayOrder` int NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `AttributeControlTypeId` int NOT NULL,
    CONSTRAINT `PK_CheckoutAttribute` PRIMARY KEY (`Id`)
);

CREATE TABLE `CrossSellProduct` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId1` int NOT NULL,
    `ProductId2` int NOT NULL,
    CONSTRAINT `PK_CrossSellProduct` PRIMARY KEY (`Id`)
);

CREATE TABLE `Currency` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CurrencyCode` varchar(5) CHARACTER SET utf8mb4 NOT NULL,
    `Rate` decimal(18,8) NOT NULL,
    `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 NULL,
    `CustomFormatting` varchar(50) CHARACTER SET utf8mb4 NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `DomainEndings` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `RoundOrderItemsEnabled` tinyint(1) NOT NULL,
    `RoundNumDecimals` int NOT NULL,
    `RoundOrderTotalEnabled` tinyint(1) NOT NULL,
    `RoundOrderTotalDenominator` decimal(18,4) NOT NULL,
    `RoundOrderTotalRule` int NOT NULL,
    CONSTRAINT `PK_Currency` PRIMARY KEY (`Id`)
);

CREATE TABLE `CustomerRole` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `FreeShipping` tinyint(1) NOT NULL,
    `TaxExempt` tinyint(1) NOT NULL,
    `TaxDisplayType` int NULL,
    `Active` tinyint(1) NOT NULL,
    `IsSystemRole` tinyint(1) NOT NULL,
    `SystemName` varchar(255) CHARACTER SET utf8mb4 NULL,
    `OrderTotalMinimum` decimal(18,2) NULL,
    `OrderTotalMaximum` decimal(18,2) NULL,
    CONSTRAINT `PK_CustomerRole` PRIMARY KEY (`Id`)
);

CREATE TABLE `DeliveryTime` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `ColorHexValue` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `IsDefault` tinyint(1) NULL,
    `MinDays` int NULL,
    `MaxDays` int NULL,
    CONSTRAINT `PK_DeliveryTime` PRIMARY KEY (`Id`)
);

CREATE TABLE `Discount` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DiscountTypeId` int NOT NULL,
    `UsePercentage` tinyint(1) NOT NULL,
    `DiscountPercentage` decimal(18,4) NOT NULL,
    `DiscountAmount` decimal(18,4) NOT NULL,
    `StartDateUtc` datetime(6) NULL,
    `EndDateUtc` datetime(6) NULL,
    `RequiresCouponCode` tinyint(1) NOT NULL,
    `CouponCode` varchar(100) CHARACTER SET utf8mb4 NULL,
    `DiscountLimitationId` int NOT NULL,
    `LimitationTimes` int NOT NULL,
    CONSTRAINT `PK_Discount` PRIMARY KEY (`Id`)
);

CREATE TABLE `EmailAccount` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayName` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Host` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Port` int NOT NULL,
    `Username` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Password` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `EnableSsl` tinyint(1) NOT NULL,
    `UseDefaultCredentials` tinyint(1) NOT NULL,
    CONSTRAINT `PK_EmailAccount` PRIMARY KEY (`Id`)
);

CREATE TABLE `GenericAttribute` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `KeyGroup` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Key` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StoreId` int NOT NULL,
    CONSTRAINT `PK_GenericAttribute` PRIMARY KEY (`Id`)
);

CREATE TABLE `Language` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `LanguageCulture` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `UniqueSeoCode` varchar(2) CHARACTER SET utf8mb4 NOT NULL,
    `FlagImageFileName` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Rtl` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Language` PRIMARY KEY (`Id`)
);

CREATE TABLE `ManufacturerTemplate` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `ViewPath` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_ManufacturerTemplate` PRIMARY KEY (`Id`)
);

CREATE TABLE `MeasureDimension` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `SystemKeyword` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Ratio` decimal(18,8) NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_MeasureDimension` PRIMARY KEY (`Id`)
);

CREATE TABLE `MeasureWeight` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `SystemKeyword` longtext CHARACTER SET utf8mb4 NULL,
    `Ratio` decimal(18,8) NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_MeasureWeight` PRIMARY KEY (`Id`)
);

CREATE TABLE `MediaFolder` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ParentId` int NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CanDetectTracks` tinyint(1) NOT NULL,
    `Metadata` longtext CHARACTER SET utf8mb4 NULL,
    `FilesCount` int NOT NULL,
    `Discriminator` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ResKey` longtext CHARACTER SET utf8mb4 NULL,
    `IncludePath` tinyint(1) NULL,
    `Order` int NULL,
    CONSTRAINT `PK_MediaFolder` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MediaFolder_MediaFolder_ParentId` FOREIGN KEY (`ParentId`) REFERENCES `MediaFolder` (`Id`)
);

CREATE TABLE `MediaStorage` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Data` longblob NOT NULL,
    CONSTRAINT `PK_MediaStorage` PRIMARY KEY (`Id`)
);

CREATE TABLE `MediaTag` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_MediaTag` PRIMARY KEY (`Id`)
);

CREATE TABLE `MenuRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SystemName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `IsSystemMenu` tinyint(1) NOT NULL,
    `Template` varchar(400) CHARACTER SET utf8mb4 NULL,
    `WidgetZone` longtext CHARACTER SET utf8mb4 NULL,
    `Title` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    CONSTRAINT `PK_MenuRecord` PRIMARY KEY (`Id`)
);

CREATE TABLE `MessageTemplate` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `To` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `ReplyTo` varchar(500) CHARACTER SET utf8mb4 NULL,
    `ModelTypes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `LastModelTree` longtext CHARACTER SET utf8mb4 NULL,
    `BccEmailAddresses` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Subject` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `Body` longtext CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `EmailAccountId` int NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SendManually` tinyint(1) NOT NULL,
    `Attachment1FileId` int NULL,
    `Attachment2FileId` int NULL,
    `Attachment3FileId` int NULL,
    CONSTRAINT `PK_MessageTemplate` PRIMARY KEY (`Id`)
);

CREATE TABLE `NamedEntity` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityName` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `Slug` longtext CHARACTER SET utf8mb4 NULL,
    `LastMod` datetime(6) NOT NULL,
    `LanguageId` int NULL,
    CONSTRAINT `PK_NamedEntity` PRIMARY KEY (`Id`)
);

CREATE TABLE `NewsLetterSubscription` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `NewsLetterSubscriptionGuid` char(36) NOT NULL,
    `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Active` tinyint(1) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `StoreId` int NOT NULL,
    `WorkingLanguageId` int NOT NULL,
    CONSTRAINT `PK_NewsLetterSubscription` PRIMARY KEY (`Id`)
);

CREATE TABLE `PaymentMethod` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PaymentMethodSystemName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `FullDescription` longtext CHARACTER SET utf8mb4 NULL,
    `RoundOrderTotalEnabled` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    CONSTRAINT `PK_PaymentMethod` PRIMARY KEY (`Id`)
);

CREATE TABLE `PermissionRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SystemName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_PermissionRecord` PRIMARY KEY (`Id`)
);

CREATE TABLE `ProductAttribute` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `Alias` varchar(100) CHARACTER SET utf8mb4 NULL,
    `AllowFiltering` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `FacetTemplateHint` int NOT NULL,
    `IndexOptionNames` tinyint(1) NOT NULL,
    `ExportMappings` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ProductAttribute` PRIMARY KEY (`Id`)
);

CREATE TABLE `ProductTag` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Published` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ProductTag` PRIMARY KEY (`Id`)
);

CREATE TABLE `ProductTemplate` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `ViewPath` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_ProductTemplate` PRIMARY KEY (`Id`)
);

CREATE TABLE `QuantityUnit` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `NamePlural` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `IsDefault` tinyint(1) NOT NULL,
    CONSTRAINT `PK_QuantityUnit` PRIMARY KEY (`Id`)
);

CREATE TABLE `RelatedProduct` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId1` int NOT NULL,
    `ProductId2` int NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_RelatedProduct` PRIMARY KEY (`Id`)
);

CREATE TABLE `RuleSet` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Description` varchar(400) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `Scope` int NOT NULL,
    `IsSubGroup` tinyint(1) NOT NULL,
    `LogicalOperator` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `LastProcessedOnUtc` datetime(6) NULL,
    CONSTRAINT `PK_RuleSet` PRIMARY KEY (`Id`)
);

CREATE TABLE `ScheduleTask` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Alias` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CronExpression` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `Type` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Enabled` tinyint(1) NOT NULL,
    `Priority` int NOT NULL,
    `StopOnError` tinyint(1) NOT NULL,
    `NextRunUtc` datetime(6) NULL,
    `IsHidden` tinyint(1) NOT NULL,
    `RunPerMachine` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ScheduleTask` PRIMARY KEY (`Id`)
);

CREATE TABLE `Setting` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    `StoreId` int NOT NULL,
    CONSTRAINT `PK_Setting` PRIMARY KEY (`Id`)
);

CREATE TABLE `ShippingMethod` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `IgnoreCharges` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ShippingMethod` PRIMARY KEY (`Id`)
);

CREATE TABLE `SpecificationAttribute` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Alias` varchar(30) CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `ShowOnProductPage` tinyint(1) NOT NULL,
    `AllowFiltering` tinyint(1) NOT NULL,
    `FacetSorting` int NOT NULL,
    `FacetTemplateHint` int NOT NULL,
    `IndexOptionNames` tinyint(1) NOT NULL,
    CONSTRAINT `PK_SpecificationAttribute` PRIMARY KEY (`Id`)
);

CREATE TABLE `StoreMapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `EntityName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `StoreId` int NOT NULL,
    CONSTRAINT `PK_StoreMapping` PRIMARY KEY (`Id`)
);

CREATE TABLE `SyncMapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `SourceKey` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `EntityName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `ContextName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `SourceHash` varchar(40) CHARACTER SET utf8mb4 NULL,
    `CustomInt` int NULL,
    `CustomString` longtext CHARACTER SET utf8mb4 NULL,
    `CustomBool` tinyint(1) NULL,
    `SyncedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_SyncMapping` PRIMARY KEY (`Id`)
);

CREATE TABLE `TaxCategory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_TaxCategory` PRIMARY KEY (`Id`)
);

CREATE TABLE `ThemeVariable` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Theme` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Name` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Value` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `StoreId` int NOT NULL,
    CONSTRAINT `PK_ThemeVariable` PRIMARY KEY (`Id`)
);

CREATE TABLE `Topic` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SystemName` longtext CHARACTER SET utf8mb4 NULL,
    `IsSystemTopic` tinyint(1) NOT NULL,
    `HtmlId` longtext CHARACTER SET utf8mb4 NULL,
    `BodyCssClass` longtext CHARACTER SET utf8mb4 NULL,
    `IncludeInSitemap` tinyint(1) NOT NULL,
    `IsPasswordProtected` tinyint(1) NOT NULL,
    `Password` longtext CHARACTER SET utf8mb4 NULL,
    `Title` longtext CHARACTER SET utf8mb4 NULL,
    `ShortTitle` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Intro` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Body` longtext CHARACTER SET utf8mb4 NULL,
    `MetaKeywords` longtext CHARACTER SET utf8mb4 NULL,
    `MetaDescription` longtext CHARACTER SET utf8mb4 NULL,
    `MetaTitle` longtext CHARACTER SET utf8mb4 NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `RenderAsWidget` tinyint(1) NOT NULL,
    `WidgetZone` longtext CHARACTER SET utf8mb4 NULL,
    `WidgetWrapContent` tinyint(1) NULL,
    `WidgetShowTitle` tinyint(1) NOT NULL,
    `WidgetBordered` tinyint(1) NOT NULL,
    `Priority` int NOT NULL,
    `TitleTag` longtext CHARACTER SET utf8mb4 NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    `IsPublished` tinyint(1) NOT NULL,
    `CookieType` int NULL,
    CONSTRAINT `PK_Topic` PRIMARY KEY (`Id`)
);

CREATE TABLE `UrlRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `EntityName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `LanguageId` int NOT NULL,
    CONSTRAINT `PK_UrlRecord` PRIMARY KEY (`Id`)
);

CREATE TABLE `Country` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `AllowsBilling` tinyint(1) NOT NULL,
    `AllowsShipping` tinyint(1) NOT NULL,
    `TwoLetterIsoCode` longtext CHARACTER SET utf8mb4 NULL,
    `ThreeLetterIsoCode` longtext CHARACTER SET utf8mb4 NULL,
    `NumericIsoCode` int NOT NULL,
    `SubjectToVat` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `DisplayCookieManager` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `AddressFormat` longtext CHARACTER SET utf8mb4 NULL,
    `DefaultCurrencyId` int NULL,
    CONSTRAINT `PK_Country` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Country_Currency_DefaultCurrencyId` FOREIGN KEY (`DefaultCurrencyId`) REFERENCES `Currency` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Store` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Url` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `SslEnabled` tinyint(1) NOT NULL,
    `SecureUrl` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ForceSslForAllPages` tinyint(1) NOT NULL,
    `Hosts` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `LogoMediaFileId` int NOT NULL,
    `FavIconMediaFileId` int NULL,
    `PngIconMediaFileId` int NULL,
    `AppleTouchIconMediaFileId` int NULL,
    `MsTileImageMediaFileId` int NULL,
    `MsTileColor` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `HtmlBodyId` longtext CHARACTER SET utf8mb4 NULL,
    `ContentDeliveryNetwork` varchar(400) CHARACTER SET utf8mb4 NULL,
    `PrimaryStoreCurrencyId` int NOT NULL,
    `PrimaryExchangeRateCurrencyId` int NOT NULL,
    CONSTRAINT `PK_Store` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Store_Currency_PrimaryExchangeRateCurrencyId` FOREIGN KEY (`PrimaryExchangeRateCurrencyId`) REFERENCES `Currency` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Store_Currency_PrimaryStoreCurrencyId` FOREIGN KEY (`PrimaryStoreCurrencyId`) REFERENCES `Currency` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `AclRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `EntityName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRoleId` int NOT NULL,
    `IsIdle` tinyint(1) NOT NULL,
    CONSTRAINT `PK_AclRecord` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AclRecord_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `CustomerRole` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `QueuedEmail` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Priority` int NOT NULL,
    `From` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `To` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `ReplyTo` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CC` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Bcc` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Subject` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `Body` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `SentTries` int NOT NULL,
    `SentOnUtc` datetime(6) NULL,
    `EmailAccountId` int NOT NULL,
    `SendManually` tinyint(1) NOT NULL,
    CONSTRAINT `PK_QueuedEmail` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_QueuedEmail_EmailAccount_EmailAccountId` FOREIGN KEY (`EmailAccountId`) REFERENCES `EmailAccount` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `LocaleStringResource` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LanguageId` int NOT NULL,
    `ResourceName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `ResourceValue` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IsFromPlugin` tinyint(1) NULL,
    `IsTouched` tinyint(1) NULL,
    CONSTRAINT `PK_LocaleStringResource` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LocaleStringResource_Language_LanguageId` FOREIGN KEY (`LanguageId`) REFERENCES `Language` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `LocalizedProperty` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `EntityId` int NOT NULL,
    `LanguageId` int NOT NULL,
    `LocaleKeyGroup` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `LocaleKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LocaleValue` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_LocalizedProperty` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LocalizedProperty_Language_LanguageId` FOREIGN KEY (`LanguageId`) REFERENCES `Language` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `MediaFile` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FolderId` int NULL,
    `Name` varchar(300) CHARACTER SET utf8mb4 NULL,
    `Alt` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Title` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Extension` varchar(50) CHARACTER SET utf8mb4 NULL,
    `MimeType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `MediaType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Size` int NOT NULL,
    `PixelSize` int NULL,
    `Metadata` longtext CHARACTER SET utf8mb4 NULL,
    `Width` int NULL,
    `Height` int NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `IsTransient` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `Hidden` tinyint(1) NOT NULL,
    `Version` int NOT NULL,
    `MediaStorageId` int NULL,
    CONSTRAINT `PK_MediaFile` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MediaFile_MediaFolder_FolderId` FOREIGN KEY (`FolderId`) REFERENCES `MediaFolder` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_MediaFile_MediaStorage_MediaStorageId` FOREIGN KEY (`MediaStorageId`) REFERENCES `MediaStorage` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `MenuItemRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `MenuId` int NOT NULL,
    `ParentItemId` int NOT NULL,
    `ProviderName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Model` longtext CHARACTER SET utf8mb4 NULL,
    `Title` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ShortDescription` varchar(400) CHARACTER SET utf8mb4 NULL,
    `PermissionNames` longtext CHARACTER SET utf8mb4 NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `BeginGroup` tinyint(1) NOT NULL,
    `ShowExpanded` tinyint(1) NOT NULL,
    `NoFollow` tinyint(1) NOT NULL,
    `NewWindow` tinyint(1) NOT NULL,
    `Icon` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Style` varchar(10) CHARACTER SET utf8mb4 NULL,
    `IconColor` varchar(100) CHARACTER SET utf8mb4 NULL,
    `HtmlId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CssClass` varchar(100) CHARACTER SET utf8mb4 NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    CONSTRAINT `PK_MenuItemRecord` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MenuItemRecord_MenuRecord_MenuId` FOREIGN KEY (`MenuId`) REFERENCES `MenuRecord` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `PermissionRoleMapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Allow` tinyint(1) NOT NULL,
    `PermissionRecordId` int NOT NULL,
    `CustomerRoleId` int NOT NULL,
    CONSTRAINT `PK_PermissionRoleMapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PermissionRoleMapping_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `CustomerRole` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_PermissionRoleMapping_PermissionRecord_PermissionRecordId` FOREIGN KEY (`PermissionRecordId`) REFERENCES `PermissionRecord` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ProductAttributeOptionsSet` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ProductAttributeId` int NOT NULL,
    CONSTRAINT `PK_ProductAttributeOptionsSet` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductAttributeOptionsSet_ProductAttribute_ProductAttribute~` FOREIGN KEY (`ProductAttributeId`) REFERENCES `ProductAttribute` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Rule` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RuleSetId` int NOT NULL,
    `RuleType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Operator` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Rule` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Rule_RuleSet_RuleSetId` FOREIGN KEY (`RuleSetId`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RuleSet_CustomerRole_Mapping` (
    `CustomerRole_Id` int NOT NULL,
    `RuleSetEntity_Id` int NOT NULL,
    CONSTRAINT `PK_RuleSet_CustomerRole_Mapping` PRIMARY KEY (`CustomerRole_Id`, `RuleSetEntity_Id`),
    CONSTRAINT `FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRol` FOREIGN KEY (`CustomerRole_Id`) REFERENCES `CustomerRole` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RuleSet_Discount_Mapping` (
    `Discount_Id` int NOT NULL,
    `RuleSetEntity_Id` int NOT NULL,
    CONSTRAINT `PK_RuleSet_Discount_Mapping` PRIMARY KEY (`Discount_Id`, `RuleSetEntity_Id`),
    CONSTRAINT `FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `Discount` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RuleSet_PaymentMethod_Mapping` (
    `PaymentMethod_Id` int NOT NULL,
    `RuleSetEntity_Id` int NOT NULL,
    CONSTRAINT `PK_RuleSet_PaymentMethod_Mapping` PRIMARY KEY (`PaymentMethod_Id`, `RuleSetEntity_Id`),
    CONSTRAINT `FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMe` FOREIGN KEY (`PaymentMethod_Id`) REFERENCES `PaymentMethod` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_I` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ExportProfile` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `FolderName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `FileNamePattern` varchar(400) CHARACTER SET utf8mb4 NULL,
    `SystemName` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ProviderSystemName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IsSystemProfile` tinyint(1) NOT NULL,
    `Enabled` tinyint(1) NOT NULL,
    `ExportRelatedData` tinyint(1) NOT NULL,
    `Filtering` longtext CHARACTER SET utf8mb4 NULL,
    `Projection` longtext CHARACTER SET utf8mb4 NULL,
    `ProviderConfigData` longtext CHARACTER SET utf8mb4 NULL,
    `ResultInfo` longtext CHARACTER SET utf8mb4 NULL,
    `Offset` int NOT NULL,
    `Limit` int NOT NULL,
    `BatchSize` int NOT NULL,
    `PerStore` tinyint(1) NOT NULL,
    `EmailAccountId` int NOT NULL,
    `CompletedEmailAddresses` varchar(400) CHARACTER SET utf8mb4 NULL,
    `CreateZipArchive` tinyint(1) NOT NULL,
    `Cleanup` tinyint(1) NOT NULL,
    `SchedulingTaskId` int NOT NULL,
    CONSTRAINT `PK_ExportProfile` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ExportProfile_ScheduleTask_SchedulingTaskId` FOREIGN KEY (`SchedulingTaskId`) REFERENCES `ScheduleTask` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ImportProfile` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `FolderName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `FileTypeId` int NOT NULL,
    `EntityTypeId` int NOT NULL,
    `Enabled` tinyint(1) NOT NULL,
    `ImportRelatedData` tinyint(1) NOT NULL,
    `Skip` int NOT NULL,
    `Take` int NOT NULL,
    `UpdateOnly` tinyint(1) NOT NULL,
    `KeyFieldNames` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `FileTypeConfiguration` longtext CHARACTER SET utf8mb4 NULL,
    `ExtraData` longtext CHARACTER SET utf8mb4 NULL,
    `ColumnMapping` longtext CHARACTER SET utf8mb4 NULL,
    `ResultInfo` longtext CHARACTER SET utf8mb4 NULL,
    `SchedulingTaskId` int NOT NULL,
    CONSTRAINT `PK_ImportProfile` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ImportProfile_ScheduleTask_SchedulingTaskId` FOREIGN KEY (`SchedulingTaskId`) REFERENCES `ScheduleTask` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ScheduleTaskHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ScheduleTaskId` int NOT NULL,
    `IsRunning` tinyint(1) NOT NULL,
    `MachineName` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `StartedOnUtc` datetime(6) NOT NULL,
    `FinishedOnUtc` datetime(6) NULL,
    `SucceededOnUtc` datetime(6) NULL,
    `Error` longtext CHARACTER SET utf8mb4 NULL,
    `ProgressPercent` int NULL,
    `ProgressMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ScheduleTaskHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ScheduleTaskHistory_ScheduleTask_ScheduleTaskId` FOREIGN KEY (`ScheduleTaskId`) REFERENCES `ScheduleTask` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RuleSet_ShippingMethod_Mapping` (
    `ShippingMethod_Id` int NOT NULL,
    `RuleSetEntity_Id` int NOT NULL,
    CONSTRAINT `PK_RuleSet_ShippingMethod_Mapping` PRIMARY KEY (`ShippingMethod_Id`, `RuleSetEntity_Id`),
    CONSTRAINT `FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_Shippin` FOREIGN KEY (`ShippingMethod_Id`) REFERENCES `ShippingMethod` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `SpecificationAttributeOption` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SpecificationAttributeId` int NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Alias` varchar(30) CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    `NumberValue` decimal(18,4) NOT NULL,
    `MediaFileId` int NOT NULL,
    `Color` varchar(100) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_SpecificationAttributeOption` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_SpecificationAttributeOption_SpecificationAttribute_Specific~` FOREIGN KEY (`SpecificationAttributeId`) REFERENCES `SpecificationAttribute` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `StateProvince` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CountryId` int NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Abbreviation` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_StateProvince` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_StateProvince_Country_CountryId` FOREIGN KEY (`CountryId`) REFERENCES `Country` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Category` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `FullName` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `BottomDescription` longtext CHARACTER SET utf8mb4 NULL,
    `ExternalLink` varchar(255) CHARACTER SET utf8mb4 NULL,
    `BadgeText` varchar(400) CHARACTER SET utf8mb4 NULL,
    `BadgeStyle` int NOT NULL,
    `Alias` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CategoryTemplateId` int NOT NULL,
    `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 NULL,
    `MetaDescription` longtext CHARACTER SET utf8mb4 NULL,
    `MetaTitle` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ParentCategoryId` int NOT NULL,
    `MediaFileId` int NULL,
    `PageSize` int NULL,
    `AllowCustomersToSelectPageSize` tinyint(1) NULL,
    `PageSizeOptions` varchar(200) CHARACTER SET utf8mb4 NULL,
    `PriceRanges` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ShowOnHomePage` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `DefaultViewMode` longtext CHARACTER SET utf8mb4 NULL,
    `HasDiscountsApplied` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Category` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Category_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `CheckoutAttributeValue` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `PriceAdjustment` decimal(18,4) NOT NULL,
    `WeightAdjustment` decimal(18,4) NOT NULL,
    `IsPreSelected` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `Color` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CheckoutAttributeId` int NOT NULL,
    `MediaFileId` int NULL,
    CONSTRAINT `PK_CheckoutAttributeValue` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CheckoutAttributeValue_CheckoutAttribute_CheckoutAttributeId` FOREIGN KEY (`CheckoutAttributeId`) REFERENCES `CheckoutAttribute` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_CheckoutAttributeValue_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `Download` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `DownloadGuid` char(36) NOT NULL,
    `UseDownloadUrl` tinyint(1) NOT NULL,
    `DownloadUrl` longtext CHARACTER SET utf8mb4 NULL,
    `IsTransient` tinyint(1) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `MediaFileId` int NULL,
    `EntityId` int NOT NULL,
    `EntityName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `FileVersion` varchar(30) CHARACTER SET utf8mb4 NULL,
    `Changelog` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Download` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Download_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `Manufacturer` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `BottomDescription` longtext CHARACTER SET utf8mb4 NULL,
    `ManufacturerTemplateId` int NOT NULL,
    `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 NULL,
    `MetaDescription` longtext CHARACTER SET utf8mb4 NULL,
    `MetaTitle` varchar(400) CHARACTER SET utf8mb4 NULL,
    `MediaFileId` int NULL,
    `PageSize` int NULL,
    `AllowCustomersToSelectPageSize` tinyint(1) NULL,
    `PageSizeOptions` varchar(200) CHARACTER SET utf8mb4 NULL,
    `PriceRanges` varchar(400) CHARACTER SET utf8mb4 NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `HasDiscountsApplied` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Manufacturer` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Manufacturer_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `MediaFile_Tag_Mapping` (
    `MediaFile_Id` int NOT NULL,
    `MediaTag_Id` int NOT NULL,
    CONSTRAINT `PK_MediaFile_Tag_Mapping` PRIMARY KEY (`MediaFile_Id`, `MediaTag_Id`),
    CONSTRAINT `FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id` FOREIGN KEY (`MediaFile_Id`) REFERENCES `MediaFile` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id` FOREIGN KEY (`MediaTag_Id`) REFERENCES `MediaTag` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `MediaTrack` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `MediaFileId` int NOT NULL,
    `Album` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `EntityId` int NOT NULL,
    `EntityName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Property` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_MediaTrack` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_MediaTrack_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `QueuedEmailAttachment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `QueuedEmailId` int NOT NULL,
    `StorageLocation` int NOT NULL,
    `Path` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `MediaFileId` int NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `MimeType` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `MediaStorageId` int NULL,
    CONSTRAINT `PK_QueuedEmailAttachment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_QueuedEmailAttachment_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_QueuedEmailAttachment_MediaStorage_MediaStorageId` FOREIGN KEY (`MediaStorageId`) REFERENCES `MediaStorage` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_QueuedEmailAttachment_QueuedEmail_QueuedEmailId` FOREIGN KEY (`QueuedEmailId`) REFERENCES `QueuedEmail` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ProductAttributeOption` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductAttributeOptionsSetId` int NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `Alias` varchar(100) CHARACTER SET utf8mb4 NULL,
    `MediaFileId` int NOT NULL,
    `Color` varchar(100) CHARACTER SET utf8mb4 NULL,
    `PriceAdjustment` decimal(18,4) NOT NULL,
    `WeightAdjustment` decimal(18,4) NOT NULL,
    `IsPreSelected` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `ValueTypeId` int NOT NULL,
    `LinkedProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    CONSTRAINT `PK_ProductAttributeOption` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductAttributeOption_ProductAttributeOptionsSet_ProductAtt~` FOREIGN KEY (`ProductAttributeOptionsSetId`) REFERENCES `ProductAttributeOptionsSet` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ExportDeployment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProfileId` int NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Enabled` tinyint(1) NOT NULL,
    `ResultInfo` longtext CHARACTER SET utf8mb4 NULL,
    `DeploymentTypeId` int NOT NULL,
    `Username` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Password` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Url` longtext CHARACTER SET utf8mb4 NULL,
    `HttpTransmissionTypeId` int NOT NULL,
    `HttpTransmissionType` int NOT NULL,
    `FileSystemPath` varchar(400) CHARACTER SET utf8mb4 NULL,
    `SubFolder` varchar(400) CHARACTER SET utf8mb4 NULL,
    `EmailAddresses` longtext CHARACTER SET utf8mb4 NULL,
    `EmailSubject` varchar(400) CHARACTER SET utf8mb4 NULL,
    `EmailAccountId` int NOT NULL,
    `PassiveMode` tinyint(1) NOT NULL,
    `UseSsl` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ExportDeployment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ExportDeployment_ExportProfile_ProfileId` FOREIGN KEY (`ProfileId`) REFERENCES `ExportProfile` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Address` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Salutation` longtext CHARACTER SET utf8mb4 NULL,
    `Title` longtext CHARACTER SET utf8mb4 NULL,
    `FirstName` longtext CHARACTER SET utf8mb4 NULL,
    `LastName` longtext CHARACTER SET utf8mb4 NULL,
    `Email` longtext CHARACTER SET utf8mb4 NULL,
    `Company` longtext CHARACTER SET utf8mb4 NULL,
    `CountryId` int NULL,
    `StateProvinceId` int NULL,
    `City` longtext CHARACTER SET utf8mb4 NULL,
    `Address1` longtext CHARACTER SET utf8mb4 NULL,
    `Address2` longtext CHARACTER SET utf8mb4 NULL,
    `ZipPostalCode` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `FaxNumber` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_Address` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Address_Country_CountryId` FOREIGN KEY (`CountryId`) REFERENCES `Country` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Address_StateProvince_StateProvinceId` FOREIGN KEY (`StateProvinceId`) REFERENCES `StateProvince` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Discount_AppliedToCategories` (
    `Discount_Id` int NOT NULL,
    `Category_Id` int NOT NULL,
    CONSTRAINT `PK_Discount_AppliedToCategories` PRIMARY KEY (`Discount_Id`, `Category_Id`),
    CONSTRAINT `FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id` FOREIGN KEY (`Category_Id`) REFERENCES `Category` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `Discount` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RuleSet_Category_Mapping` (
    `Category_Id` int NOT NULL,
    `RuleSetEntity_Id` int NOT NULL,
    CONSTRAINT `PK_RuleSet_Category_Mapping` PRIMARY KEY (`Category_Id`, `RuleSetEntity_Id`),
    CONSTRAINT `FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id` FOREIGN KEY (`Category_Id`) REFERENCES `Category` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `RuleSet` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Product` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductTypeId` int NOT NULL,
    `ParentGroupedProductId` int NOT NULL,
    `Visibility` int NOT NULL,
    `VisibleIndividually` tinyint(1) NOT NULL,
    `Condition` int NOT NULL,
    `Name` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `ShortDescription` longtext CHARACTER SET utf8mb4 NULL,
    `FullDescription` longtext CHARACTER SET utf8mb4 NULL,
    `AdminComment` longtext CHARACTER SET utf8mb4 NULL,
    `ProductTemplateId` int NOT NULL,
    `ShowOnHomePage` tinyint(1) NOT NULL,
    `HomePageDisplayOrder` int NOT NULL,
    `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 NULL,
    `MetaDescription` longtext CHARACTER SET utf8mb4 NULL,
    `MetaTitle` varchar(400) CHARACTER SET utf8mb4 NULL,
    `AllowCustomerReviews` tinyint(1) NOT NULL,
    `ApprovedRatingSum` int NOT NULL,
    `NotApprovedRatingSum` int NOT NULL,
    `ApprovedTotalReviews` int NOT NULL,
    `NotApprovedTotalReviews` int NOT NULL,
    `SubjectToAcl` tinyint(1) NOT NULL,
    `LimitedToStores` tinyint(1) NOT NULL,
    `Sku` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ManufacturerPartNumber` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Gtin` varchar(400) CHARACTER SET utf8mb4 NULL,
    `IsGiftCard` tinyint(1) NOT NULL,
    `GiftCardTypeId` int NOT NULL,
    `RequireOtherProducts` tinyint(1) NOT NULL,
    `RequiredProductIds` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `AutomaticallyAddRequiredProducts` tinyint(1) NOT NULL,
    `IsDownload` tinyint(1) NOT NULL,
    `DownloadId` int NOT NULL,
    `UnlimitedDownloads` tinyint(1) NOT NULL,
    `MaxNumberOfDownloads` int NOT NULL,
    `DownloadExpirationDays` int NULL,
    `DownloadActivationTypeId` int NOT NULL,
    `HasSampleDownload` tinyint(1) NOT NULL,
    `SampleDownloadId` int NULL,
    `HasUserAgreement` tinyint(1) NOT NULL,
    `UserAgreementText` longtext CHARACTER SET utf8mb4 NULL,
    `IsRecurring` tinyint(1) NOT NULL,
    `RecurringCycleLength` int NOT NULL,
    `RecurringCyclePeriodId` int NOT NULL,
    `RecurringTotalCycles` int NOT NULL,
    `IsShipEnabled` tinyint(1) NOT NULL,
    `IsFreeShipping` tinyint(1) NOT NULL,
    `AdditionalShippingCharge` decimal(18,4) NOT NULL,
    `IsTaxExempt` tinyint(1) NOT NULL,
    `IsEsd` tinyint(1) NOT NULL,
    `TaxCategoryId` int NOT NULL,
    `ManageInventoryMethodId` int NOT NULL,
    `StockQuantity` int NOT NULL,
    `DisplayStockAvailability` tinyint(1) NOT NULL,
    `DisplayStockQuantity` tinyint(1) NOT NULL,
    `MinStockQuantity` int NOT NULL,
    `LowStockActivityId` int NOT NULL,
    `NotifyAdminForQuantityBelow` int NOT NULL,
    `BackorderModeId` int NOT NULL,
    `AllowBackInStockSubscriptions` tinyint(1) NOT NULL,
    `OrderMinimumQuantity` int NOT NULL,
    `OrderMaximumQuantity` int NOT NULL,
    `QuantityStep` int NOT NULL,
    `QuantiyControlType` int NOT NULL,
    `HideQuantityControl` tinyint(1) NOT NULL,
    `AllowedQuantities` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `DisableBuyButton` tinyint(1) NOT NULL,
    `DisableWishlistButton` tinyint(1) NOT NULL,
    `AvailableForPreOrder` tinyint(1) NOT NULL,
    `CallForPrice` tinyint(1) NOT NULL,
    `Price` decimal(18,4) NOT NULL,
    `OldPrice` decimal(18,4) NOT NULL,
    `ProductCost` decimal(18,4) NOT NULL,
    `SpecialPrice` decimal(18,4) NULL,
    `SpecialPriceStartDateTimeUtc` datetime(6) NULL,
    `SpecialPriceEndDateTimeUtc` datetime(6) NULL,
    `CustomerEntersPrice` tinyint(1) NOT NULL,
    `MinimumCustomerEnteredPrice` decimal(18,4) NOT NULL,
    `MaximumCustomerEnteredPrice` decimal(18,4) NOT NULL,
    `HasTierPrices` tinyint(1) NOT NULL,
    `LowestAttributeCombinationPrice` decimal(18,4) NULL,
    `AttributeChoiceBehaviour` int NOT NULL,
    `Weight` decimal(18,4) NOT NULL,
    `Length` decimal(18,4) NOT NULL,
    `Width` decimal(18,4) NOT NULL,
    `Height` decimal(18,4) NOT NULL,
    `AvailableStartDateTimeUtc` datetime(6) NULL,
    `AvailableEndDateTimeUtc` datetime(6) NULL,
    `DisplayOrder` int NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `IsSystemProduct` tinyint(1) NOT NULL,
    `SystemName` varchar(400) CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `DeliveryTimeId` int NULL,
    `QuantityUnitId` int NULL,
    `CustomsTariffNumber` varchar(30) CHARACTER SET utf8mb4 NULL,
    `CountryOfOriginId` int NULL,
    `BasePriceEnabled` tinyint(1) NOT NULL,
    `BasePriceMeasureUnit` varchar(50) CHARACTER SET utf8mb4 NULL,
    `BasePriceAmount` decimal(18,4) NULL,
    `BasePriceBaseAmount` int NULL,
    `BundleTitleText` varchar(400) CHARACTER SET utf8mb4 NULL,
    `BundlePerItemShipping` tinyint(1) NOT NULL,
    `BundlePerItemPricing` tinyint(1) NOT NULL,
    `BundlePerItemShoppingCart` tinyint(1) NOT NULL,
    `MainPictureId` int NULL,
    `HasPreviewPicture` tinyint(1) NOT NULL,
    `HasDiscountsApplied` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Product` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_Country_CountryOfOriginId` FOREIGN KEY (`CountryOfOriginId`) REFERENCES `Country` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Product_DeliveryTime_DeliveryTimeId` FOREIGN KEY (`DeliveryTimeId`) REFERENCES `DeliveryTime` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Product_Download_SampleDownloadId` FOREIGN KEY (`SampleDownloadId`) REFERENCES `Download` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Product_QuantityUnit_QuantityUnitId` FOREIGN KEY (`QuantityUnitId`) REFERENCES `QuantityUnit` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `Discount_AppliedToManufacturers` (
    `Discount_Id` int NOT NULL,
    `Manufacturer_Id` int NOT NULL,
    CONSTRAINT `PK_Discount_AppliedToManufacturers` PRIMARY KEY (`Discount_Id`, `Manufacturer_Id`),
    CONSTRAINT `FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `Discount` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufact` FOREIGN KEY (`Manufacturer_Id`) REFERENCES `Manufacturer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Affiliate` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Active` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `AddressId` int NOT NULL,
    CONSTRAINT `PK_Affiliate` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Affiliate_Address_AddressId` FOREIGN KEY (`AddressId`) REFERENCES `Address` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Customer` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerGuid` char(36) NOT NULL,
    `Username` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Password` varchar(500) CHARACTER SET utf8mb4 NULL,
    `PasswordFormatId` int NOT NULL,
    `PasswordSalt` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AdminComment` longtext CHARACTER SET utf8mb4 NULL,
    `IsTaxExempt` tinyint(1) NOT NULL,
    `AffiliateId` int NOT NULL,
    `Active` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `IsSystemAccount` tinyint(1) NOT NULL,
    `SystemName` varchar(500) CHARACTER SET utf8mb4 NULL,
    `LastIpAddress` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `LastLoginDateUtc` datetime(6) NULL,
    `LastActivityDateUtc` datetime(6) NOT NULL,
    `Salutation` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Title` varchar(100) CHARACTER SET utf8mb4 NULL,
    `FirstName` varchar(225) CHARACTER SET utf8mb4 NULL,
    `LastName` varchar(225) CHARACTER SET utf8mb4 NULL,
    `FullName` varchar(450) CHARACTER SET utf8mb4 NULL,
    `Company` varchar(255) CHARACTER SET utf8mb4 NULL,
    `CustomerNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
    `BirthDate` datetime(6) NULL,
    `Gender` longtext CHARACTER SET utf8mb4 NULL,
    `VatNumberStatusId` int NOT NULL,
    `TimeZoneId` longtext CHARACTER SET utf8mb4 NULL,
    `TaxDisplayTypeId` int NOT NULL,
    `LastForumVisit` datetime(6) NULL,
    `LastUserAgent` longtext CHARACTER SET utf8mb4 NULL,
    `LastUserDeviceType` longtext CHARACTER SET utf8mb4 NULL,
    `BillingAddress_Id` int NULL,
    `ShippingAddress_Id` int NULL,
    CONSTRAINT `PK_Customer` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Customer_Address_BillingAddress_Id` FOREIGN KEY (`BillingAddress_Id`) REFERENCES `Address` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Customer_Address_ShippingAddress_Id` FOREIGN KEY (`ShippingAddress_Id`) REFERENCES `Address` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Discount_AppliedToProducts` (
    `Discount_Id` int NOT NULL,
    `Product_Id` int NOT NULL,
    CONSTRAINT `PK_Discount_AppliedToProducts` PRIMARY KEY (`Discount_Id`, `Product_Id`),
    CONSTRAINT `FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `Discount` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id` FOREIGN KEY (`Product_Id`) REFERENCES `Product` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Product_Category_Mapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CategoryId` int NOT NULL,
    `ProductId` int NOT NULL,
    `IsFeaturedProduct` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `IsSystemMapping` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Product_Category_Mapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_Category_Mapping_Category_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Category` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Product_Category_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Product_Manufacturer_Mapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ManufacturerId` int NOT NULL,
    `ProductId` int NOT NULL,
    `IsFeaturedProduct` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Product_Manufacturer_Mapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_Manufacturer_Mapping_Manufacturer_ManufacturerId` FOREIGN KEY (`ManufacturerId`) REFERENCES `Manufacturer` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Product_Manufacturer_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Product_MediaFile_Mapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `MediaFileId` int NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Product_MediaFile_Mapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_MediaFile_Mapping_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `MediaFile` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Product_MediaFile_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Product_ProductAttribute_Mapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `ProductAttributeId` int NOT NULL,
    `TextPrompt` longtext CHARACTER SET utf8mb4 NULL,
    `CustomData` longtext CHARACTER SET utf8mb4 NULL,
    `IsRequired` tinyint(1) NOT NULL,
    `AttributeControlTypeId` int NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Product_ProductAttribute_Mapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_ProductAttribute_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Product_ProductAttribute_Mapping_ProductAttribute_ProductAtt~` FOREIGN KEY (`ProductAttributeId`) REFERENCES `ProductAttribute` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Product_ProductTag_Mapping` (
    `Product_Id` int NOT NULL,
    `ProductTag_Id` int NOT NULL,
    CONSTRAINT `PK_Product_ProductTag_Mapping` PRIMARY KEY (`Product_Id`, `ProductTag_Id`),
    CONSTRAINT `FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id` FOREIGN KEY (`Product_Id`) REFERENCES `Product` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id` FOREIGN KEY (`ProductTag_Id`) REFERENCES `ProductTag` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Product_SpecificationAttribute_Mapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SpecificationAttributeOptionId` int NOT NULL,
    `ProductId` int NOT NULL,
    `AllowFiltering` tinyint(1) NULL,
    `ShowOnProductPage` tinyint(1) NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Product_SpecificationAttribute_Mapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Product_SpecificationAttribute_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Product_SpecificationAttribute_Mapping_SpecificationAttribut~` FOREIGN KEY (`SpecificationAttributeOptionId`) REFERENCES `SpecificationAttributeOption` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ProductBundleItem` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `BundleProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    `Discount` decimal(18,4) NULL,
    `DiscountPercentage` tinyint(1) NOT NULL,
    `Name` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ShortDescription` longtext CHARACTER SET utf8mb4 NULL,
    `FilterAttributes` tinyint(1) NOT NULL,
    `HideThumbnail` tinyint(1) NOT NULL,
    `Visible` tinyint(1) NOT NULL,
    `Published` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ProductBundleItem` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductBundleItem_Product_BundleProductId` FOREIGN KEY (`BundleProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ProductBundleItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ProductVariantAttributeCombination` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `Sku` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Gtin` varchar(400) CHARACTER SET utf8mb4 NULL,
    `ManufacturerPartNumber` varchar(400) CHARACTER SET utf8mb4 NULL,
    `Price` decimal(18,4) NULL,
    `Length` decimal(18,4) NULL,
    `Width` decimal(18,4) NULL,
    `Height` decimal(18,4) NULL,
    `BasePriceAmount` decimal(18,4) NULL,
    `BasePriceBaseAmount` int NULL,
    `AssignedMediaFileIds` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `DeliveryTimeId` int NULL,
    `QuantityUnitId` int NULL,
    `AttributesXml` longtext CHARACTER SET utf8mb4 NULL,
    `StockQuantity` int NOT NULL,
    `AllowOutOfStockOrders` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ProductVariantAttributeCombination` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductVariantAttributeCombination_DeliveryTime_DeliveryTime~` FOREIGN KEY (`DeliveryTimeId`) REFERENCES `DeliveryTime` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_ProductVariantAttributeCombination_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ProductVariantAttributeCombination_QuantityUnit_QuantityUnit~` FOREIGN KEY (`QuantityUnitId`) REFERENCES `QuantityUnit` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `TierPrice` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `StoreId` int NOT NULL,
    `Quantity` int NOT NULL,
    `Price` decimal(18,4) NOT NULL,
    `CalculationMethod` int NOT NULL,
    `CustomerRoleId` int NULL,
    CONSTRAINT `PK_TierPrice` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_TierPrice_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `CustomerRole` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_TierPrice_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ActivityLog` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ActivityLogTypeId` int NOT NULL,
    `CustomerId` int NOT NULL,
    `Comment` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ActivityLog` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ActivityLog_ActivityLogType_ActivityLogTypeId` FOREIGN KEY (`ActivityLogTypeId`) REFERENCES `ActivityLogType` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ActivityLog_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `BackInStockSubscription` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StoreId` int NOT NULL,
    `ProductId` int NOT NULL,
    `CustomerId` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_BackInStockSubscription` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_BackInStockSubscription_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_BackInStockSubscription_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `CustomerAddresses` (
    `Customer_Id` int NOT NULL,
    `Address_Id` int NOT NULL,
    CONSTRAINT `PK_CustomerAddresses` PRIMARY KEY (`Customer_Id`, `Address_Id`),
    CONSTRAINT `FK_dbo.CustomerAddresses_dbo.Address_Address_Id` FOREIGN KEY (`Address_Id`) REFERENCES `Address` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id` FOREIGN KEY (`Customer_Id`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `CustomerContent` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerId` int NOT NULL,
    `IpAddress` varchar(200) CHARACTER SET utf8mb4 NULL,
    `IsApproved` tinyint(1) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_CustomerContent` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CustomerContent_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `CustomerRoleMapping` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerId` int NOT NULL,
    `CustomerRoleId` int NOT NULL,
    `IsSystemMapping` tinyint(1) NOT NULL,
    CONSTRAINT `PK_CustomerRoleMapping` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CustomerRoleMapping_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_CustomerRoleMapping_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `CustomerRole` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ExternalAuthenticationRecord` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerId` int NOT NULL,
    `Email` longtext CHARACTER SET utf8mb4 NULL,
    `ExternalIdentifier` longtext CHARACTER SET utf8mb4 NULL,
    `ExternalDisplayIdentifier` longtext CHARACTER SET utf8mb4 NULL,
    `OAuthToken` longtext CHARACTER SET utf8mb4 NULL,
    `OAuthAccessToken` longtext CHARACTER SET utf8mb4 NULL,
    `ProviderSystemName` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ExternalAuthenticationRecord` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ExternalAuthenticationRecord_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Log` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LogLevelId` int NOT NULL,
    `ShortMessage` longtext CHARACTER SET utf8mb4 NOT NULL,
    `FullMessage` longtext CHARACTER SET utf8mb4 NULL,
    `IpAddress` varchar(200) CHARACTER SET utf8mb4 NULL,
    `CustomerId` int NULL,
    `PageUrl` varchar(1500) CHARACTER SET utf8mb4 NULL,
    `ReferrerUrl` varchar(1500) CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `Logger` varchar(400) CHARACTER SET utf8mb4 NOT NULL,
    `HttpMethod` varchar(10) CHARACTER SET utf8mb4 NULL,
    `UserName` varchar(100) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Log` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Log_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `Order` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderNumber` longtext CHARACTER SET utf8mb4 NULL,
    `OrderGuid` char(36) NOT NULL,
    `StoreId` int NOT NULL,
    `CustomerId` int NOT NULL,
    `BillingAddressId` int NOT NULL,
    `ShippingAddressId` int NULL,
    `PaymentMethodSystemName` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerCurrencyCode` longtext CHARACTER SET utf8mb4 NULL,
    `CurrencyRate` decimal(18,8) NOT NULL,
    `VatNumber` longtext CHARACTER SET utf8mb4 NULL,
    `OrderSubtotalInclTax` decimal(18,4) NOT NULL,
    `OrderSubtotalExclTax` decimal(18,4) NOT NULL,
    `OrderSubTotalDiscountInclTax` decimal(18,4) NOT NULL,
    `OrderSubTotalDiscountExclTax` decimal(18,4) NOT NULL,
    `OrderShippingInclTax` decimal(18,4) NOT NULL,
    `OrderShippingExclTax` decimal(18,4) NOT NULL,
    `OrderShippingTaxRate` decimal(18,4) NOT NULL,
    `PaymentMethodAdditionalFeeInclTax` decimal(18,4) NOT NULL,
    `PaymentMethodAdditionalFeeExclTax` decimal(18,4) NOT NULL,
    `PaymentMethodAdditionalFeeTaxRate` decimal(18,4) NOT NULL,
    `TaxRates` longtext CHARACTER SET utf8mb4 NULL,
    `OrderTax` decimal(18,4) NOT NULL,
    `OrderDiscount` decimal(18,4) NOT NULL,
    `CreditBalance` decimal(18,4) NOT NULL,
    `OrderTotalRounding` decimal(18,4) NOT NULL,
    `OrderTotal` decimal(18,4) NOT NULL,
    `RefundedAmount` decimal(18,4) NOT NULL,
    `RewardPointsWereAdded` tinyint(1) NOT NULL,
    `CheckoutAttributeDescription` longtext CHARACTER SET utf8mb4 NULL,
    `CheckoutAttributesXml` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerLanguageId` int NOT NULL,
    `AffiliateId` int NOT NULL,
    `CustomerIp` longtext CHARACTER SET utf8mb4 NULL,
    `AllowStoringCreditCardNumber` tinyint(1) NOT NULL,
    `CardType` longtext CHARACTER SET utf8mb4 NULL,
    `CardName` longtext CHARACTER SET utf8mb4 NULL,
    `CardNumber` longtext CHARACTER SET utf8mb4 NULL,
    `MaskedCreditCardNumber` longtext CHARACTER SET utf8mb4 NULL,
    `CardCvv2` longtext CHARACTER SET utf8mb4 NULL,
    `CardExpirationMonth` longtext CHARACTER SET utf8mb4 NULL,
    `CardExpirationYear` longtext CHARACTER SET utf8mb4 NULL,
    `AllowStoringDirectDebit` tinyint(1) NOT NULL,
    `DirectDebitAccountHolder` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitAccountNumber` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitBankCode` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitBankName` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitBIC` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitCountry` longtext CHARACTER SET utf8mb4 NULL,
    `DirectDebitIban` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerOrderComment` longtext CHARACTER SET utf8mb4 NULL,
    `AuthorizationTransactionId` longtext CHARACTER SET utf8mb4 NULL,
    `AuthorizationTransactionCode` longtext CHARACTER SET utf8mb4 NULL,
    `AuthorizationTransactionResult` longtext CHARACTER SET utf8mb4 NULL,
    `CaptureTransactionId` longtext CHARACTER SET utf8mb4 NULL,
    `CaptureTransactionResult` longtext CHARACTER SET utf8mb4 NULL,
    `SubscriptionTransactionId` longtext CHARACTER SET utf8mb4 NULL,
    `PurchaseOrderNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PaidDateUtc` datetime(6) NULL,
    `ShippingMethod` longtext CHARACTER SET utf8mb4 NULL,
    `ShippingRateComputationMethodSystemName` longtext CHARACTER SET utf8mb4 NULL,
    `Deleted` tinyint(1) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    `RewardPointsRemaining` int NULL,
    `HasNewPaymentNotification` tinyint(1) NOT NULL,
    `AcceptThirdPartyEmailHandOver` tinyint(1) NOT NULL,
    `OrderStatusId` int NOT NULL,
    `PaymentStatusId` int NOT NULL,
    `ShippingStatusId` int NOT NULL,
    `CustomerTaxDisplayTypeId` int NOT NULL,
    CONSTRAINT `PK_Order` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Order_Address_BillingAddressId` FOREIGN KEY (`BillingAddressId`) REFERENCES `Address` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Order_Address_ShippingAddressId` FOREIGN KEY (`ShippingAddressId`) REFERENCES `Address` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Order_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ReturnRequest` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StoreId` int NOT NULL,
    `OrderItemId` int NOT NULL,
    `CustomerId` int NOT NULL,
    `Quantity` int NOT NULL,
    `ReasonForReturn` longtext CHARACTER SET utf8mb4 NOT NULL,
    `RequestedAction` longtext CHARACTER SET utf8mb4 NOT NULL,
    `RequestedActionUpdatedOnUtc` datetime(6) NULL,
    `CustomerComments` longtext CHARACTER SET utf8mb4 NULL,
    `StaffNotes` longtext CHARACTER SET utf8mb4 NULL,
    `AdminComment` longtext CHARACTER SET utf8mb4 NULL,
    `ReturnRequestStatusId` int NOT NULL,
    `RefundToWallet` tinyint(1) NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ReturnRequest` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ReturnRequest_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ProductVariantAttributeValue` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductVariantAttributeId` int NOT NULL,
    `Name` varchar(450) CHARACTER SET utf8mb4 NULL,
    `Alias` varchar(100) CHARACTER SET utf8mb4 NULL,
    `MediaFileId` int NOT NULL,
    `Color` varchar(100) CHARACTER SET utf8mb4 NULL,
    `PriceAdjustment` decimal(18,4) NOT NULL,
    `WeightAdjustment` decimal(18,4) NOT NULL,
    `IsPreSelected` tinyint(1) NOT NULL,
    `DisplayOrder` int NOT NULL,
    `ValueTypeId` int NOT NULL,
    `LinkedProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    CONSTRAINT `PK_ProductVariantAttributeValue` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductVariantAttributeValue_Product_ProductAttribute_Mappin~` FOREIGN KEY (`ProductVariantAttributeId`) REFERENCES `Product_ProductAttribute_Mapping` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ProductBundleItemAttributeFilter` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `BundleItemId` int NOT NULL,
    `AttributeId` int NOT NULL,
    `AttributeValueId` int NOT NULL,
    `IsPreSelected` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ProductBundleItemAttributeFilter` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductBundleItemAttributeFilter_ProductBundleItem_BundleIte~` FOREIGN KEY (`BundleItemId`) REFERENCES `ProductBundleItem` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `ShoppingCartItem` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StoreId` int NOT NULL,
    `ParentItemId` int NULL,
    `BundleItemId` int NULL,
    `CustomerId` int NOT NULL,
    `ProductId` int NOT NULL,
    `AttributesXml` longtext CHARACTER SET utf8mb4 NULL,
    `CustomerEnteredPrice` decimal(18,4) NOT NULL,
    `Quantity` int NOT NULL,
    `ShoppingCartTypeId` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UpdatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ShoppingCartItem` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ShoppingCartItem_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ShoppingCartItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ShoppingCartItem_ProductBundleItem_BundleItemId` FOREIGN KEY (`BundleItemId`) REFERENCES `ProductBundleItem` (`Id`) ON DELETE SET NULL
);

CREATE TABLE `ProductReview` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NULL,
    `ReviewText` longtext CHARACTER SET utf8mb4 NULL,
    `Rating` int NOT NULL,
    `HelpfulYesTotal` int NOT NULL,
    `HelpfulNoTotal` int NOT NULL,
    CONSTRAINT `PK_ProductReview` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductReview_CustomerContent_Id` FOREIGN KEY (`Id`) REFERENCES `CustomerContent` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProductReview_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `DiscountUsageHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `DiscountId` int NOT NULL,
    `OrderId` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_DiscountUsageHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_DiscountUsageHistory_Discount_DiscountId` FOREIGN KEY (`DiscountId`) REFERENCES `Discount` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_DiscountUsageHistory_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Order` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `OrderItem` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderItemGuid` char(36) NOT NULL,
    `OrderId` int NOT NULL,
    `ProductId` int NOT NULL,
    `Quantity` int NOT NULL,
    `UnitPriceInclTax` decimal(18,4) NOT NULL,
    `UnitPriceExclTax` decimal(18,4) NOT NULL,
    `PriceInclTax` decimal(18,4) NOT NULL,
    `PriceExclTax` decimal(18,4) NOT NULL,
    `TaxRate` decimal(18,4) NOT NULL,
    `DiscountAmountInclTax` decimal(18,4) NOT NULL,
    `DiscountAmountExclTax` decimal(18,4) NOT NULL,
    `AttributeDescription` longtext CHARACTER SET utf8mb4 NULL,
    `AttributesXml` longtext CHARACTER SET utf8mb4 NULL,
    `DownloadCount` int NOT NULL,
    `IsDownloadActivated` tinyint(1) NOT NULL,
    `LicenseDownloadId` int NULL,
    `ItemWeight` decimal(18,4) NULL,
    `BundleData` longtext CHARACTER SET utf8mb4 NULL,
    `ProductCost` decimal(18,4) NOT NULL,
    `DeliveryTimeId` int NULL,
    `DisplayDeliveryTime` tinyint(1) NOT NULL,
    CONSTRAINT `PK_OrderItem` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderItem_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_OrderItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Product` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `OrderNote` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderId` int NOT NULL,
    `Note` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DisplayToCustomer` tinyint(1) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_OrderNote` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderNote_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `RecurringPayment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CycleLength` int NOT NULL,
    `CyclePeriodId` int NOT NULL,
    `TotalCycles` int NOT NULL,
    `StartDateUtc` datetime(6) NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `Deleted` tinyint(1) NOT NULL,
    `InitialOrderId` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_RecurringPayment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RecurringPayment_Order_InitialOrderId` FOREIGN KEY (`InitialOrderId`) REFERENCES `Order` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RewardPointsHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CustomerId` int NOT NULL,
    `Points` int NOT NULL,
    `PointsBalance` int NOT NULL,
    `UsedAmount` decimal(18,4) NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `UsedWithOrder_Id` int NULL,
    CONSTRAINT `PK_RewardPointsHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RewardPointsHistory_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RewardPointsHistory_Order_UsedWithOrder_Id` FOREIGN KEY (`UsedWithOrder_Id`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Shipment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderId` int NOT NULL,
    `TrackingNumber` longtext CHARACTER SET utf8mb4 NULL,
    `TrackingUrl` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `TotalWeight` decimal(18,4) NULL,
    `ShippedDateUtc` datetime(6) NULL,
    `DeliveryDateUtc` datetime(6) NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_Shipment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Shipment_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `WalletHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `StoreId` int NOT NULL,
    `CustomerId` int NOT NULL,
    `OrderId` int NULL,
    `Amount` decimal(18,4) NOT NULL,
    `AmountBalance` decimal(18,4) NOT NULL,
    `AmountBalancePerStore` decimal(18,4) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    `Reason` int NULL,
    `Message` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `AdminComment` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_WalletHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_WalletHistory_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_WalletHistory_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ProductReviewHelpfulness` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductReviewId` int NOT NULL,
    `WasHelpful` tinyint(1) NOT NULL,
    CONSTRAINT `PK_ProductReviewHelpfulness` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductReviewHelpfulness_CustomerContent_Id` FOREIGN KEY (`Id`) REFERENCES `CustomerContent` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProductReviewHelpfulness_ProductReview_ProductReviewId` FOREIGN KEY (`ProductReviewId`) REFERENCES `ProductReview` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `GiftCard` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `GiftCardTypeId` int NOT NULL,
    `PurchasedWithOrderItemId` int NULL,
    `Amount` decimal(18,4) NOT NULL,
    `IsGiftCardActivated` tinyint(1) NOT NULL,
    `GiftCardCouponCode` longtext CHARACTER SET utf8mb4 NULL,
    `RecipientName` longtext CHARACTER SET utf8mb4 NULL,
    `RecipientEmail` longtext CHARACTER SET utf8mb4 NULL,
    `SenderName` longtext CHARACTER SET utf8mb4 NULL,
    `SenderEmail` longtext CHARACTER SET utf8mb4 NULL,
    `Message` longtext CHARACTER SET utf8mb4 NULL,
    `IsRecipientNotified` tinyint(1) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_GiftCard` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GiftCard_OrderItem_PurchasedWithOrderItemId` FOREIGN KEY (`PurchasedWithOrderItemId`) REFERENCES `OrderItem` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `RecurringPaymentHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RecurringPaymentId` int NOT NULL,
    `OrderId` int NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_RecurringPaymentHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RecurringPaymentHistory_RecurringPayment_RecurringPaymentId` FOREIGN KEY (`RecurringPaymentId`) REFERENCES `RecurringPayment` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `ShipmentItem` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ShipmentId` int NOT NULL,
    `OrderItemId` int NOT NULL,
    `Quantity` int NOT NULL,
    CONSTRAINT `PK_ShipmentItem` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ShipmentItem_Shipment_ShipmentId` FOREIGN KEY (`ShipmentId`) REFERENCES `Shipment` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `GiftCardUsageHistory` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `GiftCardId` int NOT NULL,
    `UsedWithOrderId` int NOT NULL,
    `UsedValue` decimal(18,4) NOT NULL,
    `CreatedOnUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_GiftCardUsageHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GiftCardUsageHistory_GiftCard_GiftCardId` FOREIGN KEY (`GiftCardId`) REFERENCES `GiftCard` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_GiftCardUsageHistory_Order_UsedWithOrderId` FOREIGN KEY (`UsedWithOrderId`) REFERENCES `Order` (`Id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_AclRecord_CustomerRoleId` ON `AclRecord` (`CustomerRoleId`);

CREATE INDEX `IX_AclRecord_EntityId_EntityName` ON `AclRecord` (`EntityId`, `EntityName`);

CREATE INDEX `IX_AclRecord_IsIdle` ON `AclRecord` (`IsIdle`);

CREATE UNIQUE INDEX `IX_ActivityLog_ActivityLogTypeId` ON `ActivityLog` (`ActivityLogTypeId`);

CREATE INDEX `IX_ActivityLog_CreatedOnUtc` ON `ActivityLog` (`CreatedOnUtc`);

CREATE UNIQUE INDEX `IX_ActivityLog_CustomerId` ON `ActivityLog` (`CustomerId`);

CREATE INDEX `IX_Address_CountryId` ON `Address` (`CountryId`);

CREATE INDEX `IX_Address_StateProvinceId` ON `Address` (`StateProvinceId`);

CREATE INDEX `IX_Affiliate_AddressId` ON `Affiliate` (`AddressId`);

CREATE INDEX `IX_BackInStockSubscription_CustomerId` ON `BackInStockSubscription` (`CustomerId`);

CREATE INDEX `IX_BackInStockSubscription_ProductId` ON `BackInStockSubscription` (`ProductId`);

CREATE INDEX `IX_Category_DisplayOrder` ON `Category` (`DisplayOrder`);

CREATE INDEX `IX_Category_LimitedToStores` ON `Category` (`LimitedToStores`);

CREATE INDEX `IX_Category_MediaFileId` ON `Category` (`MediaFileId`);

CREATE INDEX `IX_Category_ParentCategoryId` ON `Category` (`ParentCategoryId`);

CREATE INDEX `IX_Category_SubjectToAcl` ON `Category` (`SubjectToAcl`);

CREATE INDEX `IX_Deleted1` ON `Category` (`Deleted`);

CREATE INDEX `IX_CheckoutAttributeValue_CheckoutAttributeId` ON `CheckoutAttributeValue` (`CheckoutAttributeId`);

CREATE INDEX `IX_CheckoutAttributeValue_MediaFileId` ON `CheckoutAttributeValue` (`MediaFileId`);

CREATE INDEX `IX_Country_DefaultCurrencyId` ON `Country` (`DefaultCurrencyId`);

CREATE INDEX `IX_Country_DisplayOrder` ON `Country` (`DisplayOrder`);

CREATE INDEX `IX_Currency_DisplayOrder` ON `Currency` (`DisplayOrder`);

CREATE UNIQUE INDEX `IX_Customer_BillingAddress_Id` ON `Customer` (`BillingAddress_Id`);

CREATE INDEX `IX_Customer_BirthDate` ON `Customer` (`BirthDate`);

CREATE INDEX `IX_Customer_Company` ON `Customer` (`Company`);

CREATE INDEX `IX_Customer_CreatedOn` ON `Customer` (`CreatedOnUtc`);

CREATE INDEX `IX_Customer_CustomerGuid` ON `Customer` (`CustomerGuid`);

CREATE INDEX `IX_Customer_CustomerNumber` ON `Customer` (`CustomerNumber`);

CREATE INDEX `IX_Customer_Deleted_IsSystemAccount` ON `Customer` (`Deleted`, `IsSystemAccount`);

CREATE INDEX `IX_Customer_Email` ON `Customer` (`Email`);

CREATE INDEX `IX_Customer_FullName` ON `Customer` (`FullName`);

CREATE INDEX `IX_Customer_LastActivity` ON `Customer` (`LastActivityDateUtc`);

CREATE INDEX `IX_Customer_LastIpAddress` ON `Customer` (`LastIpAddress`);

CREATE UNIQUE INDEX `IX_Customer_ShippingAddress_Id` ON `Customer` (`ShippingAddress_Id`);

CREATE INDEX `IX_Customer_Username` ON `Customer` (`Username`);

CREATE INDEX `IX_Deleted4` ON `Customer` (`Deleted`);

CREATE INDEX `IX_IsSystemAccount` ON `Customer` (`IsSystemAccount`);

CREATE INDEX `IX_SystemName` ON `Customer` (`SystemName`);

CREATE INDEX `IX_CustomerAddresses_Address_Id` ON `CustomerAddresses` (`Address_Id`);

CREATE INDEX `IX_CustomerContent_CustomerId` ON `CustomerContent` (`CustomerId`);

CREATE INDEX `IX_Active` ON `CustomerRole` (`Active`);

CREATE INDEX `IX_CustomerRole_SystemName_IsSystemRole` ON `CustomerRole` (`SystemName`, `IsSystemRole`);

CREATE INDEX `IX_IsSystemRole` ON `CustomerRole` (`IsSystemRole`);

CREATE INDEX `IX_SystemName1` ON `CustomerRole` (`SystemName`);

CREATE INDEX `IX_CustomerRoleMapping_CustomerId` ON `CustomerRoleMapping` (`CustomerId`);

CREATE INDEX `IX_CustomerRoleMapping_CustomerRoleId` ON `CustomerRoleMapping` (`CustomerRoleId`);

CREATE INDEX `IX_IsSystemMapping1` ON `CustomerRoleMapping` (`IsSystemMapping`);

CREATE INDEX `IX_Discount_AppliedToCategories_Category_Id` ON `Discount_AppliedToCategories` (`Category_Id`);

CREATE INDEX `IX_Discount_AppliedToManufacturers_Manufacturer_Id` ON `Discount_AppliedToManufacturers` (`Manufacturer_Id`);

CREATE INDEX `IX_Discount_AppliedToProducts_Product_Id` ON `Discount_AppliedToProducts` (`Product_Id`);

CREATE INDEX `IX_DiscountUsageHistory_DiscountId` ON `DiscountUsageHistory` (`DiscountId`);

CREATE INDEX `IX_DiscountUsageHistory_OrderId` ON `DiscountUsageHistory` (`OrderId`);

CREATE INDEX `IX_Download_MediaFileId` ON `Download` (`MediaFileId`);

CREATE INDEX `IX_DownloadGuid` ON `Download` (`DownloadGuid`);

CREATE INDEX `IX_EntityId_EntityName` ON `Download` (`EntityId`, `EntityName`);

CREATE INDEX `IX_UpdatedOn_IsTransient` ON `Download` (`UpdatedOnUtc`, `IsTransient`);

CREATE INDEX `IX_ExportDeployment_ProfileId` ON `ExportDeployment` (`ProfileId`);

CREATE INDEX `IX_ExportProfile_SchedulingTaskId` ON `ExportProfile` (`SchedulingTaskId`);

CREATE INDEX `IX_ExternalAuthenticationRecord_CustomerId` ON `ExternalAuthenticationRecord` (`CustomerId`);

CREATE INDEX `IX_GenericAttribute_EntityId_and_KeyGroup` ON `GenericAttribute` (`EntityId`, `KeyGroup`);

CREATE INDEX `IX_GenericAttribute_Key` ON `GenericAttribute` (`Key`);

CREATE INDEX `IX_GiftCard_PurchasedWithOrderItemId` ON `GiftCard` (`PurchasedWithOrderItemId`);

CREATE INDEX `IX_GiftCardUsageHistory_GiftCardId` ON `GiftCardUsageHistory` (`GiftCardId`);

CREATE INDEX `IX_GiftCardUsageHistory_UsedWithOrderId` ON `GiftCardUsageHistory` (`UsedWithOrderId`);

CREATE INDEX `IX_ImportProfile_SchedulingTaskId` ON `ImportProfile` (`SchedulingTaskId`);

CREATE INDEX `IX_Language_DisplayOrder` ON `Language` (`DisplayOrder`);

CREATE INDEX `IX_LocaleStringResource` ON `LocaleStringResource` (`ResourceName`, `LanguageId`);

CREATE INDEX `IX_LocaleStringResource_LanguageId` ON `LocaleStringResource` (`LanguageId`);

CREATE INDEX `IX_LocalizedProperty_Compound` ON `LocalizedProperty` (`EntityId`, `LocaleKey`, `LocaleKeyGroup`, `LanguageId`);

CREATE INDEX `IX_LocalizedProperty_Key` ON `LocalizedProperty` (`Id`);

CREATE INDEX `IX_LocalizedProperty_LanguageId` ON `LocalizedProperty` (`LanguageId`);

CREATE INDEX `IX_LocalizedProperty_LocaleKeyGroup` ON `LocalizedProperty` (`LocaleKeyGroup`);

CREATE INDEX `IX_Log_CreatedOnUtc` ON `Log` (`CreatedOnUtc`);

CREATE INDEX `IX_Log_CustomerId` ON `Log` (`CustomerId`);

CREATE INDEX `IX_Log_Level` ON `Log` (`LogLevelId`);

CREATE INDEX `IX_Log_Logger` ON `Log` (`Logger`);

CREATE INDEX `IX_Deleted` ON `Manufacturer` (`Deleted`);

CREATE INDEX `IX_Manufacturer_DisplayOrder` ON `Manufacturer` (`DisplayOrder`);

CREATE INDEX `IX_Manufacturer_LimitedToStores` ON `Manufacturer` (`LimitedToStores`);

CREATE INDEX `IX_Manufacturer_MediaFileId` ON `Manufacturer` (`MediaFileId`);

CREATE INDEX `IX_SubjectToAcl` ON `Manufacturer` (`SubjectToAcl`);

CREATE INDEX `IX_Media_Extension` ON `MediaFile` (`FolderId`, `Extension`, `PixelSize`, `Deleted`);

CREATE INDEX `IX_Media_FolderId` ON `MediaFile` (`FolderId`, `Deleted`);

CREATE INDEX `IX_Media_MediaType` ON `MediaFile` (`FolderId`, `MediaType`, `Extension`, `PixelSize`, `Deleted`);

CREATE INDEX `IX_Media_Name` ON `MediaFile` (`FolderId`, `Name`, `Deleted`);

CREATE INDEX `IX_Media_PixelSize` ON `MediaFile` (`FolderId`, `PixelSize`, `Deleted`);

CREATE INDEX `IX_Media_Size` ON `MediaFile` (`FolderId`, `Size`, `Deleted`);

CREATE INDEX `IX_Media_UpdatedOnUtc` ON `MediaFile` (`FolderId`, `Deleted`);

CREATE INDEX `IX_MediaFile_MediaStorageId` ON `MediaFile` (`MediaStorageId`);

CREATE INDEX `IX_MediaFile_Tag_Mapping_MediaTag_Id` ON `MediaFile_Tag_Mapping` (`MediaTag_Id`);

CREATE UNIQUE INDEX `IX_NameParentId` ON `MediaFolder` (`ParentId`, `Name`);

CREATE INDEX `IX_MediaTag_Name` ON `MediaTag` (`Name`);

CREATE INDEX `IX_Album` ON `MediaTrack` (`Album`);

CREATE INDEX `IX_MediaTrack_Composite` ON `MediaTrack` (`MediaFileId`, `EntityId`, `EntityName`, `Property`);

CREATE INDEX `IX_MenuItem_DisplayOrder` ON `MenuItemRecord` (`DisplayOrder`);

CREATE INDEX `IX_MenuItem_LimitedToStores` ON `MenuItemRecord` (`LimitedToStores`);

CREATE INDEX `IX_MenuItem_ParentItemId` ON `MenuItemRecord` (`ParentItemId`);

CREATE INDEX `IX_MenuItem_Published` ON `MenuItemRecord` (`Published`);

CREATE INDEX `IX_MenuItem_SubjectToAcl` ON `MenuItemRecord` (`SubjectToAcl`);

CREATE INDEX `IX_MenuItemRecord_MenuId` ON `MenuItemRecord` (`MenuId`);

CREATE INDEX `IX_Menu_LimitedToStores` ON `MenuRecord` (`LimitedToStores`);

CREATE INDEX `IX_Menu_Published` ON `MenuRecord` (`Published`);

CREATE INDEX `IX_Menu_SubjectToAcl` ON `MenuRecord` (`SubjectToAcl`);

CREATE INDEX `IX_Menu_SystemName_IsSystemMenu` ON `MenuRecord` (`SystemName`, `IsSystemMenu`);

CREATE INDEX `IX_Active1` ON `NewsLetterSubscription` (`Active`);

CREATE INDEX `IX_NewsletterSubscription_Email_StoreId` ON `NewsLetterSubscription` (`Email`, `StoreId`);

CREATE INDEX `IX_Deleted3` ON `Order` (`Deleted`);

CREATE INDEX `IX_Order_BillingAddressId` ON `Order` (`BillingAddressId`);

CREATE INDEX `IX_Order_CustomerId` ON `Order` (`CustomerId`);

CREATE INDEX `IX_Order_ShippingAddressId` ON `Order` (`ShippingAddressId`);

CREATE INDEX `IX_OrderItem_OrderId` ON `OrderItem` (`OrderId`);

CREATE INDEX `IX_OrderItem_ProductId` ON `OrderItem` (`ProductId`);

CREATE INDEX `IX_OrderNote_OrderId` ON `OrderNote` (`OrderId`);

CREATE INDEX `IX_SystemName2` ON `PermissionRecord` (`SystemName`);

CREATE INDEX `IX_PermissionRoleMapping_CustomerRoleId` ON `PermissionRoleMapping` (`CustomerRoleId`);

CREATE INDEX `IX_PermissionRoleMapping_PermissionRecordId` ON `PermissionRoleMapping` (`PermissionRecordId`);

CREATE INDEX `IX_Deleted2` ON `Product` (`Deleted`);

CREATE INDEX `IX_Gtin1` ON `Product` (`Gtin`);

CREATE INDEX `IX_IsSystemProduct` ON `Product` (`IsSystemProduct`);

CREATE INDEX `IX_ManufacturerPartNumber1` ON `Product` (`ManufacturerPartNumber`);

CREATE INDEX `IX_Product_CountryOfOriginId` ON `Product` (`CountryOfOriginId`);

CREATE INDEX `IX_Product_DeliveryTimeId` ON `Product` (`DeliveryTimeId`);

CREATE INDEX `IX_Product_LimitedToStores` ON `Product` (`LimitedToStores`);

CREATE INDEX `IX_Product_Name` ON `Product` (`Name`);

CREATE INDEX `IX_Product_ParentGroupedProductId` ON `Product` (`ParentGroupedProductId`);

CREATE INDEX `IX_Product_PriceDatesEtc` ON `Product` (`Price`, `AvailableStartDateTimeUtc`, `AvailableEndDateTimeUtc`, `Published`, `Deleted`);

CREATE INDEX `IX_Product_Published` ON `Product` (`Published`);

CREATE INDEX `IX_Product_Published_Deleted_IsSystemProduct` ON `Product` (`Published`, `Deleted`, `IsSystemProduct`);

CREATE INDEX `IX_Product_QuantityUnitId` ON `Product` (`QuantityUnitId`);

CREATE INDEX `IX_Product_SampleDownloadId` ON `Product` (`SampleDownloadId`);

CREATE INDEX `IX_Product_ShowOnHomepage` ON `Product` (`ShowOnHomePage`);

CREATE INDEX `IX_Product_Sku` ON `Product` (`Sku`);

CREATE INDEX `IX_Product_SubjectToAcl` ON `Product` (`SubjectToAcl`);

CREATE INDEX `IX_Product_SystemName_IsSystemProduct` ON `Product` (`SystemName`, `IsSystemProduct`);

CREATE INDEX `IX_SeekExport1` ON `Product` (`Published`, `Id`, `Visibility`, `Deleted`, `IsSystemProduct`, `AvailableStartDateTimeUtc`, `AvailableEndDateTimeUtc`);

CREATE INDEX `IX_Visibility` ON `Product` (`Visibility`);

CREATE INDEX `IX_IsFeaturedProduct1` ON `Product_Category_Mapping` (`IsFeaturedProduct`);

CREATE INDEX `IX_IsSystemMapping` ON `Product_Category_Mapping` (`IsSystemMapping`);

CREATE INDEX `IX_PCM_Product_and_Category` ON `Product_Category_Mapping` (`CategoryId`, `ProductId`);

CREATE INDEX `IX_Product_Category_Mapping_ProductId` ON `Product_Category_Mapping` (`ProductId`);

CREATE INDEX `IX_IsFeaturedProduct` ON `Product_Manufacturer_Mapping` (`IsFeaturedProduct`);

CREATE INDEX `IX_PMM_Product_and_Manufacturer` ON `Product_Manufacturer_Mapping` (`ManufacturerId`, `ProductId`);

CREATE INDEX `IX_Product_Manufacturer_Mapping_ProductId` ON `Product_Manufacturer_Mapping` (`ProductId`);

CREATE INDEX `IX_Product_MediaFile_Mapping_MediaFileId` ON `Product_MediaFile_Mapping` (`MediaFileId`);

CREATE INDEX `IX_Product_MediaFile_Mapping_ProductId` ON `Product_MediaFile_Mapping` (`ProductId`);

CREATE INDEX `IX_AttributeControlTypeId` ON `Product_ProductAttribute_Mapping` (`AttributeControlTypeId`);

CREATE INDEX `IX_Product_ProductAttribute_Mapping_ProductAttributeId` ON `Product_ProductAttribute_Mapping` (`ProductAttributeId`);

CREATE INDEX `IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder` ON `Product_ProductAttribute_Mapping` (`ProductId`, `DisplayOrder`);

CREATE INDEX `IX_Product_ProductTag_Mapping_ProductTag_Id` ON `Product_ProductTag_Mapping` (`ProductTag_Id`);

CREATE INDEX `IX_Product_SpecificationAttribute_Mapping_ProductId` ON `Product_SpecificationAttribute_Mapping` (`ProductId`);

CREATE INDEX `IX_PSAM_AllowFiltering` ON `Product_SpecificationAttribute_Mapping` (`AllowFiltering`);

CREATE INDEX `IX_PSAM_SpecificationAttributeOptionId_AllowFiltering` ON `Product_SpecificationAttribute_Mapping` (`SpecificationAttributeOptionId`, `AllowFiltering`);

CREATE INDEX `IX_AllowFiltering` ON `ProductAttribute` (`AllowFiltering`);

CREATE INDEX `IX_DisplayOrder` ON `ProductAttribute` (`DisplayOrder`);

CREATE INDEX `IX_ProductAttributeOption_ProductAttributeOptionsSetId` ON `ProductAttributeOption` (`ProductAttributeOptionsSetId`);

CREATE INDEX `IX_ProductAttributeOptionsSet_ProductAttributeId` ON `ProductAttributeOptionsSet` (`ProductAttributeId`);

CREATE INDEX `IX_ProductBundleItem_BundleProductId` ON `ProductBundleItem` (`BundleProductId`);

CREATE INDEX `IX_ProductBundleItem_ProductId` ON `ProductBundleItem` (`ProductId`);

CREATE INDEX `IX_ProductBundleItemAttributeFilter_BundleItemId` ON `ProductBundleItemAttributeFilter` (`BundleItemId`);

CREATE INDEX `IX_ProductReview_ProductId` ON `ProductReview` (`ProductId`);

CREATE INDEX `IX_ProductReviewHelpfulness_ProductReviewId` ON `ProductReviewHelpfulness` (`ProductReviewId`);

CREATE INDEX `IX_ProductTag_Name` ON `ProductTag` (`Name`);

CREATE INDEX `IX_ProductTag_Published` ON `ProductTag` (`Published`);

CREATE INDEX `IX_Gtin` ON `ProductVariantAttributeCombination` (`Gtin`);

CREATE INDEX `IX_IsActive` ON `ProductVariantAttributeCombination` (`IsActive`);

CREATE INDEX `IX_ManufacturerPartNumber` ON `ProductVariantAttributeCombination` (`ManufacturerPartNumber`);

CREATE INDEX `IX_ProductVariantAttributeCombination_DeliveryTimeId` ON `ProductVariantAttributeCombination` (`DeliveryTimeId`);

CREATE INDEX `IX_ProductVariantAttributeCombination_ProductId` ON `ProductVariantAttributeCombination` (`ProductId`);

CREATE INDEX `IX_ProductVariantAttributeCombination_QuantityUnitId` ON `ProductVariantAttributeCombination` (`QuantityUnitId`);

CREATE INDEX `IX_ProductVariantAttributeCombination_SKU` ON `ProductVariantAttributeCombination` (`Sku`);

CREATE INDEX `IX_StockQuantity_AllowOutOfStockOrders` ON `ProductVariantAttributeCombination` (`StockQuantity`, `AllowOutOfStockOrders`);

CREATE INDEX `IX_Name` ON `ProductVariantAttributeValue` (`Name`);

CREATE INDEX `IX_ProductVariantAttributeValue_ProductVariantAttributeId_Displa` ON `ProductVariantAttributeValue` (`ProductVariantAttributeId`, `DisplayOrder`);

CREATE INDEX `IX_ValueTypeId` ON `ProductVariantAttributeValue` (`ValueTypeId`);

CREATE INDEX `[IX_QueuedEmail_CreatedOnUtc]` ON `QueuedEmail` (`CreatedOnUtc`);

CREATE INDEX `IX_EmailAccountId` ON `QueuedEmail` (`EmailAccountId`);

CREATE INDEX `IX_MediaFileId` ON `QueuedEmailAttachment` (`MediaFileId`);

CREATE INDEX `IX_MediaStorageId` ON `QueuedEmailAttachment` (`MediaStorageId`);

CREATE INDEX `IX_QueuedEmailId` ON `QueuedEmailAttachment` (`QueuedEmailId`);

CREATE INDEX `IX_RecurringPayment_InitialOrderId` ON `RecurringPayment` (`InitialOrderId`);

CREATE INDEX `IX_RecurringPaymentHistory_RecurringPaymentId` ON `RecurringPaymentHistory` (`RecurringPaymentId`);

CREATE INDEX `IX_RelatedProduct_ProductId1` ON `RelatedProduct` (`ProductId1`);

CREATE INDEX `IX_ReturnRequest_CustomerId` ON `ReturnRequest` (`CustomerId`);

CREATE INDEX `IX_RewardPointsHistory_CustomerId` ON `RewardPointsHistory` (`CustomerId`);

CREATE UNIQUE INDEX `IX_RewardPointsHistory_UsedWithOrder_Id` ON `RewardPointsHistory` (`UsedWithOrder_Id`);

CREATE INDEX `IX_PageBuilder_DisplayOrder` ON `Rule` (`DisplayOrder`);

CREATE INDEX `IX_PageBuilder_RuleType` ON `Rule` (`RuleType`);

CREATE INDEX `IX_Rule_RuleSetId` ON `Rule` (`RuleSetId`);

CREATE INDEX `IX_IsSubGroup` ON `RuleSet` (`IsSubGroup`);

CREATE INDEX `IX_RuleSetEntity_Scope` ON `RuleSet` (`IsActive`, `Scope`);

CREATE INDEX `IX_RuleSet_Category_Mapping_RuleSetEntity_Id` ON `RuleSet_Category_Mapping` (`RuleSetEntity_Id`);

CREATE INDEX `IX_RuleSet_CustomerRole_Mapping_RuleSetEntity_Id` ON `RuleSet_CustomerRole_Mapping` (`RuleSetEntity_Id`);

CREATE INDEX `IX_RuleSet_Discount_Mapping_RuleSetEntity_Id` ON `RuleSet_Discount_Mapping` (`RuleSetEntity_Id`);

CREATE INDEX `IX_RuleSet_PaymentMethod_Mapping_RuleSetEntity_Id` ON `RuleSet_PaymentMethod_Mapping` (`RuleSetEntity_Id`);

CREATE INDEX `IX_RuleSet_ShippingMethod_Mapping_RuleSetEntity_Id` ON `RuleSet_ShippingMethod_Mapping` (`RuleSetEntity_Id`);

CREATE INDEX `IX_NextRun_Enabled` ON `ScheduleTask` (`NextRunUtc`, `Enabled`);

CREATE INDEX `IX_Type` ON `ScheduleTask` (`Type`);

CREATE INDEX `IX_MachineName_IsRunning` ON `ScheduleTaskHistory` (`MachineName`, `IsRunning`);

CREATE INDEX `IX_ScheduleTaskHistory_ScheduleTaskId` ON `ScheduleTaskHistory` (`ScheduleTaskId`);

CREATE INDEX `IX_Started_Finished` ON `ScheduleTaskHistory` (`StartedOnUtc`, `FinishedOnUtc`);

CREATE INDEX `IX_Setting_Name` ON `Setting` (`Name`);

CREATE INDEX `IX_Setting_StoreId` ON `Setting` (`StoreId`);

CREATE INDEX `IX_Shipment_OrderId` ON `Shipment` (`OrderId`);

CREATE INDEX `IX_ShipmentItem_ShipmentId` ON `ShipmentItem` (`ShipmentId`);

CREATE INDEX `IX_ShoppingCartItem_BundleItemId` ON `ShoppingCartItem` (`BundleItemId`);

CREATE INDEX `IX_ShoppingCartItem_CustomerId` ON `ShoppingCartItem` (`CustomerId`);

CREATE INDEX `IX_ShoppingCartItem_ProductId` ON `ShoppingCartItem` (`ProductId`);

CREATE INDEX `IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId` ON `ShoppingCartItem` (`ShoppingCartTypeId`, `CustomerId`);

CREATE INDEX `IX_AllowFiltering1` ON `SpecificationAttribute` (`AllowFiltering`);

CREATE INDEX `IX_SpecificationAttributeOption_SpecificationAttributeId` ON `SpecificationAttributeOption` (`SpecificationAttributeId`);

CREATE INDEX `IX_StateProvince_CountryId` ON `StateProvince` (`CountryId`);

CREATE INDEX `IX_Store_PrimaryExchangeRateCurrencyId` ON `Store` (`PrimaryExchangeRateCurrencyId`);

CREATE INDEX `IX_Store_PrimaryStoreCurrencyId` ON `Store` (`PrimaryStoreCurrencyId`);

CREATE INDEX `IX_StoreMapping_EntityId_EntityName` ON `StoreMapping` (`EntityId`, `EntityName`);

CREATE UNIQUE INDEX `IX_SyncMapping_ByEntity` ON `SyncMapping` (`EntityId`, `EntityName`, `ContextName`);

CREATE UNIQUE INDEX `IX_SyncMapping_BySource` ON `SyncMapping` (`SourceKey`, `EntityName`, `ContextName`);

CREATE INDEX `IX_TierPrice_CustomerRoleId` ON `TierPrice` (`CustomerRoleId`);

CREATE INDEX `IX_TierPrice_ProductId` ON `TierPrice` (`ProductId`);

CREATE UNIQUE INDEX `IX_UrlRecord_Slug` ON `UrlRecord` (`Slug`);

CREATE INDEX `IX_StoreId_CreatedOn` ON `WalletHistory` (`StoreId`, `CreatedOnUtc`);

CREATE INDEX `IX_WalletHistory_CustomerId` ON `WalletHistory` (`CustomerId`);

CREATE INDEX `IX_WalletHistory_OrderId` ON `WalletHistory` (`OrderId`);

/*
INSERT INTO `__EFMigrationsHistory_Core` (`MigrationId`, `ProductVersion`)
VALUES ('20210818075146_Initial', '5.0.5');
*/

COMMIT;

