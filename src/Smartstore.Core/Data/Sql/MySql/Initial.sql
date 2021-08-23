-- MySQL dump 10.13  Distrib 8.0.26, for Win64 (x86_64)
--
-- Host: localhost    Database: smartstorecoreempty
-- ------------------------------------------------------
-- Server version	8.0.26

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `aclrecord`
--

DROP TABLE IF EXISTS `aclrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aclrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `EntityName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CustomerRoleId` int NOT NULL,
  `IsIdle` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AclRecord_CustomerRoleId` (`CustomerRoleId`),
  KEY `IX_AclRecord_EntityId_EntityName` (`EntityId`,`EntityName`),
  KEY `IX_AclRecord_IsIdle` (`IsIdle`),
  CONSTRAINT `FK_AclRecord_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `customerrole` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `activitylog`
--

DROP TABLE IF EXISTS `activitylog`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activitylog` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ActivityLogTypeId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `Comment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ActivityLog_ActivityLogTypeId` (`ActivityLogTypeId`),
  UNIQUE KEY `IX_ActivityLog_CustomerId` (`CustomerId`),
  KEY `IX_ActivityLog_CreatedOnUtc` (`CreatedOnUtc`),
  CONSTRAINT `FK_ActivityLog_ActivityLogType_ActivityLogTypeId` FOREIGN KEY (`ActivityLogTypeId`) REFERENCES `activitylogtype` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ActivityLog_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `activitylogtype`
--

DROP TABLE IF EXISTS `activitylogtype`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activitylogtype` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SystemKeyword` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `address`
--

DROP TABLE IF EXISTS `address`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `address` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Salutation` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Title` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FirstName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LastName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Email` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Company` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CountryId` int DEFAULT NULL,
  `StateProvinceId` int DEFAULT NULL,
  `City` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Address1` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Address2` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ZipPostalCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PhoneNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FaxNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Address_CountryId` (`CountryId`),
  KEY `IX_Address_StateProvinceId` (`StateProvinceId`),
  CONSTRAINT `FK_Address_Country_CountryId` FOREIGN KEY (`CountryId`) REFERENCES `country` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Address_StateProvince_StateProvinceId` FOREIGN KEY (`StateProvinceId`) REFERENCES `stateprovince` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `affiliate`
--

DROP TABLE IF EXISTS `affiliate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `affiliate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Active` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `AddressId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Affiliate_AddressId` (`AddressId`),
  CONSTRAINT `FK_Affiliate_Address_AddressId` FOREIGN KEY (`AddressId`) REFERENCES `address` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `backinstocksubscription`
--

DROP TABLE IF EXISTS `backinstocksubscription`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `backinstocksubscription` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StoreId` int NOT NULL,
  `ProductId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_BackInStockSubscription_CustomerId` (`CustomerId`),
  KEY `IX_BackInStockSubscription_ProductId` (`ProductId`),
  CONSTRAINT `FK_BackInStockSubscription_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_BackInStockSubscription_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `campaign`
--

DROP TABLE IF EXISTS `campaign`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `campaign` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Subject` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Body` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `category`
--

DROP TABLE IF EXISTS `category`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `category` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FullName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `BottomDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ExternalLink` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BadgeText` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BadgeStyle` int NOT NULL,
  `Alias` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CategoryTemplateId` int NOT NULL,
  `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MetaDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaTitle` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ParentCategoryId` int NOT NULL,
  `MediaFileId` int DEFAULT NULL,
  `PageSize` int DEFAULT NULL,
  `AllowCustomersToSelectPageSize` tinyint(1) DEFAULT NULL,
  `PageSizeOptions` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PriceRanges` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ShowOnHomePage` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `DefaultViewMode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `HasDiscountsApplied` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Category_DisplayOrder` (`DisplayOrder`),
  KEY `IX_Category_LimitedToStores` (`LimitedToStores`),
  KEY `IX_Category_MediaFileId` (`MediaFileId`),
  KEY `IX_Category_ParentCategoryId` (`ParentCategoryId`),
  KEY `IX_Category_SubjectToAcl` (`SubjectToAcl`),
  KEY `IX_Deleted1` (`Deleted`),
  CONSTRAINT `FK_Category_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `categorytemplate`
--

DROP TABLE IF EXISTS `categorytemplate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categorytemplate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ViewPath` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `checkoutattribute`
--

DROP TABLE IF EXISTS `checkoutattribute`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checkoutattribute` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `IsActive` tinyint(1) NOT NULL,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TextPrompt` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsRequired` tinyint(1) NOT NULL,
  `ShippableProductRequired` tinyint(1) NOT NULL,
  `IsTaxExempt` tinyint(1) NOT NULL,
  `TaxCategoryId` int NOT NULL,
  `DisplayOrder` int NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `AttributeControlTypeId` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `checkoutattributevalue`
--

DROP TABLE IF EXISTS `checkoutattributevalue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checkoutattributevalue` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PriceAdjustment` decimal(18,4) NOT NULL,
  `WeightAdjustment` decimal(18,4) NOT NULL,
  `IsPreSelected` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `Color` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CheckoutAttributeId` int NOT NULL,
  `MediaFileId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CheckoutAttributeValue_CheckoutAttributeId` (`CheckoutAttributeId`),
  KEY `IX_CheckoutAttributeValue_MediaFileId` (`MediaFileId`),
  CONSTRAINT `FK_CheckoutAttributeValue_CheckoutAttribute_CheckoutAttributeId` FOREIGN KEY (`CheckoutAttributeId`) REFERENCES `checkoutattribute` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_CheckoutAttributeValue_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `country`
--

DROP TABLE IF EXISTS `country`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `country` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AllowsBilling` tinyint(1) NOT NULL,
  `AllowsShipping` tinyint(1) NOT NULL,
  `TwoLetterIsoCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ThreeLetterIsoCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `NumericIsoCode` int NOT NULL,
  `SubjectToVat` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `DisplayCookieManager` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `AddressFormat` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DefaultCurrencyId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Country_DefaultCurrencyId` (`DefaultCurrencyId`),
  KEY `IX_Country_DisplayOrder` (`DisplayOrder`),
  CONSTRAINT `FK_Country_Currency_DefaultCurrencyId` FOREIGN KEY (`DefaultCurrencyId`) REFERENCES `currency` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=238 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `crosssellproduct`
--

DROP TABLE IF EXISTS `crosssellproduct`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `crosssellproduct` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId1` int NOT NULL,
  `ProductId2` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `currency`
--

DROP TABLE IF EXISTS `currency`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `currency` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CurrencyCode` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Rate` decimal(18,8) NOT NULL,
  `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CustomFormatting` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `DomainEndings` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `RoundOrderItemsEnabled` tinyint(1) NOT NULL,
  `RoundNumDecimals` int NOT NULL,
  `RoundOrderTotalEnabled` tinyint(1) NOT NULL,
  `RoundOrderTotalDenominator` decimal(18,4) NOT NULL,
  `RoundOrderTotalRule` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Currency_DisplayOrder` (`DisplayOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer`
--

DROP TABLE IF EXISTS `customer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customer` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerGuid` char(36) NOT NULL,
  `Username` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Email` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Password` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PasswordFormatId` int NOT NULL,
  `PasswordSalt` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AdminComment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsTaxExempt` tinyint(1) NOT NULL,
  `AffiliateId` int NOT NULL,
  `Active` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `IsSystemAccount` tinyint(1) NOT NULL,
  `SystemName` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LastIpAddress` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `LastLoginDateUtc` datetime(6) DEFAULT NULL,
  `LastActivityDateUtc` datetime(6) NOT NULL,
  `Salutation` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Title` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FirstName` varchar(225) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LastName` varchar(225) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FullName` varchar(450) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Company` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CustomerNumber` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BirthDate` datetime(6) DEFAULT NULL,
  `Gender` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `VatNumberStatusId` int NOT NULL,
  `TimeZoneId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `TaxDisplayTypeId` int NOT NULL,
  `LastForumVisit` datetime(6) DEFAULT NULL,
  `LastUserAgent` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LastUserDeviceType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `BillingAddress_Id` int DEFAULT NULL,
  `ShippingAddress_Id` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Customer_BillingAddress_Id` (`BillingAddress_Id`),
  UNIQUE KEY `IX_Customer_ShippingAddress_Id` (`ShippingAddress_Id`),
  KEY `IX_Customer_BirthDate` (`BirthDate`),
  KEY `IX_Customer_Company` (`Company`),
  KEY `IX_Customer_CreatedOn` (`CreatedOnUtc`),
  KEY `IX_Customer_CustomerGuid` (`CustomerGuid`),
  KEY `IX_Customer_CustomerNumber` (`CustomerNumber`),
  KEY `IX_Customer_Deleted_IsSystemAccount` (`Deleted`,`IsSystemAccount`),
  KEY `IX_Customer_Email` (`Email`),
  KEY `IX_Customer_FullName` (`FullName`),
  KEY `IX_Customer_LastActivity` (`LastActivityDateUtc`),
  KEY `IX_Customer_LastIpAddress` (`LastIpAddress`),
  KEY `IX_Customer_Username` (`Username`),
  KEY `IX_Deleted4` (`Deleted`),
  KEY `IX_IsSystemAccount` (`IsSystemAccount`),
  KEY `IX_SystemName` (`SystemName`),
  CONSTRAINT `FK_Customer_Address_BillingAddress_Id` FOREIGN KEY (`BillingAddress_Id`) REFERENCES `address` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Customer_Address_ShippingAddress_Id` FOREIGN KEY (`ShippingAddress_Id`) REFERENCES `address` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customeraddresses`
