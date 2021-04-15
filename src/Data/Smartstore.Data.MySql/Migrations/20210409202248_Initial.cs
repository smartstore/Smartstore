using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Smartstore.Core.Data;
using Smartstore.Data.Migrations;

namespace Smartstore.Data.MySql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (DbMigrationManager.Instance.SuppressInitialCreate<SmartDbContext>())
            {
                return;
            }

            migrationBuilder.CreateTable(
                name: "ActivityLogType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemKeyword = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaign",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    Subject = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    Body = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaign", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    ViewPath = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckoutAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    TextPrompt = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShippableProductRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsTaxExempt = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TaxCategoryId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttributeControlTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutAttribute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossSellProduct",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId1 = table.Column<int>(type: "int", nullable: false),
                    ProductId2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossSellProduct", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(5) CHARACTER SET utf8mb4", maxLength: 5, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DisplayLocale = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    CustomFormatting = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DomainEndings = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    RoundOrderItemsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RoundNumDecimals = table.Column<int>(type: "int", nullable: false),
                    RoundOrderTotalEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RoundOrderTotalDenominator = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RoundOrderTotalRule = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    FreeShipping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TaxExempt = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TaxDisplayType = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SystemName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    OrderTotalMinimum = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OrderTotalMaximum = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryTime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    ColorHexValue = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    DisplayLocale = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    MinDays = table.Column<int>(type: "int", nullable: true),
                    MaxDays = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryTime", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Discount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    DiscountTypeId = table.Column<int>(type: "int", nullable: false),
                    UsePercentage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequiresCouponCode = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CouponCode = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    DiscountLimitationId = table.Column<int>(type: "int", nullable: false),
                    LimitationTimes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    Host = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    EnableSsl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UseDefaultCredentials = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenericAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    KeyGroup = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Key = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Value = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenericAttribute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Language",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    LanguageCulture = table.Column<string>(type: "varchar(20) CHARACTER SET utf8mb4", maxLength: 20, nullable: false),
                    UniqueSeoCode = table.Column<string>(type: "varchar(2) CHARACTER SET utf8mb4", maxLength: 2, nullable: false),
                    FlagImageFileName = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    Rtl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManufacturerTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    ViewPath = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManufacturerTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasureDimension",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    SystemKeyword = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    Ratio = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureDimension", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasureWeight",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    SystemKeyword = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Ratio = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureWeight", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaFolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    CanDetectTracks = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Metadata = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    FilesCount = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<string>(type: "varchar(128) CHARACTER SET utf8mb4", maxLength: 128, nullable: false),
                    ResKey = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IncludePath = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFolder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFolder_MediaFolder_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MediaFolder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaStorage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStorage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    IsSystemMenu = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Template = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    WidgetZone = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    Title = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    To = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: false),
                    ReplyTo = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    ModelTypes = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    LastModelTree = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    BccEmailAddresses = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailAccountId = table.Column<int>(type: "int", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SendManually = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Attachment1FileId = table.Column<int>(type: "int", nullable: true),
                    Attachment2FileId = table.Column<int>(type: "int", nullable: true),
                    Attachment3FileId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NamedEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    DisplayName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Slug = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    LastMod = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamedEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsLetterSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NewsLetterSubscriptionGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    Email = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    WorkingLanguageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsLetterSubscription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PaymentMethodSystemName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    FullDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    RoundOrderTotalEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    Description = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    Alias = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    AllowFiltering = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    FacetTemplateHint = table.Column<int>(type: "int", nullable: false),
                    IndexOptionNames = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExportMappings = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttribute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    ViewPath = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuantityUnit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    NamePlural = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    DisplayLocale = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuantityUnit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelatedProduct",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId1 = table.Column<int>(type: "int", nullable: false),
                    ProductId2 = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedProduct", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IsSubGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LogicalOperator = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastProcessedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: false),
                    Alias = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    CronExpression = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "varchar(800) CHARACTER SET utf8mb4", maxLength: 800, nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StopOnError = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NextRunUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsHidden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RunPerMachine = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Setting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Value = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Setting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IgnoreCharges = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationAttribute",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    Alias = table.Column<string>(type: "varchar(30) CHARACTER SET utf8mb4", maxLength: 30, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ShowOnProductPage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowFiltering = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FacetSorting = table.Column<int>(type: "int", nullable: false),
                    FacetTemplateHint = table.Column<int>(type: "int", nullable: false),
                    IndexOptionNames = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationAttribute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IsSystemTopic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HtmlId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    BodyCssClass = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IncludeInSitemap = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPasswordProtected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Password = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Title = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ShortTitle = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    Intro = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    Body = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    MetaKeywords = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    MetaDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    MetaTitle = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RenderAsWidget = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    WidgetZone = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    WidgetWrapContent = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    WidgetShowTitle = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    WidgetBordered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    TitleTag = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CookieType = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topic", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UrlRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Slug = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    AllowsBilling = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowsShipping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoLetterIsoCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ThreeLetterIsoCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    NumericIsoCode = table.Column<int>(type: "int", nullable: false),
                    SubjectToVat = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DisplayCookieManager = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddressFormat = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    DefaultCurrencyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Country_Currency_DefaultCurrencyId",
                        column: x => x.DefaultCurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Url = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    SslEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SecureUrl = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ForceSslForAllPages = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Hosts = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    LogoMediaFileId = table.Column<int>(type: "int", nullable: false),
                    FavIconMediaFileId = table.Column<int>(type: "int", nullable: true),
                    PngIconMediaFileId = table.Column<int>(type: "int", nullable: true),
                    AppleTouchIconMediaFileId = table.Column<int>(type: "int", nullable: true),
                    MsTileImageMediaFileId = table.Column<int>(type: "int", nullable: true),
                    MsTileColor = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    HtmlBodyId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ContentDeliveryNetwork = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    PrimaryStoreCurrencyId = table.Column<int>(type: "int", nullable: false),
                    PrimaryExchangeRateCurrencyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Store_Currency_PrimaryExchangeRateCurrencyId",
                        column: x => x.PrimaryExchangeRateCurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Store_Currency_PrimaryStoreCurrencyId",
                        column: x => x.PrimaryStoreCurrencyId,
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AclRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    CustomerRoleId = table.Column<int>(type: "int", nullable: false),
                    IsIdle = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AclRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AclRecord_CustomerRole_CustomerRoleId",
                        column: x => x.CustomerRoleId,
                        principalTable: "CustomerRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueuedEmail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    From = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: false),
                    To = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: false),
                    ReplyTo = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    CC = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    Bcc = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentTries = table.Column<int>(type: "int", nullable: false),
                    SentOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EmailAccountId = table.Column<int>(type: "int", nullable: false),
                    SendManually = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedEmail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueuedEmail_EmailAccount_EmailAccountId",
                        column: x => x.EmailAccountId,
                        principalTable: "EmailAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocaleStringResource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    ResourceName = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    ResourceValue = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    IsFromPlugin = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IsTouched = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleStringResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocaleStringResource_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalizedProperty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    LocaleKeyGroup = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    LocaleKey = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    LocaleValue = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalizedProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalizedProperty_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(300) CHARACTER SET utf8mb4", maxLength: 300, nullable: true),
                    Alt = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Title = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Extension = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    MimeType = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    MediaType = table.Column<string>(type: "varchar(20) CHARACTER SET utf8mb4", maxLength: 20, nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    PixelSize = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsTransient = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Hidden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    MediaStorageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFile_MediaFolder_FolderId",
                        column: x => x.FolderId,
                        principalTable: "MediaFolder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MediaFile_MediaStorage_MediaStorageId",
                        column: x => x.MediaStorageId,
                        principalTable: "MediaStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    ParentItemId = table.Column<int>(type: "int", nullable: false),
                    ProviderName = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Title = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ShortDescription = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    PermissionNames = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    BeginGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowExpanded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NoFollow = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NewWindow = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Icon = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    Style = table.Column<string>(type: "varchar(10) CHARACTER SET utf8mb4", maxLength: 10, nullable: true),
                    IconColor = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    HtmlId = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    CssClass = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemRecord_MenuRecord_MenuId",
                        column: x => x.MenuId,
                        principalTable: "MenuRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Allow = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PermissionRecordId = table.Column<int>(type: "int", nullable: false),
                    CustomerRoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRoleMapping_CustomerRole_CustomerRoleId",
                        column: x => x.CustomerRoleId,
                        principalTable: "CustomerRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionRoleMapping_PermissionRecord_PermissionRecordId",
                        column: x => x.PermissionRecordId,
                        principalTable: "PermissionRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeOptionsSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ProductAttributeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeOptionsSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeOptionsSet_ProductAttribute_ProductAttribute~",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RuleSetId = table.Column<int>(type: "int", nullable: false),
                    RuleType = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "varchar(20) CHARACTER SET utf8mb4", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rule_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet_CustomerRole_Mapping",
                columns: table => new
                {
                    CustomerRole_Id = table.Column<int>(type: "int", nullable: false),
                    RuleSetEntity_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet_CustomerRole_Mapping", x => new { x.CustomerRole_Id, x.RuleSetEntity_Id });
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_CustomerRole_Mapping_dbo.CustomerRole_CustomerRole_Id",
                        column: x => x.CustomerRole_Id,
                        principalTable: "CustomerRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_CustomerRole_Mapping_dbo.RuleSet_RuleSetEntity_Id",
                        column: x => x.RuleSetEntity_Id,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet_Discount_Mapping",
                columns: table => new
                {
                    Discount_Id = table.Column<int>(type: "int", nullable: false),
                    RuleSetEntity_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet_Discount_Mapping", x => new { x.Discount_Id, x.RuleSetEntity_Id });
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_Discount_Mapping_dbo.Discount_Discount_Id",
                        column: x => x.Discount_Id,
                        principalTable: "Discount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_Discount_Mapping_dbo.RuleSet_RuleSetEntity_Id",
                        column: x => x.RuleSetEntity_Id,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet_PaymentMethod_Mapping",
                columns: table => new
                {
                    PaymentMethod_Id = table.Column<int>(type: "int", nullable: false),
                    RuleSetEntity_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet_PaymentMethod_Mapping", x => new { x.PaymentMethod_Id, x.RuleSetEntity_Id });
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.PaymentMethod_PaymentMethod_Id",
                        column: x => x.PaymentMethod_Id,
                        principalTable: "PaymentMethod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_PaymentMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id",
                        column: x => x.RuleSetEntity_Id,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTaskHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScheduleTaskId = table.Column<int>(type: "int", nullable: false),
                    IsRunning = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MachineName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    StartedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SucceededOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Error = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    ProgressMessage = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTaskHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTaskHistory_ScheduleTask_ScheduleTaskId",
                        column: x => x.ScheduleTaskId,
                        principalTable: "ScheduleTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet_ShippingMethod_Mapping",
                columns: table => new
                {
                    ShippingMethod_Id = table.Column<int>(type: "int", nullable: false),
                    RuleSetEntity_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet_ShippingMethod_Mapping", x => new { x.ShippingMethod_Id, x.RuleSetEntity_Id });
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.RuleSet_RuleSetEntity_Id",
                        column: x => x.RuleSetEntity_Id,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_ShippingMethod_Mapping_dbo.ShippingMethod_ShippingMethod_Id",
                        column: x => x.ShippingMethod_Id,
                        principalTable: "ShippingMethod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecificationAttributeOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SpecificationAttributeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    Alias = table.Column<string>(type: "varchar(30) CHARACTER SET utf8mb4", maxLength: 30, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    NumberValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MediaFileId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationAttributeOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificationAttributeOption_SpecificationAttribute_Specific~",
                        column: x => x.SpecificationAttributeId,
                        principalTable: "SpecificationAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateProvince",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: false),
                    Abbreviation = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateProvince", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateProvince_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    FullName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Description = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    BottomDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ExternalLink = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    BadgeText = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    BadgeStyle = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    CategoryTemplateId = table.Column<int>(type: "int", nullable: false),
                    MetaKeywords = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    MetaDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    MetaTitle = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: false),
                    MediaFileId = table.Column<int>(type: "int", nullable: true),
                    PageSize = table.Column<int>(type: "int", nullable: true),
                    AllowCustomersToSelectPageSize = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    PageSizeOptions = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    PriceRanges = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ShowOnHomePage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DefaultViewMode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    HasDiscountsApplied = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Category_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CheckoutAttributeValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    PriceAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsPreSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    CheckoutAttributeId = table.Column<int>(type: "int", nullable: false),
                    MediaFileId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutAttributeValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutAttributeValue_CheckoutAttribute_CheckoutAttributeId",
                        column: x => x.CheckoutAttributeId,
                        principalTable: "CheckoutAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckoutAttributeValue_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Download",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DownloadGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    UseDownloadUrl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DownloadUrl = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    IsTransient = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MediaFileId = table.Column<int>(type: "int", nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    FileVersion = table.Column<string>(type: "varchar(30) CHARACTER SET utf8mb4", maxLength: 30, nullable: true),
                    Changelog = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Download", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Download_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Manufacturer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    BottomDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ManufacturerTemplateId = table.Column<int>(type: "int", nullable: false),
                    MetaKeywords = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    MetaDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    MetaTitle = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    MediaFileId = table.Column<int>(type: "int", nullable: true),
                    PageSize = table.Column<int>(type: "int", nullable: true),
                    AllowCustomersToSelectPageSize = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    PageSizeOptions = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    PriceRanges = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HasDiscountsApplied = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manufacturer_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MediaFile_Tag_Mapping",
                columns: table => new
                {
                    MediaFile_Id = table.Column<int>(type: "int", nullable: false),
                    MediaTag_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile_Tag_Mapping", x => new { x.MediaFile_Id, x.MediaTag_Id });
                    table.ForeignKey(
                        name: "FK_dbo.MediaFile_Tag_Mapping_dbo.MediaFile_MediaFile_Id",
                        column: x => x.MediaFile_Id,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.MediaFile_Tag_Mapping_dbo.MediaTag_MediaTag_Id",
                        column: x => x.MediaTag_Id,
                        principalTable: "MediaTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaTrack",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MediaFileId = table.Column<int>(type: "int", nullable: false),
                    Album = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: false),
                    Property = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTrack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaTrack_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueuedEmailAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QueuedEmailId = table.Column<int>(type: "int", nullable: false),
                    StorageLocation = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    MediaFileId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    MimeType = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: false),
                    MediaStorageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedEmailAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueuedEmailAttachment_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QueuedEmailAttachment_MediaStorage_MediaStorageId",
                        column: x => x.MediaStorageId,
                        principalTable: "MediaStorage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QueuedEmailAttachment_QueuedEmail_QueuedEmailId",
                        column: x => x.QueuedEmailId,
                        principalTable: "QueuedEmail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductAttributeOptionsSetId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    Alias = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    MediaFileId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    PriceAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsPreSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ValueTypeId = table.Column<int>(type: "int", nullable: false),
                    LinkedProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeOption_ProductAttributeOptionsSet_ProductAtt~",
                        column: x => x.ProductAttributeOptionsSetId,
                        principalTable: "ProductAttributeOptionsSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Salutation = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Title = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    FirstName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    LastName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Email = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Company = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    StateProvinceId = table.Column<int>(type: "int", nullable: true),
                    City = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Address1 = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Address2 = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ZipPostalCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    PhoneNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    FaxNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Address_StateProvince_StateProvinceId",
                        column: x => x.StateProvinceId,
                        principalTable: "StateProvince",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Discount_AppliedToCategories",
                columns: table => new
                {
                    Discount_Id = table.Column<int>(type: "int", nullable: false),
                    Category_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount_AppliedToCategories", x => new { x.Discount_Id, x.Category_Id });
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToCategories_dbo.Category_Category_Id",
                        column: x => x.Category_Id,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToCategories_dbo.Discount_Discount_Id",
                        column: x => x.Discount_Id,
                        principalTable: "Discount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet_Category_Mapping",
                columns: table => new
                {
                    Category_Id = table.Column<int>(type: "int", nullable: false),
                    RuleSetEntity_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet_Category_Mapping", x => new { x.Category_Id, x.RuleSetEntity_Id });
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_Category_Mapping_dbo.Category_Category_Id",
                        column: x => x.Category_Id,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.RuleSet_Category_Mapping_dbo.RuleSet_RuleSetEntity_Id",
                        column: x => x.RuleSetEntity_Id,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    ParentGroupedProductId = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    VisibleIndividually = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    ShortDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    FullDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    AdminComment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ProductTemplateId = table.Column<int>(type: "int", nullable: false),
                    ShowOnHomePage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HomePageDisplayOrder = table.Column<int>(type: "int", nullable: false),
                    MetaKeywords = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    MetaDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    MetaTitle = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    AllowCustomerReviews = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ApprovedRatingSum = table.Column<int>(type: "int", nullable: false),
                    NotApprovedRatingSum = table.Column<int>(type: "int", nullable: false),
                    ApprovedTotalReviews = table.Column<int>(type: "int", nullable: false),
                    NotApprovedTotalReviews = table.Column<int>(type: "int", nullable: false),
                    SubjectToAcl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LimitedToStores = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Sku = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Gtin = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    IsGiftCard = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GiftCardTypeId = table.Column<int>(type: "int", nullable: false),
                    RequireOtherProducts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiredProductIds = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    AutomaticallyAddRequiredProducts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDownload = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DownloadId = table.Column<int>(type: "int", nullable: false),
                    UnlimitedDownloads = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxNumberOfDownloads = table.Column<int>(type: "int", nullable: false),
                    DownloadExpirationDays = table.Column<int>(type: "int", nullable: true),
                    DownloadActivationTypeId = table.Column<int>(type: "int", nullable: false),
                    HasSampleDownload = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SampleDownloadId = table.Column<int>(type: "int", nullable: true),
                    HasUserAgreement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserAgreementText = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    IsRecurring = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecurringCycleLength = table.Column<int>(type: "int", nullable: false),
                    RecurringCyclePeriodId = table.Column<int>(type: "int", nullable: false),
                    RecurringTotalCycles = table.Column<int>(type: "int", nullable: false),
                    IsShipEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsFreeShipping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AdditionalShippingCharge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsTaxExempt = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsEsd = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TaxCategoryId = table.Column<int>(type: "int", nullable: false),
                    ManageInventoryMethodId = table.Column<int>(type: "int", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    DisplayStockAvailability = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayStockQuantity = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MinStockQuantity = table.Column<int>(type: "int", nullable: false),
                    LowStockActivityId = table.Column<int>(type: "int", nullable: false),
                    NotifyAdminForQuantityBelow = table.Column<int>(type: "int", nullable: false),
                    BackorderModeId = table.Column<int>(type: "int", nullable: false),
                    AllowBackInStockSubscriptions = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OrderMinimumQuantity = table.Column<int>(type: "int", nullable: false),
                    OrderMaximumQuantity = table.Column<int>(type: "int", nullable: false),
                    QuantityStep = table.Column<int>(type: "int", nullable: false),
                    QuantiyControlType = table.Column<int>(type: "int", nullable: false),
                    HideQuantityControl = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowedQuantities = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    DisableBuyButton = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisableWishlistButton = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AvailableForPreOrder = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CallForPrice = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SpecialPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SpecialPriceStartDateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SpecialPriceEndDateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CustomerEntersPrice = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MinimumCustomerEnteredPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaximumCustomerEnteredPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HasTierPrices = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LowestAttributeCombinationPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AttributeChoiceBehaviour = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Length = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AvailableStartDateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AvailableEndDateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystemProduct = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SystemName = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeliveryTimeId = table.Column<int>(type: "int", nullable: true),
                    QuantityUnitId = table.Column<int>(type: "int", nullable: true),
                    CustomsTariffNumber = table.Column<string>(type: "varchar(30) CHARACTER SET utf8mb4", maxLength: 30, nullable: true),
                    CountryOfOriginId = table.Column<int>(type: "int", nullable: true),
                    BasePriceEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BasePriceMeasureUnit = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    BasePriceAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BasePriceBaseAmount = table.Column<int>(type: "int", nullable: true),
                    BundleTitleText = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    BundlePerItemShipping = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BundlePerItemPricing = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BundlePerItemShoppingCart = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MainPictureId = table.Column<int>(type: "int", nullable: true),
                    HasPreviewPicture = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HasDiscountsApplied = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Country_CountryOfOriginId",
                        column: x => x.CountryOfOriginId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Product_DeliveryTime_DeliveryTimeId",
                        column: x => x.DeliveryTimeId,
                        principalTable: "DeliveryTime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Product_Download_SampleDownloadId",
                        column: x => x.SampleDownloadId,
                        principalTable: "Download",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Product_QuantityUnit_QuantityUnitId",
                        column: x => x.QuantityUnitId,
                        principalTable: "QuantityUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Discount_AppliedToManufacturers",
                columns: table => new
                {
                    Discount_Id = table.Column<int>(type: "int", nullable: false),
                    Manufacturer_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount_AppliedToManufacturers", x => new { x.Discount_Id, x.Manufacturer_Id });
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToManufacturers_dbo.Discount_Discount_Id",
                        column: x => x.Discount_Id,
                        principalTable: "Discount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToManufacturers_dbo.Manufacturer_Manufacturer_Id",
                        column: x => x.Manufacturer_Id,
                        principalTable: "Manufacturer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Affiliate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddressId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affiliate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Affiliate_Address_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    Username = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    Password = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    PasswordFormatId = table.Column<int>(type: "int", nullable: false),
                    PasswordSalt = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    AdminComment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    IsTaxExempt = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AffiliateId = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystemAccount = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SystemName = table.Column<string>(type: "varchar(500) CHARACTER SET utf8mb4", maxLength: 500, nullable: true),
                    LastIpAddress = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastActivityDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Salutation = table.Column<string>(type: "varchar(50) CHARACTER SET utf8mb4", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "varchar(225) CHARACTER SET utf8mb4", maxLength: 225, nullable: true),
                    LastName = table.Column<string>(type: "varchar(225) CHARACTER SET utf8mb4", maxLength: 225, nullable: true),
                    FullName = table.Column<string>(type: "varchar(450) CHARACTER SET utf8mb4", maxLength: 450, nullable: true),
                    Company = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4", maxLength: 255, nullable: true),
                    CustomerNumber = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Gender = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    VatNumberStatusId = table.Column<int>(type: "int", nullable: false),
                    TimeZoneId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    TaxDisplayTypeId = table.Column<int>(type: "int", nullable: false),
                    LastForumVisit = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUserAgent = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    LastUserDeviceType = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    BillingAddress_Id = table.Column<int>(type: "int", nullable: true),
                    ShippingAddress_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_Address_BillingAddress_Id",
                        column: x => x.BillingAddress_Id,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customer_Address_ShippingAddress_Id",
                        column: x => x.ShippingAddress_Id,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Discount_AppliedToProducts",
                columns: table => new
                {
                    Discount_Id = table.Column<int>(type: "int", nullable: false),
                    Product_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount_AppliedToProducts", x => new { x.Discount_Id, x.Product_Id });
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToProducts_dbo.Discount_Discount_Id",
                        column: x => x.Discount_Id,
                        principalTable: "Discount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.Discount_AppliedToProducts_dbo.Product_Product_Id",
                        column: x => x.Product_Id,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_Category_Mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    IsFeaturedProduct = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystemMapping = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Category_Mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Category_Mapping_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_Category_Mapping_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product_Manufacturer_Mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ManufacturerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    IsFeaturedProduct = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_Manufacturer_Mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Manufacturer_Mapping_Manufacturer_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_Manufacturer_Mapping_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product_MediaFile_Mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MediaFileId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_MediaFile_Mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_MediaFile_Mapping_MediaFile_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_MediaFile_Mapping_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_ProductAttribute_Mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductAttributeId = table.Column<int>(type: "int", nullable: false),
                    TextPrompt = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CustomData = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttributeControlTypeId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_ProductAttribute_Mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_ProductAttribute_Mapping_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_ProductAttribute_Mapping_ProductAttribute_ProductAtt~",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttribute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_ProductTag_Mapping",
                columns: table => new
                {
                    Product_Id = table.Column<int>(type: "int", nullable: false),
                    ProductTag_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_ProductTag_Mapping", x => new { x.Product_Id, x.ProductTag_Id });
                    table.ForeignKey(
                        name: "FK_dbo.Product_ProductTag_Mapping_dbo.Product_Product_Id",
                        column: x => x.Product_Id,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.Product_ProductTag_Mapping_dbo.ProductTag_ProductTag_Id",
                        column: x => x.ProductTag_Id,
                        principalTable: "ProductTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product_SpecificationAttribute_Mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SpecificationAttributeOptionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AllowFiltering = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ShowOnProductPage = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product_SpecificationAttribute_Mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_SpecificationAttribute_Mapping_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_SpecificationAttribute_Mapping_SpecificationAttribut~",
                        column: x => x.SpecificationAttributeOptionId,
                        principalTable: "SpecificationAttributeOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBundleItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BundleProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DiscountPercentage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ShortDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    FilterAttributes = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HideThumbnail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Visible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Published = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBundleItem_Product_BundleProductId",
                        column: x => x.BundleProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductBundleItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeCombination",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Gtin = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Length = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Width = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BasePriceAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BasePriceBaseAmount = table.Column<int>(type: "int", nullable: true),
                    AssignedMediaFileIds = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeliveryTimeId = table.Column<int>(type: "int", nullable: true),
                    QuantityUnitId = table.Column<int>(type: "int", nullable: true),
                    AttributesXml = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    AllowOutOfStockOrders = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeCombination", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeCombination_DeliveryTime_DeliveryTime~",
                        column: x => x.DeliveryTimeId,
                        principalTable: "DeliveryTime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeCombination_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeCombination_QuantityUnit_QuantityUnit~",
                        column: x => x.QuantityUnitId,
                        principalTable: "QuantityUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TierPrice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CalculationMethod = table.Column<int>(type: "int", nullable: false),
                    CustomerRoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierPrice_CustomerRole_CustomerRoleId",
                        column: x => x.CustomerRoleId,
                        principalTable: "CustomerRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TierPrice_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ActivityLogTypeId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLog_ActivityLogType_ActivityLogTypeId",
                        column: x => x.ActivityLogTypeId,
                        principalTable: "ActivityLogType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityLog_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BackInStockSubscription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackInStockSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackInStockSubscription_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackInStockSubscription_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                columns: table => new
                {
                    Customer_Id = table.Column<int>(type: "int", nullable: false),
                    Address_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => new { x.Customer_Id, x.Address_Id });
                    table.ForeignKey(
                        name: "FK_dbo.CustomerAddresses_dbo.Address_Address_Id",
                        column: x => x.Address_Id,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id",
                        column: x => x.Customer_Id,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContent_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CustomerRoleId = table.Column<int>(type: "int", nullable: false),
                    IsSystemMapping = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerRoleMapping_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerRoleMapping_CustomerRole_CustomerRoleId",
                        column: x => x.CustomerRoleId,
                        principalTable: "CustomerRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthenticationRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ExternalIdentifier = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ExternalDisplayIdentifier = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    OAuthToken = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    OAuthAccessToken = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ProviderSystemName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthenticationRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalAuthenticationRecord_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LogLevelId = table.Column<int>(type: "int", nullable: false),
                    ShortMessage = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    FullMessage = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(200) CHARACTER SET utf8mb4", maxLength: 200, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    PageUrl = table.Column<string>(type: "varchar(1500) CHARACTER SET utf8mb4", maxLength: 1500, nullable: true),
                    ReferrerUrl = table.Column<string>(type: "varchar(1500) CHARACTER SET utf8mb4", maxLength: 1500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Logger = table.Column<string>(type: "varchar(400) CHARACTER SET utf8mb4", maxLength: 400, nullable: false),
                    HttpMethod = table.Column<string>(type: "varchar(10) CHARACTER SET utf8mb4", maxLength: 10, nullable: true),
                    UserName = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Log_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    OrderGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    BillingAddressId = table.Column<int>(type: "int", nullable: false),
                    ShippingAddressId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodSystemName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CustomerCurrencyCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CurrencyRate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    VatNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    OrderSubtotalInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderSubtotalExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderSubTotalDiscountInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderSubTotalDiscountExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderShippingInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderShippingExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderShippingTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentMethodAdditionalFeeInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentMethodAdditionalFeeExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentMethodAdditionalFeeTaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxRates = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    OrderTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderDiscount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreditBalance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderTotalRounding = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RewardPointsWereAdded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CheckoutAttributeDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CheckoutAttributesXml = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CustomerLanguageId = table.Column<int>(type: "int", nullable: false),
                    AffiliateId = table.Column<int>(type: "int", nullable: false),
                    CustomerIp = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    AllowStoringCreditCardNumber = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CardType = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CardName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CardNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    MaskedCreditCardNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CardCvv2 = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CardExpirationMonth = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CardExpirationYear = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    AllowStoringDirectDebit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DirectDebitAccountHolder = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitAccountNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitBankCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitBankName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitBIC = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitCountry = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    DirectDebitIban = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CustomerOrderComment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    AuthorizationTransactionId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    AuthorizationTransactionCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    AuthorizationTransactionResult = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CaptureTransactionId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CaptureTransactionResult = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    SubscriptionTransactionId = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    PaidDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ShippingMethod = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ShippingRateComputationMethodSystemName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RewardPointsRemaining = table.Column<int>(type: "int", nullable: true),
                    HasNewPaymentNotification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AcceptThirdPartyEmailHandOver = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OrderStatusId = table.Column<int>(type: "int", nullable: false),
                    PaymentStatusId = table.Column<int>(type: "int", nullable: false),
                    ShippingStatusId = table.Column<int>(type: "int", nullable: false),
                    CustomerTaxDisplayTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_Address_BillingAddressId",
                        column: x => x.BillingAddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Order_Address_ShippingAddressId",
                        column: x => x.ShippingAddressId,
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Order_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReasonForReturn = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    RequestedAction = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: false),
                    RequestedActionUpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CustomerComments = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    StaffNotes = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    AdminComment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ReturnRequestStatusId = table.Column<int>(type: "int", nullable: false),
                    RefundToWallet = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequest_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductVariantAttributeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    Alias = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    MediaFileId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "varchar(100) CHARACTER SET utf8mb4", maxLength: 100, nullable: true),
                    PriceAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightAdjustment = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsPreSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ValueTypeId = table.Column<int>(type: "int", nullable: false),
                    LinkedProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeValue_Product_ProductAttribute_Mappin~",
                        column: x => x.ProductVariantAttributeId,
                        principalTable: "Product_ProductAttribute_Mapping",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBundleItemAttributeFilter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BundleItemId = table.Column<int>(type: "int", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    AttributeValueId = table.Column<int>(type: "int", nullable: false),
                    IsPreSelected = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundleItemAttributeFilter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBundleItemAttributeFilter_ProductBundleItem_BundleIte~",
                        column: x => x.BundleItemId,
                        principalTable: "ProductBundleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingCartItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    ParentItemId = table.Column<int>(type: "int", nullable: true),
                    BundleItemId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AttributesXml = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    CustomerEnteredPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ShoppingCartTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCartItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingCartItem_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingCartItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShoppingCartItem_ProductBundleItem_BundleItemId",
                        column: x => x.BundleItemId,
                        principalTable: "ProductBundleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductReview",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    ReviewText = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    HelpfulYesTotal = table.Column<int>(type: "int", nullable: false),
                    HelpfulNoTotal = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReview_CustomerContent_Id",
                        column: x => x.Id,
                        principalTable: "CustomerContent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductReview_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountUsageHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DiscountId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountUsageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountUsageHistory_Discount_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountUsageHistory_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderItemGuid = table.Column<Guid>(type: "char(36)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmountInclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmountExclTax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AttributeDescription = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    AttributesXml = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    IsDownloadActivated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LicenseDownloadId = table.Column<int>(type: "int", nullable: true),
                    ItemWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BundleData = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    ProductCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DeliveryTimeId = table.Column<int>(type: "int", nullable: true),
                    DisplayDeliveryTime = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderNote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: false),
                    DisplayToCustomer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderNote_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringPayment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CycleLength = table.Column<int>(type: "int", nullable: false),
                    CyclePeriodId = table.Column<int>(type: "int", nullable: false),
                    TotalCycles = table.Column<int>(type: "int", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InitialOrderId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPayment_Order_InitialOrderId",
                        column: x => x.InitialOrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardPointsHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PointsBalance = table.Column<int>(type: "int", nullable: false),
                    UsedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Message = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedWithOrder_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardPointsHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardPointsHistory_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RewardPointsHistory_Order_UsedWithOrder_Id",
                        column: x => x.UsedWithOrder_Id,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    TrackingNumber = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    TrackingUrl = table.Column<string>(type: "varchar(2000) CHARACTER SET utf8mb4", maxLength: 2000, nullable: true),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ShippedDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeliveryDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipment_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AmountBalance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AmountBalancePerStore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "varchar(1000) CHARACTER SET utf8mb4", maxLength: 1000, nullable: true),
                    AdminComment = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletHistory_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WalletHistory_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductReviewHelpfulness",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductReviewId = table.Column<int>(type: "int", nullable: false),
                    WasHelpful = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviewHelpfulness", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviewHelpfulness_CustomerContent_Id",
                        column: x => x.Id,
                        principalTable: "CustomerContent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductReviewHelpfulness_ProductReview_ProductReviewId",
                        column: x => x.ProductReviewId,
                        principalTable: "ProductReview",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftCard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GiftCardTypeId = table.Column<int>(type: "int", nullable: false),
                    PurchasedWithOrderItemId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsGiftCardActivated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GiftCardCouponCode = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    RecipientName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    RecipientEmail = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    SenderName = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    SenderEmail = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    Message = table.Column<string>(type: "longtext CHARACTER SET utf8mb4", nullable: true),
                    IsRecipientNotified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCard_OrderItem_PurchasedWithOrderItemId",
                        column: x => x.PurchasedWithOrderItemId,
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringPaymentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecurringPaymentId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPaymentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPaymentHistory_RecurringPayment_RecurringPaymentId",
                        column: x => x.RecurringPaymentId,
                        principalTable: "RecurringPayment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentItem_Shipment_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardUsageHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GiftCardId = table.Column<int>(type: "int", nullable: false),
                    UsedWithOrderId = table.Column<int>(type: "int", nullable: false),
                    UsedValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardUsageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCardUsageHistory_GiftCard_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiftCardUsageHistory_Order_UsedWithOrderId",
                        column: x => x.UsedWithOrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AclRecord_CustomerRoleId",
                table: "AclRecord",
                column: "CustomerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AclRecord_EntityId_EntityName",
                table: "AclRecord",
                columns: new[] { "EntityId", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_AclRecord_IsIdle",
                table: "AclRecord",
                column: "IsIdle");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_ActivityLogTypeId",
                table: "ActivityLog",
                column: "ActivityLogTypeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_CreatedOnUtc",
                table: "ActivityLog",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_CustomerId",
                table: "ActivityLog",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Address_CountryId",
                table: "Address",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateProvinceId",
                table: "Address",
                column: "StateProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Affiliate_AddressId",
                table: "Affiliate",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_BackInStockSubscription_CustomerId",
                table: "BackInStockSubscription",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BackInStockSubscription_ProductId",
                table: "BackInStockSubscription",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_DisplayOrder",
                table: "Category",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Category_LimitedToStores",
                table: "Category",
                column: "LimitedToStores");

            migrationBuilder.CreateIndex(
                name: "IX_Category_MediaFileId",
                table: "Category",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentCategoryId",
                table: "Category",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_SubjectToAcl",
                table: "Category",
                column: "SubjectToAcl");

            migrationBuilder.CreateIndex(
                name: "IX_Deleted1",
                table: "Category",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutAttributeValue_CheckoutAttributeId",
                table: "CheckoutAttributeValue",
                column: "CheckoutAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutAttributeValue_MediaFileId",
                table: "CheckoutAttributeValue",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Country_DefaultCurrencyId",
                table: "Country",
                column: "DefaultCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Country_DisplayOrder",
                table: "Country",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Currency_DisplayOrder",
                table: "Currency",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_BillingAddress_Id",
                table: "Customer",
                column: "BillingAddress_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_BirthDate",
                table: "Customer",
                column: "BirthDate");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Company",
                table: "Customer",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CreatedOn",
                table: "Customer",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerGuid",
                table: "Customer",
                column: "CustomerGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CustomerNumber",
                table: "Customer",
                column: "CustomerNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Deleted_IsSystemAccount",
                table: "Customer",
                columns: new[] { "Deleted", "IsSystemAccount" });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Email",
                table: "Customer",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_FullName",
                table: "Customer",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_LastActivity",
                table: "Customer",
                column: "LastActivityDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_LastIpAddress",
                table: "Customer",
                column: "LastIpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_ShippingAddress_Id",
                table: "Customer",
                column: "ShippingAddress_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Username",
                table: "Customer",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Deleted4",
                table: "Customer",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_IsSystemAccount",
                table: "Customer",
                column: "IsSystemAccount");

            migrationBuilder.CreateIndex(
                name: "IX_SystemName",
                table: "Customer",
                column: "SystemName");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_Address_Id",
                table: "CustomerAddresses",
                column: "Address_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContent_CustomerId",
                table: "CustomerContent",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Active",
                table: "CustomerRole",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRole_SystemName_IsSystemRole",
                table: "CustomerRole",
                columns: new[] { "SystemName", "IsSystemRole" });

            migrationBuilder.CreateIndex(
                name: "IX_IsSystemRole",
                table: "CustomerRole",
                column: "IsSystemRole");

            migrationBuilder.CreateIndex(
                name: "IX_SystemName1",
                table: "CustomerRole",
                column: "SystemName");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRoleMapping_CustomerId",
                table: "CustomerRoleMapping",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRoleMapping_CustomerRoleId",
                table: "CustomerRoleMapping",
                column: "CustomerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_IsSystemMapping1",
                table: "CustomerRoleMapping",
                column: "IsSystemMapping");

            migrationBuilder.CreateIndex(
                name: "IX_Discount_AppliedToCategories_Category_Id",
                table: "Discount_AppliedToCategories",
                column: "Category_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Discount_AppliedToManufacturers_Manufacturer_Id",
                table: "Discount_AppliedToManufacturers",
                column: "Manufacturer_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Discount_AppliedToProducts_Product_Id",
                table: "Discount_AppliedToProducts",
                column: "Product_Id");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsageHistory_DiscountId",
                table: "DiscountUsageHistory",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsageHistory_OrderId",
                table: "DiscountUsageHistory",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Download_MediaFileId",
                table: "Download",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadGuid",
                table: "Download",
                column: "DownloadGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntityId_EntityName",
                table: "Download",
                columns: new[] { "EntityId", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_UpdatedOn_IsTransient",
                table: "Download",
                columns: new[] { "UpdatedOnUtc", "IsTransient" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthenticationRecord_CustomerId",
                table: "ExternalAuthenticationRecord",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GenericAttribute_EntityId_and_KeyGroup",
                table: "GenericAttribute",
                columns: new[] { "EntityId", "KeyGroup" });

            migrationBuilder.CreateIndex(
                name: "IX_GenericAttribute_Key",
                table: "GenericAttribute",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_PurchasedWithOrderItemId",
                table: "GiftCard",
                column: "PurchasedWithOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardUsageHistory_GiftCardId",
                table: "GiftCardUsageHistory",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardUsageHistory_UsedWithOrderId",
                table: "GiftCardUsageHistory",
                column: "UsedWithOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Language_DisplayOrder",
                table: "Language",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_LocaleStringResource",
                table: "LocaleStringResource",
                columns: new[] { "ResourceName", "LanguageId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocaleStringResource_LanguageId",
                table: "LocaleStringResource",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedProperty_Compound",
                table: "LocalizedProperty",
                columns: new[] { "EntityId", "LocaleKey", "LocaleKeyGroup", "LanguageId" });

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedProperty_LanguageId",
                table: "LocalizedProperty",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedProperty_LocaleKeyGroup",
                table: "LocalizedProperty",
                column: "LocaleKeyGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Log_CreatedOnUtc",
                table: "Log",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Log_CustomerId",
                table: "Log",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Log_Level",
                table: "Log",
                column: "LogLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Log_Logger",
                table: "Log",
                column: "Logger");

            migrationBuilder.CreateIndex(
                name: "IX_Deleted",
                table: "Manufacturer",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturer_DisplayOrder",
                table: "Manufacturer",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturer_LimitedToStores",
                table: "Manufacturer",
                column: "LimitedToStores");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturer_MediaFileId",
                table: "Manufacturer",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectToAcl",
                table: "Manufacturer",
                column: "SubjectToAcl");

            migrationBuilder.CreateIndex(
                name: "IX_Media_Extension",
                table: "MediaFile",
                columns: new[] { "FolderId", "Extension", "PixelSize", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_FolderId",
                table: "MediaFile",
                columns: new[] { "FolderId", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_MediaType",
                table: "MediaFile",
                columns: new[] { "FolderId", "MediaType", "Extension", "PixelSize", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_Name",
                table: "MediaFile",
                columns: new[] { "FolderId", "Name", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_PixelSize",
                table: "MediaFile",
                columns: new[] { "FolderId", "PixelSize", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_Size",
                table: "MediaFile",
                columns: new[] { "FolderId", "Size", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Media_UpdatedOnUtc",
                table: "MediaFile",
                columns: new[] { "FolderId", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_MediaStorageId",
                table: "MediaFile",
                column: "MediaStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_Tag_Mapping_MediaTag_Id",
                table: "MediaFile_Tag_Mapping",
                column: "MediaTag_Id");

            migrationBuilder.CreateIndex(
                name: "IX_NameParentId",
                table: "MediaFolder",
                columns: new[] { "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaTag_Name",
                table: "MediaTag",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Album",
                table: "MediaTrack",
                column: "Album");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTrack_Composite",
                table: "MediaTrack",
                columns: new[] { "MediaFileId", "EntityId", "EntityName", "Property" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_DisplayOrder",
                table: "MenuItemRecord",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_LimitedToStores",
                table: "MenuItemRecord",
                column: "LimitedToStores");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_ParentItemId",
                table: "MenuItemRecord",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_Published",
                table: "MenuItemRecord",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_SubjectToAcl",
                table: "MenuItemRecord",
                column: "SubjectToAcl");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecord_MenuId",
                table: "MenuItemRecord",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_LimitedToStores",
                table: "MenuRecord",
                column: "LimitedToStores");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Published",
                table: "MenuRecord",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_SubjectToAcl",
                table: "MenuRecord",
                column: "SubjectToAcl");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_SystemName_IsSystemMenu",
                table: "MenuRecord",
                columns: new[] { "SystemName", "IsSystemMenu" });

            migrationBuilder.CreateIndex(
                name: "IX_Active1",
                table: "NewsLetterSubscription",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscription_Email_StoreId",
                table: "NewsLetterSubscription",
                columns: new[] { "Email", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deleted3",
                table: "Order",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Order_BillingAddressId",
                table: "Order",
                column: "BillingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_CustomerId",
                table: "Order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ShippingAddressId",
                table: "Order",
                column: "ShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ProductId",
                table: "OrderItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNote_OrderId",
                table: "OrderNote",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemName2",
                table: "PermissionRecord",
                column: "SystemName");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRoleMapping_CustomerRoleId",
                table: "PermissionRoleMapping",
                column: "CustomerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRoleMapping_PermissionRecordId",
                table: "PermissionRoleMapping",
                column: "PermissionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Deleted2",
                table: "Product",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Gtin1",
                table: "Product",
                column: "Gtin");

            migrationBuilder.CreateIndex(
                name: "IX_IsSystemProduct",
                table: "Product",
                column: "IsSystemProduct");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerPartNumber1",
                table: "Product",
                column: "ManufacturerPartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CountryOfOriginId",
                table: "Product",
                column: "CountryOfOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_DeliveryTimeId",
                table: "Product",
                column: "DeliveryTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_LimitedToStores",
                table: "Product",
                column: "LimitedToStores");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Name",
                table: "Product",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ParentGroupedProductId",
                table: "Product",
                column: "ParentGroupedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_PriceDatesEtc",
                table: "Product",
                columns: new[] { "Price", "AvailableStartDateTimeUtc", "AvailableEndDateTimeUtc", "Published", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_Published",
                table: "Product",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Published_Deleted_IsSystemProduct",
                table: "Product",
                columns: new[] { "Published", "Deleted", "IsSystemProduct" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_QuantityUnitId",
                table: "Product",
                column: "QuantityUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SampleDownloadId",
                table: "Product",
                column: "SampleDownloadId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ShowOnHomepage",
                table: "Product",
                column: "ShowOnHomePage");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Sku",
                table: "Product",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SubjectToAcl",
                table: "Product",
                column: "SubjectToAcl");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SystemName_IsSystemProduct",
                table: "Product",
                columns: new[] { "SystemName", "IsSystemProduct" });

            migrationBuilder.CreateIndex(
                name: "IX_SeekExport1",
                table: "Product",
                columns: new[] { "Published", "Id", "Visibility", "Deleted", "IsSystemProduct", "AvailableStartDateTimeUtc", "AvailableEndDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Visibility",
                table: "Product",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_IsFeaturedProduct1",
                table: "Product_Category_Mapping",
                column: "IsFeaturedProduct");

            migrationBuilder.CreateIndex(
                name: "IX_IsSystemMapping",
                table: "Product_Category_Mapping",
                column: "IsSystemMapping");

            migrationBuilder.CreateIndex(
                name: "IX_PCM_Product_and_Category",
                table: "Product_Category_Mapping",
                columns: new[] { "CategoryId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_Category_Mapping_ProductId",
                table: "Product_Category_Mapping",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_IsFeaturedProduct",
                table: "Product_Manufacturer_Mapping",
                column: "IsFeaturedProduct");

            migrationBuilder.CreateIndex(
                name: "IX_PMM_Product_and_Manufacturer",
                table: "Product_Manufacturer_Mapping",
                columns: new[] { "ManufacturerId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_Manufacturer_Mapping_ProductId",
                table: "Product_Manufacturer_Mapping",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_MediaFile_Mapping_MediaFileId",
                table: "Product_MediaFile_Mapping",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_MediaFile_Mapping_ProductId",
                table: "Product_MediaFile_Mapping",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeControlTypeId",
                table: "Product_ProductAttribute_Mapping",
                column: "AttributeControlTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductAttribute_Mapping_ProductAttributeId",
                table: "Product_ProductAttribute_Mapping",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder",
                table: "Product_ProductAttribute_Mapping",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductTag_Mapping_ProductTag_Id",
                table: "Product_ProductTag_Mapping",
                column: "ProductTag_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SpecificationAttribute_Mapping_ProductId",
                table: "Product_SpecificationAttribute_Mapping",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SpecificationAttribute_Mapping_SpecificationAttribut~",
                table: "Product_SpecificationAttribute_Mapping",
                column: "SpecificationAttributeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AllowFiltering",
                table: "ProductAttribute",
                column: "AllowFiltering");

            migrationBuilder.CreateIndex(
                name: "IX_DisplayOrder",
                table: "ProductAttribute",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeOption_ProductAttributeOptionsSetId",
                table: "ProductAttributeOption",
                column: "ProductAttributeOptionsSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeOptionsSet_ProductAttributeId",
                table: "ProductAttributeOptionsSet",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItem_BundleProductId",
                table: "ProductBundleItem",
                column: "BundleProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItem_ProductId",
                table: "ProductBundleItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItemAttributeFilter_BundleItemId",
                table: "ProductBundleItemAttributeFilter",
                column: "BundleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReview_ProductId",
                table: "ProductReview",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewHelpfulness_ProductReviewId",
                table: "ProductReviewHelpfulness",
                column: "ProductReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTag_Name",
                table: "ProductTag",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTag_Published",
                table: "ProductTag",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Gtin",
                table: "ProductVariantAttributeCombination",
                column: "Gtin");

            migrationBuilder.CreateIndex(
                name: "IX_IsActive",
                table: "ProductVariantAttributeCombination",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturerPartNumber",
                table: "ProductVariantAttributeCombination",
                column: "ManufacturerPartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeCombination_DeliveryTimeId",
                table: "ProductVariantAttributeCombination",
                column: "DeliveryTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeCombination_ProductId",
                table: "ProductVariantAttributeCombination",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeCombination_QuantityUnitId",
                table: "ProductVariantAttributeCombination",
                column: "QuantityUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeCombination_SKU",
                table: "ProductVariantAttributeCombination",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "IX_StockQuantity_AllowOutOfStockOrders",
                table: "ProductVariantAttributeCombination",
                columns: new[] { "StockQuantity", "AllowOutOfStockOrders" });

            migrationBuilder.CreateIndex(
                name: "IX_Name",
                table: "ProductVariantAttributeValue",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeValue_ProductVariantAttributeId_DisplayOrder",
                table: "ProductVariantAttributeValue",
                columns: new[] { "ProductVariantAttributeId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ValueTypeId",
                table: "ProductVariantAttributeValue",
                column: "ValueTypeId");

            migrationBuilder.CreateIndex(
                name: "[IX_QueuedEmail_CreatedOnUtc]",
                table: "QueuedEmail",
                column: "CreatedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAccountId",
                table: "QueuedEmail",
                column: "EmailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFileId",
                table: "QueuedEmailAttachment",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaStorageId",
                table: "QueuedEmailAttachment",
                column: "MediaStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_QueuedEmailId",
                table: "QueuedEmailAttachment",
                column: "QueuedEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayment_InitialOrderId",
                table: "RecurringPayment",
                column: "InitialOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPaymentHistory_RecurringPaymentId",
                table: "RecurringPaymentHistory",
                column: "RecurringPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedProduct_ProductId1",
                table: "RelatedProduct",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequest_CustomerId",
                table: "ReturnRequest",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsHistory_CustomerId",
                table: "RewardPointsHistory",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsHistory_UsedWithOrder_Id",
                table: "RewardPointsHistory",
                column: "UsedWithOrder_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_DisplayOrder",
                table: "Rule",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PageBuilder_RuleType",
                table: "Rule",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_Rule_RuleSetId",
                table: "Rule",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_IsSubGroup",
                table: "RuleSet",
                column: "IsSubGroup");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSetEntity_Scope",
                table: "RuleSet",
                columns: new[] { "IsActive", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_Category_Mapping_RuleSetEntity_Id",
                table: "RuleSet_Category_Mapping",
                column: "RuleSetEntity_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_CustomerRole_Mapping_RuleSetEntity_Id",
                table: "RuleSet_CustomerRole_Mapping",
                column: "RuleSetEntity_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_Discount_Mapping_RuleSetEntity_Id",
                table: "RuleSet_Discount_Mapping",
                column: "RuleSetEntity_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_PaymentMethod_Mapping_RuleSetEntity_Id",
                table: "RuleSet_PaymentMethod_Mapping",
                column: "RuleSetEntity_Id");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSet_ShippingMethod_Mapping_RuleSetEntity_Id",
                table: "RuleSet_ShippingMethod_Mapping",
                column: "RuleSetEntity_Id");

            migrationBuilder.CreateIndex(
                name: "IX_NextRun_Enabled",
                table: "ScheduleTask",
                columns: new[] { "NextRunUtc", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_Type",
                table: "ScheduleTask",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MachineName_IsRunning",
                table: "ScheduleTaskHistory",
                columns: new[] { "MachineName", "IsRunning" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTaskHistory_ScheduleTaskId",
                table: "ScheduleTaskHistory",
                column: "ScheduleTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Started_Finished",
                table: "ScheduleTaskHistory",
                columns: new[] { "StartedOnUtc", "FinishedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Setting_Name",
                table: "Setting",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Setting_StoreId",
                table: "Setting",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_OrderId",
                table: "Shipment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentItem_ShipmentId",
                table: "ShipmentItem",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItem_BundleItemId",
                table: "ShoppingCartItem",
                column: "BundleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItem_CustomerId",
                table: "ShoppingCartItem",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItem_ProductId",
                table: "ShoppingCartItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId",
                table: "ShoppingCartItem",
                columns: new[] { "ShoppingCartTypeId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllowFiltering1",
                table: "SpecificationAttribute",
                column: "AllowFiltering");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationAttributeOption_SpecificationAttributeId",
                table: "SpecificationAttributeOption",
                column: "SpecificationAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_StateProvince_CountryId",
                table: "StateProvince",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Store_PrimaryExchangeRateCurrencyId",
                table: "Store",
                column: "PrimaryExchangeRateCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Store_PrimaryStoreCurrencyId",
                table: "Store",
                column: "PrimaryStoreCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreMapping_EntityId_EntityName",
                table: "StoreMapping",
                columns: new[] { "EntityId", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_TierPrice_CustomerRoleId",
                table: "TierPrice",
                column: "CustomerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TierPrice_ProductId",
                table: "TierPrice",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlRecord_Slug",
                table: "UrlRecord",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreId_CreatedOn",
                table: "WalletHistory",
                columns: new[] { "StoreId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletHistory_CustomerId",
                table: "WalletHistory",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletHistory_OrderId",
                table: "WalletHistory",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AclRecord");

            migrationBuilder.DropTable(
                name: "ActivityLog");

            migrationBuilder.DropTable(
                name: "Affiliate");

            migrationBuilder.DropTable(
                name: "BackInStockSubscription");

            migrationBuilder.DropTable(
                name: "Campaign");

            migrationBuilder.DropTable(
                name: "CategoryTemplate");

            migrationBuilder.DropTable(
                name: "CheckoutAttributeValue");

            migrationBuilder.DropTable(
                name: "CrossSellProduct");

            migrationBuilder.DropTable(
                name: "CustomerAddresses");

            migrationBuilder.DropTable(
                name: "CustomerRoleMapping");

            migrationBuilder.DropTable(
                name: "Discount_AppliedToCategories");

            migrationBuilder.DropTable(
                name: "Discount_AppliedToManufacturers");

            migrationBuilder.DropTable(
                name: "Discount_AppliedToProducts");

            migrationBuilder.DropTable(
                name: "DiscountUsageHistory");

            migrationBuilder.DropTable(
                name: "ExternalAuthenticationRecord");

            migrationBuilder.DropTable(
                name: "GenericAttribute");

            migrationBuilder.DropTable(
                name: "GiftCardUsageHistory");

            migrationBuilder.DropTable(
                name: "LocaleStringResource");

            migrationBuilder.DropTable(
                name: "LocalizedProperty");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "ManufacturerTemplate");

            migrationBuilder.DropTable(
                name: "MeasureDimension");

            migrationBuilder.DropTable(
                name: "MeasureWeight");

            migrationBuilder.DropTable(
                name: "MediaFile_Tag_Mapping");

            migrationBuilder.DropTable(
                name: "MediaTrack");

            migrationBuilder.DropTable(
                name: "MenuItemRecord");

            migrationBuilder.DropTable(
                name: "MessageTemplate");

            migrationBuilder.DropTable(
                name: "NamedEntity");

            migrationBuilder.DropTable(
                name: "NewsLetterSubscription");

            migrationBuilder.DropTable(
                name: "OrderNote");

            migrationBuilder.DropTable(
                name: "PermissionRoleMapping");

            migrationBuilder.DropTable(
                name: "Product_Category_Mapping");

            migrationBuilder.DropTable(
                name: "Product_Manufacturer_Mapping");

            migrationBuilder.DropTable(
                name: "Product_MediaFile_Mapping");

            migrationBuilder.DropTable(
                name: "Product_ProductTag_Mapping");

            migrationBuilder.DropTable(
                name: "Product_SpecificationAttribute_Mapping");

            migrationBuilder.DropTable(
                name: "ProductAttributeOption");

            migrationBuilder.DropTable(
                name: "ProductBundleItemAttributeFilter");

            migrationBuilder.DropTable(
                name: "ProductReviewHelpfulness");

            migrationBuilder.DropTable(
                name: "ProductTemplate");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeCombination");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeValue");

            migrationBuilder.DropTable(
                name: "QueuedEmailAttachment");

            migrationBuilder.DropTable(
                name: "RecurringPaymentHistory");

            migrationBuilder.DropTable(
                name: "RelatedProduct");

            migrationBuilder.DropTable(
                name: "ReturnRequest");

            migrationBuilder.DropTable(
                name: "RewardPointsHistory");

            migrationBuilder.DropTable(
                name: "Rule");

            migrationBuilder.DropTable(
                name: "RuleSet_Category_Mapping");

            migrationBuilder.DropTable(
                name: "RuleSet_CustomerRole_Mapping");

            migrationBuilder.DropTable(
                name: "RuleSet_Discount_Mapping");

            migrationBuilder.DropTable(
                name: "RuleSet_PaymentMethod_Mapping");

            migrationBuilder.DropTable(
                name: "RuleSet_ShippingMethod_Mapping");

            migrationBuilder.DropTable(
                name: "ScheduleTaskHistory");

            migrationBuilder.DropTable(
                name: "Setting");

            migrationBuilder.DropTable(
                name: "ShipmentItem");

            migrationBuilder.DropTable(
                name: "ShoppingCartItem");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "StoreMapping");

            migrationBuilder.DropTable(
                name: "TaxCategory");

            migrationBuilder.DropTable(
                name: "TierPrice");

            migrationBuilder.DropTable(
                name: "Topic");

            migrationBuilder.DropTable(
                name: "UrlRecord");

            migrationBuilder.DropTable(
                name: "WalletHistory");

            migrationBuilder.DropTable(
                name: "ActivityLogType");

            migrationBuilder.DropTable(
                name: "CheckoutAttribute");

            migrationBuilder.DropTable(
                name: "GiftCard");

            migrationBuilder.DropTable(
                name: "Language");

            migrationBuilder.DropTable(
                name: "MediaTag");

            migrationBuilder.DropTable(
                name: "MenuRecord");

            migrationBuilder.DropTable(
                name: "PermissionRecord");

            migrationBuilder.DropTable(
                name: "Manufacturer");

            migrationBuilder.DropTable(
                name: "ProductTag");

            migrationBuilder.DropTable(
                name: "SpecificationAttributeOption");

            migrationBuilder.DropTable(
                name: "ProductAttributeOptionsSet");

            migrationBuilder.DropTable(
                name: "ProductReview");

            migrationBuilder.DropTable(
                name: "Product_ProductAttribute_Mapping");

            migrationBuilder.DropTable(
                name: "QueuedEmail");

            migrationBuilder.DropTable(
                name: "RecurringPayment");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Discount");

            migrationBuilder.DropTable(
                name: "PaymentMethod");

            migrationBuilder.DropTable(
                name: "RuleSet");

            migrationBuilder.DropTable(
                name: "ShippingMethod");

            migrationBuilder.DropTable(
                name: "ScheduleTask");

            migrationBuilder.DropTable(
                name: "Shipment");

            migrationBuilder.DropTable(
                name: "ProductBundleItem");

            migrationBuilder.DropTable(
                name: "CustomerRole");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "SpecificationAttribute");

            migrationBuilder.DropTable(
                name: "CustomerContent");

            migrationBuilder.DropTable(
                name: "ProductAttribute");

            migrationBuilder.DropTable(
                name: "EmailAccount");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "DeliveryTime");

            migrationBuilder.DropTable(
                name: "Download");

            migrationBuilder.DropTable(
                name: "QuantityUnit");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "MediaFile");

            migrationBuilder.DropTable(
                name: "StateProvince");

            migrationBuilder.DropTable(
                name: "MediaFolder");

            migrationBuilder.DropTable(
                name: "MediaStorage");

            migrationBuilder.DropTable(
                name: "Country");

            migrationBuilder.DropTable(
                name: "Currency");
        }
    }
}
