
/****** Object:  Table [dbo].[AclRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AclRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[EntityName] [nvarchar](400) NOT NULL,
	[CustomerRoleId] [int] NOT NULL,
	[IsIdle] [bit] NOT NULL,
 CONSTRAINT [PK_AclRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ActivityLog]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActivityLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ActivityLogTypeId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Comment] [nvarchar](max) NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ActivityLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ActivityLogType]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActivityLogType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SystemKeyword] [nvarchar](100) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Enabled] [bit] NOT NULL,
 CONSTRAINT [PK_ActivityLogType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Address]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Address](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Salutation] [nvarchar](max) NULL,
	[Title] [nvarchar](max) NULL,
	[FirstName] [nvarchar](max) NULL,
	[LastName] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[Company] [nvarchar](max) NULL,
	[CountryId] [int] NULL,
	[StateProvinceId] [int] NULL,
	[City] [nvarchar](max) NULL,
	[Address1] [nvarchar](max) NULL,
	[Address2] [nvarchar](max) NULL,
	[ZipPostalCode] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[FaxNumber] [nvarchar](max) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Address] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Affiliate]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Affiliate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Active] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[AddressId] [int] NOT NULL,
 CONSTRAINT [PK_Affiliate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BackInStockSubscription]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BackInStockSubscription](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StoreId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_BackInStockSubscription] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Campaign]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Campaign](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Subject] [nvarchar](max) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
 CONSTRAINT [PK_Campaign] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Category]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Category](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[FullName] [nvarchar](400) NULL,
	[Description] [nvarchar](max) NULL,
	[BottomDescription] [nvarchar](max) NULL,
	[ExternalLink] [nvarchar](255) NULL,
	[BadgeText] [nvarchar](400) NULL,
	[BadgeStyle] [int] NOT NULL,
	[Alias] [nvarchar](100) NULL,
	[CategoryTemplateId] [int] NOT NULL,
	[MetaKeywords] [nvarchar](400) NULL,
	[MetaDescription] [nvarchar](4000) NULL,
	[MetaTitle] [nvarchar](400) NULL,
	[ParentCategoryId] [int] NOT NULL,
	[MediaFileId] [int] NULL,
	[PageSize] [int] NULL,
	[AllowCustomersToSelectPageSize] [bit] NULL,
	[PageSizeOptions] [nvarchar](200) NULL,
	[PriceRanges] [nvarchar](400) NULL,
	[ShowOnHomePage] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[DefaultViewMode] [nvarchar](max) NULL,
	[HasDiscountsApplied] [bit] NOT NULL,
 CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CategoryTemplate]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CategoryTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[ViewPath] [nvarchar](400) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_CategoryTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CheckoutAttribute]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CheckoutAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[TextPrompt] [nvarchar](max) NULL,
	[IsRequired] [bit] NOT NULL,
	[ShippableProductRequired] [bit] NOT NULL,
	[IsTaxExempt] [bit] NOT NULL,
	[TaxCategoryId] [int] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[AttributeControlTypeId] [int] NOT NULL,
 CONSTRAINT [PK_CheckoutAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CheckoutAttributeValue]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CheckoutAttributeValue](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[PriceAdjustment] [decimal](18, 4) NOT NULL,
	[WeightAdjustment] [decimal](18, 4) NOT NULL,
	[IsPreSelected] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[Color] [nvarchar](100) NULL,
	[CheckoutAttributeId] [int] NOT NULL,
	[MediaFileId] [int] NULL,
 CONSTRAINT [PK_CheckoutAttributeValue] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Country]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Country](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](4000) NULL,
	[AllowsBilling] [bit] NOT NULL,
	[AllowsShipping] [bit] NOT NULL,
	[TwoLetterIsoCode] [nvarchar](max) NULL,
	[ThreeLetterIsoCode] [nvarchar](max) NULL,
	[NumericIsoCode] [int] NOT NULL,
	[SubjectToVat] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[DisplayCookieManager] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[AddressFormat] [nvarchar](max) NULL,
	[DefaultCurrencyId] [int] NULL,
 CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CrossSellProduct]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CrossSellProduct](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId1] [int] NOT NULL,
	[ProductId2] [int] NOT NULL,
 CONSTRAINT [PK_CrossSellProduct] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Currency]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Currency](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[CurrencyCode] [nvarchar](5) NOT NULL,
	[Rate] [decimal](18, 8) NOT NULL,
	[DisplayLocale] [nvarchar](50) NULL,
	[CustomFormatting] [nvarchar](50) NULL,
	[LimitedToStores] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[DomainEndings] [nvarchar](1000) NULL,
	[RoundOrderItemsEnabled] [bit] NOT NULL,
	[RoundNumDecimals] [int] NOT NULL,
	[RoundOrderTotalEnabled] [bit] NOT NULL,
	[RoundOrderTotalDenominator] [decimal](18, 4) NOT NULL,
	[RoundOrderTotalRule] [int] NOT NULL,
 CONSTRAINT [PK_Currency] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Customer]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerGuid] [uniqueidentifier] NOT NULL,
	[Username] [nvarchar](500) NULL,
	[Email] [nvarchar](500) NULL,
	[Password] [nvarchar](500) NULL,
	[PasswordFormatId] [int] NOT NULL,
	[PasswordSalt] [nvarchar](500) NULL,
	[AdminComment] [nvarchar](4000) NULL,
	[IsTaxExempt] [bit] NOT NULL,
	[AffiliateId] [int] NOT NULL,
	[Active] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[IsSystemAccount] [bit] NOT NULL,
	[SystemName] [nvarchar](500) NULL,
	[LastIpAddress] [nvarchar](100) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[LastLoginDateUtc] [datetime2](7) NULL,
	[LastActivityDateUtc] [datetime2](7) NOT NULL,
	[Salutation] [nvarchar](50) NULL,
	[Title] [nvarchar](100) NULL,
	[FirstName] [nvarchar](225) NULL,
	[LastName] [nvarchar](225) NULL,
	[FullName] [nvarchar](450) NULL,
	[Company] [nvarchar](255) NULL,
	[CustomerNumber] [nvarchar](100) NULL,
	[BirthDate] [datetime2](7) NULL,
	[Gender] [nvarchar](max) NULL,
	[VatNumberStatusId] [int] NOT NULL,
	[TimeZoneId] [nvarchar](max) NULL,
	[TaxDisplayTypeId] [int] NOT NULL,
	[LastForumVisit] [datetime2](7) NULL,
	[LastUserAgent] [nvarchar](max) NULL,
	[LastUserDeviceType] [nvarchar](max) NULL,
	[BillingAddress_Id] [int] NULL,
	[ShippingAddress_Id] [int] NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerAddresses]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerAddresses](
	[Customer_Id] [int] NOT NULL,
	[Address_Id] [int] NOT NULL,
 CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY CLUSTERED 
(
	[Customer_Id] ASC,
	[Address_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerContent]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerContent](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[IpAddress] [nvarchar](200) NULL,
	[IsApproved] [bit] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_CustomerContent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerRole]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerRole](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NOT NULL,
	[FreeShipping] [bit] NOT NULL,
	[TaxExempt] [bit] NOT NULL,
	[TaxDisplayType] [int] NULL,
	[Active] [bit] NOT NULL,
	[IsSystemRole] [bit] NOT NULL,
	[SystemName] [nvarchar](255) NULL,
	[OrderTotalMinimum] [decimal](18, 2) NULL,
	[OrderTotalMaximum] [decimal](18, 2) NULL,
 CONSTRAINT [PK_CustomerRole] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerRoleMapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerRoleMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[CustomerRoleId] [int] NOT NULL,
	[IsSystemMapping] [bit] NOT NULL,
 CONSTRAINT [PK_CustomerRoleMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeliveryTime]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeliveryTime](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[ColorHexValue] [nvarchar](50) NOT NULL,
	[DisplayLocale] [nvarchar](50) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsDefault] [bit] NULL,
	[MinDays] [int] NULL,
	[MaxDays] [int] NULL,
 CONSTRAINT [PK_DeliveryTime] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Discount]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Discount](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[DiscountTypeId] [int] NOT NULL,
	[UsePercentage] [bit] NOT NULL,
	[DiscountPercentage] [decimal](18, 4) NOT NULL,
	[DiscountAmount] [decimal](18, 4) NOT NULL,
	[StartDateUtc] [datetime2](7) NULL,
	[EndDateUtc] [datetime2](7) NULL,
	[RequiresCouponCode] [bit] NOT NULL,
	[CouponCode] [nvarchar](100) NULL,
	[DiscountLimitationId] [int] NOT NULL,
	[LimitationTimes] [int] NOT NULL,
 CONSTRAINT [PK_Discount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Discount_AppliedToCategories]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Discount_AppliedToCategories](
	[Discount_Id] [int] NOT NULL,
	[Category_Id] [int] NOT NULL,
 CONSTRAINT [PK_Discount_AppliedToCategories] PRIMARY KEY CLUSTERED 
(
	[Discount_Id] ASC,
	[Category_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Discount_AppliedToManufacturers]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Discount_AppliedToManufacturers](
	[Discount_Id] [int] NOT NULL,
	[Manufacturer_Id] [int] NOT NULL,
 CONSTRAINT [PK_Discount_AppliedToManufacturers] PRIMARY KEY CLUSTERED 
(
	[Discount_Id] ASC,
	[Manufacturer_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Discount_AppliedToProducts]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Discount_AppliedToProducts](
	[Discount_Id] [int] NOT NULL,
	[Product_Id] [int] NOT NULL,
 CONSTRAINT [PK_Discount_AppliedToProducts] PRIMARY KEY CLUSTERED 
(
	[Discount_Id] ASC,
	[Product_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DiscountUsageHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DiscountUsageHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DiscountId] [int] NOT NULL,
	[OrderId] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DiscountUsageHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Download]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Download](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DownloadGuid] [uniqueidentifier] NOT NULL,
	[UseDownloadUrl] [bit] NOT NULL,
	[DownloadUrl] [nvarchar](4000) NULL,
	[IsTransient] [bit] NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[MediaFileId] [int] NULL,
	[EntityId] [int] NOT NULL,
	[EntityName] [nvarchar](100) NULL,
	[FileVersion] [nvarchar](30) NULL,
	[Changelog] [nvarchar](max) NULL,
 CONSTRAINT [PK_Download] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EmailAccount]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmailAccount](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[DisplayName] [nvarchar](255) NULL,
	[Host] [nvarchar](255) NOT NULL,
	[Port] [int] NOT NULL,
	[Username] [nvarchar](255) NOT NULL,
	[Password] [nvarchar](255) NOT NULL,
	[EnableSsl] [bit] NOT NULL,
	[UseDefaultCredentials] [bit] NOT NULL,
 CONSTRAINT [PK_EmailAccount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExportDeployment]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExportDeployment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProfileId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[ResultInfo] [nvarchar](max) NULL,
	[DeploymentTypeId] [int] NOT NULL,
	[Username] [nvarchar](400) NULL,
	[Password] [nvarchar](400) NULL,
	[Url] [nvarchar](4000) NULL,
	[HttpTransmissionTypeId] [int] NOT NULL,
	[HttpTransmissionType] [int] NOT NULL,
	[FileSystemPath] [nvarchar](400) NULL,
	[SubFolder] [nvarchar](400) NULL,
	[EmailAddresses] [nvarchar](4000) NULL,
	[EmailSubject] [nvarchar](400) NULL,
	[EmailAccountId] [int] NOT NULL,
	[PassiveMode] [bit] NOT NULL,
	[UseSsl] [bit] NOT NULL,
 CONSTRAINT [PK_ExportDeployment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExportProfile]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExportProfile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[FolderName] [nvarchar](400) NOT NULL,
	[FileNamePattern] [nvarchar](400) NULL,
	[SystemName] [nvarchar](400) NULL,
	[ProviderSystemName] [nvarchar](4000) NOT NULL,
	[IsSystemProfile] [bit] NOT NULL,
	[Enabled] [bit] NOT NULL,
	[ExportRelatedData] [bit] NOT NULL,
	[Filtering] [nvarchar](max) NULL,
	[Projection] [nvarchar](max) NULL,
	[ProviderConfigData] [nvarchar](max) NULL,
	[ResultInfo] [nvarchar](max) NULL,
	[Offset] [int] NOT NULL,
	[Limit] [int] NOT NULL,
	[BatchSize] [int] NOT NULL,
	[PerStore] [bit] NOT NULL,
	[EmailAccountId] [int] NOT NULL,
	[CompletedEmailAddresses] [nvarchar](400) NULL,
	[CreateZipArchive] [bit] NOT NULL,
	[Cleanup] [bit] NOT NULL,
	[SchedulingTaskId] [int] NOT NULL,
 CONSTRAINT [PK_ExportProfile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ExternalAuthenticationRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExternalAuthenticationRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Email] [nvarchar](4000) NULL,
	[ExternalIdentifier] [nvarchar](4000) NULL,
	[ExternalDisplayIdentifier] [nvarchar](4000) NULL,
	[OAuthToken] [nvarchar](4000) NULL,
	[OAuthAccessToken] [nvarchar](4000) NULL,
	[ProviderSystemName] [nvarchar](4000) NULL,
 CONSTRAINT [PK_ExternalAuthenticationRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GenericAttribute]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GenericAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[KeyGroup] [nvarchar](400) NOT NULL,
	[Key] [nvarchar](400) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	[StoreId] [int] NOT NULL,
 CONSTRAINT [PK_GenericAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GiftCard]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GiftCard](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GiftCardTypeId] [int] NOT NULL,
	[PurchasedWithOrderItemId] [int] NULL,
	[Amount] [decimal](18, 4) NOT NULL,
	[IsGiftCardActivated] [bit] NOT NULL,
	[GiftCardCouponCode] [nvarchar](max) NULL,
	[RecipientName] [nvarchar](max) NULL,
	[RecipientEmail] [nvarchar](max) NULL,
	[SenderName] [nvarchar](max) NULL,
	[SenderEmail] [nvarchar](max) NULL,
	[Message] [nvarchar](max) NULL,
	[IsRecipientNotified] [bit] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_GiftCard] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GiftCardUsageHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GiftCardUsageHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GiftCardId] [int] NOT NULL,
	[UsedWithOrderId] [int] NOT NULL,
	[UsedValue] [decimal](18, 4) NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_GiftCardUsageHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ImportProfile]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ImportProfile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[FolderName] [nvarchar](100) NOT NULL,
	[FileTypeId] [int] NOT NULL,
	[EntityTypeId] [int] NOT NULL,
	[Enabled] [bit] NOT NULL,
	[ImportRelatedData] [bit] NOT NULL,
	[Skip] [int] NOT NULL,
	[Take] [int] NOT NULL,
	[UpdateOnly] [bit] NOT NULL,
	[KeyFieldNames] [nvarchar](1000) NULL,
	[FileTypeConfiguration] [nvarchar](max) NULL,
	[ExtraData] [nvarchar](max) NULL,
	[ColumnMapping] [nvarchar](max) NULL,
	[ResultInfo] [nvarchar](max) NULL,
	[SchedulingTaskId] [int] NOT NULL,
 CONSTRAINT [PK_ImportProfile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Language]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Language](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[LanguageCulture] [nvarchar](20) NOT NULL,
	[UniqueSeoCode] [nvarchar](2) NOT NULL,
	[FlagImageFileName] [nvarchar](50) NOT NULL,
	[Rtl] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Language] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LocaleStringResource]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LocaleStringResource](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LanguageId] [int] NOT NULL,
	[ResourceName] [nvarchar](200) NOT NULL,
	[ResourceValue] [nvarchar](max) NOT NULL,
	[IsFromPlugin] [bit] NULL,
	[IsTouched] [bit] NULL,
 CONSTRAINT [PK_LocaleStringResource] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LocalizedProperty]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LocalizedProperty](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[LanguageId] [int] NOT NULL,
	[LocaleKeyGroup] [nvarchar](150) NOT NULL,
	[LocaleKey] [nvarchar](255) NOT NULL,
	[LocaleValue] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_LocalizedProperty] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Log]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Log](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LogLevelId] [int] NOT NULL,
	[ShortMessage] [nvarchar](4000) NOT NULL,
	[FullMessage] [nvarchar](max) NULL,
	[IpAddress] [nvarchar](200) NULL,
	[CustomerId] [int] NULL,
	[PageUrl] [nvarchar](1500) NULL,
	[ReferrerUrl] [nvarchar](1500) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[Logger] [nvarchar](400) NOT NULL,
	[HttpMethod] [nvarchar](10) NULL,
	[UserName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Manufacturer]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Manufacturer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[BottomDescription] [nvarchar](max) NULL,
	[ManufacturerTemplateId] [int] NOT NULL,
	[MetaKeywords] [nvarchar](400) NULL,
	[MetaDescription] [nvarchar](4000) NULL,
	[MetaTitle] [nvarchar](400) NULL,
	[MediaFileId] [int] NULL,
	[PageSize] [int] NULL,
	[AllowCustomersToSelectPageSize] [bit] NULL,
	[PageSizeOptions] [nvarchar](200) NULL,
	[PriceRanges] [nvarchar](400) NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[HasDiscountsApplied] [bit] NOT NULL,
 CONSTRAINT [PK_Manufacturer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ManufacturerTemplate]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ManufacturerTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[ViewPath] [nvarchar](400) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_ManufacturerTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MeasureDimension]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MeasureDimension](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[SystemKeyword] [nvarchar](100) NOT NULL,
	[Ratio] [decimal](18, 8) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_MeasureDimension] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MeasureWeight]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MeasureWeight](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[SystemKeyword] [nvarchar](max) NULL,
	[Ratio] [decimal](18, 8) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_MeasureWeight] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaFile]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaFile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FolderId] [int] NULL,
	[Name] [nvarchar](300) NULL,
	[Alt] [nvarchar](400) NULL,
	[Title] [nvarchar](400) NULL,
	[Extension] [nvarchar](50) NULL,
	[MimeType] [nvarchar](100) NOT NULL,
	[MediaType] [nvarchar](20) NOT NULL,
	[Size] [int] NOT NULL,
	[PixelSize] [int] NULL,
	[Metadata] [nvarchar](max) NULL,
	[Width] [int] NULL,
	[Height] [int] NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[IsTransient] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[Hidden] [bit] NOT NULL,
	[Version] [int] NOT NULL,
	[MediaStorageId] [int] NULL,
 CONSTRAINT [PK_MediaFile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaFile_Tag_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaFile_Tag_Mapping](
	[MediaFile_Id] [int] NOT NULL,
	[MediaTag_Id] [int] NOT NULL,
 CONSTRAINT [PK_MediaFile_Tag_Mapping] PRIMARY KEY CLUSTERED 
(
	[MediaFile_Id] ASC,
	[MediaTag_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaFolder]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaFolder](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ParentId] [int] NULL,
	[Name] [nvarchar](255) NOT NULL,
	[Slug] [nvarchar](255) NULL,
	[CanDetectTracks] [bit] NOT NULL,
	[Metadata] [nvarchar](max) NULL,
	[FilesCount] [int] NOT NULL,
	[Discriminator] [nvarchar](128) NOT NULL,
	[ResKey] [nvarchar](max) NULL,
	[IncludePath] [bit] NULL,
	[Order] [int] NULL,
 CONSTRAINT [PK_MediaFolder] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaStorage]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaStorage](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Data] [varbinary](max) NOT NULL,
 CONSTRAINT [PK_MediaStorage] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaTag]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaTag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_MediaTag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MediaTrack]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MediaTrack](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MediaFileId] [int] NOT NULL,
	[Album] [nvarchar](50) NOT NULL,
	[EntityId] [int] NOT NULL,
	[EntityName] [nvarchar](255) NOT NULL,
	[Property] [nvarchar](255) NULL,
 CONSTRAINT [PK_MediaTrack] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MenuItemRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MenuItemRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MenuId] [int] NOT NULL,
	[ParentItemId] [int] NOT NULL,
	[ProviderName] [nvarchar](100) NULL,
	[Model] [nvarchar](max) NULL,
	[Title] [nvarchar](400) NULL,
	[ShortDescription] [nvarchar](400) NULL,
	[PermissionNames] [nvarchar](max) NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[BeginGroup] [bit] NOT NULL,
	[ShowExpanded] [bit] NOT NULL,
	[NoFollow] [bit] NOT NULL,
	[NewWindow] [bit] NOT NULL,
	[Icon] [nvarchar](100) NULL,
	[Style] [nvarchar](10) NULL,
	[IconColor] [nvarchar](100) NULL,
	[HtmlId] [nvarchar](100) NULL,
	[CssClass] [nvarchar](100) NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
 CONSTRAINT [PK_MenuItemRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MenuRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MenuRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SystemName] [nvarchar](400) NOT NULL,
	[IsSystemMenu] [bit] NOT NULL,
	[Template] [nvarchar](400) NULL,
	[WidgetZone] [nvarchar](4000) NULL,
	[Title] [nvarchar](400) NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
 CONSTRAINT [PK_MenuRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MessageTemplate]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MessageTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[To] [nvarchar](500) NOT NULL,
	[ReplyTo] [nvarchar](500) NULL,
	[ModelTypes] [nvarchar](500) NULL,
	[LastModelTree] [nvarchar](max) NULL,
	[BccEmailAddresses] [nvarchar](200) NULL,
	[Subject] [nvarchar](1000) NULL,
	[Body] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[EmailAccountId] [int] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[SendManually] [bit] NOT NULL,
	[Attachment1FileId] [int] NULL,
	[Attachment2FileId] [int] NULL,
	[Attachment3FileId] [int] NULL,
 CONSTRAINT [PK_MessageTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NamedEntity]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NamedEntity](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityName] [nvarchar](max) NULL,
	[DisplayName] [nvarchar](max) NULL,
	[Slug] [nvarchar](max) NULL,
	[LastMod] [datetime2](7) NOT NULL,
	[LanguageId] [int] NULL,
 CONSTRAINT [PK_NamedEntity] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NewsLetterSubscription]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewsLetterSubscription](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NewsLetterSubscriptionGuid] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[Active] [bit] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[StoreId] [int] NOT NULL,
	[WorkingLanguageId] [int] NOT NULL,
 CONSTRAINT [PK_NewsLetterSubscription] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Order]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Order](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderNumber] [nvarchar](4000) NULL,
	[OrderGuid] [uniqueidentifier] NOT NULL,
	[StoreId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[BillingAddressId] [int] NOT NULL,
	[ShippingAddressId] [int] NULL,
	[PaymentMethodSystemName] [nvarchar](4000) NULL,
	[CustomerCurrencyCode] [nvarchar](4000) NULL,
	[CurrencyRate] [decimal](18, 8) NOT NULL,
	[VatNumber] [nvarchar](4000) NULL,
	[OrderSubtotalInclTax] [decimal](18, 4) NOT NULL,
	[OrderSubtotalExclTax] [decimal](18, 4) NOT NULL,
	[OrderSubTotalDiscountInclTax] [decimal](18, 4) NOT NULL,
	[OrderSubTotalDiscountExclTax] [decimal](18, 4) NOT NULL,
	[OrderShippingInclTax] [decimal](18, 4) NOT NULL,
	[OrderShippingExclTax] [decimal](18, 4) NOT NULL,
	[OrderShippingTaxRate] [decimal](18, 4) NOT NULL,
	[PaymentMethodAdditionalFeeInclTax] [decimal](18, 4) NOT NULL,
	[PaymentMethodAdditionalFeeExclTax] [decimal](18, 4) NOT NULL,
	[PaymentMethodAdditionalFeeTaxRate] [decimal](18, 4) NOT NULL,
	[TaxRates] [nvarchar](4000) NULL,
	[OrderTax] [decimal](18, 4) NOT NULL,
	[OrderDiscount] [decimal](18, 4) NOT NULL,
	[CreditBalance] [decimal](18, 4) NOT NULL,
	[OrderTotalRounding] [decimal](18, 4) NOT NULL,
	[OrderTotal] [decimal](18, 4) NOT NULL,
	[RefundedAmount] [decimal](18, 4) NOT NULL,
	[RewardPointsWereAdded] [bit] NOT NULL,
	[CheckoutAttributeDescription] [nvarchar](4000) NULL,
	[CheckoutAttributesXml] [nvarchar](4000) NULL,
	[CustomerLanguageId] [int] NOT NULL,
	[AffiliateId] [int] NOT NULL,
	[CustomerIp] [nvarchar](4000) NULL,
	[AllowStoringCreditCardNumber] [bit] NOT NULL,
	[CardType] [nvarchar](4000) NULL,
	[CardName] [nvarchar](4000) NULL,
	[CardNumber] [nvarchar](4000) NULL,
	[MaskedCreditCardNumber] [nvarchar](4000) NULL,
	[CardCvv2] [nvarchar](4000) NULL,
	[CardExpirationMonth] [nvarchar](4000) NULL,
	[CardExpirationYear] [nvarchar](4000) NULL,
	[AllowStoringDirectDebit] [bit] NOT NULL,
	[DirectDebitAccountHolder] [nvarchar](4000) NULL,
	[DirectDebitAccountNumber] [nvarchar](4000) NULL,
	[DirectDebitBankCode] [nvarchar](4000) NULL,
	[DirectDebitBankName] [nvarchar](4000) NULL,
	[DirectDebitBIC] [nvarchar](4000) NULL,
	[DirectDebitCountry] [nvarchar](4000) NULL,
	[DirectDebitIban] [nvarchar](4000) NULL,
	[CustomerOrderComment] [nvarchar](max) NULL,
	[AuthorizationTransactionId] [nvarchar](4000) NULL,
	[AuthorizationTransactionCode] [nvarchar](4000) NULL,
	[AuthorizationTransactionResult] [nvarchar](4000) NULL,
	[CaptureTransactionId] [nvarchar](4000) NULL,
	[CaptureTransactionResult] [nvarchar](4000) NULL,
	[SubscriptionTransactionId] [nvarchar](4000) NULL,
	[PurchaseOrderNumber] [nvarchar](4000) NULL,
	[PaidDateUtc] [datetime2](7) NULL,
	[ShippingMethod] [nvarchar](4000) NULL,
	[ShippingRateComputationMethodSystemName] [nvarchar](4000) NULL,
	[Deleted] [bit] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[RewardPointsRemaining] [int] NULL,
	[HasNewPaymentNotification] [bit] NOT NULL,
	[AcceptThirdPartyEmailHandOver] [bit] NOT NULL,
	[OrderStatusId] [int] NOT NULL,
	[PaymentStatusId] [int] NOT NULL,
	[ShippingStatusId] [int] NOT NULL,
	[CustomerTaxDisplayTypeId] [int] NOT NULL,
 CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderItem]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderItemGuid] [uniqueidentifier] NOT NULL,
	[OrderId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[UnitPriceInclTax] [decimal](18, 4) NOT NULL,
	[UnitPriceExclTax] [decimal](18, 4) NOT NULL,
	[PriceInclTax] [decimal](18, 4) NOT NULL,
	[PriceExclTax] [decimal](18, 4) NOT NULL,
	[TaxRate] [decimal](18, 4) NOT NULL,
	[DiscountAmountInclTax] [decimal](18, 4) NOT NULL,
	[DiscountAmountExclTax] [decimal](18, 4) NOT NULL,
	[AttributeDescription] [nvarchar](max) NULL,
	[AttributesXml] [nvarchar](max) NULL,
	[DownloadCount] [int] NOT NULL,
	[IsDownloadActivated] [bit] NOT NULL,
	[LicenseDownloadId] [int] NULL,
	[ItemWeight] [decimal](18, 4) NULL,
	[BundleData] [nvarchar](max) NULL,
	[ProductCost] [decimal](18, 4) NOT NULL,
	[DeliveryTimeId] [int] NULL,
	[DisplayDeliveryTime] [bit] NOT NULL,
 CONSTRAINT [PK_OrderItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderNote]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderNote](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[Note] [nvarchar](max) NOT NULL,
	[DisplayToCustomer] [bit] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_OrderNote] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PaymentMethod]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PaymentMethod](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PaymentMethodSystemName] [nvarchar](4000) NOT NULL,
	[FullDescription] [nvarchar](4000) NULL,
	[RoundOrderTotalEnabled] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
 CONSTRAINT [PK_PaymentMethod] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PermissionRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PermissionRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SystemName] [nvarchar](255) NOT NULL,
 CONSTRAINT [PK_PermissionRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PermissionRoleMapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PermissionRoleMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Allow] [bit] NOT NULL,
	[PermissionRecordId] [int] NOT NULL,
	[CustomerRoleId] [int] NOT NULL,
 CONSTRAINT [PK_PermissionRoleMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductTypeId] [int] NOT NULL,
	[ParentGroupedProductId] [int] NOT NULL,
	[Visibility] [int] NOT NULL,
	[VisibleIndividually] [bit] NOT NULL,
	[Condition] [int] NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[ShortDescription] [nvarchar](4000) NULL,
	[FullDescription] [nvarchar](max) NULL,
	[AdminComment] [nvarchar](4000) NULL,
	[ProductTemplateId] [int] NOT NULL,
	[ShowOnHomePage] [bit] NOT NULL,
	[HomePageDisplayOrder] [int] NOT NULL,
	[MetaKeywords] [nvarchar](400) NULL,
	[MetaDescription] [nvarchar](4000) NULL,
	[MetaTitle] [nvarchar](400) NULL,
	[AllowCustomerReviews] [bit] NOT NULL,
	[ApprovedRatingSum] [int] NOT NULL,
	[NotApprovedRatingSum] [int] NOT NULL,
	[ApprovedTotalReviews] [int] NOT NULL,
	[NotApprovedTotalReviews] [int] NOT NULL,
	[SubjectToAcl] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
	[Sku] [nvarchar](400) NULL,
	[ManufacturerPartNumber] [nvarchar](400) NULL,
	[Gtin] [nvarchar](400) NULL,
	[IsGiftCard] [bit] NOT NULL,
	[GiftCardTypeId] [int] NOT NULL,
	[RequireOtherProducts] [bit] NOT NULL,
	[RequiredProductIds] [nvarchar](1000) NULL,
	[AutomaticallyAddRequiredProducts] [bit] NOT NULL,
	[IsDownload] [bit] NOT NULL,
	[DownloadId] [int] NOT NULL,
	[UnlimitedDownloads] [bit] NOT NULL,
	[MaxNumberOfDownloads] [int] NOT NULL,
	[DownloadExpirationDays] [int] NULL,
	[DownloadActivationTypeId] [int] NOT NULL,
	[HasSampleDownload] [bit] NOT NULL,
	[SampleDownloadId] [int] NULL,
	[HasUserAgreement] [bit] NOT NULL,
	[UserAgreementText] [nvarchar](4000) NULL,
	[IsRecurring] [bit] NOT NULL,
	[RecurringCycleLength] [int] NOT NULL,
	[RecurringCyclePeriodId] [int] NOT NULL,
	[RecurringTotalCycles] [int] NOT NULL,
	[IsShipEnabled] [bit] NOT NULL,
	[IsFreeShipping] [bit] NOT NULL,
	[AdditionalShippingCharge] [decimal](18, 4) NOT NULL,
	[IsTaxExempt] [bit] NOT NULL,
	[IsEsd] [bit] NOT NULL,
	[TaxCategoryId] [int] NOT NULL,
	[ManageInventoryMethodId] [int] NOT NULL,
	[StockQuantity] [int] NOT NULL,
	[DisplayStockAvailability] [bit] NOT NULL,
	[DisplayStockQuantity] [bit] NOT NULL,
	[MinStockQuantity] [int] NOT NULL,
	[LowStockActivityId] [int] NOT NULL,
	[NotifyAdminForQuantityBelow] [int] NOT NULL,
	[BackorderModeId] [int] NOT NULL,
	[AllowBackInStockSubscriptions] [bit] NOT NULL,
	[OrderMinimumQuantity] [int] NOT NULL,
	[OrderMaximumQuantity] [int] NOT NULL,
	[QuantityStep] [int] NOT NULL,
	[QuantiyControlType] [int] NOT NULL,
	[HideQuantityControl] [bit] NOT NULL,
	[AllowedQuantities] [nvarchar](1000) NULL,
	[DisableBuyButton] [bit] NOT NULL,
	[DisableWishlistButton] [bit] NOT NULL,
	[AvailableForPreOrder] [bit] NOT NULL,
	[CallForPrice] [bit] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[OldPrice] [decimal](18, 4) NOT NULL,
	[ProductCost] [decimal](18, 4) NOT NULL,
	[SpecialPrice] [decimal](18, 4) NULL,
	[SpecialPriceStartDateTimeUtc] [datetime2](7) NULL,
	[SpecialPriceEndDateTimeUtc] [datetime2](7) NULL,
	[CustomerEntersPrice] [bit] NOT NULL,
	[MinimumCustomerEnteredPrice] [decimal](18, 4) NOT NULL,
	[MaximumCustomerEnteredPrice] [decimal](18, 4) NOT NULL,
	[HasTierPrices] [bit] NOT NULL,
	[LowestAttributeCombinationPrice] [decimal](18, 4) NULL,
	[AttributeChoiceBehaviour] [int] NOT NULL,
	[Weight] [decimal](18, 4) NOT NULL,
	[Length] [decimal](18, 4) NOT NULL,
	[Width] [decimal](18, 4) NOT NULL,
	[Height] [decimal](18, 4) NOT NULL,
	[AvailableStartDateTimeUtc] [datetime2](7) NULL,
	[AvailableEndDateTimeUtc] [datetime2](7) NULL,
	[DisplayOrder] [int] NOT NULL,
	[Published] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[IsSystemProduct] [bit] NOT NULL,
	[SystemName] [nvarchar](400) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[DeliveryTimeId] [int] NULL,
	[QuantityUnitId] [int] NULL,
	[CustomsTariffNumber] [nvarchar](30) NULL,
	[CountryOfOriginId] [int] NULL,
	[BasePriceEnabled] [bit] NOT NULL,
	[BasePriceMeasureUnit] [nvarchar](50) NULL,
	[BasePriceAmount] [decimal](18, 4) NULL,
	[BasePriceBaseAmount] [int] NULL,
	[BundleTitleText] [nvarchar](400) NULL,
	[BundlePerItemShipping] [bit] NOT NULL,
	[BundlePerItemPricing] [bit] NOT NULL,
	[BundlePerItemShoppingCart] [bit] NOT NULL,
	[MainPictureId] [int] NULL,
	[HasPreviewPicture] [bit] NOT NULL,
	[HasDiscountsApplied] [bit] NOT NULL,
 CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_Category_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_Category_Mapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CategoryId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[IsFeaturedProduct] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsSystemMapping] [bit] NOT NULL,
 CONSTRAINT [PK_Product_Category_Mapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_Manufacturer_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_Manufacturer_Mapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ManufacturerId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[IsFeaturedProduct] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Product_Manufacturer_Mapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_MediaFile_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_MediaFile_Mapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[MediaFileId] [int] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Product_MediaFile_Mapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_ProductAttribute_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_ProductAttribute_Mapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[ProductAttributeId] [int] NOT NULL,
	[TextPrompt] [nvarchar](4000) NULL,
	[CustomData] [nvarchar](max) NULL,
	[IsRequired] [bit] NOT NULL,
	[AttributeControlTypeId] [int] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Product_ProductAttribute_Mapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_ProductTag_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_ProductTag_Mapping](
	[Product_Id] [int] NOT NULL,
	[ProductTag_Id] [int] NOT NULL,
 CONSTRAINT [PK_Product_ProductTag_Mapping] PRIMARY KEY CLUSTERED 
(
	[Product_Id] ASC,
	[ProductTag_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product_SpecificationAttribute_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product_SpecificationAttribute_Mapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SpecificationAttributeOptionId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[AllowFiltering] [bit] NULL,
	[ShowOnProductPage] [bit] NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Product_SpecificationAttribute_Mapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductAttribute]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](4000) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[Alias] [nvarchar](100) NULL,
	[AllowFiltering] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[FacetTemplateHint] [int] NOT NULL,
	[IndexOptionNames] [bit] NOT NULL,
	[ExportMappings] [nvarchar](max) NULL,
 CONSTRAINT [PK_ProductAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductAttributeOption]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductAttributeOption](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductAttributeOptionsSetId] [int] NOT NULL,
	[Name] [nvarchar](4000) NULL,
	[Alias] [nvarchar](100) NULL,
	[MediaFileId] [int] NOT NULL,
	[Color] [nvarchar](100) NULL,
	[PriceAdjustment] [decimal](18, 4) NOT NULL,
	[WeightAdjustment] [decimal](18, 4) NOT NULL,
	[IsPreSelected] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[ValueTypeId] [int] NOT NULL,
	[LinkedProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
 CONSTRAINT [PK_ProductAttributeOption] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductAttributeOptionsSet]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductAttributeOptionsSet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NULL,
	[ProductAttributeId] [int] NOT NULL,
 CONSTRAINT [PK_ProductAttributeOptionsSet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductBundleItem]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductBundleItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[BundleProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[Discount] [decimal](18, 4) NULL,
	[DiscountPercentage] [bit] NOT NULL,
	[Name] [nvarchar](400) NULL,
	[ShortDescription] [nvarchar](max) NULL,
	[FilterAttributes] [bit] NOT NULL,
	[HideThumbnail] [bit] NOT NULL,
	[Visible] [bit] NOT NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ProductBundleItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductBundleItemAttributeFilter]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductBundleItemAttributeFilter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BundleItemId] [int] NOT NULL,
	[AttributeId] [int] NOT NULL,
	[AttributeValueId] [int] NOT NULL,
	[IsPreSelected] [bit] NOT NULL,
 CONSTRAINT [PK_ProductBundleItemAttributeFilter] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductReview]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductReview](
	[Id] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[Title] [nvarchar](4000) NULL,
	[ReviewText] [nvarchar](max) NULL,
	[Rating] [int] NOT NULL,
	[HelpfulYesTotal] [int] NOT NULL,
	[HelpfulNoTotal] [int] NOT NULL,
 CONSTRAINT [PK_ProductReview] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductReviewHelpfulness]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductReviewHelpfulness](
	[Id] [int] NOT NULL,
	[ProductReviewId] [int] NOT NULL,
	[WasHelpful] [bit] NOT NULL,
 CONSTRAINT [PK_ProductReviewHelpfulness] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductTag]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductTag](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Published] [bit] NOT NULL,
 CONSTRAINT [PK_ProductTag] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductTemplate]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductTemplate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[ViewPath] [nvarchar](400) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_ProductTemplate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductVariantAttributeCombination]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductVariantAttributeCombination](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[Sku] [nvarchar](400) NULL,
	[Gtin] [nvarchar](400) NULL,
	[ManufacturerPartNumber] [nvarchar](400) NULL,
	[Price] [decimal](18, 4) NULL,
	[Length] [decimal](18, 4) NULL,
	[Width] [decimal](18, 4) NULL,
	[Height] [decimal](18, 4) NULL,
	[BasePriceAmount] [decimal](18, 4) NULL,
	[BasePriceBaseAmount] [int] NULL,
	[AssignedMediaFileIds] [nvarchar](1000) NULL,
	[IsActive] [bit] NOT NULL,
	[DeliveryTimeId] [int] NULL,
	[QuantityUnitId] [int] NULL,
	[AttributesXml] [nvarchar](max) NULL,
	[StockQuantity] [int] NOT NULL,
	[AllowOutOfStockOrders] [bit] NOT NULL,
 CONSTRAINT [PK_ProductVariantAttributeCombination] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductVariantAttributeValue]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductVariantAttributeValue](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductVariantAttributeId] [int] NOT NULL,
	[Name] [nvarchar](450) NULL,
	[Alias] [nvarchar](100) NULL,
	[MediaFileId] [int] NOT NULL,
	[Color] [nvarchar](100) NULL,
	[PriceAdjustment] [decimal](18, 4) NOT NULL,
	[WeightAdjustment] [decimal](18, 4) NOT NULL,
	[IsPreSelected] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[ValueTypeId] [int] NOT NULL,
	[LinkedProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
 CONSTRAINT [PK_ProductVariantAttributeValue] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuantityUnit]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuantityUnit](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[NamePlural] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](50) NULL,
	[DisplayLocale] [nvarchar](50) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsDefault] [bit] NOT NULL,
 CONSTRAINT [PK_QuantityUnit] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QueuedEmail]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QueuedEmail](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Priority] [int] NOT NULL,
	[From] [nvarchar](500) NOT NULL,
	[To] [nvarchar](500) NOT NULL,
	[ReplyTo] [nvarchar](500) NULL,
	[CC] [nvarchar](500) NULL,
	[Bcc] [nvarchar](500) NULL,
	[Subject] [nvarchar](1000) NULL,
	[Body] [nvarchar](max) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[SentTries] [int] NOT NULL,
	[SentOnUtc] [datetime2](7) NULL,
	[EmailAccountId] [int] NOT NULL,
	[SendManually] [bit] NOT NULL,
 CONSTRAINT [PK_QueuedEmail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QueuedEmailAttachment]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QueuedEmailAttachment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[QueuedEmailId] [int] NOT NULL,
	[StorageLocation] [int] NOT NULL,
	[Path] [nvarchar](1000) NULL,
	[MediaFileId] [int] NULL,
	[Name] [nvarchar](200) NOT NULL,
	[MimeType] [nvarchar](200) NOT NULL,
	[MediaStorageId] [int] NULL,
 CONSTRAINT [PK_QueuedEmailAttachment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RecurringPayment]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RecurringPayment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CycleLength] [int] NOT NULL,
	[CyclePeriodId] [int] NOT NULL,
	[TotalCycles] [int] NOT NULL,
	[StartDateUtc] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[InitialOrderId] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RecurringPayment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RecurringPaymentHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RecurringPaymentHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RecurringPaymentId] [int] NOT NULL,
	[OrderId] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_RecurringPaymentHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RelatedProduct]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RelatedProduct](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId1] [int] NOT NULL,
	[ProductId2] [int] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_RelatedProduct] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ReturnRequest]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReturnRequest](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StoreId] [int] NOT NULL,
	[OrderItemId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[ReasonForReturn] [nvarchar](4000) NOT NULL,
	[RequestedAction] [nvarchar](4000) NOT NULL,
	[RequestedActionUpdatedOnUtc] [datetime2](7) NULL,
	[CustomerComments] [nvarchar](4000) NULL,
	[StaffNotes] [nvarchar](4000) NULL,
	[AdminComment] [nvarchar](4000) NULL,
	[ReturnRequestStatusId] [int] NOT NULL,
	[RefundToWallet] [bit] NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ReturnRequest] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RewardPointsHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RewardPointsHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Points] [int] NOT NULL,
	[PointsBalance] [int] NOT NULL,
	[UsedAmount] [decimal](18, 4) NOT NULL,
	[Message] [nvarchar](4000) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UsedWithOrder_Id] [int] NULL,
 CONSTRAINT [PK_RewardPointsHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rule]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rule](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleSetId] [int] NOT NULL,
	[RuleType] [nvarchar](100) NOT NULL,
	[Operator] [nvarchar](20) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](400) NULL,
	[IsActive] [bit] NOT NULL,
	[Scope] [int] NOT NULL,
	[IsSubGroup] [bit] NOT NULL,
	[LogicalOperator] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
	[LastProcessedOnUtc] [datetime2](7) NULL,
 CONSTRAINT [PK_RuleSet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet_Category_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet_Category_Mapping](
	[Category_Id] [int] NOT NULL,
	[RuleSetEntity_Id] [int] NOT NULL,
 CONSTRAINT [PK_RuleSet_Category_Mapping] PRIMARY KEY CLUSTERED 
(
	[Category_Id] ASC,
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet_CustomerRole_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet_CustomerRole_Mapping](
	[CustomerRole_Id] [int] NOT NULL,
	[RuleSetEntity_Id] [int] NOT NULL,
 CONSTRAINT [PK_RuleSet_CustomerRole_Mapping] PRIMARY KEY CLUSTERED 
(
	[CustomerRole_Id] ASC,
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet_Discount_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet_Discount_Mapping](
	[Discount_Id] [int] NOT NULL,
	[RuleSetEntity_Id] [int] NOT NULL,
 CONSTRAINT [PK_RuleSet_Discount_Mapping] PRIMARY KEY CLUSTERED 
(
	[Discount_Id] ASC,
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet_PaymentMethod_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet_PaymentMethod_Mapping](
	[PaymentMethod_Id] [int] NOT NULL,
	[RuleSetEntity_Id] [int] NOT NULL,
 CONSTRAINT [PK_RuleSet_PaymentMethod_Mapping] PRIMARY KEY CLUSTERED 
(
	[PaymentMethod_Id] ASC,
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RuleSet_ShippingMethod_Mapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RuleSet_ShippingMethod_Mapping](
	[ShippingMethod_Id] [int] NOT NULL,
	[RuleSetEntity_Id] [int] NOT NULL,
 CONSTRAINT [PK_RuleSet_ShippingMethod_Mapping] PRIMARY KEY CLUSTERED 
(
	[ShippingMethod_Id] ASC,
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ScheduleTask]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ScheduleTask](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[Alias] [nvarchar](500) NULL,
	[CronExpression] [nvarchar](1000) NULL,
	[Type] [nvarchar](400) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Priority] [int] NOT NULL,
	[StopOnError] [bit] NOT NULL,
	[NextRunUtc] [datetime2](7) NULL,
	[IsHidden] [bit] NOT NULL,
	[RunPerMachine] [bit] NOT NULL,
 CONSTRAINT [PK_ScheduleTask] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ScheduleTaskHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ScheduleTaskHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScheduleTaskId] [int] NOT NULL,
	[IsRunning] [bit] NOT NULL,
	[MachineName] [nvarchar](400) NOT NULL,
	[StartedOnUtc] [datetime2](7) NOT NULL,
	[FinishedOnUtc] [datetime2](7) NULL,
	[SucceededOnUtc] [datetime2](7) NULL,
	[Error] [nvarchar](max) NULL,
	[ProgressPercent] [int] NULL,
	[ProgressMessage] [nvarchar](1000) NULL,
 CONSTRAINT [PK_ScheduleTaskHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Setting]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Setting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[StoreId] [int] NOT NULL,
 CONSTRAINT [PK_Setting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Shipment]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Shipment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[TrackingNumber] [nvarchar](max) NULL,
	[TrackingUrl] [nvarchar](2000) NULL,
	[TotalWeight] [decimal](18, 4) NULL,
	[ShippedDateUtc] [datetime2](7) NULL,
	[DeliveryDateUtc] [datetime2](7) NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Shipment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShipmentItem]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShipmentItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ShipmentId] [int] NOT NULL,
	[OrderItemId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
 CONSTRAINT [PK_ShipmentItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShippingMethod]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShippingMethod](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IgnoreCharges] [bit] NOT NULL,
	[LimitedToStores] [bit] NOT NULL,
 CONSTRAINT [PK_ShippingMethod] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ShoppingCartItem]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ShoppingCartItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StoreId] [int] NOT NULL,
	[ParentItemId] [int] NULL,
	[BundleItemId] [int] NULL,
	[CustomerId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[AttributesXml] [nvarchar](max) NULL,
	[CustomerEnteredPrice] [decimal](18, 4) NOT NULL,
	[Quantity] [int] NOT NULL,
	[ShoppingCartTypeId] [int] NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[UpdatedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_ShoppingCartItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SpecificationAttribute]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SpecificationAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](4000) NOT NULL,
	[Alias] [nvarchar](30) NULL,
	[DisplayOrder] [int] NOT NULL,
	[ShowOnProductPage] [bit] NOT NULL,
	[AllowFiltering] [bit] NOT NULL,
	[FacetSorting] [int] NOT NULL,
	[FacetTemplateHint] [int] NOT NULL,
	[IndexOptionNames] [bit] NOT NULL,
 CONSTRAINT [PK_SpecificationAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SpecificationAttributeOption]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SpecificationAttributeOption](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SpecificationAttributeId] [int] NOT NULL,
	[Name] [nvarchar](4000) NOT NULL,
	[Alias] [nvarchar](30) NULL,
	[DisplayOrder] [int] NOT NULL,
	[NumberValue] [decimal](18, 4) NOT NULL,
	[MediaFileId] [int] NOT NULL,
	[Color] [nvarchar](100) NULL,
 CONSTRAINT [PK_SpecificationAttributeOption] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StateProvince]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StateProvince](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CountryId] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Abbreviation] [nvarchar](100) NULL,
	[Published] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_StateProvince] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Store]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Store](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Url] [nvarchar](400) NOT NULL,
	[SslEnabled] [bit] NOT NULL,
	[SecureUrl] [nvarchar](400) NULL,
	[ForceSslForAllPages] [bit] NOT NULL,
	[Hosts] [nvarchar](1000) NULL,
	[LogoMediaFileId] [int] NOT NULL,
	[FavIconMediaFileId] [int] NULL,
	[PngIconMediaFileId] [int] NULL,
	[AppleTouchIconMediaFileId] [int] NULL,
	[MsTileImageMediaFileId] [int] NULL,
	[MsTileColor] [nvarchar](max) NULL,
	[DisplayOrder] [int] NOT NULL,
	[HtmlBodyId] [nvarchar](max) NULL,
	[ContentDeliveryNetwork] [nvarchar](400) NULL,
	[PrimaryStoreCurrencyId] [int] NOT NULL,
	[PrimaryExchangeRateCurrencyId] [int] NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StoreMapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StoreMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[EntityName] [nvarchar](400) NOT NULL,
	[StoreId] [int] NOT NULL,
 CONSTRAINT [PK_StoreMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SyncMapping]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SyncMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[SourceKey] [nvarchar](150) NOT NULL,
	[EntityName] [nvarchar](100) NOT NULL,
	[ContextName] [nvarchar](100) NOT NULL,
	[SourceHash] [nvarchar](40) NULL,
	[CustomInt] [int] NULL,
	[CustomString] [nvarchar](max) NULL,
	[CustomBool] [bit] NULL,
	[SyncedOnUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_SyncMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaxCategory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaxCategory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_TaxCategory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ThemeVariable]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ThemeVariable](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Theme] [nvarchar](400) NULL,
	[Name] [nvarchar](400) NULL,
	[Value] [nvarchar](2000) NULL,
	[StoreId] [int] NOT NULL,
 CONSTRAINT [PK_ThemeVariable] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TierPrice]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TierPrice](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[StoreId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[CalculationMethod] [int] NOT NULL,
	[CustomerRoleId] [int] NULL,
 CONSTRAINT [PK_TierPrice] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Topic]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Topic](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SystemName] [nvarchar](max) NULL,
	[IsSystemTopic] [bit] NOT NULL,
	[HtmlId] [nvarchar](max) NULL,
	[BodyCssClass] [nvarchar](max) NULL,
	[IncludeInSitemap] [bit] NOT NULL,
	[IsPasswordProtected] [bit] NOT NULL,
	[Password] [nvarchar](max) NULL,
	[Title] [nvarchar](max) NULL,
	[ShortTitle] [nvarchar](50) NULL,
	[Intro] [nvarchar](255) NULL,
	[Body] [nvarchar](max) NULL,
	[MetaKeywords] [nvarchar](max) NULL,
	[MetaDescription] [nvarchar](max) NULL,
	[MetaTitle] [nvarchar](max) NULL,
	[LimitedToStores] [bit] NOT NULL,
	[RenderAsWidget] [bit] NOT NULL,
	[WidgetZone] [nvarchar](max) NULL,
	[WidgetWrapContent] [bit] NULL,
	[WidgetShowTitle] [bit] NOT NULL,
	[WidgetBordered] [bit] NOT NULL,
	[Priority] [int] NOT NULL,
	[TitleTag] [nvarchar](max) NULL,
	[SubjectToAcl] [bit] NOT NULL,
	[IsPublished] [bit] NOT NULL,
	[CookieType] [int] NULL,
 CONSTRAINT [PK_Topic] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UrlRecord]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UrlRecord](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityId] [int] NOT NULL,
	[EntityName] [nvarchar](400) NOT NULL,
	[Slug] [nvarchar](400) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[LanguageId] [int] NOT NULL,
 CONSTRAINT [PK_UrlRecord] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WalletHistory]    Script Date: 18.08.2021 13:38:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WalletHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StoreId] [int] NOT NULL,
	[CustomerId] [int] NOT NULL,
	[OrderId] [int] NULL,
	[Amount] [decimal](18, 4) NOT NULL,
	[AmountBalance] [decimal](18, 4) NOT NULL,
	[AmountBalancePerStore] [decimal](18, 4) NOT NULL,
	[CreatedOnUtc] [datetime2](7) NOT NULL,
	[Reason] [int] NULL,
	[Message] [nvarchar](1000) NULL,
	[AdminComment] [nvarchar](4000) NULL,
 CONSTRAINT [PK_WalletHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_AclRecord_CustomerRoleId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AclRecord_CustomerRoleId] ON [dbo].[AclRecord]
(
	[CustomerRoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AclRecord_EntityId_EntityName]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AclRecord_EntityId_EntityName] ON [dbo].[AclRecord]
(
	[EntityId] ASC,
	[EntityName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AclRecord_IsIdle]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AclRecord_IsIdle] ON [dbo].[AclRecord]
(
	[IsIdle] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ActivityLog_ActivityLogTypeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ActivityLog_ActivityLogTypeId] ON [dbo].[ActivityLog]
(
	[ActivityLogTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ActivityLog_CreatedOnUtc]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ActivityLog_CreatedOnUtc] ON [dbo].[ActivityLog]
(
	[CreatedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ActivityLog_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_ActivityLog_CustomerId] ON [dbo].[ActivityLog]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Address_CountryId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Address_CountryId] ON [dbo].[Address]
(
	[CountryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Address_StateProvinceId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Address_StateProvinceId] ON [dbo].[Address]
(
	[StateProvinceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Affiliate_AddressId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Affiliate_AddressId] ON [dbo].[Affiliate]
(
	[AddressId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_BackInStockSubscription_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_BackInStockSubscription_CustomerId] ON [dbo].[BackInStockSubscription]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_BackInStockSubscription_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_BackInStockSubscription_ProductId] ON [dbo].[BackInStockSubscription]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Category_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Category_DisplayOrder] ON [dbo].[Category]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Category_LimitedToStores]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Category_LimitedToStores] ON [dbo].[Category]
(
	[LimitedToStores] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Category_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Category_MediaFileId] ON [dbo].[Category]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Category_ParentCategoryId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Category_ParentCategoryId] ON [dbo].[Category]
(
	[ParentCategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Category_SubjectToAcl]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Category_SubjectToAcl] ON [dbo].[Category]
(
	[SubjectToAcl] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deleted1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Deleted1] ON [dbo].[Category]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CheckoutAttributeValue_CheckoutAttributeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CheckoutAttributeValue_CheckoutAttributeId] ON [dbo].[CheckoutAttributeValue]
(
	[CheckoutAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CheckoutAttributeValue_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CheckoutAttributeValue_MediaFileId] ON [dbo].[CheckoutAttributeValue]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Country_DefaultCurrencyId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Country_DefaultCurrencyId] ON [dbo].[Country]
(
	[DefaultCurrencyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Country_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Country_DisplayOrder] ON [dbo].[Country]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Currency_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Currency_DisplayOrder] ON [dbo].[Currency]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_BillingAddress_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Customer_BillingAddress_Id] ON [dbo].[Customer]
(
	[BillingAddress_Id] ASC
)
WHERE ([BillingAddress_Id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_BirthDate]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_BirthDate] ON [dbo].[Customer]
(
	[BirthDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_Company]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_Company] ON [dbo].[Customer]
(
	[Company] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_CreatedOn]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_CreatedOn] ON [dbo].[Customer]
(
	[CreatedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_CustomerGuid]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_CustomerGuid] ON [dbo].[Customer]
(
	[CustomerGuid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_CustomerNumber]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_CustomerNumber] ON [dbo].[Customer]
(
	[CustomerNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_Deleted_IsSystemAccount]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_Deleted_IsSystemAccount] ON [dbo].[Customer]
(
	[Deleted] ASC,
	[IsSystemAccount] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_Email]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_Email] ON [dbo].[Customer]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_FullName]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_FullName] ON [dbo].[Customer]
(
	[FullName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_LastActivity]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_LastActivity] ON [dbo].[Customer]
(
	[LastActivityDateUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_LastIpAddress]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_LastIpAddress] ON [dbo].[Customer]
(
	[LastIpAddress] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Customer_ShippingAddress_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Customer_ShippingAddress_Id] ON [dbo].[Customer]
(
	[ShippingAddress_Id] ASC
)
WHERE ([ShippingAddress_Id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Customer_Username]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Customer_Username] ON [dbo].[Customer]
(
	[Username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deleted4]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Deleted4] ON [dbo].[Customer]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSystemAccount]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSystemAccount] ON [dbo].[Customer]
(
	[IsSystemAccount] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SystemName]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SystemName] ON [dbo].[Customer]
(
	[SystemName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CustomerAddresses_Address_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CustomerAddresses_Address_Id] ON [dbo].[CustomerAddresses]
(
	[Address_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CustomerContent_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CustomerContent_CustomerId] ON [dbo].[CustomerContent]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Active]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Active] ON [dbo].[CustomerRole]
(
	[Active] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_CustomerRole_SystemName_IsSystemRole]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CustomerRole_SystemName_IsSystemRole] ON [dbo].[CustomerRole]
(
	[SystemName] ASC,
	[IsSystemRole] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSystemRole]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSystemRole] ON [dbo].[CustomerRole]
(
	[IsSystemRole] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SystemName1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SystemName1] ON [dbo].[CustomerRole]
(
	[SystemName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CustomerRoleMapping_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CustomerRoleMapping_CustomerId] ON [dbo].[CustomerRoleMapping]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CustomerRoleMapping_CustomerRoleId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_CustomerRoleMapping_CustomerRoleId] ON [dbo].[CustomerRoleMapping]
(
	[CustomerRoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSystemMapping1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSystemMapping1] ON [dbo].[CustomerRoleMapping]
(
	[IsSystemMapping] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Discount_AppliedToCategories_Category_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Discount_AppliedToCategories_Category_Id] ON [dbo].[Discount_AppliedToCategories]
(
	[Category_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Discount_AppliedToManufacturers_Manufacturer_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Discount_AppliedToManufacturers_Manufacturer_Id] ON [dbo].[Discount_AppliedToManufacturers]
(
	[Manufacturer_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Discount_AppliedToProducts_Product_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Discount_AppliedToProducts_Product_Id] ON [dbo].[Discount_AppliedToProducts]
(
	[Product_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DiscountUsageHistory_DiscountId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_DiscountUsageHistory_DiscountId] ON [dbo].[DiscountUsageHistory]
(
	[DiscountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DiscountUsageHistory_OrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_DiscountUsageHistory_OrderId] ON [dbo].[DiscountUsageHistory]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Download_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Download_MediaFileId] ON [dbo].[Download]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DownloadGuid]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_DownloadGuid] ON [dbo].[Download]
(
	[DownloadGuid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_EntityId_EntityName]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_EntityId_EntityName] ON [dbo].[Download]
(
	[EntityId] ASC,
	[EntityName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_UpdatedOn_IsTransient]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_UpdatedOn_IsTransient] ON [dbo].[Download]
(
	[UpdatedOnUtc] ASC,
	[IsTransient] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ExportDeployment_ProfileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ExportDeployment_ProfileId] ON [dbo].[ExportDeployment]
(
	[ProfileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ExportProfile_SchedulingTaskId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ExportProfile_SchedulingTaskId] ON [dbo].[ExportProfile]
(
	[SchedulingTaskId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ExternalAuthenticationRecord_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ExternalAuthenticationRecord_CustomerId] ON [dbo].[ExternalAuthenticationRecord]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_GenericAttribute_EntityId_and_KeyGroup]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_GenericAttribute_EntityId_and_KeyGroup] ON [dbo].[GenericAttribute]
(
	[EntityId] ASC,
	[KeyGroup] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_GenericAttribute_Key]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_GenericAttribute_Key] ON [dbo].[GenericAttribute]
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_GiftCard_PurchasedWithOrderItemId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_GiftCard_PurchasedWithOrderItemId] ON [dbo].[GiftCard]
(
	[PurchasedWithOrderItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_GiftCardUsageHistory_GiftCardId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_GiftCardUsageHistory_GiftCardId] ON [dbo].[GiftCardUsageHistory]
(
	[GiftCardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_GiftCardUsageHistory_UsedWithOrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_GiftCardUsageHistory_UsedWithOrderId] ON [dbo].[GiftCardUsageHistory]
(
	[UsedWithOrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ImportProfile_SchedulingTaskId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ImportProfile_SchedulingTaskId] ON [dbo].[ImportProfile]
(
	[SchedulingTaskId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Language_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Language_DisplayOrder] ON [dbo].[Language]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_LocaleStringResource]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocaleStringResource] ON [dbo].[LocaleStringResource]
(
	[ResourceName] ASC,
	[LanguageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LocaleStringResource_LanguageId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocaleStringResource_LanguageId] ON [dbo].[LocaleStringResource]
(
	[LanguageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_LocalizedProperty_Compound]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocalizedProperty_Compound] ON [dbo].[LocalizedProperty]
(
	[EntityId] ASC,
	[LocaleKey] ASC,
	[LocaleKeyGroup] ASC,
	[LanguageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LocalizedProperty_Key]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocalizedProperty_Key] ON [dbo].[LocalizedProperty]
(
	[Id] ASC
)
INCLUDE([EntityId],[LocaleKeyGroup],[LocaleKey]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LocalizedProperty_LanguageId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocalizedProperty_LanguageId] ON [dbo].[LocalizedProperty]
(
	[LanguageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_LocalizedProperty_LocaleKeyGroup]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_LocalizedProperty_LocaleKeyGroup] ON [dbo].[LocalizedProperty]
(
	[LocaleKeyGroup] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Log_CreatedOnUtc]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Log_CreatedOnUtc] ON [dbo].[Log]
(
	[CreatedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Log_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Log_CustomerId] ON [dbo].[Log]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Log_Level]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Log_Level] ON [dbo].[Log]
(
	[LogLevelId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Log_Logger]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Log_Logger] ON [dbo].[Log]
(
	[Logger] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deleted]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Deleted] ON [dbo].[Manufacturer]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Manufacturer_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Manufacturer_DisplayOrder] ON [dbo].[Manufacturer]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Manufacturer_LimitedToStores]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Manufacturer_LimitedToStores] ON [dbo].[Manufacturer]
(
	[LimitedToStores] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Manufacturer_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Manufacturer_MediaFileId] ON [dbo].[Manufacturer]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SubjectToAcl]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SubjectToAcl] ON [dbo].[Manufacturer]
(
	[SubjectToAcl] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Media_Extension]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_Extension] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[Extension] ASC,
	[PixelSize] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Media_FolderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_FolderId] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Media_MediaType]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_MediaType] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[MediaType] ASC,
	[Extension] ASC,
	[PixelSize] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Media_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_Name] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[Name] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Media_PixelSize]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_PixelSize] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[PixelSize] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Media_Size]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_Size] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[Size] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Media_UpdatedOnUtc]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Media_UpdatedOnUtc] ON [dbo].[MediaFile]
(
	[FolderId] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MediaFile_MediaStorageId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaFile_MediaStorageId] ON [dbo].[MediaFile]
(
	[MediaStorageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MediaFile_Tag_Mapping_MediaTag_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaFile_Tag_Mapping_MediaTag_Id] ON [dbo].[MediaFile_Tag_Mapping]
(
	[MediaTag_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_NameParentId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_NameParentId] ON [dbo].[MediaFolder]
(
	[ParentId] ASC,
	[Name] ASC
)
WHERE ([ParentId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_MediaTag_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaTag_Name] ON [dbo].[MediaTag]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Album]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Album] ON [dbo].[MediaTrack]
(
	[Album] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_MediaTrack_Composite]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaTrack_Composite] ON [dbo].[MediaTrack]
(
	[MediaFileId] ASC,
	[EntityId] ASC,
	[EntityName] ASC,
	[Property] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItem_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItem_DisplayOrder] ON [dbo].[MenuItemRecord]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItem_LimitedToStores]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItem_LimitedToStores] ON [dbo].[MenuItemRecord]
(
	[LimitedToStores] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItem_ParentItemId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItem_ParentItemId] ON [dbo].[MenuItemRecord]
(
	[ParentItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItem_Published]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItem_Published] ON [dbo].[MenuItemRecord]
(
	[Published] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItem_SubjectToAcl]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItem_SubjectToAcl] ON [dbo].[MenuItemRecord]
(
	[SubjectToAcl] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MenuItemRecord_MenuId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MenuItemRecord_MenuId] ON [dbo].[MenuItemRecord]
(
	[MenuId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Menu_LimitedToStores]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Menu_LimitedToStores] ON [dbo].[MenuRecord]
(
	[LimitedToStores] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Menu_Published]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Menu_Published] ON [dbo].[MenuRecord]
(
	[Published] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Menu_SubjectToAcl]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Menu_SubjectToAcl] ON [dbo].[MenuRecord]
(
	[SubjectToAcl] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Menu_SystemName_IsSystemMenu]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Menu_SystemName_IsSystemMenu] ON [dbo].[MenuRecord]
(
	[SystemName] ASC,
	[IsSystemMenu] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Active1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Active1] ON [dbo].[NewsLetterSubscription]
(
	[Active] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_NewsletterSubscription_Email_StoreId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_NewsletterSubscription_Email_StoreId] ON [dbo].[NewsLetterSubscription]
(
	[Email] ASC,
	[StoreId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deleted3]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Deleted3] ON [dbo].[Order]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Order_BillingAddressId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Order_BillingAddressId] ON [dbo].[Order]
(
	[BillingAddressId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Order_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Order_CustomerId] ON [dbo].[Order]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Order_ShippingAddressId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Order_ShippingAddressId] ON [dbo].[Order]
(
	[ShippingAddressId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderItem_OrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_OrderItem_OrderId] ON [dbo].[OrderItem]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderItem_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_OrderItem_ProductId] ON [dbo].[OrderItem]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderNote_OrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_OrderNote_OrderId] ON [dbo].[OrderNote]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SystemName2]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SystemName2] ON [dbo].[PermissionRecord]
(
	[SystemName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PermissionRoleMapping_CustomerRoleId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PermissionRoleMapping_CustomerRoleId] ON [dbo].[PermissionRoleMapping]
(
	[CustomerRoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PermissionRoleMapping_PermissionRecordId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PermissionRoleMapping_PermissionRecordId] ON [dbo].[PermissionRoleMapping]
(
	[PermissionRecordId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Deleted2]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Deleted2] ON [dbo].[Product]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Gtin1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Gtin1] ON [dbo].[Product]
(
	[Gtin] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSystemProduct]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSystemProduct] ON [dbo].[Product]
(
	[IsSystemProduct] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ManufacturerPartNumber1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ManufacturerPartNumber1] ON [dbo].[Product]
(
	[ManufacturerPartNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_CountryOfOriginId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_CountryOfOriginId] ON [dbo].[Product]
(
	[CountryOfOriginId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_DeliveryTimeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_DeliveryTimeId] ON [dbo].[Product]
(
	[DeliveryTimeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_LimitedToStores]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_LimitedToStores] ON [dbo].[Product]
(
	[LimitedToStores] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Product_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Name] ON [dbo].[Product]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_ParentGroupedProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_ParentGroupedProductId] ON [dbo].[Product]
(
	[ParentGroupedProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_PriceDatesEtc]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_PriceDatesEtc] ON [dbo].[Product]
(
	[Price] ASC,
	[AvailableStartDateTimeUtc] ASC,
	[AvailableEndDateTimeUtc] ASC,
	[Published] ASC,
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_Published]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Published] ON [dbo].[Product]
(
	[Published] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_Published_Deleted_IsSystemProduct]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Published_Deleted_IsSystemProduct] ON [dbo].[Product]
(
	[Published] ASC,
	[Deleted] ASC,
	[IsSystemProduct] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_QuantityUnitId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_QuantityUnitId] ON [dbo].[Product]
(
	[QuantityUnitId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_SampleDownloadId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_SampleDownloadId] ON [dbo].[Product]
(
	[SampleDownloadId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_ShowOnHomepage]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_ShowOnHomepage] ON [dbo].[Product]
(
	[ShowOnHomePage] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Product_Sku]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Sku] ON [dbo].[Product]
(
	[Sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_SubjectToAcl]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_SubjectToAcl] ON [dbo].[Product]
(
	[SubjectToAcl] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Product_SystemName_IsSystemProduct]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_SystemName_IsSystemProduct] ON [dbo].[Product]
(
	[SystemName] ASC,
	[IsSystemProduct] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SeekExport1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SeekExport1] ON [dbo].[Product]
(
	[Published] ASC,
	[Id] ASC,
	[Visibility] ASC,
	[Deleted] ASC,
	[IsSystemProduct] ASC,
	[AvailableStartDateTimeUtc] ASC,
	[AvailableEndDateTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Visibility]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Visibility] ON [dbo].[Product]
(
	[Visibility] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsFeaturedProduct1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsFeaturedProduct1] ON [dbo].[Product_Category_Mapping]
(
	[IsFeaturedProduct] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSystemMapping]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSystemMapping] ON [dbo].[Product_Category_Mapping]
(
	[IsSystemMapping] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PCM_Product_and_Category]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PCM_Product_and_Category] ON [dbo].[Product_Category_Mapping]
(
	[CategoryId] ASC,
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_Category_Mapping_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Category_Mapping_ProductId] ON [dbo].[Product_Category_Mapping]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsFeaturedProduct]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsFeaturedProduct] ON [dbo].[Product_Manufacturer_Mapping]
(
	[IsFeaturedProduct] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PMM_Product_and_Manufacturer]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PMM_Product_and_Manufacturer] ON [dbo].[Product_Manufacturer_Mapping]
(
	[ManufacturerId] ASC,
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_Manufacturer_Mapping_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_Manufacturer_Mapping_ProductId] ON [dbo].[Product_Manufacturer_Mapping]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_MediaFile_Mapping_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_MediaFile_Mapping_MediaFileId] ON [dbo].[Product_MediaFile_Mapping]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_MediaFile_Mapping_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_MediaFile_Mapping_ProductId] ON [dbo].[Product_MediaFile_Mapping]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AttributeControlTypeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AttributeControlTypeId] ON [dbo].[Product_ProductAttribute_Mapping]
(
	[AttributeControlTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_ProductAttribute_Mapping_ProductAttributeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_ProductAttribute_Mapping_ProductAttributeId] ON [dbo].[Product_ProductAttribute_Mapping]
(
	[ProductAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder] ON [dbo].[Product_ProductAttribute_Mapping]
(
	[ProductId] ASC,
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_ProductTag_Mapping_ProductTag_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_ProductTag_Mapping_ProductTag_Id] ON [dbo].[Product_ProductTag_Mapping]
(
	[ProductTag_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Product_SpecificationAttribute_Mapping_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Product_SpecificationAttribute_Mapping_ProductId] ON [dbo].[Product_SpecificationAttribute_Mapping]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PSAM_AllowFiltering]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PSAM_AllowFiltering] ON [dbo].[Product_SpecificationAttribute_Mapping]
(
	[AllowFiltering] ASC
)
INCLUDE([ProductId],[SpecificationAttributeOptionId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PSAM_SpecificationAttributeOptionId_AllowFiltering]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PSAM_SpecificationAttributeOptionId_AllowFiltering] ON [dbo].[Product_SpecificationAttribute_Mapping]
(
	[SpecificationAttributeOptionId] ASC,
	[AllowFiltering] ASC
)
INCLUDE([ProductId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AllowFiltering]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AllowFiltering] ON [dbo].[ProductAttribute]
(
	[AllowFiltering] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_DisplayOrder] ON [dbo].[ProductAttribute]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductAttributeOption_ProductAttributeOptionsSetId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductAttributeOption_ProductAttributeOptionsSetId] ON [dbo].[ProductAttributeOption]
(
	[ProductAttributeOptionsSetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductAttributeOptionsSet_ProductAttributeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductAttributeOptionsSet_ProductAttributeId] ON [dbo].[ProductAttributeOptionsSet]
(
	[ProductAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductBundleItem_BundleProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductBundleItem_BundleProductId] ON [dbo].[ProductBundleItem]
(
	[BundleProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductBundleItem_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductBundleItem_ProductId] ON [dbo].[ProductBundleItem]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductBundleItemAttributeFilter_BundleItemId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductBundleItemAttributeFilter_BundleItemId] ON [dbo].[ProductBundleItemAttributeFilter]
(
	[BundleItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductReview_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductReview_ProductId] ON [dbo].[ProductReview]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductReviewHelpfulness_ProductReviewId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductReviewHelpfulness_ProductReviewId] ON [dbo].[ProductReviewHelpfulness]
(
	[ProductReviewId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ProductTag_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductTag_Name] ON [dbo].[ProductTag]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductTag_Published]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductTag_Published] ON [dbo].[ProductTag]
(
	[Published] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Gtin]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Gtin] ON [dbo].[ProductVariantAttributeCombination]
(
	[Gtin] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsActive]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsActive] ON [dbo].[ProductVariantAttributeCombination]
(
	[IsActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ManufacturerPartNumber]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ManufacturerPartNumber] ON [dbo].[ProductVariantAttributeCombination]
(
	[ManufacturerPartNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductVariantAttributeCombination_DeliveryTimeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductVariantAttributeCombination_DeliveryTimeId] ON [dbo].[ProductVariantAttributeCombination]
(
	[DeliveryTimeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductVariantAttributeCombination_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductVariantAttributeCombination_ProductId] ON [dbo].[ProductVariantAttributeCombination]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductVariantAttributeCombination_QuantityUnitId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductVariantAttributeCombination_QuantityUnitId] ON [dbo].[ProductVariantAttributeCombination]
(
	[QuantityUnitId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_ProductVariantAttributeCombination_SKU]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductVariantAttributeCombination_SKU] ON [dbo].[ProductVariantAttributeCombination]
(
	[Sku] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StockQuantity_AllowOutOfStockOrders]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_StockQuantity_AllowOutOfStockOrders] ON [dbo].[ProductVariantAttributeCombination]
(
	[StockQuantity] ASC,
	[AllowOutOfStockOrders] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Name] ON [dbo].[ProductVariantAttributeValue]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ProductVariantAttributeValue_ProductVariantAttributeId_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ProductVariantAttributeValue_ProductVariantAttributeId_DisplayOrder] ON [dbo].[ProductVariantAttributeValue]
(
	[ProductVariantAttributeId] ASC,
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ValueTypeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ValueTypeId] ON [dbo].[ProductVariantAttributeValue]
(
	[ValueTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [[IX_QueuedEmail_CreatedOnUtc]]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [[IX_QueuedEmail_CreatedOnUtc]]] ON [dbo].[QueuedEmail]
(
	[CreatedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_EmailAccountId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_EmailAccountId] ON [dbo].[QueuedEmail]
(
	[EmailAccountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MediaFileId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaFileId] ON [dbo].[QueuedEmailAttachment]
(
	[MediaFileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_MediaStorageId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MediaStorageId] ON [dbo].[QueuedEmailAttachment]
(
	[MediaStorageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QueuedEmailId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_QueuedEmailId] ON [dbo].[QueuedEmailAttachment]
(
	[QueuedEmailId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RecurringPayment_InitialOrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RecurringPayment_InitialOrderId] ON [dbo].[RecurringPayment]
(
	[InitialOrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RecurringPaymentHistory_RecurringPaymentId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RecurringPaymentHistory_RecurringPaymentId] ON [dbo].[RecurringPaymentHistory]
(
	[RecurringPaymentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RelatedProduct_ProductId1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RelatedProduct_ProductId1] ON [dbo].[RelatedProduct]
(
	[ProductId1] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ReturnRequest_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ReturnRequest_CustomerId] ON [dbo].[ReturnRequest]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RewardPointsHistory_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RewardPointsHistory_CustomerId] ON [dbo].[RewardPointsHistory]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RewardPointsHistory_UsedWithOrder_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_RewardPointsHistory_UsedWithOrder_Id] ON [dbo].[RewardPointsHistory]
(
	[UsedWithOrder_Id] ASC
)
WHERE ([UsedWithOrder_Id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PageBuilder_DisplayOrder]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PageBuilder_DisplayOrder] ON [dbo].[Rule]
(
	[DisplayOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_PageBuilder_RuleType]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_PageBuilder_RuleType] ON [dbo].[Rule]
(
	[RuleType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Rule_RuleSetId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Rule_RuleSetId] ON [dbo].[Rule]
(
	[RuleSetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_IsSubGroup]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_IsSubGroup] ON [dbo].[RuleSet]
(
	[IsSubGroup] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSetEntity_Scope]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSetEntity_Scope] ON [dbo].[RuleSet]
(
	[IsActive] ASC,
	[Scope] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSet_Category_Mapping_RuleSetEntity_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSet_Category_Mapping_RuleSetEntity_Id] ON [dbo].[RuleSet_Category_Mapping]
(
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSet_CustomerRole_Mapping_RuleSetEntity_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSet_CustomerRole_Mapping_RuleSetEntity_Id] ON [dbo].[RuleSet_CustomerRole_Mapping]
(
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSet_Discount_Mapping_RuleSetEntity_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSet_Discount_Mapping_RuleSetEntity_Id] ON [dbo].[RuleSet_Discount_Mapping]
(
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSet_PaymentMethod_Mapping_RuleSetEntity_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSet_PaymentMethod_Mapping_RuleSetEntity_Id] ON [dbo].[RuleSet_PaymentMethod_Mapping]
(
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RuleSet_ShippingMethod_Mapping_RuleSetEntity_Id]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_RuleSet_ShippingMethod_Mapping_RuleSetEntity_Id] ON [dbo].[RuleSet_ShippingMethod_Mapping]
(
	[RuleSetEntity_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_NextRun_Enabled]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_NextRun_Enabled] ON [dbo].[ScheduleTask]
(
	[NextRunUtc] ASC,
	[Enabled] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Type]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Type] ON [dbo].[ScheduleTask]
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_MachineName_IsRunning]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_MachineName_IsRunning] ON [dbo].[ScheduleTaskHistory]
(
	[MachineName] ASC,
	[IsRunning] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ScheduleTaskHistory_ScheduleTaskId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ScheduleTaskHistory_ScheduleTaskId] ON [dbo].[ScheduleTaskHistory]
(
	[ScheduleTaskId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Started_Finished]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Started_Finished] ON [dbo].[ScheduleTaskHistory]
(
	[StartedOnUtc] ASC,
	[FinishedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Setting_Name]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Setting_Name] ON [dbo].[Setting]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Setting_StoreId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Setting_StoreId] ON [dbo].[Setting]
(
	[StoreId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Shipment_OrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Shipment_OrderId] ON [dbo].[Shipment]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShipmentItem_ShipmentId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ShipmentItem_ShipmentId] ON [dbo].[ShipmentItem]
(
	[ShipmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShoppingCartItem_BundleItemId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ShoppingCartItem_BundleItemId] ON [dbo].[ShoppingCartItem]
(
	[BundleItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShoppingCartItem_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ShoppingCartItem_CustomerId] ON [dbo].[ShoppingCartItem]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShoppingCartItem_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ShoppingCartItem_ProductId] ON [dbo].[ShoppingCartItem]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId] ON [dbo].[ShoppingCartItem]
(
	[ShoppingCartTypeId] ASC,
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AllowFiltering1]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_AllowFiltering1] ON [dbo].[SpecificationAttribute]
(
	[AllowFiltering] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SpecificationAttributeOption_SpecificationAttributeId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_SpecificationAttributeOption_SpecificationAttributeId] ON [dbo].[SpecificationAttributeOption]
(
	[SpecificationAttributeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StateProvince_CountryId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_StateProvince_CountryId] ON [dbo].[StateProvince]
(
	[CountryId] ASC
)
INCLUDE([DisplayOrder]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Store_PrimaryExchangeRateCurrencyId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Store_PrimaryExchangeRateCurrencyId] ON [dbo].[Store]
(
	[PrimaryExchangeRateCurrencyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Store_PrimaryStoreCurrencyId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_Store_PrimaryStoreCurrencyId] ON [dbo].[Store]
(
	[PrimaryStoreCurrencyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_StoreMapping_EntityId_EntityName]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_StoreMapping_EntityId_EntityName] ON [dbo].[StoreMapping]
(
	[EntityId] ASC,
	[EntityName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SyncMapping_ByEntity]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_SyncMapping_ByEntity] ON [dbo].[SyncMapping]
(
	[EntityId] ASC,
	[EntityName] ASC,
	[ContextName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SyncMapping_BySource]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_SyncMapping_BySource] ON [dbo].[SyncMapping]
(
	[SourceKey] ASC,
	[EntityName] ASC,
	[ContextName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TierPrice_CustomerRoleId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_TierPrice_CustomerRoleId] ON [dbo].[TierPrice]
(
	[CustomerRoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TierPrice_ProductId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_TierPrice_ProductId] ON [dbo].[TierPrice]
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_UrlRecord_Slug]    Script Date: 18.08.2021 13:38:56 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_UrlRecord_Slug] ON [dbo].[UrlRecord]
(
	[Slug] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_StoreId_CreatedOn]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_StoreId_CreatedOn] ON [dbo].[WalletHistory]
(
	[StoreId] ASC,
	[CreatedOnUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_WalletHistory_CustomerId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_WalletHistory_CustomerId] ON [dbo].[WalletHistory]
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_WalletHistory_OrderId]    Script Date: 18.08.2021 13:38:56 ******/
CREATE NONCLUSTERED INDEX [IX_WalletHistory_OrderId] ON [dbo].[WalletHistory]
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AclRecord]  WITH CHECK ADD  CONSTRAINT [FK_AclRecord_CustomerRole_CustomerRoleId] FOREIGN KEY([CustomerRoleId])
REFERENCES [dbo].[CustomerRole] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AclRecord] CHECK CONSTRAINT [FK_AclRecord_CustomerRole_CustomerRoleId]
GO
ALTER TABLE [dbo].[ActivityLog]  WITH CHECK ADD  CONSTRAINT [FK_ActivityLog_ActivityLogType_ActivityLogTypeId] FOREIGN KEY([ActivityLogTypeId])
REFERENCES [dbo].[ActivityLogType] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ActivityLog] CHECK CONSTRAINT [FK_ActivityLog_ActivityLogType_ActivityLogTypeId]
GO
ALTER TABLE [dbo].[ActivityLog]  WITH CHECK ADD  CONSTRAINT [FK_ActivityLog_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ActivityLog] CHECK CONSTRAINT [FK_ActivityLog_Customer_CustomerId]
GO
ALTER TABLE [dbo].[Address]  WITH CHECK ADD  CONSTRAINT [FK_Address_Country_CountryId] FOREIGN KEY([CountryId])
REFERENCES [dbo].[Country] ([Id])
GO
ALTER TABLE [dbo].[Address] CHECK CONSTRAINT [FK_Address_Country_CountryId]
GO
ALTER TABLE [dbo].[Address]  WITH CHECK ADD  CONSTRAINT [FK_Address_StateProvince_StateProvinceId] FOREIGN KEY([StateProvinceId])
REFERENCES [dbo].[StateProvince] ([Id])
GO
ALTER TABLE [dbo].[Address] CHECK CONSTRAINT [FK_Address_StateProvince_StateProvinceId]
GO
ALTER TABLE [dbo].[Affiliate]  WITH CHECK ADD  CONSTRAINT [FK_Affiliate_Address_AddressId] FOREIGN KEY([AddressId])
REFERENCES [dbo].[Address] ([Id])
GO
ALTER TABLE [dbo].[Affiliate] CHECK CONSTRAINT [FK_Affiliate_Address_AddressId]
GO
ALTER TABLE [dbo].[BackInStockSubscription]  WITH CHECK ADD  CONSTRAINT [FK_BackInStockSubscription_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BackInStockSubscription] CHECK CONSTRAINT [FK_BackInStockSubscription_Customer_CustomerId]
GO
ALTER TABLE [dbo].[BackInStockSubscription]  WITH CHECK ADD  CONSTRAINT [FK_BackInStockSubscription_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BackInStockSubscription] CHECK CONSTRAINT [FK_BackInStockSubscription_Product_ProductId]
GO
ALTER TABLE [dbo].[Category]  WITH CHECK ADD  CONSTRAINT [FK_Category_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Category] CHECK CONSTRAINT [FK_Category_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[CheckoutAttributeValue]  WITH CHECK ADD  CONSTRAINT [FK_CheckoutAttributeValue_CheckoutAttribute_CheckoutAttributeId] FOREIGN KEY([CheckoutAttributeId])
REFERENCES [dbo].[CheckoutAttribute] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CheckoutAttributeValue] CHECK CONSTRAINT [FK_CheckoutAttributeValue_CheckoutAttribute_CheckoutAttributeId]
GO
ALTER TABLE [dbo].[CheckoutAttributeValue]  WITH CHECK ADD  CONSTRAINT [FK_CheckoutAttributeValue_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[CheckoutAttributeValue] CHECK CONSTRAINT [FK_CheckoutAttributeValue_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[Country]  WITH CHECK ADD  CONSTRAINT [FK_Country_Currency_DefaultCurrencyId] FOREIGN KEY([DefaultCurrencyId])
REFERENCES [dbo].[Currency] ([Id])
GO
ALTER TABLE [dbo].[Country] CHECK CONSTRAINT [FK_Country_Currency_DefaultCurrencyId]
GO
ALTER TABLE [dbo].[Customer]  WITH CHECK ADD  CONSTRAINT [FK_Customer_Address_BillingAddress_Id] FOREIGN KEY([BillingAddress_Id])
REFERENCES [dbo].[Address] ([Id])
GO
ALTER TABLE [dbo].[Customer] CHECK CONSTRAINT [FK_Customer_Address_BillingAddress_Id]
GO
ALTER TABLE [dbo].[Customer]  WITH CHECK ADD  CONSTRAINT [FK_Customer_Address_ShippingAddress_Id] FOREIGN KEY([ShippingAddress_Id])
REFERENCES [dbo].[Address] ([Id])
GO
ALTER TABLE [dbo].[Customer] CHECK CONSTRAINT [FK_Customer_Address_ShippingAddress_Id]
GO
ALTER TABLE [dbo].[CustomerAddresses]  WITH CHECK ADD  CONSTRAINT [FK_dbo.CustomerAddresses_dbo.Address_Address_Id] FOREIGN KEY([Address_Id])
REFERENCES [dbo].[Address] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CustomerAddresses] CHECK CONSTRAINT [FK_dbo.CustomerAddresses_dbo.Address_Address_Id]
GO
ALTER TABLE [dbo].[CustomerAddresses]  WITH CHECK ADD  CONSTRAINT [FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id] FOREIGN KEY([Customer_Id])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CustomerAddresses] CHECK CONSTRAINT [FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id]
GO
ALTER TABLE [dbo].[CustomerContent]  WITH CHECK ADD  CONSTRAINT [FK_CustomerContent_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
GO
ALTER TABLE [dbo].[CustomerContent] CHECK CONSTRAINT [FK_CustomerContent_Customer_CustomerId]
GO
ALTER TABLE [dbo].[CustomerRoleMapping]  WITH CHECK ADD  CONSTRAINT [FK_CustomerRoleMapping_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CustomerRoleMapping] CHECK CONSTRAINT [FK_CustomerRoleMapping_Customer_CustomerId]
GO
ALTER TABLE [dbo].[CustomerRoleMapping]  WITH CHECK ADD  CONSTRAINT [FK_CustomerRoleMapping_CustomerRole_CustomerRoleId] FOREIGN KEY([CustomerRoleId])
REFERENCES [dbo].[CustomerRole] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CustomerRoleMapping] CHECK CONSTRAINT [FK_CustomerRoleMapping_CustomerRole_CustomerRoleId]
GO
ALTER TABLE [dbo].[Discount_AppliedToCategories]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id] FOREIGN KEY([Category_Id])
REFERENCES [dbo].[Category] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToCategories] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id]
GO
ALTER TABLE [dbo].[Discount_AppliedToCategories]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id] FOREIGN KEY([Discount_Id])
REFERENCES [dbo].[Discount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToCategories] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id]
GO
ALTER TABLE [dbo].[Discount_AppliedToManufacturers]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id] FOREIGN KEY([Discount_Id])
REFERENCES [dbo].[Discount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToManufacturers] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id]
GO
ALTER TABLE [dbo].[Discount_AppliedToManufacturers]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufacturer_Id] FOREIGN KEY([Manufacturer_Id])
REFERENCES [dbo].[Manufacturer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToManufacturers] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufacturer_Id]
GO
ALTER TABLE [dbo].[Discount_AppliedToProducts]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id] FOREIGN KEY([Discount_Id])
REFERENCES [dbo].[Discount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToProducts] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id]
GO
ALTER TABLE [dbo].[Discount_AppliedToProducts]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id] FOREIGN KEY([Product_Id])
REFERENCES [dbo].[Product] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Discount_AppliedToProducts] CHECK CONSTRAINT [FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id]
GO
ALTER TABLE [dbo].[DiscountUsageHistory]  WITH CHECK ADD  CONSTRAINT [FK_DiscountUsageHistory_Discount_DiscountId] FOREIGN KEY([DiscountId])
REFERENCES [dbo].[Discount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DiscountUsageHistory] CHECK CONSTRAINT [FK_DiscountUsageHistory_Discount_DiscountId]
GO
ALTER TABLE [dbo].[DiscountUsageHistory]  WITH CHECK ADD  CONSTRAINT [FK_DiscountUsageHistory_Order_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DiscountUsageHistory] CHECK CONSTRAINT [FK_DiscountUsageHistory_Order_OrderId]
GO
ALTER TABLE [dbo].[Download]  WITH CHECK ADD  CONSTRAINT [FK_Download_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Download] CHECK CONSTRAINT [FK_Download_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[ExportDeployment]  WITH CHECK ADD  CONSTRAINT [FK_ExportDeployment_ExportProfile_ProfileId] FOREIGN KEY([ProfileId])
REFERENCES [dbo].[ExportProfile] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ExportDeployment] CHECK CONSTRAINT [FK_ExportDeployment_ExportProfile_ProfileId]
GO
ALTER TABLE [dbo].[ExportProfile]  WITH CHECK ADD  CONSTRAINT [FK_ExportProfile_ScheduleTask_SchedulingTaskId] FOREIGN KEY([SchedulingTaskId])
REFERENCES [dbo].[ScheduleTask] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ExportProfile] CHECK CONSTRAINT [FK_ExportProfile_ScheduleTask_SchedulingTaskId]
GO
ALTER TABLE [dbo].[ExternalAuthenticationRecord]  WITH CHECK ADD  CONSTRAINT [FK_ExternalAuthenticationRecord_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ExternalAuthenticationRecord] CHECK CONSTRAINT [FK_ExternalAuthenticationRecord_Customer_CustomerId]
GO
ALTER TABLE [dbo].[GiftCard]  WITH CHECK ADD  CONSTRAINT [FK_GiftCard_OrderItem_PurchasedWithOrderItemId] FOREIGN KEY([PurchasedWithOrderItemId])
REFERENCES [dbo].[OrderItem] ([Id])
GO
ALTER TABLE [dbo].[GiftCard] CHECK CONSTRAINT [FK_GiftCard_OrderItem_PurchasedWithOrderItemId]
GO
ALTER TABLE [dbo].[GiftCardUsageHistory]  WITH CHECK ADD  CONSTRAINT [FK_GiftCardUsageHistory_GiftCard_GiftCardId] FOREIGN KEY([GiftCardId])
REFERENCES [dbo].[GiftCard] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[GiftCardUsageHistory] CHECK CONSTRAINT [FK_GiftCardUsageHistory_GiftCard_GiftCardId]
GO
ALTER TABLE [dbo].[GiftCardUsageHistory]  WITH CHECK ADD  CONSTRAINT [FK_GiftCardUsageHistory_Order_UsedWithOrderId] FOREIGN KEY([UsedWithOrderId])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[GiftCardUsageHistory] CHECK CONSTRAINT [FK_GiftCardUsageHistory_Order_UsedWithOrderId]
GO
ALTER TABLE [dbo].[ImportProfile]  WITH CHECK ADD  CONSTRAINT [FK_ImportProfile_ScheduleTask_SchedulingTaskId] FOREIGN KEY([SchedulingTaskId])
REFERENCES [dbo].[ScheduleTask] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ImportProfile] CHECK CONSTRAINT [FK_ImportProfile_ScheduleTask_SchedulingTaskId]
GO
ALTER TABLE [dbo].[LocaleStringResource]  WITH CHECK ADD  CONSTRAINT [FK_LocaleStringResource_Language_LanguageId] FOREIGN KEY([LanguageId])
REFERENCES [dbo].[Language] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LocaleStringResource] CHECK CONSTRAINT [FK_LocaleStringResource_Language_LanguageId]
GO
ALTER TABLE [dbo].[LocalizedProperty]  WITH CHECK ADD  CONSTRAINT [FK_LocalizedProperty_Language_LanguageId] FOREIGN KEY([LanguageId])
REFERENCES [dbo].[Language] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LocalizedProperty] CHECK CONSTRAINT [FK_LocalizedProperty_Language_LanguageId]
GO
ALTER TABLE [dbo].[Log]  WITH CHECK ADD  CONSTRAINT [FK_Log_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Log] CHECK CONSTRAINT [FK_Log_Customer_CustomerId]
GO
ALTER TABLE [dbo].[Manufacturer]  WITH CHECK ADD  CONSTRAINT [FK_Manufacturer_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Manufacturer] CHECK CONSTRAINT [FK_Manufacturer_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[MediaFile]  WITH CHECK ADD  CONSTRAINT [FK_MediaFile_MediaFolder_FolderId] FOREIGN KEY([FolderId])
REFERENCES [dbo].[MediaFolder] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[MediaFile] CHECK CONSTRAINT [FK_MediaFile_MediaFolder_FolderId]
GO
ALTER TABLE [dbo].[MediaFile]  WITH CHECK ADD  CONSTRAINT [FK_MediaFile_MediaStorage_MediaStorageId] FOREIGN KEY([MediaStorageId])
REFERENCES [dbo].[MediaStorage] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[MediaFile] CHECK CONSTRAINT [FK_MediaFile_MediaStorage_MediaStorageId]
GO
ALTER TABLE [dbo].[MediaFile_Tag_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id] FOREIGN KEY([MediaFile_Id])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MediaFile_Tag_Mapping] CHECK CONSTRAINT [FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id]
GO
ALTER TABLE [dbo].[MediaFile_Tag_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id] FOREIGN KEY([MediaTag_Id])
REFERENCES [dbo].[MediaTag] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MediaFile_Tag_Mapping] CHECK CONSTRAINT [FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id]
GO
ALTER TABLE [dbo].[MediaFolder]  WITH CHECK ADD  CONSTRAINT [FK_MediaFolder_MediaFolder_ParentId] FOREIGN KEY([ParentId])
REFERENCES [dbo].[MediaFolder] ([Id])
GO
ALTER TABLE [dbo].[MediaFolder] CHECK CONSTRAINT [FK_MediaFolder_MediaFolder_ParentId]
GO
ALTER TABLE [dbo].[MediaTrack]  WITH CHECK ADD  CONSTRAINT [FK_MediaTrack_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MediaTrack] CHECK CONSTRAINT [FK_MediaTrack_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[MenuItemRecord]  WITH CHECK ADD  CONSTRAINT [FK_MenuItemRecord_MenuRecord_MenuId] FOREIGN KEY([MenuId])
REFERENCES [dbo].[MenuRecord] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MenuItemRecord] CHECK CONSTRAINT [FK_MenuItemRecord_MenuRecord_MenuId]
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Address_BillingAddressId] FOREIGN KEY([BillingAddressId])
REFERENCES [dbo].[Address] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_Address_BillingAddressId]
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Address_ShippingAddressId] FOREIGN KEY([ShippingAddressId])
REFERENCES [dbo].[Address] ([Id])
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_Address_ShippingAddressId]
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_Customer_CustomerId]
GO
ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Order_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[OrderItem] CHECK CONSTRAINT [FK_OrderItem_Order_OrderId]
GO
ALTER TABLE [dbo].[OrderItem]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[OrderItem] CHECK CONSTRAINT [FK_OrderItem_Product_ProductId]
GO
ALTER TABLE [dbo].[OrderNote]  WITH CHECK ADD  CONSTRAINT [FK_OrderNote_Order_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[OrderNote] CHECK CONSTRAINT [FK_OrderNote_Order_OrderId]
GO
ALTER TABLE [dbo].[PermissionRoleMapping]  WITH CHECK ADD  CONSTRAINT [FK_PermissionRoleMapping_CustomerRole_CustomerRoleId] FOREIGN KEY([CustomerRoleId])
REFERENCES [dbo].[CustomerRole] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PermissionRoleMapping] CHECK CONSTRAINT [FK_PermissionRoleMapping_CustomerRole_CustomerRoleId]
GO
ALTER TABLE [dbo].[PermissionRoleMapping]  WITH CHECK ADD  CONSTRAINT [FK_PermissionRoleMapping_PermissionRecord_PermissionRecordId] FOREIGN KEY([PermissionRecordId])
REFERENCES [dbo].[PermissionRecord] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PermissionRoleMapping] CHECK CONSTRAINT [FK_PermissionRoleMapping_PermissionRecord_PermissionRecordId]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_Country_CountryOfOriginId] FOREIGN KEY([CountryOfOriginId])
REFERENCES [dbo].[Country] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_Country_CountryOfOriginId]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_DeliveryTime_DeliveryTimeId] FOREIGN KEY([DeliveryTimeId])
REFERENCES [dbo].[DeliveryTime] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_DeliveryTime_DeliveryTimeId]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_Download_SampleDownloadId] FOREIGN KEY([SampleDownloadId])
REFERENCES [dbo].[Download] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_Download_SampleDownloadId]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_QuantityUnit_QuantityUnitId] FOREIGN KEY([QuantityUnitId])
REFERENCES [dbo].[QuantityUnit] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_QuantityUnit_QuantityUnitId]
GO
ALTER TABLE [dbo].[Product_Category_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_Category_Mapping_Category_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[Category] ([Id])
GO
ALTER TABLE [dbo].[Product_Category_Mapping] CHECK CONSTRAINT [FK_Product_Category_Mapping_Category_CategoryId]
GO
ALTER TABLE [dbo].[Product_Category_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_Category_Mapping_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[Product_Category_Mapping] CHECK CONSTRAINT [FK_Product_Category_Mapping_Product_ProductId]
GO
ALTER TABLE [dbo].[Product_Manufacturer_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_Manufacturer_Mapping_Manufacturer_ManufacturerId] FOREIGN KEY([ManufacturerId])
REFERENCES [dbo].[Manufacturer] ([Id])
GO
ALTER TABLE [dbo].[Product_Manufacturer_Mapping] CHECK CONSTRAINT [FK_Product_Manufacturer_Mapping_Manufacturer_ManufacturerId]
GO
ALTER TABLE [dbo].[Product_Manufacturer_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_Manufacturer_Mapping_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[Product_Manufacturer_Mapping] CHECK CONSTRAINT [FK_Product_Manufacturer_Mapping_Product_ProductId]
GO
ALTER TABLE [dbo].[Product_MediaFile_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_MediaFile_Mapping_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_MediaFile_Mapping] CHECK CONSTRAINT [FK_Product_MediaFile_Mapping_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[Product_MediaFile_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_MediaFile_Mapping_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_MediaFile_Mapping] CHECK CONSTRAINT [FK_Product_MediaFile_Mapping_Product_ProductId]
GO
ALTER TABLE [dbo].[Product_ProductAttribute_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_ProductAttribute_Mapping_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[Product_ProductAttribute_Mapping] CHECK CONSTRAINT [FK_Product_ProductAttribute_Mapping_Product_ProductId]
GO
ALTER TABLE [dbo].[Product_ProductAttribute_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_ProductAttribute_Mapping_ProductAttribute_ProductAttributeId] FOREIGN KEY([ProductAttributeId])
REFERENCES [dbo].[ProductAttribute] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_ProductAttribute_Mapping] CHECK CONSTRAINT [FK_Product_ProductAttribute_Mapping_ProductAttribute_ProductAttributeId]
GO
ALTER TABLE [dbo].[Product_ProductTag_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id] FOREIGN KEY([Product_Id])
REFERENCES [dbo].[Product] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_ProductTag_Mapping] CHECK CONSTRAINT [FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id]
GO
ALTER TABLE [dbo].[Product_ProductTag_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id] FOREIGN KEY([ProductTag_Id])
REFERENCES [dbo].[ProductTag] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_ProductTag_Mapping] CHECK CONSTRAINT [FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id]
GO
ALTER TABLE [dbo].[Product_SpecificationAttribute_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_SpecificationAttribute_Mapping_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[Product_SpecificationAttribute_Mapping] CHECK CONSTRAINT [FK_Product_SpecificationAttribute_Mapping_Product_ProductId]
GO
ALTER TABLE [dbo].[Product_SpecificationAttribute_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_Product_SpecificationAttribute_Mapping_SpecificationAttributeOption_SpecificationAttributeOptionId] FOREIGN KEY([SpecificationAttributeOptionId])
REFERENCES [dbo].[SpecificationAttributeOption] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Product_SpecificationAttribute_Mapping] CHECK CONSTRAINT [FK_Product_SpecificationAttribute_Mapping_SpecificationAttributeOption_SpecificationAttributeOptionId]
GO
ALTER TABLE [dbo].[ProductAttributeOption]  WITH CHECK ADD  CONSTRAINT [FK_ProductAttributeOption_ProductAttributeOptionsSet_ProductAttributeOptionsSetId] FOREIGN KEY([ProductAttributeOptionsSetId])
REFERENCES [dbo].[ProductAttributeOptionsSet] ([Id])
GO
ALTER TABLE [dbo].[ProductAttributeOption] CHECK CONSTRAINT [FK_ProductAttributeOption_ProductAttributeOptionsSet_ProductAttributeOptionsSetId]
GO
ALTER TABLE [dbo].[ProductAttributeOptionsSet]  WITH CHECK ADD  CONSTRAINT [FK_ProductAttributeOptionsSet_ProductAttribute_ProductAttributeId] FOREIGN KEY([ProductAttributeId])
REFERENCES [dbo].[ProductAttribute] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProductAttributeOptionsSet] CHECK CONSTRAINT [FK_ProductAttributeOptionsSet_ProductAttribute_ProductAttributeId]
GO
ALTER TABLE [dbo].[ProductBundleItem]  WITH CHECK ADD  CONSTRAINT [FK_ProductBundleItem_Product_BundleProductId] FOREIGN KEY([BundleProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[ProductBundleItem] CHECK CONSTRAINT [FK_ProductBundleItem_Product_BundleProductId]
GO
ALTER TABLE [dbo].[ProductBundleItem]  WITH CHECK ADD  CONSTRAINT [FK_ProductBundleItem_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[ProductBundleItem] CHECK CONSTRAINT [FK_ProductBundleItem_Product_ProductId]
GO
ALTER TABLE [dbo].[ProductBundleItemAttributeFilter]  WITH CHECK ADD  CONSTRAINT [FK_ProductBundleItemAttributeFilter_ProductBundleItem_BundleItemId] FOREIGN KEY([BundleItemId])
REFERENCES [dbo].[ProductBundleItem] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProductBundleItemAttributeFilter] CHECK CONSTRAINT [FK_ProductBundleItemAttributeFilter_ProductBundleItem_BundleItemId]
GO
ALTER TABLE [dbo].[ProductReview]  WITH CHECK ADD  CONSTRAINT [FK_ProductReview_CustomerContent_Id] FOREIGN KEY([Id])
REFERENCES [dbo].[CustomerContent] ([Id])
GO
ALTER TABLE [dbo].[ProductReview] CHECK CONSTRAINT [FK_ProductReview_CustomerContent_Id]
GO
ALTER TABLE [dbo].[ProductReview]  WITH CHECK ADD  CONSTRAINT [FK_ProductReview_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[ProductReview] CHECK CONSTRAINT [FK_ProductReview_Product_ProductId]
GO
ALTER TABLE [dbo].[ProductReviewHelpfulness]  WITH CHECK ADD  CONSTRAINT [FK_ProductReviewHelpfulness_CustomerContent_Id] FOREIGN KEY([Id])
REFERENCES [dbo].[CustomerContent] ([Id])
GO
ALTER TABLE [dbo].[ProductReviewHelpfulness] CHECK CONSTRAINT [FK_ProductReviewHelpfulness_CustomerContent_Id]
GO
ALTER TABLE [dbo].[ProductReviewHelpfulness]  WITH CHECK ADD  CONSTRAINT [FK_ProductReviewHelpfulness_ProductReview_ProductReviewId] FOREIGN KEY([ProductReviewId])
REFERENCES [dbo].[ProductReview] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProductReviewHelpfulness] CHECK CONSTRAINT [FK_ProductReviewHelpfulness_ProductReview_ProductReviewId]
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination]  WITH CHECK ADD  CONSTRAINT [FK_ProductVariantAttributeCombination_DeliveryTime_DeliveryTimeId] FOREIGN KEY([DeliveryTimeId])
REFERENCES [dbo].[DeliveryTime] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination] CHECK CONSTRAINT [FK_ProductVariantAttributeCombination_DeliveryTime_DeliveryTimeId]
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination]  WITH CHECK ADD  CONSTRAINT [FK_ProductVariantAttributeCombination_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination] CHECK CONSTRAINT [FK_ProductVariantAttributeCombination_Product_ProductId]
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination]  WITH CHECK ADD  CONSTRAINT [FK_ProductVariantAttributeCombination_QuantityUnit_QuantityUnitId] FOREIGN KEY([QuantityUnitId])
REFERENCES [dbo].[QuantityUnit] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[ProductVariantAttributeCombination] CHECK CONSTRAINT [FK_ProductVariantAttributeCombination_QuantityUnit_QuantityUnitId]
GO
ALTER TABLE [dbo].[ProductVariantAttributeValue]  WITH CHECK ADD  CONSTRAINT [FK_ProductVariantAttributeValue_Product_ProductAttribute_Mapping_ProductVariantAttributeId] FOREIGN KEY([ProductVariantAttributeId])
REFERENCES [dbo].[Product_ProductAttribute_Mapping] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProductVariantAttributeValue] CHECK CONSTRAINT [FK_ProductVariantAttributeValue_Product_ProductAttribute_Mapping_ProductVariantAttributeId]
GO
ALTER TABLE [dbo].[QueuedEmail]  WITH CHECK ADD  CONSTRAINT [FK_QueuedEmail_EmailAccount_EmailAccountId] FOREIGN KEY([EmailAccountId])
REFERENCES [dbo].[EmailAccount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QueuedEmail] CHECK CONSTRAINT [FK_QueuedEmail_EmailAccount_EmailAccountId]
GO
ALTER TABLE [dbo].[QueuedEmailAttachment]  WITH CHECK ADD  CONSTRAINT [FK_QueuedEmailAttachment_MediaFile_MediaFileId] FOREIGN KEY([MediaFileId])
REFERENCES [dbo].[MediaFile] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[QueuedEmailAttachment] CHECK CONSTRAINT [FK_QueuedEmailAttachment_MediaFile_MediaFileId]
GO
ALTER TABLE [dbo].[QueuedEmailAttachment]  WITH CHECK ADD  CONSTRAINT [FK_QueuedEmailAttachment_MediaStorage_MediaStorageId] FOREIGN KEY([MediaStorageId])
REFERENCES [dbo].[MediaStorage] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[QueuedEmailAttachment] CHECK CONSTRAINT [FK_QueuedEmailAttachment_MediaStorage_MediaStorageId]
GO
ALTER TABLE [dbo].[QueuedEmailAttachment]  WITH CHECK ADD  CONSTRAINT [FK_QueuedEmailAttachment_QueuedEmail_QueuedEmailId] FOREIGN KEY([QueuedEmailId])
REFERENCES [dbo].[QueuedEmail] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QueuedEmailAttachment] CHECK CONSTRAINT [FK_QueuedEmailAttachment_QueuedEmail_QueuedEmailId]
GO
ALTER TABLE [dbo].[RecurringPayment]  WITH CHECK ADD  CONSTRAINT [FK_RecurringPayment_Order_InitialOrderId] FOREIGN KEY([InitialOrderId])
REFERENCES [dbo].[Order] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RecurringPayment] CHECK CONSTRAINT [FK_RecurringPayment_Order_InitialOrderId]
GO
ALTER TABLE [dbo].[RecurringPaymentHistory]  WITH CHECK ADD  CONSTRAINT [FK_RecurringPaymentHistory_RecurringPayment_RecurringPaymentId] FOREIGN KEY([RecurringPaymentId])
REFERENCES [dbo].[RecurringPayment] ([Id])
GO
ALTER TABLE [dbo].[RecurringPaymentHistory] CHECK CONSTRAINT [FK_RecurringPaymentHistory_RecurringPayment_RecurringPaymentId]
GO
ALTER TABLE [dbo].[ReturnRequest]  WITH CHECK ADD  CONSTRAINT [FK_ReturnRequest_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ReturnRequest] CHECK CONSTRAINT [FK_ReturnRequest_Customer_CustomerId]
GO
ALTER TABLE [dbo].[RewardPointsHistory]  WITH CHECK ADD  CONSTRAINT [FK_RewardPointsHistory_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RewardPointsHistory] CHECK CONSTRAINT [FK_RewardPointsHistory_Customer_CustomerId]
GO
ALTER TABLE [dbo].[RewardPointsHistory]  WITH CHECK ADD  CONSTRAINT [FK_RewardPointsHistory_Order_UsedWithOrder_Id] FOREIGN KEY([UsedWithOrder_Id])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[RewardPointsHistory] CHECK CONSTRAINT [FK_RewardPointsHistory_Order_UsedWithOrder_Id]
GO
ALTER TABLE [dbo].[Rule]  WITH CHECK ADD  CONSTRAINT [FK_Rule_RuleSet_RuleSetId] FOREIGN KEY([RuleSetId])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Rule] CHECK CONSTRAINT [FK_Rule_RuleSet_RuleSetId]
GO
ALTER TABLE [dbo].[RuleSet_Category_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id] FOREIGN KEY([Category_Id])
REFERENCES [dbo].[Category] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_Category_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id]
GO
ALTER TABLE [dbo].[RuleSet_Category_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id] FOREIGN KEY([RuleSetEntity_Id])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_Category_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id]
GO
ALTER TABLE [dbo].[RuleSet_CustomerRole_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRole_Id] FOREIGN KEY([CustomerRole_Id])
REFERENCES [dbo].[CustomerRole] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_CustomerRole_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRole_Id]
GO
ALTER TABLE [dbo].[RuleSet_CustomerRole_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id] FOREIGN KEY([RuleSetEntity_Id])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_CustomerRole_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id]
GO
ALTER TABLE [dbo].[RuleSet_Discount_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id] FOREIGN KEY([Discount_Id])
REFERENCES [dbo].[Discount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_Discount_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id]
GO
ALTER TABLE [dbo].[RuleSet_Discount_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id] FOREIGN KEY([RuleSetEntity_Id])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_Discount_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id]
GO
ALTER TABLE [dbo].[RuleSet_PaymentMethod_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMethod_Id] FOREIGN KEY([PaymentMethod_Id])
REFERENCES [dbo].[PaymentMethod] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_PaymentMethod_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMethod_Id]
GO
ALTER TABLE [dbo].[RuleSet_PaymentMethod_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id] FOREIGN KEY([RuleSetEntity_Id])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_PaymentMethod_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id]
GO
ALTER TABLE [dbo].[RuleSet_ShippingMethod_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id] FOREIGN KEY([RuleSetEntity_Id])
REFERENCES [dbo].[RuleSet] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_ShippingMethod_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id]
GO
ALTER TABLE [dbo].[RuleSet_ShippingMethod_Mapping]  WITH CHECK ADD  CONSTRAINT [FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_ShippingMethod_Id] FOREIGN KEY([ShippingMethod_Id])
REFERENCES [dbo].[ShippingMethod] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RuleSet_ShippingMethod_Mapping] CHECK CONSTRAINT [FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_ShippingMethod_Id]
GO
ALTER TABLE [dbo].[ScheduleTaskHistory]  WITH CHECK ADD  CONSTRAINT [FK_ScheduleTaskHistory_ScheduleTask_ScheduleTaskId] FOREIGN KEY([ScheduleTaskId])
REFERENCES [dbo].[ScheduleTask] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ScheduleTaskHistory] CHECK CONSTRAINT [FK_ScheduleTaskHistory_ScheduleTask_ScheduleTaskId]
GO
ALTER TABLE [dbo].[Shipment]  WITH CHECK ADD  CONSTRAINT [FK_Shipment_Order_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[Shipment] CHECK CONSTRAINT [FK_Shipment_Order_OrderId]
GO
ALTER TABLE [dbo].[ShipmentItem]  WITH CHECK ADD  CONSTRAINT [FK_ShipmentItem_Shipment_ShipmentId] FOREIGN KEY([ShipmentId])
REFERENCES [dbo].[Shipment] ([Id])
GO
ALTER TABLE [dbo].[ShipmentItem] CHECK CONSTRAINT [FK_ShipmentItem_Shipment_ShipmentId]
GO
ALTER TABLE [dbo].[ShoppingCartItem]  WITH CHECK ADD  CONSTRAINT [FK_ShoppingCartItem_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ShoppingCartItem] CHECK CONSTRAINT [FK_ShoppingCartItem_Customer_CustomerId]
GO
ALTER TABLE [dbo].[ShoppingCartItem]  WITH CHECK ADD  CONSTRAINT [FK_ShoppingCartItem_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ShoppingCartItem] CHECK CONSTRAINT [FK_ShoppingCartItem_Product_ProductId]
GO
ALTER TABLE [dbo].[ShoppingCartItem]  WITH CHECK ADD  CONSTRAINT [FK_ShoppingCartItem_ProductBundleItem_BundleItemId] FOREIGN KEY([BundleItemId])
REFERENCES [dbo].[ProductBundleItem] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[ShoppingCartItem] CHECK CONSTRAINT [FK_ShoppingCartItem_ProductBundleItem_BundleItemId]
GO
ALTER TABLE [dbo].[SpecificationAttributeOption]  WITH CHECK ADD  CONSTRAINT [FK_SpecificationAttributeOption_SpecificationAttribute_SpecificationAttributeId] FOREIGN KEY([SpecificationAttributeId])
REFERENCES [dbo].[SpecificationAttribute] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SpecificationAttributeOption] CHECK CONSTRAINT [FK_SpecificationAttributeOption_SpecificationAttribute_SpecificationAttributeId]
GO
ALTER TABLE [dbo].[StateProvince]  WITH CHECK ADD  CONSTRAINT [FK_StateProvince_Country_CountryId] FOREIGN KEY([CountryId])
REFERENCES [dbo].[Country] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[StateProvince] CHECK CONSTRAINT [FK_StateProvince_Country_CountryId]
GO
ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_Store_Currency_PrimaryExchangeRateCurrencyId] FOREIGN KEY([PrimaryExchangeRateCurrencyId])
REFERENCES [dbo].[Currency] ([Id])
GO
ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_Store_Currency_PrimaryExchangeRateCurrencyId]
GO
ALTER TABLE [dbo].[Store]  WITH CHECK ADD  CONSTRAINT [FK_Store_Currency_PrimaryStoreCurrencyId] FOREIGN KEY([PrimaryStoreCurrencyId])
REFERENCES [dbo].[Currency] ([Id])
GO
ALTER TABLE [dbo].[Store] CHECK CONSTRAINT [FK_Store_Currency_PrimaryStoreCurrencyId]
GO
ALTER TABLE [dbo].[TierPrice]  WITH CHECK ADD  CONSTRAINT [FK_TierPrice_CustomerRole_CustomerRoleId] FOREIGN KEY([CustomerRoleId])
REFERENCES [dbo].[CustomerRole] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TierPrice] CHECK CONSTRAINT [FK_TierPrice_CustomerRole_CustomerRoleId]
GO
ALTER TABLE [dbo].[TierPrice]  WITH CHECK ADD  CONSTRAINT [FK_TierPrice_Product_ProductId] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([Id])
GO
ALTER TABLE [dbo].[TierPrice] CHECK CONSTRAINT [FK_TierPrice_Product_ProductId]
GO
ALTER TABLE [dbo].[WalletHistory]  WITH CHECK ADD  CONSTRAINT [FK_WalletHistory_Customer_CustomerId] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[WalletHistory] CHECK CONSTRAINT [FK_WalletHistory_Customer_CustomerId]
GO
ALTER TABLE [dbo].[WalletHistory]  WITH CHECK ADD  CONSTRAINT [FK_WalletHistory_Order_OrderId] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Order] ([Id])
GO
ALTER TABLE [dbo].[WalletHistory] CHECK CONSTRAINT [FK_WalletHistory_Order_OrderId]
GO