--

DROP TABLE IF EXISTS `customeraddresses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customeraddresses` (
  `Customer_Id` int NOT NULL,
  `Address_Id` int NOT NULL,
  PRIMARY KEY (`Customer_Id`,`Address_Id`),
  KEY `IX_CustomerAddresses_Address_Id` (`Address_Id`),
  CONSTRAINT `FK_dbo.CustomerAddresses_dbo.Address_Address_Id` FOREIGN KEY (`Address_Id`) REFERENCES `address` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id` FOREIGN KEY (`Customer_Id`) REFERENCES `customer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customercontent`
--

DROP TABLE IF EXISTS `customercontent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customercontent` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerId` int NOT NULL,
  `IpAddress` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsApproved` tinyint(1) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CustomerContent_CustomerId` (`CustomerId`),
  CONSTRAINT `FK_CustomerContent_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customerrole`
--

DROP TABLE IF EXISTS `customerrole`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customerrole` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FreeShipping` tinyint(1) NOT NULL,
  `TaxExempt` tinyint(1) NOT NULL,
  `TaxDisplayType` int DEFAULT NULL,
  `Active` tinyint(1) NOT NULL,
  `IsSystemRole` tinyint(1) NOT NULL,
  `SystemName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `OrderTotalMinimum` decimal(18,2) DEFAULT NULL,
  `OrderTotalMaximum` decimal(18,2) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Active` (`Active`),
  KEY `IX_CustomerRole_SystemName_IsSystemRole` (`SystemName`,`IsSystemRole`),
  KEY `IX_IsSystemRole` (`IsSystemRole`),
  KEY `IX_SystemName1` (`SystemName`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customerrolemapping`
--

DROP TABLE IF EXISTS `customerrolemapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customerrolemapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerId` int NOT NULL,
  `CustomerRoleId` int NOT NULL,
  `IsSystemMapping` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CustomerRoleMapping_CustomerId` (`CustomerId`),
  KEY `IX_CustomerRoleMapping_CustomerRoleId` (`CustomerRoleId`),
  KEY `IX_IsSystemMapping1` (`IsSystemMapping`),
  CONSTRAINT `FK_CustomerRoleMapping_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_CustomerRoleMapping_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `customerrole` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `deliverytime`
--

DROP TABLE IF EXISTS `deliverytime`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `deliverytime` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ColorHexValue` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  `IsDefault` tinyint(1) DEFAULT NULL,
  `MinDays` int DEFAULT NULL,
  `MaxDays` int DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discount`
--

DROP TABLE IF EXISTS `discount`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discount` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DiscountTypeId` int NOT NULL,
  `UsePercentage` tinyint(1) NOT NULL,
  `DiscountPercentage` decimal(18,4) NOT NULL,
  `DiscountAmount` decimal(18,4) NOT NULL,
  `StartDateUtc` datetime(6) DEFAULT NULL,
  `EndDateUtc` datetime(6) DEFAULT NULL,
  `RequiresCouponCode` tinyint(1) NOT NULL,
  `CouponCode` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DiscountLimitationId` int NOT NULL,
  `LimitationTimes` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discount_appliedtocategories`
--

DROP TABLE IF EXISTS `discount_appliedtocategories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discount_appliedtocategories` (
  `Discount_Id` int NOT NULL,
  `Category_Id` int NOT NULL,
  PRIMARY KEY (`Discount_Id`,`Category_Id`),
  KEY `IX_Discount_AppliedToCategories_Category_Id` (`Category_Id`),
  CONSTRAINT `FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id` FOREIGN KEY (`Category_Id`) REFERENCES `category` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `discount` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discount_appliedtomanufacturers`
--

DROP TABLE IF EXISTS `discount_appliedtomanufacturers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discount_appliedtomanufacturers` (
  `Discount_Id` int NOT NULL,
  `Manufacturer_Id` int NOT NULL,
  PRIMARY KEY (`Discount_Id`,`Manufacturer_Id`),
  KEY `IX_Discount_AppliedToManufacturers_Manufacturer_Id` (`Manufacturer_Id`),
  CONSTRAINT `FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `discount` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufact` FOREIGN KEY (`Manufacturer_Id`) REFERENCES `manufacturer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discount_appliedtoproducts`
--

DROP TABLE IF EXISTS `discount_appliedtoproducts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discount_appliedtoproducts` (
  `Discount_Id` int NOT NULL,
  `Product_Id` int NOT NULL,
  PRIMARY KEY (`Discount_Id`,`Product_Id`),
  KEY `IX_Discount_AppliedToProducts_Product_Id` (`Product_Id`),
  CONSTRAINT `FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `discount` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id` FOREIGN KEY (`Product_Id`) REFERENCES `product` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discountusagehistory`
--

DROP TABLE IF EXISTS `discountusagehistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discountusagehistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DiscountId` int NOT NULL,
  `OrderId` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_DiscountUsageHistory_DiscountId` (`DiscountId`),
  KEY `IX_DiscountUsageHistory_OrderId` (`OrderId`),
  CONSTRAINT `FK_DiscountUsageHistory_Discount_DiscountId` FOREIGN KEY (`DiscountId`) REFERENCES `discount` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_DiscountUsageHistory_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `order` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `download`
--

DROP TABLE IF EXISTS `download`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `download` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DownloadGuid` char(36) NOT NULL,
  `UseDownloadUrl` tinyint(1) NOT NULL,
  `DownloadUrl` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsTransient` tinyint(1) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `MediaFileId` int DEFAULT NULL,
  `EntityId` int NOT NULL,
  `EntityName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FileVersion` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Changelog` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_Download_MediaFileId` (`MediaFileId`),
  KEY `IX_DownloadGuid` (`DownloadGuid`),
  KEY `IX_EntityId_EntityName` (`EntityId`,`EntityName`),
  KEY `IX_UpdatedOn_IsTransient` (`UpdatedOnUtc`,`IsTransient`),
  CONSTRAINT `FK_Download_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `emailaccount`
--

DROP TABLE IF EXISTS `emailaccount`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `emailaccount` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Host` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Port` int NOT NULL,
  `Username` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `EnableSsl` tinyint(1) NOT NULL,
  `UseDefaultCredentials` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `exportdeployment`
--

DROP TABLE IF EXISTS `exportdeployment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `exportdeployment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProfileId` int NOT NULL,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  `ResultInfo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DeploymentTypeId` int NOT NULL,
  `Username` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Password` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Url` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `HttpTransmissionTypeId` int NOT NULL,
  `HttpTransmissionType` int NOT NULL,
  `FileSystemPath` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `SubFolder` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EmailAddresses` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `EmailSubject` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EmailAccountId` int NOT NULL,
  `PassiveMode` tinyint(1) NOT NULL,
  `UseSsl` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ExportDeployment_ProfileId` (`ProfileId`),
  CONSTRAINT `FK_ExportDeployment_ExportProfile_ProfileId` FOREIGN KEY (`ProfileId`) REFERENCES `exportprofile` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `exportprofile`
--

DROP TABLE IF EXISTS `exportprofile`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `exportprofile` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FolderName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileNamePattern` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `SystemName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ProviderSystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsSystemProfile` tinyint(1) NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  `ExportRelatedData` tinyint(1) NOT NULL,
  `Filtering` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Projection` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProviderConfigData` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ResultInfo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Offset` int NOT NULL,
  `Limit` int NOT NULL,
  `BatchSize` int NOT NULL,
  `PerStore` tinyint(1) NOT NULL,
  `EmailAccountId` int NOT NULL,
  `CompletedEmailAddresses` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreateZipArchive` tinyint(1) NOT NULL,
  `Cleanup` tinyint(1) NOT NULL,
  `SchedulingTaskId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ExportProfile_SchedulingTaskId` (`SchedulingTaskId`),
  CONSTRAINT `FK_ExportProfile_ScheduleTask_SchedulingTaskId` FOREIGN KEY (`SchedulingTaskId`) REFERENCES `scheduletask` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `externalauthenticationrecord`
--

DROP TABLE IF EXISTS `externalauthenticationrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `externalauthenticationrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerId` int NOT NULL,
  `Email` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ExternalIdentifier` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ExternalDisplayIdentifier` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `OAuthToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `OAuthAccessToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProviderSystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_ExternalAuthenticationRecord_CustomerId` (`CustomerId`),
  CONSTRAINT `FK_ExternalAuthenticationRecord_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `genericattribute`
--

DROP TABLE IF EXISTS `genericattribute`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `genericattribute` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `KeyGroup` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Key` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StoreId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_GenericAttribute_EntityId_and_KeyGroup` (`EntityId`,`KeyGroup`),
  KEY `IX_GenericAttribute_Key` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `giftcard`
--

DROP TABLE IF EXISTS `giftcard`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `giftcard` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GiftCardTypeId` int NOT NULL,
  `PurchasedWithOrderItemId` int DEFAULT NULL,
  `Amount` decimal(18,4) NOT NULL,
  `IsGiftCardActivated` tinyint(1) NOT NULL,
  `GiftCardCouponCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RecipientName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RecipientEmail` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SenderName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SenderEmail` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Message` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsRecipientNotified` tinyint(1) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_GiftCard_PurchasedWithOrderItemId` (`PurchasedWithOrderItemId`),
  CONSTRAINT `FK_GiftCard_OrderItem_PurchasedWithOrderItemId` FOREIGN KEY (`PurchasedWithOrderItemId`) REFERENCES `orderitem` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `giftcardusagehistory`
--

DROP TABLE IF EXISTS `giftcardusagehistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `giftcardusagehistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GiftCardId` int NOT NULL,
  `UsedWithOrderId` int NOT NULL,
  `UsedValue` decimal(18,4) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_GiftCardUsageHistory_GiftCardId` (`GiftCardId`),
  KEY `IX_GiftCardUsageHistory_UsedWithOrderId` (`UsedWithOrderId`),
  CONSTRAINT `FK_GiftCardUsageHistory_GiftCard_GiftCardId` FOREIGN KEY (`GiftCardId`) REFERENCES `giftcard` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_GiftCardUsageHistory_Order_UsedWithOrderId` FOREIGN KEY (`UsedWithOrderId`) REFERENCES `order` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `importprofile`
--

DROP TABLE IF EXISTS `importprofile`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `importprofile` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FolderName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileTypeId` int NOT NULL,
  `EntityTypeId` int NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  `ImportRelatedData` tinyint(1) NOT NULL,
  `Skip` int NOT NULL,
  `Take` int NOT NULL,
  `UpdateOnly` tinyint(1) NOT NULL,
  `KeyFieldNames` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FileTypeConfiguration` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ExtraData` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ColumnMapping` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ResultInfo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SchedulingTaskId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ImportProfile_SchedulingTaskId` (`SchedulingTaskId`),
  CONSTRAINT `FK_ImportProfile_ScheduleTask_SchedulingTaskId` FOREIGN KEY (`SchedulingTaskId`) REFERENCES `scheduletask` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `language`
--

DROP TABLE IF EXISTS `language`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `language` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LanguageCulture` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UniqueSeoCode` varchar(2) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FlagImageFileName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Rtl` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Language_DisplayOrder` (`DisplayOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `localestringresource`
--

DROP TABLE IF EXISTS `localestringresource`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `localestringresource` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LanguageId` int NOT NULL,
  `ResourceName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ResourceValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsFromPlugin` tinyint(1) DEFAULT NULL,
  `IsTouched` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_LocaleStringResource` (`ResourceName`,`LanguageId`),
  KEY `IX_LocaleStringResource_LanguageId` (`LanguageId`),
  CONSTRAINT `FK_LocaleStringResource_Language_LanguageId` FOREIGN KEY (`LanguageId`) REFERENCES `language` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6605 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `localizedproperty`
--

DROP TABLE IF EXISTS `localizedproperty`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `localizedproperty` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `LanguageId` int NOT NULL,
  `LocaleKeyGroup` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LocaleKey` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LocaleValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_LocalizedProperty_Compound` (`EntityId`,`LocaleKey`,`LocaleKeyGroup`,`LanguageId`),
  KEY `IX_LocalizedProperty_Key` (`Id`),
  KEY `IX_LocalizedProperty_LanguageId` (`LanguageId`),
  KEY `IX_LocalizedProperty_LocaleKeyGroup` (`LocaleKeyGroup`),
  CONSTRAINT `FK_LocalizedProperty_Language_LanguageId` FOREIGN KEY (`LanguageId`) REFERENCES `language` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `log`
--

DROP TABLE IF EXISTS `log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `log` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LogLevelId` int NOT NULL,
  `ShortMessage` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FullMessage` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IpAddress` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CustomerId` int DEFAULT NULL,
  `PageUrl` varchar(1500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ReferrerUrl` varchar(1500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `Logger` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `HttpMethod` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `UserName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Log_CreatedOnUtc` (`CreatedOnUtc`),
  KEY `IX_Log_CustomerId` (`CustomerId`),
  KEY `IX_Log_Level` (`LogLevelId`),
  KEY `IX_Log_Logger` (`Logger`),
  CONSTRAINT `FK_Log_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=491 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `manufacturer`
--

DROP TABLE IF EXISTS `manufacturer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `manufacturer` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `BottomDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ManufacturerTemplateId` int NOT NULL,
  `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MetaDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaTitle` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MediaFileId` int DEFAULT NULL,
  `PageSize` int DEFAULT NULL,
  `AllowCustomersToSelectPageSize` tinyint(1) DEFAULT NULL,
  `PageSizeOptions` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PriceRanges` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `HasDiscountsApplied` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Deleted` (`Deleted`),
  KEY `IX_Manufacturer_DisplayOrder` (`DisplayOrder`),
  KEY `IX_Manufacturer_LimitedToStores` (`LimitedToStores`),
  KEY `IX_Manufacturer_MediaFileId` (`MediaFileId`),
  KEY `IX_SubjectToAcl` (`SubjectToAcl`),
  CONSTRAINT `FK_Manufacturer_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `manufacturertemplate`
--

DROP TABLE IF EXISTS `manufacturertemplate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `manufacturertemplate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ViewPath` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `measuredimension`
--

DROP TABLE IF EXISTS `measuredimension`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `measuredimension` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SystemKeyword` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Ratio` decimal(18,8) NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `measureweight`
--

DROP TABLE IF EXISTS `measureweight`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `measureweight` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SystemKeyword` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Ratio` decimal(18,8) NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediafile`
--

DROP TABLE IF EXISTS `mediafile`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediafile` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FolderId` int DEFAULT NULL,
  `Name` varchar(300) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Alt` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Title` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Extension` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MimeType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MediaType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Size` int NOT NULL,
  `PixelSize` int DEFAULT NULL,
  `Metadata` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Width` int DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `IsTransient` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `Hidden` tinyint(1) NOT NULL,
  `Version` int NOT NULL,
  `MediaStorageId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Media_Extension` (`FolderId`,`Extension`,`PixelSize`,`Deleted`),
  KEY `IX_Media_FolderId` (`FolderId`,`Deleted`),
  KEY `IX_Media_MediaType` (`FolderId`,`MediaType`,`Extension`,`PixelSize`,`Deleted`),
  KEY `IX_Media_Name` (`FolderId`,`Name`,`Deleted`),
  KEY `IX_Media_PixelSize` (`FolderId`,`PixelSize`,`Deleted`),
  KEY `IX_Media_Size` (`FolderId`,`Size`,`Deleted`),
  KEY `IX_Media_UpdatedOnUtc` (`FolderId`,`Deleted`),
  KEY `IX_MediaFile_MediaStorageId` (`MediaStorageId`),
  CONSTRAINT `FK_MediaFile_MediaFolder_FolderId` FOREIGN KEY (`FolderId`) REFERENCES `mediafolder` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_MediaFile_MediaStorage_MediaStorageId` FOREIGN KEY (`MediaStorageId`) REFERENCES `mediastorage` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediafile_tag_mapping`
--

DROP TABLE IF EXISTS `mediafile_tag_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediafile_tag_mapping` (
  `MediaFile_Id` int NOT NULL,
  `MediaTag_Id` int NOT NULL,
  PRIMARY KEY (`MediaFile_Id`,`MediaTag_Id`),
  KEY `IX_MediaFile_Tag_Mapping_MediaTag_Id` (`MediaTag_Id`),
  CONSTRAINT `FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id` FOREIGN KEY (`MediaFile_Id`) REFERENCES `mediafile` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id` FOREIGN KEY (`MediaTag_Id`) REFERENCES `mediatag` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediafolder`
--

DROP TABLE IF EXISTS `mediafolder`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediafolder` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ParentId` int DEFAULT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Slug` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CanDetectTracks` tinyint(1) NOT NULL,
  `Metadata` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FilesCount` int NOT NULL,
  `Discriminator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ResKey` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IncludePath` tinyint(1) DEFAULT NULL,
  `Order` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_NameParentId` (`ParentId`,`Name`),
  CONSTRAINT `FK_MediaFolder_MediaFolder_ParentId` FOREIGN KEY (`ParentId`) REFERENCES `mediafolder` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediastorage`
--

DROP TABLE IF EXISTS `mediastorage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediastorage` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Data` longblob NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediatag`
--

DROP TABLE IF EXISTS `mediatag`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediatag` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MediaTag_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediatrack`
--

DROP TABLE IF EXISTS `mediatrack`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediatrack` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MediaFileId` int NOT NULL,
  `Album` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `EntityId` int NOT NULL,
  `EntityName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Property` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Album` (`Album`),
  KEY `IX_MediaTrack_Composite` (`MediaFileId`,`EntityId`,`EntityName`,`Property`),
  CONSTRAINT `FK_MediaTrack_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `menuitemrecord`
--

DROP TABLE IF EXISTS `menuitemrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `menuitemrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MenuId` int NOT NULL,
  `ParentItemId` int NOT NULL,
  `ProviderName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Model` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Title` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ShortDescription` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PermissionNames` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `BeginGroup` tinyint(1) NOT NULL,
  `ShowExpanded` tinyint(1) NOT NULL,
  `NoFollow` tinyint(1) NOT NULL,
  `NewWindow` tinyint(1) NOT NULL,
  `Icon` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Style` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IconColor` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `HtmlId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CssClass` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MenuItem_DisplayOrder` (`DisplayOrder`),
  KEY `IX_MenuItem_LimitedToStores` (`LimitedToStores`),
  KEY `IX_MenuItem_ParentItemId` (`ParentItemId`),
  KEY `IX_MenuItem_Published` (`Published`),
  KEY `IX_MenuItem_SubjectToAcl` (`SubjectToAcl`),
  KEY `IX_MenuItemRecord_MenuId` (`MenuId`),
  CONSTRAINT `FK_MenuItemRecord_MenuRecord_MenuId` FOREIGN KEY (`MenuId`) REFERENCES `menurecord` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `menurecord`
--

DROP TABLE IF EXISTS `menurecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `menurecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SystemName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsSystemMenu` tinyint(1) NOT NULL,
  `Template` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `WidgetZone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Title` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Menu_LimitedToStores` (`LimitedToStores`),
  KEY `IX_Menu_Published` (`Published`),
  KEY `IX_Menu_SubjectToAcl` (`SubjectToAcl`),
  KEY `IX_Menu_SystemName_IsSystemMenu` (`SystemName`,`IsSystemMenu`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `messagetemplate`
--

DROP TABLE IF EXISTS `messagetemplate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messagetemplate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `To` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ReplyTo` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ModelTypes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LastModelTree` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `BccEmailAddresses` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Subject` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Body` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsActive` tinyint(1) NOT NULL,
  `EmailAccountId` int NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `SendManually` tinyint(1) NOT NULL,
  `Attachment1FileId` int DEFAULT NULL,
  `Attachment2FileId` int DEFAULT NULL,
  `Attachment3FileId` int DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `namedentity`
--

DROP TABLE IF EXISTS `namedentity`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `namedentity` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DisplayName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Slug` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LastMod` datetime(6) NOT NULL,
  `LanguageId` int DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `newslettersubscription`
--

DROP TABLE IF EXISTS `newslettersubscription`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `newslettersubscription` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `NewsLetterSubscriptionGuid` char(36) NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Active` tinyint(1) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `StoreId` int NOT NULL,
  `WorkingLanguageId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Active1` (`Active`),
  KEY `IX_NewsletterSubscription_Email_StoreId` (`Email`,`StoreId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `order`
--

DROP TABLE IF EXISTS `order`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `order` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `OrderNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `OrderGuid` char(36) NOT NULL,
  `StoreId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `BillingAddressId` int NOT NULL,
  `ShippingAddressId` int DEFAULT NULL,
  `PaymentMethodSystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomerCurrencyCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CurrencyRate` decimal(18,8) NOT NULL,
  `VatNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
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
  `TaxRates` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `OrderTax` decimal(18,4) NOT NULL,
  `OrderDiscount` decimal(18,4) NOT NULL,
  `CreditBalance` decimal(18,4) NOT NULL,
  `OrderTotalRounding` decimal(18,4) NOT NULL,
  `OrderTotal` decimal(18,4) NOT NULL,
  `RefundedAmount` decimal(18,4) NOT NULL,
  `RewardPointsWereAdded` tinyint(1) NOT NULL,
  `CheckoutAttributeDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CheckoutAttributesXml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomerLanguageId` int NOT NULL,
  `AffiliateId` int NOT NULL,
  `CustomerIp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AllowStoringCreditCardNumber` tinyint(1) NOT NULL,
  `CardType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CardName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CardNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MaskedCreditCardNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CardCvv2` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CardExpirationMonth` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CardExpirationYear` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AllowStoringDirectDebit` tinyint(1) NOT NULL,
  `DirectDebitAccountHolder` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitAccountNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitBankCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitBankName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitBIC` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitCountry` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DirectDebitIban` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomerOrderComment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AuthorizationTransactionId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AuthorizationTransactionCode` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AuthorizationTransactionResult` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CaptureTransactionId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CaptureTransactionResult` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SubscriptionTransactionId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PurchaseOrderNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PaidDateUtc` datetime(6) DEFAULT NULL,
  `ShippingMethod` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ShippingRateComputationMethodSystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Deleted` tinyint(1) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `RewardPointsRemaining` int DEFAULT NULL,
  `HasNewPaymentNotification` tinyint(1) NOT NULL,
  `AcceptThirdPartyEmailHandOver` tinyint(1) NOT NULL,
  `OrderStatusId` int NOT NULL,
  `PaymentStatusId` int NOT NULL,
  `ShippingStatusId` int NOT NULL,
  `CustomerTaxDisplayTypeId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Deleted3` (`Deleted`),
  KEY `IX_Order_BillingAddressId` (`BillingAddressId`),
  KEY `IX_Order_CustomerId` (`CustomerId`),
  KEY `IX_Order_ShippingAddressId` (`ShippingAddressId`),
  CONSTRAINT `FK_Order_Address_BillingAddressId` FOREIGN KEY (`BillingAddressId`) REFERENCES `address` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Order_Address_ShippingAddressId` FOREIGN KEY (`ShippingAddressId`) REFERENCES `address` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Order_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `orderitem`
--

DROP TABLE IF EXISTS `orderitem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderitem` (
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
  `AttributeDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AttributesXml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DownloadCount` int NOT NULL,
  `IsDownloadActivated` tinyint(1) NOT NULL,
  `LicenseDownloadId` int DEFAULT NULL,
  `ItemWeight` decimal(18,4) DEFAULT NULL,
  `BundleData` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProductCost` decimal(18,4) NOT NULL,
  `DeliveryTimeId` int DEFAULT NULL,
  `DisplayDeliveryTime` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderItem_OrderId` (`OrderId`),
  KEY `IX_OrderItem_ProductId` (`ProductId`),
  CONSTRAINT `FK_OrderItem_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `order` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_OrderItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ordernote`
--

DROP TABLE IF EXISTS `ordernote`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ordernote` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `Note` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayToCustomer` tinyint(1) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderNote_OrderId` (`OrderId`),
  CONSTRAINT `FK_OrderNote_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `order` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `paymentmethod`
--

DROP TABLE IF EXISTS `paymentmethod`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `paymentmethod` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PaymentMethodSystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FullDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RoundOrderTotalEnabled` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `permissionrecord`
--

DROP TABLE IF EXISTS `permissionrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissionrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SystemName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SystemName2` (`SystemName`)
) ENGINE=InnoDB AUTO_INCREMENT=288 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `permissionrolemapping`
--

DROP TABLE IF EXISTS `permissionrolemapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissionrolemapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Allow` tinyint(1) NOT NULL,
  `PermissionRecordId` int NOT NULL,
  `CustomerRoleId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PermissionRoleMapping_CustomerRoleId` (`CustomerRoleId`),
  KEY `IX_PermissionRoleMapping_PermissionRecordId` (`PermissionRecordId`),
  CONSTRAINT `FK_PermissionRoleMapping_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `customerrole` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_PermissionRoleMapping_PermissionRecord_PermissionRecordId` FOREIGN KEY (`PermissionRecordId`) REFERENCES `permissionrecord` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product`
--

DROP TABLE IF EXISTS `product`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductTypeId` int NOT NULL,
  `ParentGroupedProductId` int NOT NULL,
  `Visibility` int NOT NULL,
  `VisibleIndividually` tinyint(1) NOT NULL,
  `Condition` int NOT NULL,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ShortDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FullDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AdminComment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProductTemplateId` int NOT NULL,
  `ShowOnHomePage` tinyint(1) NOT NULL,
  `HomePageDisplayOrder` int NOT NULL,
  `MetaKeywords` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MetaDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaTitle` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AllowCustomerReviews` tinyint(1) NOT NULL,
  `ApprovedRatingSum` int NOT NULL,
  `NotApprovedRatingSum` int NOT NULL,
  `ApprovedTotalReviews` int NOT NULL,
  `NotApprovedTotalReviews` int NOT NULL,
  `SubjectToAcl` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  `Sku` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ManufacturerPartNumber` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Gtin` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsGiftCard` tinyint(1) NOT NULL,
  `GiftCardTypeId` int NOT NULL,
  `RequireOtherProducts` tinyint(1) NOT NULL,
  `RequiredProductIds` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AutomaticallyAddRequiredProducts` tinyint(1) NOT NULL,
  `IsDownload` tinyint(1) NOT NULL,
  `DownloadId` int NOT NULL,
  `UnlimitedDownloads` tinyint(1) NOT NULL,
  `MaxNumberOfDownloads` int NOT NULL,
  `DownloadExpirationDays` int DEFAULT NULL,
  `DownloadActivationTypeId` int NOT NULL,
  `HasSampleDownload` tinyint(1) NOT NULL,
  `SampleDownloadId` int DEFAULT NULL,
  `HasUserAgreement` tinyint(1) NOT NULL,
  `UserAgreementText` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
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
  `AllowedQuantities` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisableBuyButton` tinyint(1) NOT NULL,
  `DisableWishlistButton` tinyint(1) NOT NULL,
  `AvailableForPreOrder` tinyint(1) NOT NULL,
  `CallForPrice` tinyint(1) NOT NULL,
  `Price` decimal(18,4) NOT NULL,
  `OldPrice` decimal(18,4) NOT NULL,
  `ProductCost` decimal(18,4) NOT NULL,
  `SpecialPrice` decimal(18,4) DEFAULT NULL,
  `SpecialPriceStartDateTimeUtc` datetime(6) DEFAULT NULL,
  `SpecialPriceEndDateTimeUtc` datetime(6) DEFAULT NULL,
  `CustomerEntersPrice` tinyint(1) NOT NULL,
  `MinimumCustomerEnteredPrice` decimal(18,4) NOT NULL,
  `MaximumCustomerEnteredPrice` decimal(18,4) NOT NULL,
  `HasTierPrices` tinyint(1) NOT NULL,
  `LowestAttributeCombinationPrice` decimal(18,4) DEFAULT NULL,
  `AttributeChoiceBehaviour` int NOT NULL,
  `Weight` decimal(18,4) NOT NULL,
  `Length` decimal(18,4) NOT NULL,
  `Width` decimal(18,4) NOT NULL,
  `Height` decimal(18,4) NOT NULL,
  `AvailableStartDateTimeUtc` datetime(6) DEFAULT NULL,
  `AvailableEndDateTimeUtc` datetime(6) DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `IsSystemProduct` tinyint(1) NOT NULL,
  `SystemName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `DeliveryTimeId` int DEFAULT NULL,
  `QuantityUnitId` int DEFAULT NULL,
  `CustomsTariffNumber` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CountryOfOriginId` int DEFAULT NULL,
  `BasePriceEnabled` tinyint(1) NOT NULL,
  `BasePriceMeasureUnit` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BasePriceAmount` decimal(18,4) DEFAULT NULL,
  `BasePriceBaseAmount` int DEFAULT NULL,
  `BundleTitleText` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BundlePerItemShipping` tinyint(1) NOT NULL,
  `BundlePerItemPricing` tinyint(1) NOT NULL,
  `BundlePerItemShoppingCart` tinyint(1) NOT NULL,
  `MainPictureId` int DEFAULT NULL,
  `HasPreviewPicture` tinyint(1) NOT NULL,
  `HasDiscountsApplied` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Deleted2` (`Deleted`),
  KEY `IX_Gtin1` (`Gtin`),
  KEY `IX_IsSystemProduct` (`IsSystemProduct`),
  KEY `IX_ManufacturerPartNumber1` (`ManufacturerPartNumber`),
  KEY `IX_Product_CountryOfOriginId` (`CountryOfOriginId`),
  KEY `IX_Product_DeliveryTimeId` (`DeliveryTimeId`),
  KEY `IX_Product_LimitedToStores` (`LimitedToStores`),
  KEY `IX_Product_Name` (`Name`),
  KEY `IX_Product_ParentGroupedProductId` (`ParentGroupedProductId`),
  KEY `IX_Product_PriceDatesEtc` (`Price`,`AvailableStartDateTimeUtc`,`AvailableEndDateTimeUtc`,`Published`,`Deleted`),
  KEY `IX_Product_Published` (`Published`),
  KEY `IX_Product_Published_Deleted_IsSystemProduct` (`Published`,`Deleted`,`IsSystemProduct`),
  KEY `IX_Product_QuantityUnitId` (`QuantityUnitId`),
  KEY `IX_Product_SampleDownloadId` (`SampleDownloadId`),
  KEY `IX_Product_ShowOnHomepage` (`ShowOnHomePage`),
  KEY `IX_Product_Sku` (`Sku`),
  KEY `IX_Product_SubjectToAcl` (`SubjectToAcl`),
  KEY `IX_Product_SystemName_IsSystemProduct` (`SystemName`,`IsSystemProduct`),
  KEY `IX_SeekExport1` (`Published`,`Id`,`Visibility`,`Deleted`,`IsSystemProduct`,`AvailableStartDateTimeUtc`,`AvailableEndDateTimeUtc`),
  KEY `IX_Visibility` (`Visibility`),
  CONSTRAINT `FK_Product_Country_CountryOfOriginId` FOREIGN KEY (`CountryOfOriginId`) REFERENCES `country` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Product_DeliveryTime_DeliveryTimeId` FOREIGN KEY (`DeliveryTimeId`) REFERENCES `deliverytime` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Product_Download_SampleDownloadId` FOREIGN KEY (`SampleDownloadId`) REFERENCES `download` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Product_QuantityUnit_QuantityUnitId` FOREIGN KEY (`QuantityUnitId`) REFERENCES `quantityunit` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_category_mapping`
--

DROP TABLE IF EXISTS `product_category_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_category_mapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CategoryId` int NOT NULL,
  `ProductId` int NOT NULL,
  `IsFeaturedProduct` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `IsSystemMapping` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_IsFeaturedProduct1` (`IsFeaturedProduct`),
  KEY `IX_IsSystemMapping` (`IsSystemMapping`),
  KEY `IX_PCM_Product_and_Category` (`CategoryId`,`ProductId`),
  KEY `IX_Product_Category_Mapping_ProductId` (`ProductId`),
  CONSTRAINT `FK_Product_Category_Mapping_Category_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `category` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Product_Category_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_manufacturer_mapping`
--

DROP TABLE IF EXISTS `product_manufacturer_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_manufacturer_mapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ManufacturerId` int NOT NULL,
  `ProductId` int NOT NULL,
  `IsFeaturedProduct` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_IsFeaturedProduct` (`IsFeaturedProduct`),
  KEY `IX_PMM_Product_and_Manufacturer` (`ManufacturerId`,`ProductId`),
  KEY `IX_Product_Manufacturer_Mapping_ProductId` (`ProductId`),
  CONSTRAINT `FK_Product_Manufacturer_Mapping_Manufacturer_ManufacturerId` FOREIGN KEY (`ManufacturerId`) REFERENCES `manufacturer` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Product_Manufacturer_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_mediafile_mapping`
--

DROP TABLE IF EXISTS `product_mediafile_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_mediafile_mapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `MediaFileId` int NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Product_MediaFile_Mapping_MediaFileId` (`MediaFileId`),
  KEY `IX_Product_MediaFile_Mapping_ProductId` (`ProductId`),
  CONSTRAINT `FK_Product_MediaFile_Mapping_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Product_MediaFile_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_productattribute_mapping`
--

DROP TABLE IF EXISTS `product_productattribute_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_productattribute_mapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `ProductAttributeId` int NOT NULL,
  `TextPrompt` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomData` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsRequired` tinyint(1) NOT NULL,
  `AttributeControlTypeId` int NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AttributeControlTypeId` (`AttributeControlTypeId`),
  KEY `IX_Product_ProductAttribute_Mapping_ProductAttributeId` (`ProductAttributeId`),
  KEY `IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder` (`ProductId`,`DisplayOrder`),
  CONSTRAINT `FK_Product_ProductAttribute_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Product_ProductAttribute_Mapping_ProductAttribute_ProductAtt~` FOREIGN KEY (`ProductAttributeId`) REFERENCES `productattribute` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_producttag_mapping`
--

DROP TABLE IF EXISTS `product_producttag_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_producttag_mapping` (
  `Product_Id` int NOT NULL,
  `ProductTag_Id` int NOT NULL,
  PRIMARY KEY (`Product_Id`,`ProductTag_Id`),
  KEY `IX_Product_ProductTag_Mapping_ProductTag_Id` (`ProductTag_Id`),
  CONSTRAINT `FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id` FOREIGN KEY (`Product_Id`) REFERENCES `product` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id` FOREIGN KEY (`ProductTag_Id`) REFERENCES `producttag` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_specificationattribute_mapping`
--

DROP TABLE IF EXISTS `product_specificationattribute_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `product_specificationattribute_mapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SpecificationAttributeOptionId` int NOT NULL,
  `ProductId` int NOT NULL,
  `AllowFiltering` tinyint(1) DEFAULT NULL,
  `ShowOnProductPage` tinyint(1) DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Product_SpecificationAttribute_Mapping_ProductId` (`ProductId`),
  KEY `IX_PSAM_AllowFiltering` (`AllowFiltering`),
  KEY `IX_PSAM_SpecificationAttributeOptionId_AllowFiltering` (`SpecificationAttributeOptionId`,`AllowFiltering`),
  CONSTRAINT `FK_Product_SpecificationAttribute_Mapping_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Product_SpecificationAttribute_Mapping_SpecificationAttribut~` FOREIGN KEY (`SpecificationAttributeOptionId`) REFERENCES `specificationattributeoption` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productattribute`
--

DROP TABLE IF EXISTS `productattribute`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productattribute` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Alias` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AllowFiltering` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `FacetTemplateHint` int NOT NULL,
  `IndexOptionNames` tinyint(1) NOT NULL,
  `ExportMappings` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_AllowFiltering` (`AllowFiltering`),
  KEY `IX_DisplayOrder` (`DisplayOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productattributeoption`
--

DROP TABLE IF EXISTS `productattributeoption`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productattributeoption` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductAttributeOptionsSetId` int NOT NULL,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Alias` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MediaFileId` int NOT NULL,
  `Color` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PriceAdjustment` decimal(18,4) NOT NULL,
  `WeightAdjustment` decimal(18,4) NOT NULL,
  `IsPreSelected` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `ValueTypeId` int NOT NULL,
  `LinkedProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductAttributeOption_ProductAttributeOptionsSetId` (`ProductAttributeOptionsSetId`),
  CONSTRAINT `FK_ProductAttributeOption_ProductAttributeOptionsSet_ProductAtt~` FOREIGN KEY (`ProductAttributeOptionsSetId`) REFERENCES `productattributeoptionsset` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productattributeoptionsset`
--

DROP TABLE IF EXISTS `productattributeoptionsset`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productattributeoptionsset` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ProductAttributeId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductAttributeOptionsSet_ProductAttributeId` (`ProductAttributeId`),
  CONSTRAINT `FK_ProductAttributeOptionsSet_ProductAttribute_ProductAttribute~` FOREIGN KEY (`ProductAttributeId`) REFERENCES `productattribute` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productbundleitem`
--

DROP TABLE IF EXISTS `productbundleitem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productbundleitem` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `BundleProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  `Discount` decimal(18,4) DEFAULT NULL,
  `DiscountPercentage` tinyint(1) NOT NULL,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ShortDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FilterAttributes` tinyint(1) NOT NULL,
  `HideThumbnail` tinyint(1) NOT NULL,
  `Visible` tinyint(1) NOT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductBundleItem_BundleProductId` (`BundleProductId`),
  KEY `IX_ProductBundleItem_ProductId` (`ProductId`),
  CONSTRAINT `FK_ProductBundleItem_Product_BundleProductId` FOREIGN KEY (`BundleProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ProductBundleItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productbundleitemattributefilter`
--

DROP TABLE IF EXISTS `productbundleitemattributefilter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productbundleitemattributefilter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `BundleItemId` int NOT NULL,
  `AttributeId` int NOT NULL,
  `AttributeValueId` int NOT NULL,
  `IsPreSelected` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductBundleItemAttributeFilter_BundleItemId` (`BundleItemId`),
  CONSTRAINT `FK_ProductBundleItemAttributeFilter_ProductBundleItem_BundleIte~` FOREIGN KEY (`BundleItemId`) REFERENCES `productbundleitem` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productreview`
--

DROP TABLE IF EXISTS `productreview`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productreview` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `Title` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ReviewText` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Rating` int NOT NULL,
  `HelpfulYesTotal` int NOT NULL,
  `HelpfulNoTotal` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductReview_ProductId` (`ProductId`),
  CONSTRAINT `FK_ProductReview_CustomerContent_Id` FOREIGN KEY (`Id`) REFERENCES `customercontent` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ProductReview_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productreviewhelpfulness`
--

DROP TABLE IF EXISTS `productreviewhelpfulness`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productreviewhelpfulness` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductReviewId` int NOT NULL,
  `WasHelpful` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductReviewHelpfulness_ProductReviewId` (`ProductReviewId`),
  CONSTRAINT `FK_ProductReviewHelpfulness_CustomerContent_Id` FOREIGN KEY (`Id`) REFERENCES `customercontent` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ProductReviewHelpfulness_ProductReview_ProductReviewId` FOREIGN KEY (`ProductReviewId`) REFERENCES `productreview` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `producttag`
--

DROP TABLE IF EXISTS `producttag`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `producttag` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Published` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductTag_Name` (`Name`),
  KEY `IX_ProductTag_Published` (`Published`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `producttemplate`
--

DROP TABLE IF EXISTS `producttemplate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `producttemplate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ViewPath` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productvariantattributecombination`
--

DROP TABLE IF EXISTS `productvariantattributecombination`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productvariantattributecombination` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `Sku` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Gtin` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ManufacturerPartNumber` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Price` decimal(18,4) DEFAULT NULL,
  `Length` decimal(18,4) DEFAULT NULL,
  `Width` decimal(18,4) DEFAULT NULL,
  `Height` decimal(18,4) DEFAULT NULL,
  `BasePriceAmount` decimal(18,4) DEFAULT NULL,
  `BasePriceBaseAmount` int DEFAULT NULL,
  `AssignedMediaFileIds` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `DeliveryTimeId` int DEFAULT NULL,
  `QuantityUnitId` int DEFAULT NULL,
  `AttributesXml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `StockQuantity` int NOT NULL,
  `AllowOutOfStockOrders` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Gtin` (`Gtin`),
  KEY `IX_IsActive` (`IsActive`),
  KEY `IX_ManufacturerPartNumber` (`ManufacturerPartNumber`),
  KEY `IX_ProductVariantAttributeCombination_DeliveryTimeId` (`DeliveryTimeId`),
  KEY `IX_ProductVariantAttributeCombination_ProductId` (`ProductId`),
  KEY `IX_ProductVariantAttributeCombination_QuantityUnitId` (`QuantityUnitId`),
  KEY `IX_ProductVariantAttributeCombination_SKU` (`Sku`),
  KEY `IX_StockQuantity_AllowOutOfStockOrders` (`StockQuantity`,`AllowOutOfStockOrders`),
  CONSTRAINT `FK_ProductVariantAttributeCombination_DeliveryTime_DeliveryTime~` FOREIGN KEY (`DeliveryTimeId`) REFERENCES `deliverytime` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ProductVariantAttributeCombination_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ProductVariantAttributeCombination_QuantityUnit_QuantityUnit~` FOREIGN KEY (`QuantityUnitId`) REFERENCES `quantityunit` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productvariantattributevalue`
--

DROP TABLE IF EXISTS `productvariantattributevalue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productvariantattributevalue` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductVariantAttributeId` int NOT NULL,
  `Name` varchar(450) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Alias` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MediaFileId` int NOT NULL,
  `Color` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PriceAdjustment` decimal(18,4) NOT NULL,
  `WeightAdjustment` decimal(18,4) NOT NULL,
  `IsPreSelected` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  `ValueTypeId` int NOT NULL,
  `LinkedProductId` int NOT NULL,
  `Quantity` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Name` (`Name`),
  KEY `IX_ProductVariantAttributeValue_ProductVariantAttributeId_Displa` (`ProductVariantAttributeId`,`DisplayOrder`),
  KEY `IX_ValueTypeId` (`ValueTypeId`),
  CONSTRAINT `FK_ProductVariantAttributeValue_Product_ProductAttribute_Mappin~` FOREIGN KEY (`ProductVariantAttributeId`) REFERENCES `product_productattribute_mapping` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `quantityunit`
--

DROP TABLE IF EXISTS `quantityunit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `quantityunit` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NamePlural` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayLocale` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  `IsDefault` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `queuedemail`
--

DROP TABLE IF EXISTS `queuedemail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `queuedemail` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Priority` int NOT NULL,
  `From` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `To` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ReplyTo` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CC` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Bcc` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Subject` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Body` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `SentTries` int NOT NULL,
  `SentOnUtc` datetime(6) DEFAULT NULL,
  `EmailAccountId` int NOT NULL,
  `SendManually` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `[IX_QueuedEmail_CreatedOnUtc]` (`CreatedOnUtc`),
  KEY `IX_EmailAccountId` (`EmailAccountId`),
  CONSTRAINT `FK_QueuedEmail_EmailAccount_EmailAccountId` FOREIGN KEY (`EmailAccountId`) REFERENCES `emailaccount` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `queuedemailattachment`
--

DROP TABLE IF EXISTS `queuedemailattachment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `queuedemailattachment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `QueuedEmailId` int NOT NULL,
  `StorageLocation` int NOT NULL,
  `Path` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MediaFileId` int DEFAULT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MimeType` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MediaStorageId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MediaFileId` (`MediaFileId`),
  KEY `IX_MediaStorageId` (`MediaStorageId`),
  KEY `IX_QueuedEmailId` (`QueuedEmailId`),
  CONSTRAINT `FK_QueuedEmailAttachment_MediaFile_MediaFileId` FOREIGN KEY (`MediaFileId`) REFERENCES `mediafile` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_QueuedEmailAttachment_MediaStorage_MediaStorageId` FOREIGN KEY (`MediaStorageId`) REFERENCES `mediastorage` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_QueuedEmailAttachment_QueuedEmail_QueuedEmailId` FOREIGN KEY (`QueuedEmailId`) REFERENCES `queuedemail` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `recurringpayment`
--

DROP TABLE IF EXISTS `recurringpayment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `recurringpayment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CycleLength` int NOT NULL,
  `CyclePeriodId` int NOT NULL,
  `TotalCycles` int NOT NULL,
  `StartDateUtc` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `InitialOrderId` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RecurringPayment_InitialOrderId` (`InitialOrderId`),
  CONSTRAINT `FK_RecurringPayment_Order_InitialOrderId` FOREIGN KEY (`InitialOrderId`) REFERENCES `order` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `recurringpaymenthistory`
--

DROP TABLE IF EXISTS `recurringpaymenthistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `recurringpaymenthistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RecurringPaymentId` int NOT NULL,
  `OrderId` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RecurringPaymentHistory_RecurringPaymentId` (`RecurringPaymentId`),
  CONSTRAINT `FK_RecurringPaymentHistory_RecurringPayment_RecurringPaymentId` FOREIGN KEY (`RecurringPaymentId`) REFERENCES `recurringpayment` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `relatedproduct`
--

DROP TABLE IF EXISTS `relatedproduct`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `relatedproduct` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId1` int NOT NULL,
  `ProductId2` int NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RelatedProduct_ProductId1` (`ProductId1`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `returnrequest`
--

DROP TABLE IF EXISTS `returnrequest`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `returnrequest` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StoreId` int NOT NULL,
  `OrderItemId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `Quantity` int NOT NULL,
  `ReasonForReturn` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RequestedAction` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RequestedActionUpdatedOnUtc` datetime(6) DEFAULT NULL,
  `CustomerComments` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `StaffNotes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `AdminComment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ReturnRequestStatusId` int NOT NULL,
  `RefundToWallet` tinyint(1) DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ReturnRequest_CustomerId` (`CustomerId`),
  CONSTRAINT `FK_ReturnRequest_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `rewardpointshistory`
--

DROP TABLE IF EXISTS `rewardpointshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rewardpointshistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CustomerId` int NOT NULL,
  `Points` int NOT NULL,
  `PointsBalance` int NOT NULL,
  `UsedAmount` decimal(18,4) NOT NULL,
  `Message` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UsedWithOrder_Id` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_RewardPointsHistory_UsedWithOrder_Id` (`UsedWithOrder_Id`),
  KEY `IX_RewardPointsHistory_CustomerId` (`CustomerId`),
  CONSTRAINT `FK_RewardPointsHistory_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_RewardPointsHistory_Order_UsedWithOrder_Id` FOREIGN KEY (`UsedWithOrder_Id`) REFERENCES `order` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `rule`
--

DROP TABLE IF EXISTS `rule`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rule` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RuleSetId` int NOT NULL,
  `RuleType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Operator` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PageBuilder_DisplayOrder` (`DisplayOrder`),
  KEY `IX_PageBuilder_RuleType` (`RuleType`),
  KEY `IX_Rule_RuleSetId` (`RuleSetId`),
  CONSTRAINT `FK_Rule_RuleSet_RuleSetId` FOREIGN KEY (`RuleSetId`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset`
--

DROP TABLE IF EXISTS `ruleset`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Description` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Scope` int NOT NULL,
  `IsSubGroup` tinyint(1) NOT NULL,
  `LogicalOperator` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  `LastProcessedOnUtc` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_IsSubGroup` (`IsSubGroup`),
  KEY `IX_RuleSetEntity_Scope` (`IsActive`,`Scope`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset_category_mapping`
--

DROP TABLE IF EXISTS `ruleset_category_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset_category_mapping` (
  `Category_Id` int NOT NULL,
  `RuleSetEntity_Id` int NOT NULL,
  PRIMARY KEY (`Category_Id`,`RuleSetEntity_Id`),
  KEY `IX_RuleSet_Category_Mapping_RuleSetEntity_Id` (`RuleSetEntity_Id`),
  CONSTRAINT `FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id` FOREIGN KEY (`Category_Id`) REFERENCES `category` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset_customerrole_mapping`
--

DROP TABLE IF EXISTS `ruleset_customerrole_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset_customerrole_mapping` (
  `CustomerRole_Id` int NOT NULL,
  `RuleSetEntity_Id` int NOT NULL,
  PRIMARY KEY (`CustomerRole_Id`,`RuleSetEntity_Id`),
  KEY `IX_RuleSet_CustomerRole_Mapping_RuleSetEntity_Id` (`RuleSetEntity_Id`),
  CONSTRAINT `FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRol` FOREIGN KEY (`CustomerRole_Id`) REFERENCES `customerrole` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset_discount_mapping`
--

DROP TABLE IF EXISTS `ruleset_discount_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset_discount_mapping` (
  `Discount_Id` int NOT NULL,
  `RuleSetEntity_Id` int NOT NULL,
  PRIMARY KEY (`Discount_Id`,`RuleSetEntity_Id`),
  KEY `IX_RuleSet_Discount_Mapping_RuleSetEntity_Id` (`RuleSetEntity_Id`),
  CONSTRAINT `FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id` FOREIGN KEY (`Discount_Id`) REFERENCES `discount` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset_paymentmethod_mapping`
--

DROP TABLE IF EXISTS `ruleset_paymentmethod_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset_paymentmethod_mapping` (
  `PaymentMethod_Id` int NOT NULL,
  `RuleSetEntity_Id` int NOT NULL,
  PRIMARY KEY (`PaymentMethod_Id`,`RuleSetEntity_Id`),
  KEY `IX_RuleSet_PaymentMethod_Mapping_RuleSetEntity_Id` (`RuleSetEntity_Id`),
  CONSTRAINT `FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMe` FOREIGN KEY (`PaymentMethod_Id`) REFERENCES `paymentmethod` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_I` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ruleset_shippingmethod_mapping`
--

DROP TABLE IF EXISTS `ruleset_shippingmethod_mapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ruleset_shippingmethod_mapping` (
  `ShippingMethod_Id` int NOT NULL,
  `RuleSetEntity_Id` int NOT NULL,
  PRIMARY KEY (`ShippingMethod_Id`,`RuleSetEntity_Id`),
  KEY `IX_RuleSet_ShippingMethod_Mapping_RuleSetEntity_Id` (`RuleSetEntity_Id`),
  CONSTRAINT `FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_` FOREIGN KEY (`RuleSetEntity_Id`) REFERENCES `ruleset` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_Shippin` FOREIGN KEY (`ShippingMethod_Id`) REFERENCES `shippingmethod` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `scheduletask`
--

DROP TABLE IF EXISTS `scheduletask`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduletask` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Alias` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CronExpression` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Type` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  `Priority` int NOT NULL,
  `StopOnError` tinyint(1) NOT NULL,
  `NextRunUtc` datetime(6) DEFAULT NULL,
  `IsHidden` tinyint(1) NOT NULL,
  `RunPerMachine` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_NextRun_Enabled` (`NextRunUtc`,`Enabled`),
  KEY `IX_Type` (`Type`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `scheduletaskhistory`
--

DROP TABLE IF EXISTS `scheduletaskhistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduletaskhistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ScheduleTaskId` int NOT NULL,
  `IsRunning` tinyint(1) NOT NULL,
  `MachineName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StartedOnUtc` datetime(6) NOT NULL,
  `FinishedOnUtc` datetime(6) DEFAULT NULL,
  `SucceededOnUtc` datetime(6) DEFAULT NULL,
  `Error` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProgressPercent` int DEFAULT NULL,
  `ProgressMessage` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MachineName_IsRunning` (`MachineName`,`IsRunning`),
  KEY `IX_ScheduleTaskHistory_ScheduleTaskId` (`ScheduleTaskId`),
  KEY `IX_Started_Finished` (`StartedOnUtc`,`FinishedOnUtc`),
  CONSTRAINT `FK_ScheduleTaskHistory_ScheduleTask_ScheduleTaskId` FOREIGN KEY (`ScheduleTaskId`) REFERENCES `scheduletask` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `setting`
--

DROP TABLE IF EXISTS `setting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `setting` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `StoreId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Setting_Name` (`Name`),
  KEY `IX_Setting_StoreId` (`StoreId`)
) ENGINE=InnoDB AUTO_INCREMENT=492 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `shipment`
--

DROP TABLE IF EXISTS `shipment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shipment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `TrackingNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `TrackingUrl` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `TotalWeight` decimal(18,4) DEFAULT NULL,
  `ShippedDateUtc` datetime(6) DEFAULT NULL,
  `DeliveryDateUtc` datetime(6) DEFAULT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Shipment_OrderId` (`OrderId`),
  CONSTRAINT `FK_Shipment_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `order` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `shipmentitem`
--

DROP TABLE IF EXISTS `shipmentitem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shipmentitem` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ShipmentId` int NOT NULL,
  `OrderItemId` int NOT NULL,
  `Quantity` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ShipmentItem_ShipmentId` (`ShipmentId`),
  CONSTRAINT `FK_ShipmentItem_Shipment_ShipmentId` FOREIGN KEY (`ShipmentId`) REFERENCES `shipment` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `shippingmethod`
--

DROP TABLE IF EXISTS `shippingmethod`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shippingmethod` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DisplayOrder` int NOT NULL,
  `IgnoreCharges` tinyint(1) NOT NULL,
  `LimitedToStores` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `shoppingcartitem`
--

DROP TABLE IF EXISTS `shoppingcartitem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shoppingcartitem` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StoreId` int NOT NULL,
  `ParentItemId` int DEFAULT NULL,
  `BundleItemId` int DEFAULT NULL,
  `CustomerId` int NOT NULL,
  `ProductId` int NOT NULL,
  `AttributesXml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomerEnteredPrice` decimal(18,4) NOT NULL,
  `Quantity` int NOT NULL,
  `ShoppingCartTypeId` int NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `UpdatedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ShoppingCartItem_BundleItemId` (`BundleItemId`),
  KEY `IX_ShoppingCartItem_CustomerId` (`CustomerId`),
  KEY `IX_ShoppingCartItem_ProductId` (`ProductId`),
  KEY `IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId` (`ShoppingCartTypeId`,`CustomerId`),
  CONSTRAINT `FK_ShoppingCartItem_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ShoppingCartItem_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ShoppingCartItem_ProductBundleItem_BundleItemId` FOREIGN KEY (`BundleItemId`) REFERENCES `productbundleitem` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `specificationattribute`
--

DROP TABLE IF EXISTS `specificationattribute`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `specificationattribute` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Alias` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  `ShowOnProductPage` tinyint(1) NOT NULL,
  `AllowFiltering` tinyint(1) NOT NULL,
  `FacetSorting` int NOT NULL,
  `FacetTemplateHint` int NOT NULL,
  `IndexOptionNames` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AllowFiltering1` (`AllowFiltering`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `specificationattributeoption`
--

DROP TABLE IF EXISTS `specificationattributeoption`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `specificationattributeoption` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SpecificationAttributeId` int NOT NULL,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Alias` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL,
  `NumberValue` decimal(18,4) NOT NULL,
  `MediaFileId` int NOT NULL,
  `Color` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SpecificationAttributeOption_SpecificationAttributeId` (`SpecificationAttributeId`),
  CONSTRAINT `FK_SpecificationAttributeOption_SpecificationAttribute_Specific~` FOREIGN KEY (`SpecificationAttributeId`) REFERENCES `specificationattribute` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `stateprovince`
--

DROP TABLE IF EXISTS `stateprovince`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `stateprovince` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CountryId` int NOT NULL,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Abbreviation` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Published` tinyint(1) NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_StateProvince_CountryId` (`CountryId`),
  CONSTRAINT `FK_StateProvince_Country_CountryId` FOREIGN KEY (`CountryId`) REFERENCES `country` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=127 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `store`
--

DROP TABLE IF EXISTS `store`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `store` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Url` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SslEnabled` tinyint(1) NOT NULL,
  `SecureUrl` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ForceSslForAllPages` tinyint(1) NOT NULL,
  `Hosts` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LogoMediaFileId` int NOT NULL,
  `FavIconMediaFileId` int DEFAULT NULL,
  `PngIconMediaFileId` int DEFAULT NULL,
  `AppleTouchIconMediaFileId` int DEFAULT NULL,
  `MsTileImageMediaFileId` int DEFAULT NULL,
  `MsTileColor` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DisplayOrder` int NOT NULL,
  `HtmlBodyId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ContentDeliveryNetwork` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PrimaryStoreCurrencyId` int NOT NULL,
  `PrimaryExchangeRateCurrencyId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Store_PrimaryExchangeRateCurrencyId` (`PrimaryExchangeRateCurrencyId`),
  KEY `IX_Store_PrimaryStoreCurrencyId` (`PrimaryStoreCurrencyId`),
  CONSTRAINT `FK_Store_Currency_PrimaryExchangeRateCurrencyId` FOREIGN KEY (`PrimaryExchangeRateCurrencyId`) REFERENCES `currency` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Store_Currency_PrimaryStoreCurrencyId` FOREIGN KEY (`PrimaryStoreCurrencyId`) REFERENCES `currency` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `storemapping`
--

DROP TABLE IF EXISTS `storemapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `storemapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `EntityName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StoreId` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_StoreMapping_EntityId_EntityName` (`EntityId`,`EntityName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `syncmapping`
--

DROP TABLE IF EXISTS `syncmapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `syncmapping` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `SourceKey` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `EntityName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ContextName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SourceHash` varchar(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CustomInt` int DEFAULT NULL,
  `CustomString` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CustomBool` tinyint(1) DEFAULT NULL,
  `SyncedOnUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SyncMapping_ByEntity` (`EntityId`,`EntityName`,`ContextName`),
  UNIQUE KEY `IX_SyncMapping_BySource` (`SourceKey`,`EntityName`,`ContextName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `taxcategory`
--

DROP TABLE IF EXISTS `taxcategory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `taxcategory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayOrder` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `themevariable`
--

DROP TABLE IF EXISTS `themevariable`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `themevariable` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Theme` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Name` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Value` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `StoreId` int NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tierprice`
--

DROP TABLE IF EXISTS `tierprice`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tierprice` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `StoreId` int NOT NULL,
  `Quantity` int NOT NULL,
  `Price` decimal(18,4) NOT NULL,
  `CalculationMethod` int NOT NULL,
  `CustomerRoleId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_TierPrice_CustomerRoleId` (`CustomerRoleId`),
  KEY `IX_TierPrice_ProductId` (`ProductId`),
  CONSTRAINT `FK_TierPrice_CustomerRole_CustomerRoleId` FOREIGN KEY (`CustomerRoleId`) REFERENCES `customerrole` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_TierPrice_Product_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `product` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `topic`
--

DROP TABLE IF EXISTS `topic`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `topic` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SystemName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsSystemTopic` tinyint(1) NOT NULL,
  `HtmlId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `BodyCssClass` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IncludeInSitemap` tinyint(1) NOT NULL,
  `IsPasswordProtected` tinyint(1) NOT NULL,
  `Password` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Title` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ShortTitle` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Intro` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Body` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaKeywords` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaDescription` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `MetaTitle` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `LimitedToStores` tinyint(1) NOT NULL,
  `RenderAsWidget` tinyint(1) NOT NULL,
  `WidgetZone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `WidgetWrapContent` tinyint(1) DEFAULT NULL,
  `WidgetShowTitle` tinyint(1) NOT NULL,
  `WidgetBordered` tinyint(1) NOT NULL,
  `Priority` int NOT NULL,
  `TitleTag` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SubjectToAcl` tinyint(1) NOT NULL,
  `IsPublished` tinyint(1) NOT NULL,
  `CookieType` int DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `urlrecord`
--

DROP TABLE IF EXISTS `urlrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `urlrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EntityId` int NOT NULL,
  `EntityName` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Slug` varchar(400) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `LanguageId` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_UrlRecord_Slug` (`Slug`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `wallethistory`
--

DROP TABLE IF EXISTS `wallethistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wallethistory` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StoreId` int NOT NULL,
  `CustomerId` int NOT NULL,
  `OrderId` int DEFAULT NULL,
  `Amount` decimal(18,4) NOT NULL,
  `AmountBalance` decimal(18,4) NOT NULL,
  `AmountBalancePerStore` decimal(18,4) NOT NULL,
  `CreatedOnUtc` datetime(6) NOT NULL,
  `Reason` int DEFAULT NULL,
  `Message` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AdminComment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_StoreId_CreatedOn` (`StoreId`,`CreatedOnUtc`),
  KEY `IX_WalletHistory_CustomerId` (`CustomerId`),
  KEY `IX_WalletHistory_OrderId` (`OrderId`),
  CONSTRAINT `FK_WalletHistory_Customer_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `customer` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WalletHistory_Order_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `order` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed
