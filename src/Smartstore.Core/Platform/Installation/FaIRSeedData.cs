using Smartstore.Caching.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Tasks;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media.Tasks;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Identity.Tasks;
using Smartstore.Core.Logging;
using Smartstore.Core.Logging.Tasks;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Tasks;
using Smartstore.Core.Rules;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Migrations;
using Smartstore.Scheduling;

namespace Smartstore.Core.Installation
{
    public class FaIRSeedData : InvariantSeedData
    {
        private readonly IDictionary<string, TaxCategory> _taxCategories = new Dictionary<string, TaxCategory>();
        private DeliveryTime _defaultDeliveryTime;

        protected override void Alter(Customer entity)
        {
            base.Alter(entity);

            if (entity.SystemName == SystemCustomerNames.Bot)
            {
                entity.AdminComment = "حساب مهمان سیستم برای درخواست های موتور جستجو.";
            }
            else if (entity.SystemName == SystemCustomerNames.BackgroundTask)
            {
                entity.AdminComment = "حساب سیستم برای کارهای برنامه ریزی شده.";
            }
            else if (entity.SystemName == SystemCustomerNames.PdfConverter)
            {
                entity.AdminComment = "حساب سیستم تبدیل پی دی اف.";
            }
            else if (entity.SystemName == SystemCustomerNames.WebhookClient)
            {
                entity.AdminComment = "حساب سیستم برای وب هوک کلاینت.";
            }
        }

        protected override void Alter(IList<MeasureDimension> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.SystemKeyword)
                .Alter("mm", x =>
                {
                    x.Name = "میلی متر";
                    x.Ratio = 0.001M;
                })
                .Alter("cm", x =>
                {
                    x.Name = "سانتی متر";
                    x.Ratio = 0.01M;
                })
                .Alter("m", x =>
                {
                    x.Name = "متر";
                    x.Ratio = 1M;
                })
                .Alter("inch", x =>
                {
                    x.Name = "اینچ";
                    x.Ratio = 0.0254M;
                })
                .Alter("ft", x =>
                {
                    x.Name = "فوت";
                    x.Ratio = 0.3048M;
                });
        }

        protected override void Alter(IList<MeasureWeight> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.SystemKeyword)
                           .Alter("oz", x =>
                           {
                               x.Name = "اونس";
                               x.Ratio = 0.02835M;
                               x.DisplayOrder = 10;
                           })
             .Alter("lb", x =>
             {
                 x.Name = "پوند";
                 x.Ratio = 0.4536M;
                 x.DisplayOrder = 10;
             })
             .Alter("kg", x =>
             {
                 x.Name = "کیلوگرم";
                 x.Ratio = 1M;
                 x.DisplayOrder = 1;
             })
             .Alter("g", x =>
             {
                 x.Name = "گرم";
                 x.Ratio = 0.001M;
                 x.DisplayOrder = 2;
             })
             .Alter("l", x =>
             {
                 x.Name = "لیتر";
                 x.Ratio = 1M;
                 x.DisplayOrder = 3;
             })
             .Alter("ml", x =>
             {
                 x.Name = "میلی لیتر";
                 x.Ratio = 0.001M;
                 x.DisplayOrder = 4;
             });
        }

        protected override void Alter(IList<ShippingMethod> entities)
        {
            base.Alter(entities);
            entities.Clear();
            entities.Add(new ShippingMethod()
            {
                Name = "ارسال با پیک",
                Description = "توسط پیک فروشگاه ارسال شود.",
                DisplayOrder = 0
            });
        }

        protected override void Alter(IList<Currency> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.DisplayLocale)
               .Alter("de-DE", x =>
               {
                   x.Name = "ریال";
                   x.DisplayLocale = "fa-IR";
                   x.Published = true;
                   x.Rate = 1M;
                   x.DisplayOrder = 0;
                   x.CustomFormatting = "";
                   x.CurrencyCode = "IRR";
               })
            .Remove("de-CH")
            .Remove("en-US")
            .Remove("en-GB")
            .Remove("en-AU")
            .Remove("en-CA")
            .Remove("tr-TR")
            .Remove("zh-CN")
            .Remove("zh-HK")
            .Remove("ja-JP")
            .Remove("ru-RU")
            .Remove("sv-SE");
            entities.Add(new Currency()
            {
                Name = "تومان",
                DisplayLocale = "fa-IR",
                Published = true,
                Rate = 0.1M,
                DisplayOrder = 1,
                CustomFormatting = "",
                CurrencyCode = "IRT",
            });
        }

        protected override void Alter(IList<CustomerRole> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Name)
                .Alter("Administrators", x => x.Name = "ادمین")
                .Alter("Forum Moderators", x => x.Name = "مدیرانجمن")
                .Alter("Registered", x => x.Name = "ثبت نام شده")
                .Alter("Guests", x => x.Name = "مهمان")
                .Alter("Inactive new customers", x => x.Name = "کاربر جدید غیر فعال");
        }

        protected override void Alter(Address entity)
        {
            base.Alter(entity);
            var cCountry = base.DbContext.Set<Country>().Where(x => x.Name == "ایران").FirstOrDefault();

            entity.FirstName = "میثم";
            entity.LastName = "ابراهیمی";
            entity.Email = "admin@admin.ir";
            entity.Company = "کاج";
            entity.Address1 = "تهران";
            entity.City = "تهران";
            entity.StateProvince = cCountry.StateProvinces.FirstOrDefault();
            entity.Country = cCountry;
            entity.ZipPostalCode = "13345";
        }


        protected override string TaxNameBooks => "ارزش افزوده";
        protected override string TaxNameDigitalGoods => "ارزش افزوده";
        protected override string TaxNameJewelry => "ارزش افزوده";
        protected override string TaxNameApparel => "ارزش افزوده";
        protected override string TaxNameFood => "ارزش افزوده";
        protected override string TaxNameElectronics => "ارزش افزوده";
        protected override string TaxNameTaxFree => "ارزش افزوده";
        public override decimal[] FixedTaxRates => new decimal[] { 19, 7, 0 };

        protected override void Alter(IList<TaxCategory> entities)
        {
            base.Alter(entities);

            // Clear all tax categories
            entities.Clear();

            // Add de-DE specific ones
            _taxCategories.Add("ارزش افزوده", new TaxCategory { DisplayOrder = 0, Name = "ارزش افزوده" });
            //    _taxCategories.Add("Ermäßigt", new TaxCategory { DisplayOrder = 1, Name = "ارزش افزوده" });
            //  _taxCategories.Add(TaxNameTaxFree, new TaxCategory { DisplayOrder = 2, Name = TaxNameTaxFree });

            foreach (var taxCategory in _taxCategories.Values)
            {
                entities.Add(taxCategory);
            }
        }

        protected override void Alter(IList<Country> entities)
        {
            base.Alter(entities);
            entities.Each(x => x.Published = false);

            entities.WithKey(x => x.NumericIsoCode)

            .Alter(364, x =>
            {
                x.Name = "ایران";
                x.DisplayOrder = -20;
                x.Published = true;
                #region Provinces
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "آذربایجان شرقی",
                    Published = true,
                    DisplayOrder = 1,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "آذربایجان غربی",
                    Published = true,
                    DisplayOrder = 5,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "اردبیل",
                    Published = true,
                    DisplayOrder = 10,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "اصفهان",
                    Published = true,
                    DisplayOrder = 15,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "البرز",
                    Published = true,
                    DisplayOrder = 20,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "ایلام",
                    Published = true,
                    DisplayOrder = 25,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "بوشهر",
                    Published = true,
                    DisplayOrder = 30,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "تهران",
                    Published = true,
                    DisplayOrder = 35,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "چهارمحال و بختیاری",
                    Published = true,
                    DisplayOrder = 40,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "خراسان جنوبی",
                    Published = true,
                    DisplayOrder = 45,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "خراسان رضوی",
                    Published = true,
                    DisplayOrder = 50,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "خراسان شمالی",
                    Published = true,
                    DisplayOrder = 55,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "خوزستان",
                    Published = true,
                    DisplayOrder = 60,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "زنجان",
                    Published = true,
                    DisplayOrder = 65,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "سمنان",
                    Published = true,
                    DisplayOrder = 70,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "سیستان و بلوچستان",
                    Published = true,
                    DisplayOrder = 75,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "فارس",
                    Published = true,
                    DisplayOrder = 80,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "قزوین",
                    Published = true,
                    DisplayOrder = 85,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "قم",
                    Published = true,
                    DisplayOrder = 90,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "کردستان",
                    Published = true,
                    DisplayOrder = 95,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "کرمان",
                    Published = true,
                    DisplayOrder = 100,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "کرمانشاه",
                    Published = true,
                    DisplayOrder = 105,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "کهگیلویه و بویراحمد",
                    Published = true,
                    DisplayOrder = 110,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "گلستان",
                    Published = true,
                    DisplayOrder = 115,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "گیلان",
                    Published = true,
                    DisplayOrder = 120,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "لرستان",
                    Published = true,
                    DisplayOrder = 125,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "مازندران",
                    Published = true,
                    DisplayOrder = 130,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "مرکزی",
                    Published = true,
                    DisplayOrder = 135,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "هرمزگان",
                    Published = true,
                    DisplayOrder = 140,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "همدان",
                    Published = true,
                    DisplayOrder = 145,
                });
                x.StateProvinces.Add(new StateProvince()
                {
                    Name = "یزد",
                    Published = true,
                    DisplayOrder = 150,
                });
                #endregion Provinces
            });
            #region Countries

            //entities.Each(x => x.Published = false);
            //entities.WithKey(x => x.NumericIsoCode)
            //    .Alter(276, x =>
            //    {
            //        x.Name = "Deutschland";
            //        x.DisplayOrder = -10;
            //        x.Published = true;
            //        #region Provinces
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Baden-Württemberg",
            //            Abbreviation = "BW",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Bayern",
            //            Abbreviation = "BY",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Berlin",
            //            Abbreviation = "BE",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Brandenburg",
            //            Abbreviation = "BB",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Bremen",
            //            Abbreviation = "HB",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Hamburg",
            //            Abbreviation = "HH",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Hessen",
            //            Abbreviation = "HE",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Mecklenburg-Vorpommern",
            //            Abbreviation = "MV",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Niedersachsen",
            //            Abbreviation = "NI",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Nordrhein-Westfalen",
            //            Abbreviation = "NW",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Rheinland-Pfalz",
            //            Abbreviation = "RP",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Saarland",
            //            Abbreviation = "SL",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Sachsen",
            //            Abbreviation = "SN",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Sachsen-Anhalt",
            //            Abbreviation = "ST",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Schleswig-Holstein",
            //            Abbreviation = "SH",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Thüringen",
            //            Abbreviation = "TH",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        #endregion Provinces
            //    })
            //    .Alter(40, x =>
            //    {
            //        x.Name = "Österreich";
            //        x.DisplayOrder = -5;
            //        #region Provinces
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Burgenland",
            //            Abbreviation = "Bgld.",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Kärnten",
            //            Abbreviation = "Ktn.",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Niederösterreich",
            //            Abbreviation = "NÖ",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Oberösterreich",
            //            Abbreviation = "OÖ",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Salzburg",
            //            Abbreviation = "Sbg.",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Steiermark",
            //            Abbreviation = "Stmk.",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Tirol",
            //            Abbreviation = "T",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Vorarlberg",
            //            Abbreviation = "Vbg.",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Wien",
            //            Abbreviation = "W",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        #endregion Provinces
            //    })
            //    .Alter(756, x =>
            //    {
            //        x.Name = "Schweiz";
            //        x.DisplayOrder = -1;
            //        #region Provinces
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Zürich",
            //            Abbreviation = "ZH",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Bern",
            //            Abbreviation = "BE",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Luzern",
            //            Abbreviation = "LU",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Uri",
            //            Abbreviation = "UR",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Schwyz",
            //            Abbreviation = "SZ",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Obwalden",
            //            Abbreviation = "OW",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Nidwalden",
            //            Abbreviation = "ST",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Glarus",
            //            Abbreviation = "GL",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Zug",
            //            Abbreviation = "ZG",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Freiburg",
            //            Abbreviation = "FR",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Solothurn",
            //            Abbreviation = "SO",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Basel-Stadt",
            //            Abbreviation = "BS",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Basel-Landschaft",
            //            Abbreviation = "BL",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Schaffhausen",
            //            Abbreviation = "SH",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Appenzell Ausserrhoden",
            //            Abbreviation = "AR",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Appenzell Innerrhoden",
            //            Abbreviation = "AI",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "St. Gallen",
            //            Abbreviation = "SG",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Graubünden",
            //            Abbreviation = "GR",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Aargau",
            //            Abbreviation = "AG",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Thurgau",
            //            Abbreviation = "TG",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Tessin",
            //            Abbreviation = "Ti",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Waadt",
            //            Abbreviation = "VD",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Wallis",
            //            Abbreviation = "VS",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Neuenburg",
            //            Abbreviation = "NE",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Genf",
            //            Abbreviation = "GE",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        x.StateProvinces.Add(new StateProvince()
            //        {
            //            Name = "Jura",
            //            Abbreviation = "JU",
            //            Published = true,
            //            DisplayOrder = 1,
            //        });
            //        #endregion Provinces
            //    })
            //    .Alter(840, x =>
            //    {
            //        x.Name = "Vereinigte Staaten von Amerika";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(124, x =>
            //    {
            //        x.Name = "Kanada";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(32, x =>
            //    {
            //        x.Name = "Argentinien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(51, x =>
            //    {
            //        x.Name = "Armenien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(533, x =>
            //    {
            //        x.Name = "Aruba";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(36, x =>
            //    {
            //        x.Name = "Australien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(31, x =>
            //    {
            //        x.Name = "Aserbaidschan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(44, x =>
            //    {
            //        x.Name = "Bahamas";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(50, x =>
            //    {
            //        x.Name = "Bangladesh";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(112, x =>
            //    {
            //        x.Name = "Weissrussland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(56, x =>
            //    {
            //        x.Name = "Belgien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(84, x =>
            //    {
            //        x.Name = "Belize";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(60, x =>
            //    {
            //        x.Name = "Bermudas";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(68, x =>
            //    {
            //        x.Name = "Bolivien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(70, x =>
            //    {
            //        x.Name = "Bosnien-Herzegowina";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(76, x =>
            //    {
            //        x.Name = "Brasilien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(100, x =>
            //    {
            //        x.Name = "Bulgarien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(136, x =>
            //    {
            //        x.Name = "Kaiman Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(152, x =>
            //    {
            //        x.Name = "Chile";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(156, x =>
            //    {
            //        x.Name = "China";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(170, x =>
            //    {
            //        x.Name = "Kolumbien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(188, x =>
            //    {
            //        x.Name = "Costa Rica";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(191, x =>
            //    {
            //        x.Name = "Kroatien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(192, x =>
            //    {
            //        x.Name = "Kuba";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(196, x =>
            //    {
            //        x.Name = "Zypern";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(203, x =>
            //    {
            //        x.Name = "Tschechische Republik";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(208, x =>
            //    {
            //        x.Name = "Dänemark";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(214, x =>
            //    {
            //        x.Name = "Dominikanische Republik";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(218, x =>
            //    {
            //        x.Name = "Ecuador";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(818, x =>
            //    {
            //        x.Name = "Ägypten";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(246, x =>
            //    {
            //        x.Name = "Finnland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(250, x =>
            //    {
            //        x.Name = "Frankreich";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(268, x =>
            //    {
            //        x.Name = "Georgien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(292, x =>
            //    {
            //        x.Name = "Gibraltar";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(300, x =>
            //    {
            //        x.Name = "Griechenland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(320, x =>
            //    {
            //        x.Name = "Guatemala";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(344, x =>
            //    {
            //        x.Name = "Hong Kong";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(348, x =>
            //    {
            //        x.Name = "Ungarn";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(356, x =>
            //    {
            //        x.Name = "Indien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(360, x =>
            //    {
            //        x.Name = "Indonesien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(372, x =>
            //    {
            //        x.Name = "Irland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(376, x =>
            //    {
            //        x.Name = "Israel";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(380, x =>
            //    {
            //        x.Name = "Italien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(388, x =>
            //    {
            //        x.Name = "Jamaika";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(392, x =>
            //    {
            //        x.Name = "Japan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(400, x =>
            //    {
            //        x.Name = "Jordanien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(398, x =>
            //    {
            //        x.Name = "Kasachstan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(408, x =>
            //    {
            //        x.Name = "Nord Korea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(414, x =>
            //    {
            //        x.Name = "Kuwait";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(458, x =>
            //    {
            //        x.Name = "Malaysia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(484, x =>
            //    {
            //        x.Name = "Mexiko";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(528, x =>
            //    {
            //        x.Name = "Niederlande";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(554, x =>
            //    {
            //        x.Name = "Neuseeland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(578, x =>
            //    {
            //        x.Name = "Norwegen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(586, x =>
            //    {
            //        x.Name = "Pakistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(600, x =>
            //    {
            //        x.Name = "Paraguay";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(604, x =>
            //    {
            //        x.Name = "Peru";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(608, x =>
            //    {
            //        x.Name = "Philippinen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(616, x =>
            //    {
            //        x.Name = "Polen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(620, x =>
            //    {
            //        x.Name = "Portugal";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(630, x =>
            //    {
            //        x.Name = "Puerto Rico";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(634, x =>
            //    {
            //        x.Name = "Qatar";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(642, x =>
            //    {
            //        x.Name = "Rumänien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(643, x =>
            //    {
            //        x.Name = "Rußland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(682, x =>
            //    {
            //        x.Name = "Saudi Arabien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(702, x =>
            //    {
            //        x.Name = "Singapur";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(703, x =>
            //    {
            //        x.Name = "Slowakei";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(705, x =>
            //    {
            //        x.Name = "Slowenien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(710, x =>
            //    {
            //        x.Name = "Südafrika";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(724, x =>
            //    {
            //        x.Name = "Spanien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(752, x =>
            //    {
            //        x.Name = "Schweden";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(158, x =>
            //    {
            //        x.Name = "Taiwan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(764, x =>
            //    {
            //        x.Name = "Thailand";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(792, x =>
            //    {
            //        x.Name = "Türkei";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(804, x =>
            //    {
            //        x.Name = "Ukraine";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(784, x =>
            //    {
            //        x.Name = "Vereinigte Arabische Emirate";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(826, x =>
            //    {
            //        x.Name = "Großbritannien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(581, x =>
            //    {
            //        x.Name = "United States Minor Outlying Islands";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(858, x =>
            //    {
            //        x.Name = "Uruguay";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(860, x =>
            //    {
            //        x.Name = "Usbekistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(862, x =>
            //    {
            //        x.Name = "Venezuela";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(688, x =>
            //    {
            //        x.Name = "Serbien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(4, x =>
            //    {
            //        x.Name = "Afghanistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(8, x =>
            //    {
            //        x.Name = "Albanien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(12, x =>
            //    {
            //        x.Name = "Algerien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(16, x =>
            //    {
            //        x.Name = "Samoa";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(20, x =>
            //    {
            //        x.Name = "Andorra";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(24, x =>
            //    {
            //        x.Name = "Angola";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(660, x =>
            //    {
            //        x.Name = "Anguilla";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(10, x =>
            //    {
            //        x.Name = "Antarktis";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(28, x =>
            //    {
            //        x.Name = "Antigua und Barbuda";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(48, x =>
            //    {
            //        x.Name = "Bahrain";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(52, x =>
            //    {
            //        x.Name = "Barbados";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(204, x =>
            //    {
            //        x.Name = "Benin";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(64, x =>
            //    {
            //        x.Name = "Bhutan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(72, x =>
            //    {
            //        x.Name = "Botswana";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(74, x =>
            //    {
            //        x.Name = "Bouvet Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(86, x =>
            //    {
            //        x.Name = "Britisch-Indischer Ozean";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(96, x =>
            //    {
            //        x.Name = "Brunei";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(854, x =>
            //    {
            //        x.Name = "Burkina Faso";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(108, x =>
            //    {
            //        x.Name = "Burundi";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(116, x =>
            //    {
            //        x.Name = "Kambodscha";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(120, x =>
            //    {
            //        x.Name = "Kamerun";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(132, x =>
            //    {
            //        x.Name = "Kap Verde";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(140, x =>
            //    {
            //        x.Name = "Zentralafrikanische Republik";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(148, x =>
            //    {
            //        x.Name = "Tschad";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(162, x =>
            //    {
            //        x.Name = "Christmas Island";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(166, x =>
            //    {
            //        x.Name = "Kokosinseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(174, x =>
            //    {
            //        x.Name = "Komoren";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(178, x =>
            //    {
            //        x.Name = "Kongo";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(184, x =>
            //    {
            //        x.Name = "Cook Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(384, x =>
            //    {
            //        x.Name = "Elfenbeinküste";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(262, x =>
            //    {
            //        x.Name = "Djibuti";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(212, x =>
            //    {
            //        x.Name = "Dominika";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(222, x =>
            //    {
            //        x.Name = "El Salvador";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(226, x =>
            //    {
            //        x.Name = "Äquatorial Guinea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(232, x =>
            //    {
            //        x.Name = "Eritrea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(233, x =>
            //    {
            //        x.Name = "Estland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(231, x =>
            //    {
            //        x.Name = "Äthiopien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(238, x =>
            //    {
            //        x.Name = "Falkland Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(234, x =>
            //    {
            //        x.Name = "Färöer Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(242, x =>
            //    {
            //        x.Name = "Fidschi";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(254, x =>
            //    {
            //        x.Name = "Französisch Guyana";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(258, x =>
            //    {
            //        x.Name = "Französisch Polynesien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(260, x =>
            //    {
            //        x.Name = "Französisches Süd-Territorium";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(266, x =>
            //    {
            //        x.Name = "Gabun";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(270, x =>
            //    {
            //        x.Name = "Gambia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(288, x =>
            //    {
            //        x.Name = "Ghana";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(304, x =>
            //    {
            //        x.Name = "Grönland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(308, x =>
            //    {
            //        x.Name = "Grenada";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(312, x =>
            //    {
            //        x.Name = "Guadeloupe";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(316, x =>
            //    {
            //        x.Name = "Guam";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(324, x =>
            //    {
            //        x.Name = "Guinea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(624, x =>
            //    {
            //        x.Name = "Guinea Bissau";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(328, x =>
            //    {
            //        x.Name = "Guyana";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(332, x =>
            //    {
            //        x.Name = "Haiti";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(334, x =>
            //    {
            //        x.Name = "Heard und McDonald Islands";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(340, x =>
            //    {
            //        x.Name = "Honduras";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(352, x =>
            //    {
            //        x.Name = "Island";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(364, x =>
            //    {
            //        x.Name = "Iran";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(368, x =>
            //    {
            //        x.Name = "Irak";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(404, x =>
            //    {
            //        x.Name = "Kenia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(296, x =>
            //    {
            //        x.Name = "Kiribati";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(410, x =>
            //    {
            //        x.Name = "Süd Korea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(417, x =>
            //    {
            //        x.Name = "Kirgisistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(418, x =>
            //    {
            //        x.Name = "Laos";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(428, x =>
            //    {
            //        x.Name = "Lettland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(422, x =>
            //    {
            //        x.Name = "Libanon";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(426, x =>
            //    {
            //        x.Name = "Lesotho";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(430, x =>
            //    {
            //        x.Name = "Liberia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(434, x =>
            //    {
            //        x.Name = "Libyen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(438, x =>
            //    {
            //        x.Name = "Liechtenstein";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(440, x =>
            //    {
            //        x.Name = "Litauen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(442, x =>
            //    {
            //        x.Name = "Luxemburg";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(446, x =>
            //    {
            //        x.Name = "Macao";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(807, x =>
            //    {
            //        x.Name = "Mazedonien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(450, x =>
            //    {
            //        x.Name = "Madagaskar";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(454, x =>
            //    {
            //        x.Name = "Malawi";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(462, x =>
            //    {
            //        x.Name = "Malediven";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(466, x =>
            //    {
            //        x.Name = "Mali";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(470, x =>
            //    {
            //        x.Name = "Malta";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(584, x =>
            //    {
            //        x.Name = "Marshall Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(474, x =>
            //    {
            //        x.Name = "Martinique";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(478, x =>
            //    {
            //        x.Name = "Mauretanien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(480, x =>
            //    {
            //        x.Name = "Mauritius";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(175, x =>
            //    {
            //        x.Name = "Mayotte";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(583, x =>
            //    {
            //        x.Name = "Mikronesien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(498, x =>
            //    {
            //        x.Name = "Moldavien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(492, x =>
            //    {
            //        x.Name = "Monaco";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(496, x =>
            //    {
            //        x.Name = "Mongolei";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(500, x =>
            //    {
            //        x.Name = "Montserrat";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(504, x =>
            //    {
            //        x.Name = "Marokko";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(508, x =>
            //    {
            //        x.Name = "Mocambique";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(104, x =>
            //    {
            //        x.Name = "Birma";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(516, x =>
            //    {
            //        x.Name = "Namibia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(520, x =>
            //    {
            //        x.Name = "Nauru";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(524, x =>
            //    {
            //        x.Name = "Nepal";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(530, x =>
            //    {
            //        x.Name = "Niederländische Antillen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(540, x =>
            //    {
            //        x.Name = "Neukaledonien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(558, x =>
            //    {
            //        x.Name = "Nicaragua";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(562, x =>
            //    {
            //        x.Name = "Niger";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(566, x =>
            //    {
            //        x.Name = "Nigeria";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(570, x =>
            //    {
            //        x.Name = "Niue";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(574, x =>
            //    {
            //        x.Name = "Norfolk Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(580, x =>
            //    {
            //        x.Name = "Marianen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(512, x =>
            //    {
            //        x.Name = "Oman";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(585, x =>
            //    {
            //        x.Name = "Palau";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(591, x =>
            //    {
            //        x.Name = "Panama";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(598, x =>
            //    {
            //        x.Name = "Papua Neuguinea";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(612, x =>
            //    {
            //        x.Name = "Pitcairn";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(638, x =>
            //    {
            //        x.Name = "Reunion";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(646, x =>
            //    {
            //        x.Name = "Ruanda";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(659, x =>
            //    {
            //        x.Name = "St. Kitts Nevis Anguilla";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(662, x =>
            //    {
            //        x.Name = "Saint Lucia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(670, x =>
            //    {
            //        x.Name = "St. Vincent";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(882, x =>
            //    {
            //        x.Name = "Samoa";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(674, x =>
            //    {
            //        x.Name = "San Marino";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(678, x =>
            //    {
            //        x.Name = "Sao Tome";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(686, x =>
            //    {
            //        x.Name = "Senegal";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(690, x =>
            //    {
            //        x.Name = "Seychellen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(694, x =>
            //    {
            //        x.Name = "Sierra Leone";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(90, x =>
            //    {
            //        x.Name = "Solomon Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(706, x =>
            //    {
            //        x.Name = "Somalia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(239, x =>
            //    {
            //        x.Name = "South Georgia, South Sandwich Isl.";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(144, x =>
            //    {
            //        x.Name = "Sri Lanka";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(654, x =>
            //    {
            //        x.Name = "St. Helena";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(666, x =>
            //    {
            //        x.Name = "St. Pierre und Miquelon";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(736, x =>
            //    {
            //        x.Name = "Sudan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(740, x =>
            //    {
            //        x.Name = "Surinam";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(744, x =>
            //    {
            //        x.Name = "Svalbard und Jan Mayen Islands";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(748, x =>
            //    {
            //        x.Name = "Swasiland";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(760, x =>
            //    {
            //        x.Name = "Syrien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(762, x =>
            //    {
            //        x.Name = "Tadschikistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(834, x =>
            //    {
            //        x.Name = "Tansania";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(768, x =>
            //    {
            //        x.Name = "Togo";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(772, x =>
            //    {
            //        x.Name = "Tokelau";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(776, x =>
            //    {
            //        x.Name = "Tonga";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(780, x =>
            //    {
            //        x.Name = "Trinidad Tobago";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(788, x =>
            //    {
            //        x.Name = "Tunesien";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(795, x =>
            //    {
            //        x.Name = "Turkmenistan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(796, x =>
            //    {
            //        x.Name = "Turks und Kaikos Inseln";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(798, x =>
            //    {
            //        x.Name = "Tuvalu";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(800, x =>
            //    {
            //        x.Name = "Uganda";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(548, x =>
            //    {
            //        x.Name = "Vanuatu";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(336, x =>
            //    {
            //        x.Name = "Vatikan";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(704, x =>
            //    {
            //        x.Name = "Vietnam";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(92, x =>
            //    {
            //        x.Name = "Virgin Island (Brit.)";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(850, x =>
            //    {
            //        x.Name = "Virgin Island (USA)";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(876, x =>
            //    {
            //        x.Name = "Wallis et Futuna";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(732, x =>
            //    {
            //        x.Name = "Westsahara";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(887, x =>
            //    {
            //        x.Name = "Jemen";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(894, x =>
            //    {
            //        x.Name = "Sambia";
            //        x.DisplayOrder = 100;
            //    })
            //    .Alter(716, x =>
            //    {
            //        x.Name = "Zimbabwe";
            //        x.DisplayOrder = 100;
            //    });

            #endregion Countries
        }

        protected override void Alter(IList<Topic> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.SystemName)
               .Alter("AboutUs", x =>
               {
                   x.Title = "درباره ما";
                   x.Body = "<p>توضیحات شرکت.</p>";
               })
                .Alter("CheckoutAsGuestOrRegister", x =>
                {
                    x.Title = "ثبت نام و صرفه جوی در وقت.";
                    x.Body = "<p><strong>برای راحتی ثبت نام کنید!</strong><br /></p><ul><li>دسترسی به سفارش ها و وضعیت آنها</li><li>سریع شدن مراحل خرید</li></ul>";
                })
               .Alter("ConditionsOfUse", x =>
               {
                   x.Title = "شرایط استفاده";
                   x.Body = "<p>در این محل میتوانید شرایط استفاده را وارد کنید.</p>";
               })
                .Alter("ContactUs", x =>
                {
                    x.Title = "تماس با ما";
                    x.Body = "<p>اطلاعات تماس خود را در این محل وارد کنید.</p>";
                })
                 .Alter("HomePageText", x =>
                 {
                     x.Title = "به فروشگاه ما خوش آمدید.";
                     x.Body = "<p>فروشگاه در حال بروز رسانی می باشد.</p>";
                 })
                .Alter("LoginRegistrationInfo", x =>
                {
                    x.Title = "درباره ورود و ثبت نام";
                    x.Body = "<p>اطلاعات ورود و ثبت نام خود را در این محل قرار دهید.</p>";
                })
                .Alter("PrivacyInfo", x =>
                {
                    x.Title = "سیاست حفظ حریم شخصی";
                    x.Body = "<p>سیاست حفظ حریم شخصی خود را در این محل وارد کنید.</p>";
                })
                .Alter("ShippingInfo", x =>
                {
                    x.Title = "حمل و نقل";
                    x.Body = "<p>روشهای حمل و نقل خود را در این محل وارد کنید.</p>";
                })
                  .Alter("Imprint", x =>
                  {
                      x.Title = "قانون کپی رایت";
                      x.Body = @"<p>
                           قانون کپی رایت را در این محل وارد کنید
                            </p>";
                  })

                .Alter("Disclaimer", x =>
                {
                    x.Title = "سلب مسئولیت";
                    x.Body = "<p>محل اطلاعات سلب مسئولیت.</p>";
                })
                .Alter("PaymentInfo", x =>
                {
                    x.Title = "اطلاعات پرداخت";
                    x.Body = "<p>اطلاعات پرداخت شما.</p>";
                });
        }

        protected override void Alter(UrlRecord entity)
        {
            base.Alter(entity);

            if (entity.EntityName == "Topic")
            {
                entity.Slug = entity.Slug switch
                {
                    "aboutus" => "درباره_ما",
                    "conditionsofuse" => "شرایط_استفاده",
                    "contactus" => "تماس_باما",
                    "privacyinfo" => "حریم_خصوصی",
                    "shippinginfo" => "حمل_و_نقل",
                    "imprint" => "imprint",
                    "disclaimer" => "سلب_مسئولیت",
                    "paymentinfo" => "اطلاعات_پرداخت",
                    _ => entity.Slug,
                };
            }
        }


        protected override void Alter(IList<ISettings> settings)
        {
            base.Alter(settings);

            var defaultDimensionId = DbContext.MeasureDimensions.FirstOrDefault(x => x.SystemKeyword == "m")?.Id;
            var defaultWeightId = DbContext.MeasureWeights.FirstOrDefault(x => x.SystemKeyword == "kg")?.Id;
            var defaultCountryId = DbContext.Countries.FirstOrDefault(x => x.TwoLetterIsoCode == "FA")?.Id;

            settings
                .Alter<MeasureSettings>(x =>
                {
                    x.BaseDimensionId = defaultDimensionId ?? x.BaseDimensionId;
                    x.BaseWeightId = defaultWeightId ?? x.BaseWeightId;
                })
                .Alter<SeoSettings>(x => x.MetaTitle = "فروشگاه اینترنتی")
                .Alter<OrderSettings>(x =>
                {
                    //x.ReturnRequestActions = "Reparatur,Ersatz,Gutschein";
                    //x.ReturnRequestReasons = "Falschen Artikel erhalten,Falsch bestellt,Ware fehlerhaft bzw. defekt";
                    //x.NumberOfDaysReturnRequestAvailable = 14;
                    x.ReturnRequestActions = "تعمیر، جایگزینی,اعتبار فروشگاه";
                    x.ReturnRequestReasons = "دریافت محصول اشتباه,اشتباه محصول سفارش داده شده,یک مشکل با این محصول وجود دارد";
                    x.NumberOfDaysReturnRequestAvailable = 7;
                    x.GiftCards_Activated_OrderStatusId = (int)OrderStatus.Complete;
                    x.GiftCards_Deactivated_OrderStatusId = (int)OrderStatus.Cancelled;
                    x.AnonymousCheckoutAllowed = false;
                })
                .Alter<ShippingSettings>(x => x.EstimateShippingEnabled = false)
                .Alter<TaxSettings>(x =>
                {
                    x.TaxBasedOn = TaxBasedOn.ShippingAddress;
                    x.TaxDisplayType = TaxDisplayType.IncludingTax;
                    x.DisplayTaxSuffix = false;
                    x.ShippingIsTaxable = false;
                    x.ShippingPriceIncludesTax = false;
                    x.ShippingTaxClassId = base.DbContext.Set<TaxCategory>().Where(tc => tc.Name == TaxNameApparel).Single().Id;
                    x.EuVatEnabled = false;
                    //  x.EuVatShopCountryId = base.DbContext.Set<Country>().Where(c => c.TwoLetterIsoCode == "IR").Single().Id;
                    x.EuVatAllowVatExemption = true;
                    x.EuVatUseWebService = false;
                    x.EuVatEmailAdminWhenNewVatSubmitted = true;
                });
        }

        protected override void Alter(IList<ActivityLogType> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.SystemKeyword)
                .Alter("AddNewCategory", x => { x.Name = "دسته بندی جدید اضافه شد"; })
              .Alter("AddNewCheckoutAttribute", x => { x.Name = "یک ویژگی پرداخت اضافه شد"; })
              .Alter("AddNewCustomer", x => { x.Name = "یک مشتری اضافه شد"; })
              .Alter("AddNewCustomerRole", x => { x.Name = "نقش جدید برای مشتری اضافه شد"; })
              .Alter("AddNewDiscount", x => { x.Name = "یک تخفیف اضافه شد"; })
              .Alter("AddNewGiftCard", x => { x.Name = "یک کارت هدیه اضافه شد"; })
              .Alter("AddNewManufacturer", x => { x.Name = "یک تولید کننده اضافه شد"; })
              .Alter("AddNewProduct", x => { x.Name = "یک محصول اضافه شد"; })
              .Alter("AddNewProductAttribute", x => { x.Name = "یک ویژگی محصول اضافه شد"; })
              .Alter("AddNewSetting", x => { x.Name = "اضافه شدن تنظیمات جدید"; })
              .Alter("AddNewSpecAttribute", x => { x.Name = "ویژگی مشخصات وارد شد"; })
              .Alter("AddNewWidget", x => { x.Name = "اضافه کردن یک ویجت"; })
              .Alter("DeleteCategory", x => { x.Name = "حذف یک دسته بندی"; })
              .Alter("DeleteCheckoutAttribute", x => { x.Name = "حذف یک ویژگی پرداخت"; })
              .Alter("DeleteCustomer", x => { x.Name = "حذف مشتری"; })
              .Alter("DeleteCustomerRole", x => { x.Name = "حذف نقش مشتری"; })
              .Alter("DeleteDiscount", x => { x.Name = "حذف تخفیف"; })
              .Alter("DeleteGiftCard", x => { x.Name = "حذف کارت هدیه"; })
              .Alter("DeleteManufacturer", x => { x.Name = "حذف تولید کننده"; })
              .Alter("DeleteProduct", x => { x.Name = "حذف محصول"; })
              .Alter("DeleteProductAttribute", x => { x.Name = "حدف ویژگی محصول"; })
              .Alter("DeleteReturnRequest", x => { x.Name = "حذف یک درخواست بازگشت"; })
              .Alter("DeleteSetting", x => { x.Name = "حذف تنظیمات"; })
              .Alter("DeleteSpecAttribute", x => { x.Name = "حذف ویژگی مشخصات"; })
              .Alter("DeleteWidget", x => { x.Name = "حذف ویجت"; })
              .Alter("EditCategory", x => { x.Name = "ویرایش گوره"; })
              .Alter("EditCheckoutAttribute", x => { x.Name = "ویرایش ویژگی پرداخت"; })
              .Alter("EditCustomer", x => { x.Name = "ویرایش مشتری"; })
              .Alter("EditCustomerRole", x => { x.Name = "ویرایش نقش مشتری"; })
              .Alter("EditDiscount", x => { x.Name = "ویرایش تخفیف"; })
              .Alter("EditGiftCard", x => { x.Name = "ویرایش کارت هدیه"; })
              .Alter("EditManufacturer", x => { x.Name = "ویرایش تولید کننده"; })
              .Alter("EditProduct", x => { x.Name = "ویرایش محصول"; })
              .Alter("EditProductAttribute", x => { x.Name = "ویرایش ویژگی محصول"; })
              .Alter("EditPromotionProviders", x => { x.Name = "ویرایش ارتقا توسعه دهنده"; })
              .Alter("EditReturnRequest", x => { x.Name = "ویرایش درخواست بازگشت"; })
              .Alter("EditSettings", x => { x.Name = "ویرایش تنظیمات"; })
              .Alter("EditSpecAttribute", x => { x.Name = "ویرایش ویژگی خصوصیات"; })
              .Alter("EditWidget", x => { x.Name = "ویرایش ویجت"; })
              .Alter("PublicStore.ViewCategory", x => { x.Name = "فروشگاه عمومی. مشاهده گروه"; })
              .Alter("PublicStore.ViewManufacturer", x => { x.Name = "فروشگاه عمومی. مشاهده تولید کننده"; })
              .Alter("PublicStore.ViewProduct", x => { x.Name = "فروشگاه عمومی. مشاهده محصول"; })
              .Alter("PublicStore.PlaceOrder", x => { x.Name = "فروشگاه عمومی سفارش"; })
              .Alter("PublicStore.SendPM", x => { x.Name = "فروشگاه عمومی ارسال pm"; })
              .Alter("PublicStore.ContactUs", x => { x.Name = "فروشگاه عمومی. استفاده از فرم تماس با ما"; })
              .Alter("PublicStore.AddToCompareList", x => { x.Name = "فروشگاه عمومی. استفاده از لیست مقایسه."; })
              .Alter("PublicStore.AddToShoppingCart", x => { x.Name = "فروشگاه عمومی. اضافه شدن به سبد خرید"; })
              .Alter("PublicStore.AddToWishlist", x => { x.Name = "فروشگاه عمومی. اضافه شدن به لیست علاقه مندی ها"; })
              .Alter("PublicStore.Login", x => { x.Name = "فروشگاه عمومی. ورود به سیستم"; })
              .Alter("PublicStore.Logout", x => { x.Name = "فروشگاه عمومی. خروج از سیستم."; })
              .Alter("PublicStore.AddProductReview", x => { x.Name = "فروشگاه عمومی. اضافه کردن بررسی محصول"; })
              .Alter("PublicStore.AddNewsComment", x => { x.Name = "فروشگاه عمومی. نظر برای اخبار اضافه شد"; })
              .Alter("PublicStore.AddBlogComment", x => { x.Name = "فروشگاه عمومی. نظر برای بلاگ اضافه شد."; })
              .Alter("PublicStore.AddForumTopic", x => { x.Name = "فروشگاه عمومی. یک موضوع جدید به انجمن اضافه شد."; })
              .Alter("PublicStore.EditForumTopic", x => { x.Name = "فروشگاه عمومی. ویرایش یک موضوع انجمن."; })
              .Alter("PublicStore.DeleteForumTopic", x => { x.Name = "فروشگاه عمومی. حذف یک موضع از انجمن"; })
              .Alter("PublicStore.AddForumPost", x => { x.Name = "فروشگاه عمومی. اضافه شدن یک مطلب به انجمن"; })
              .Alter("PublicStore.EditForumPost", x => { x.Name = "فروشگاه عمومی. ویرایش یک مطلب انجمن"; })
              .Alter("PublicStore.DeleteForumPost", x => { x.Name = "فروشگاه عمومی. حذف یک مطلب انجمن"; })
              .Alter("EditThemeVars", x => { x.Name = "ویرایش پارامترهای قالب"; })
              .Alter("ResetThemeVars", x => { x.Name = "تنظیم مجدد پارامترهای قالب"; })
              .Alter("ImportThemeVars", x => { x.Name = "ورود پارامترهای قالب"; })
              .Alter("ExportThemeVars", x => { x.Name = "خروجی گرفتن از پارامترهای قالب"; });
        }

        protected override void Alter(IList<TaskDescriptor> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Type)
                .Alter(nameof(QueuedMessagesSendTask), x => x.Name = "ارسال ایمیل")
                .Alter(nameof(QueuedMessagesClearTask), x => x.Name = "حذف لاگهای سیستم")
                .Alter(nameof(DeleteGuestsTask), x => x.Name = "حذف کاربران مهمان")
                .Alter(nameof(DeleteLogsTask), x => x.Name = "حذف رویدادها")
                .Alter(nameof(ClearCacheTask), x => x.Name = "حذف کش")
                .Alter(nameof(UpdateExchangeRateTask), x => x.Name = "بروز رسانی نرخ ارز")
                .Alter(nameof(TransientMediaClearTask), x => x.Name = "پاکسازی آپلودهای موقت")
                .Alter(nameof(TempFileCleanupTask), x => x.Name = "پاکسازی فایل هال موقت")
                .Alter(nameof(RebuildXmlSitemapTask), x => x.Name = "ساخت سایت مپ")
                .Alter(nameof(TargetGroupEvaluatorTask), x => x.Name = "بروز رسانی وظیفه مشتریان به نقش مشتری")
                .Alter(nameof(ProductRuleEvaluatorTask), x => x.Name = "بروز رسانی وظیفه محصولات به دسته بندی ها");
        }

        protected override void Alter(IList<SpecificationAttribute> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.DisplayOrder)
              .Alter(1, x =>{ x.Name = "سازنده پردازنده"; })
                 .Alter(2, x =>
                 {
                     x.Name = "رنگ";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "سفید";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "سیاه";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "رنگ بژ";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "قرمز";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 5).First().Name = "آبی";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 6).First().Name = "سبز";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 7).First().Name = "زرد";
                 })
                 .Alter(3, x => { x.Name = "ظرفیت دیسک سخت"; })
                .Alter(4, x =>  { x.Name = "حافظه دسترسی تصادفی"; })
                .Alter(5, x =>  { x.Name = "سیستم عامل"; })
                .Alter(6, x =>  { x.Name = "ارتباط"; })
                .Alter(7, x =>
                {
                    x.Name = "جنسیت";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "مردانه";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "خانم ها";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "تک جنسیتی";
                })
               .Alter(8, x =>
               {
                   x.Name = "ماده";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 1).Name = "فولاد ضد زنگ";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 2).Name = "تیتانیوم";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 3).Name = "پلاستیکی";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 4).Name = "آلومینیوم";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 5).Name = "چرم";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 6).Name = "نایلون";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 7).Name = "سیلیکون";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 8).Name = "سرامیک";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 9).Name = "پنبه";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 10).Name = "100٪ پنبه ارگانیک";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 11).Name = "پلی آمید";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 12).Name = "لاستیک";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 13).Name = "چوب";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 14).Name = "شیشه";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 15).Name = "اسپندکس";
                   x.SpecificationAttributeOptions.First(y => y.DisplayOrder == 16).Name = "پلی استر";
               })
                .Alter(9, x =>
                {
                    x.Name = "طراحی تکنیکی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "خودکار ، پیچ ​​تو پیچ";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "خودکار";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "کوارتز ، باتری کار می کند";
                })
                .Alter(10, x =>
                {
                    x.Name = "چنگ زدن";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "قلاب تاشو";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "قلاب تاشو ایمنی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "سگک سنجاق";
                })
                .Alter(11, x =>
                {
                    x.Name = "شیشه";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "ماده معدنی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "یاقوت کبود";
                })
                .Alter(12, x =>
                {
                    x.Name = "زبان";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "آلمانی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "انگلیسی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "فرانسوی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "ایتالیایی";
                })
                .Alter(13, x =>
                {
                    x.Name = "خروجی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "حد";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "شومیز";
                })
                .Alter(14, x =>
                {
                    x.Name = "نوع";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "ماجرایی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "داستان علمی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "تاریخ";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "اینترنت و رایانه";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 5).First().Name = "دلهره آور";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 6).First().Name = "ماشین ها";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 7).First().Name = "رمان";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 8).First().Name = "پخت و پز";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 9).First().Name = "غیر داستانی";
                })
                 .Alter(15, x =>
                 {
                     x.Name = "نوع کامپیوتر";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "دسکتاپ";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "همه در یک";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "لپ تاپ";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "تبلت";
                 })
                .Alter(16, x => { x.Name = "نوع ذخیره انبوه"; })
                .Alter(17, x => { x.Name = "اندازه (HDD خارجی)";})
                .Alter(18, x => { x.Name = "کیفیت MP3"; })
                 .Alter(19, x =>
                 {
                     x.Name = "ژانر. دسته";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "بلوز";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "جاز";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "دیسکو";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "پاپ";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 5).First().Name = "فانک";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 6).First().Name = "کلاسیک";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 7).First().Name = "آر آند بی";
                 })
                .Alter(20, x => { x.Name = "سازنده"; })
                 .Alter(21, x =>
                 {
                     x.Name = "برای چه کسی";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "برای او";
                     x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "برای شما";
                 })
                .Alter(22, x =>
                {
                    x.Name = "پیشنهاد";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "تخلیه";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "دائما ارزانترین قیمت";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "عمل";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "کاهش قیمت";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 5).First().Name = "پیشنهاد قیمت";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 6).First().Name = "پیشنهاد روزانه";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 7).First().Name = "پیشنهاد هفتگی";
                })
                .Alter(23, x =>{x.Name = "اندازه";})
                .Alter(24, x =>{x.Name = "قطر"; })
                .Alter(25, x =>
                {
                    x.Name = "چنگ زدن";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "قفل ضربه محکم و ناگهانی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "قلاب تاشو";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "بسته شدن";
                })
                .Alter(26, x =>
                {
                    x.Name = "شکل";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "بیضی شکل";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "گرد";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 3).First().Name = "قلبی شکل";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 4).First().Name = "زاویه دار";
                })
                .Alter(27, x => { x.Name = "گنجایش انبار"; })
                .Alter(28, x =>
                {
                    x.Name = "مواد دیسک";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 1).First().Name = "ماده معدنی";
                    x.SpecificationAttributeOptions.Where(y => y.DisplayOrder == 2).First().Name = "یاقوت کبود";
                });
        }

        protected override void Alter(IList<ProductAttribute> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Alias)
              .Alter("color", x => { x.Name = "رنگ"; })
                .Alter("custom-text", x => { x.Name = "متن دلخواه"; })
                .Alter("hdd", x => { x.Name = "هارد"; })
                .Alter("os", x => { x.Name = "سیستم عامل"; })
                .Alter("processor", x => { x.Name = "پردازنده"; })
                .Alter("ram", x => { x.Name = "رم"; })
                .Alter("size", x => { x.Name = "اندازه"; })
                .Alter("software", x => { x.Name = "نرم افزار"; })
                .Alter("game", x => { x.Name = "بازی"; })
                .Alter("iphone-color", x => { x.Name = "رنگ آیفن"; })
                .Alter("ipad-color", x => { x.Name = "رنگ ایپد"; })
                .Alter("memory-capacity", x => { x.Name = "ظرفیت حافظه"; })
                .Alter("width", x => { x.Name = "عرض"; })
                .Alter("length", x => { x.Name = "طول"; })
                .Alter("plate", x => { x.Name = "بشقاب"; })
                .Alter("plate-thickness", x => { x.Name = "ضخامت صفحه"; })
                .Alter("ballsize", x => { x.Name = "اندازه توپ"; })
                .Alter("leather-color", x => { x.Name = "رنگ چرم"; })
                .Alter("seat-shell", x => { x.Name = "پوسته صندلی"; })
                .Alter("base", x => { x.Name = "پایه"; })
                .Alter("style", x => { x.Name = "سبک"; })
                .Alter("framecolor", x => { x.Name = "رنگ قاب"; })
                .Alter("lenscolor", x => { x.Name = "رنگ لنز"; })
                .Alter("lenstype", x => { x.Name = "نوع لنز"; });
        }

        protected override void Alter(IList<ProductAttributeOptionsSet> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Name)
                 .Alter("General colors", x => x.Name = "رنگهای عمومی");
        }

        protected override void Alter(IList<ProductAttributeOption> entities)
        {
            base.Alter(entities);

            entities.Where(x => x.Alias == "red").Each(x => x.Name = "قرمز");
            entities.Where(x => x.Alias == "green").Each(x => x.Name = "سبز");
            entities.Where(x => x.Alias == "blue").Each(x => x.Name = "آبی");
            entities.Where(x => x.Alias == "yellow").Each(x => x.Name = "زرد");
            entities.Where(x => x.Alias == "black").Each(x => x.Name = "مشکی");
            entities.Where(x => x.Alias == "white").Each(x => x.Name = "سفید");
            entities.Where(x => x.Alias == "gray").Each(x => x.Name = "خاکستری");
            entities.Where(x => x.Alias == "silver").Each(x => x.Name = "نقره ای");
            entities.Where(x => x.Alias == "brown").Each(x => x.Name = "قهوه ای");
        }

        protected override void Alter(IList<ProductVariantAttribute> entities)
        {
            base.Alter(entities);

            entities.Where(x => x.ProductAttribute.Alias == "color" || x.ProductAttribute.Alias == "leather-color").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "black").Each(y => y.Name = "مشکی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "white").Each(y => y.Name = "سفید");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "silver").Each(y => y.Name = "نقره ای");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "red").Each(y => y.Name = "قرمز");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "gray" || y.Alias == "charcoal").Each(y => y.Name = "خاکستری");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "maroon").Each(y => y.Name = "قهوه ای قرمز");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "blue").Each(y => y.Name = "آبی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "purple").Each(y => y.Name = "بنفش");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "green").Each(y => y.Name = "سبز");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "anthracite").Each(y => y.Name = "آنتراسیت");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "brown").Each(y => y.Name = "قهوه ای");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "dark-brown").Each(y => y.Name = "قهوه ای تیره");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "natural").Each(y => y.Name = "رنگهای طبیعی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "biscuit").Each(y => y.Name = "بیسکویت");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "dark-green").Each(y => y.Name = "سبز تیره");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "light-grey").Each(y => y.Name = "خاکستری روشن");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "dark-red").Each(y => y.Name = "قرمز تیره");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "hazelnut").Each(y => y.Name = "فندقی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "fuliginous").Each(y => y.Name = "دودی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "tomato-red").Each(y => y.Name = "رنگ گوجه ای");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "yellow").Each(y => y.Name = "زرد");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "mint").Each(y => y.Name = "سبز نعناعی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "lightblue").Each(y => y.Name = "آبی کمرنگ");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "turquoise").Each(y => y.Name = "فیروزه ای");
            });

            entities.Where(x => x.ProductAttribute.Alias == "iphone-color" || x.ProductAttribute.Alias == "ipad-color").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "black").Each(y => y.Name = "مشکی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "silver").Each(y => y.Name = "نقره ای");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "spacegray").Each(y => y.Name = "نوک مدادی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "purple").Each(y => y.Name = "بنفش");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "lightblue").Each(y => y.Name = "آبی کمرنگ");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "turquoise").Each(y => y.Name = "فیروزه ای");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "yellow").Each(y => y.Name = "زرد");
            });

            entities.Where(x => x.ProductAttribute.Alias == "controller").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "without_controller").Each(y => y.Name = "بدون کنترل کننده");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "with_controller").Each(y => y.Name = "با کنترل کننده");
            });

            entities.Where(x => x.ProductAttribute.Alias == "game").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "prince-of-persia-the-forgotten-sands").Each(y => y.Name = "شاهزاده ایرانی \"زمان فراموش شده \"");
            });

            entities.Where(x => x.ProductAttribute.Alias == "seat-shell").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "cherry").Each(y => y.Name = "گیلاس");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "walnut").Each(y => y.Name = "گردو");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "wooden-black-lacquered").Each(y => y.Name = "چوب لاک سیاه");
            });

            entities.Where(x => x.ProductAttribute.Alias == "base").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "top-edge-polished").Each(y => y.Name = "لبه فوقانی صیقلی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "completely-polished").Each(y => y.Name = "کاملا صیقلی شده");
            });

            entities.Where(x => x.ProductAttribute.Alias == "plate").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "clear-glass").Each(y => y.Name = "شیشه ی تمیز");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "sandblasted-glass").Each(y => y.Name = "شیشه سندبلاست");
            });

            entities.Where(x => x.ProductAttribute.Alias == "material").Each(x =>
            {
                x.ProductVariantAttributeValues.Where(y => y.Alias == "leather-special").Each(y => y.Name = "چرم خاص");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "leather-aniline").Each(y => y.Name = "آنیلین چرمی");
                x.ProductVariantAttributeValues.Where(y => y.Alias == "mixed-linen").Each(y => y.Name = "مخلوط کتان");
            });
        }

        protected override void Alter(IList<ProductTemplate> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.ViewPath)
                .Alter("Product", x => x.Name = "الگوی استاندارد محصول");
        }

        protected override void Alter(IList<CategoryTemplate> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.ViewPath)
             .Alter("CategoryTemplate.ProductsInGridOrLines", x => x.Name = "محصولات بصورت خطی یا شبکه ای");
        }

        protected override void Alter(IList<ManufacturerTemplate> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.ViewPath)
                .Alter("ManufacturerTemplate.ProductsInGridOrLines", x => x.Name = "محصولات بصورت خطی یا شبکه ای");
        }

        protected override void Alter(IList<Category> entities)
        {
            base.Alter(entities);

            var names = new Dictionary<string, string>
            {
                { "Books", "کتاب" },
                { "Cell phones", "تلفن های هوشمند" },
                { "Chairs", "صندلی" },
                { "Cook and enjoy", "لذت پخت و پز" },
                { "Computers", "کامپیوتر" },
                { "Desktops", "کامپیوتر رومیزی" },
                { "Digital Products", "محصولات دیجیتال" },
                { "Fashion", "مد" },
                { "Furniture", "مبلمان" },
                { "Games", "بازی" },
                { "Gaming Accessories", "تجهیزات" },
                { "Gift cards", "کوپن هدیه" },
                { "Jackets", "ژاکت" },
                { "Lounger", "صندلی راحتی" },
                { "Lamps", "لامپ ها" },
                { "Notebooks", "نوت بوک" },
                { "Shoes", "کفش" },
                { "Sports", "ورزشی" },
                { "Soccer", "فوتبال" },
                { "Sunglasses", "عینک آفتابی" },
                { "Tables", "میز" },
                { "Trousers", "شلوار" },
                { "Watches", "ساعت" }
            };

            var alterer = entities.WithKey(x => x.MetaTitle);

            foreach (var kvp in names)
            {
                alterer.Alter(kvp.Key, x => x.Name = kvp.Value);
            }

            entities.Where(x => x.BadgeText.EqualsNoCase("NEW")).Each(x => x.BadgeText = "جدید");
        }

        private void AlterFashionProducts(IList<Product> entities)
        {
            entities.WithKey(x => x.Sku)
              .Alter("Fashion-112355", x =>
              {
                  x.Name = "کفش تمام ستاره";
                  x.ShortDescription = "کفش ورزشی کلاسیک!";
                  x.FullDescription = "<p>از سال 1912 و تا به امروز یک کفش ورزشی بی نظیر می باشد.</p> ";
              })
              .Alter("Fashion-987693502", x =>
              {
                  x.Name = "پیراهن آستین دار Meccanica";
                  x.ShortDescription = "پیراهن زنانه با چاپ مرسوم ، مد روز";
                  x.FullDescription = "<p> حتی در تابستان ، سبک دوکاتی با مد مطابقت دارد! با پیراهن آستین دار مكانیكا ، هر زنی می تواند علاقه خود را به دوكاتی با یك لباس راحت و همه فن حریف ابراز كند. این پیراهن در رنگ های مشکی و قرمز پرنعمت موجود است. </p>";
              })
              .Alter("Fashion-JN1107", x =>
              {
                  x.Name = "کاپشن ورزشی زنانه";
                  //  x.FullDescription = "<p>Leichtes wind- und wasserabweisendes Gewebe, Futter aus weichem Single-Jersey Strickbündchen an Arm und Bund, 2 seitliche Taschen mit Reißverschluss, Kapuze in leicht tailliertem Schnitt.</p><ul><li>Oberstoff: 100%</li><li>Polyamid Futterstoff: 65% Polyester, 35% Baumwolle</li><li>Futterstoff 2: 100% Polyester</li></ul>";

                  x.FullDescription = "<p> پارچه سبک بادگیر و ضد آب، آستر ساخته شده از دستبندهای کشباف نرم تک پیراهنی روی بازو و کمر </p> " +
                "<ul> " +
                "<li> بیرونی پارچه: 100٪ </li> " +
                "<li> پوشش پلی آمید: 65٪ پلی استر ، 35٪ پنبه </li> " +
                "<li> پوشش 2: 100٪ پلی استر </ li> </ul>";
              })
              .Alter("Fashion-65986524", x =>
              {
                  x.Name = "شلوار جین آبی کلاسیک کلارک";
                  x.ShortDescription = "شلوار جین مدرن در راحتی راحت";
                  // x.FullDescription = "<p>Echte Five-Pocket-Jeans von Joker mit zusätzlicher, aufgesetzter Uhrentasche. Dank Easy Comfort Fit mit normaler Leibhöhe und bequemer Beinweite passend für jeden Figurtyp. Gerader Beinverlauf.</p><ul><li>Material: weicher, leichterer Premium-Denim aus 100% Baumwolle</li><li>Bundweite (Zoll): 29-46</li><li>Beinlänge (Zoll): 30 bis 38</li></ul>";
                  x.FullDescription = "<p> شلوار جین واقعی پنج جیبی توسط جوکر با یک جیب ساعت اضافی. با تشکر از Easy Comfort Fit</p> " +
                "<ul> " +
                "<li> ماده: جین ممتاز و سبک تر از 100٪ پنبه ساخته شده </li> " +
                "<li> اندازه دور کمر (اینچ): 29-46 </li> " +
                "<li> طول پا (اینچ): 30 تا 38 </li> " +
                "</ul>";
               })
               .Alter("jack-1305851", x =>
               {
                   x.Name = "ژاکت کانوکا";
                   x.ShortDescription = "کت مردانه";
                   x.FullDescription = "<p>طراحی اسپرت برای تورهای ورزشی ژاکت نرم از موارد کشسان ساخته شده است. جنس پارچه به صورتی می باشد که با خیال راحت می توانید در کنار قله قدم بزنید. مواد پارچه مقاوت بسیار زیادی دارد و شما در باد شدید هم نگرانی ندارید</p>";
               })
              .Alter("Wolfskin-4032541", x =>
              {
                  x.Name = "کفش راحتی مردانه";
                  x.ShortDescription = "کفش های تفریحی مردانه";
                  x.FullDescription = "<p>شما همیشه در سفر هستید: سینما می روید و یا بار تازه افتتاح شده بروید</p>";
              })
              .Alter("Adidas-C77124", x =>
              {
                  x.Name = "کفش سوپر استار";
                  x.MetaTitle = "کفش سوپر استار";
                  x.ShortDescription = "در خیابان با انگشت شل کلاسیک می پوشد. ";
                  x.FullDescription = "<p>آدیداس سوپراستار اولین بار در سال 1969 منتشر شد و خیلی زود نام آن را بر سر زبانها آورد. امروزه این کفش  را یک افسانه سبک خیابانی می دانند. در این نسخه، کفش دارای یک رویه راحت ساخته شده از چرم کامل است.. </p>";
              });
        }

        private void AlterFurnitureProducts(IList<Product> entities)
        {
            entities.WithKey(x => x.Sku)
               .Alter("Furniture-lc6", x =>
               {
                   x.Name = "میز نهار خوری lc6";
                   x.ShortDescription = "میز ناهار خوری LC6 ، طراح: Le Corbusier ، W x H x D: 225 x 69/74 . زیر سازی: فولاد ، قسمت بالای شیشه: شفاف.";
                   x.FullDescription = "<p>ساختار لوله ای فولادی در میز LC6 بسیار زیبا رخ نمای دارد.این میز در ادارات و سالن ها کاربرد بسیار زیادی دارن. قابلیت تنظیم ارتفاع دارد که می توانید متناسب به نیاز خود آنرا اندازه کنید.</p>";
               })
                   .Alter("Furniture-ball-chair", x =>
                   {
                       x.Name = "صندلی توپی آرینو (1966)";
                       x.FullDescription = "<p>صندلی توپی یک شاهکار واقعی توسط طراح افسارنه ای ایرو آرنیو می باشد. این صندلی در دهه 60 میلادی به ثبت رسید. صندلی تخم مرغی شکل بر روی یک پایه شیپور قرار می گیرد و به خصوص به دلیل شکل و فضای آرام درون این مبلمان، ارزش ویژه ای دارد. طراحی بدنه مبلمان اجازه می دهد تا سرو صدا و عناصر نگران کننده دنیای خارج به پس زمینه وارد شوند. مکانی برای استراحت می باشد که با انتخاب رنگ صندلی به طرز ماهرانه ای با محیط زندگی و کار شما سازگار می شود. </p>";
                   })
                   .Alter("Furniture-lounge-chair", x =>
                   {
                       x.Name = "صندلی اتاق نشیمن چارلز ایامز (1956) ";
                       x.ShortDescription = "صندلی راحتی پوبی، طراح: Charles Eames، عرض: 80 سانتی متر ، طول:80 سانتی متر ، ارتفاع: 60 سانتی متر ، پوسته صندلی: تخته سه لا ، پایه (قابل چرخش): آلومینیوم ریخته شده، کوسن (روکش دار) با روکش چرم.";
                       x.FullDescription = "<p> اینگونه است که شما در یک دستکش بیس بال می نشینید. حداقل این یکی از ایده هایی بود که چارلز ایمز هنگام طراحی این صندلی باشگاه در ذهن داشت. صندلی اتاق نشیمن باید یک صندلی راحتی باشد که بتوانید در آن غرق شوید. چارلز ایمز با سه پوسته صندلی بهم پیوسته، متحرک و روکش مبلمان راحتی موفق به اجرای این طرح شد. صندلی چوبی با پایه مفصل گردان در واقع در تضاد با ویژگی های باهاوس است که مینیمالیسم و ​​عملکرد را در پیش زمینه قرار می دهد. با این وجود ، این یک کلاسیک در تاریخ باهاوس شد و هنوز هم راحتی و راحتی را در بسیاری از اتاق های نشیمن و کلوپ ها فراهم می کند. </ p> <p> ابعاد: عرض 80 سانتی متر ، عمق 60 سانتی متر ، ارتفاع کل 80 سانتی متر (پشتی ارتفاع: 60 سانتی متر) CBM: 0.70. پایه قابل چرخش ساخته شده از آلومینیوم ریخته گری سیاه با لبه های صیقلی به صورت اختیاری کاملاً کروم کاری شده. روکش مبلمان و روکش های ظریف و چرمی</p>";
                   })
                   .Alter("Furniture-cube-chair", x =>
                   {
                       x.Name = "صندلی یوزف هافمن کوبوس (1910) ";
                       x.ShortDescription = "صندلی کوبوس ، طراح: Josef Hoffmann ، عرض 93 سانتی متر، عمق 72 سانتی متر، ارتفاع 77 سانتی متر، قاب پایه: چوب راش جامد، روکش مبلمان: کف پلی اورتان جامد (از نظر ابعاد پایدار) ، پوشش: چرم";
                       x.FullDescription = "<p>صندلی راحتی کوبوس توسط یوزف هافمن طراحی شده. هم از نظر ساخت و هم از نظر طراحی از مریع های زیادی تشکیل شده است. علاوره بار این ها شکل کاملا هندسی از نوعی کوبیسم بوده. صندلی توسط یوزف هافمن در سال 1910 طراحی شده است و هنوز هم در بسیاری از اتقاهای تجاری و نشیمن ها استفاده می شود</p><p>در این مکعب یک صندلی چوبی بوده که به همراه مبل دو و سه نفره یک فضای دنج با ظاهری زیبا و پیچیده را ایجاد می کند. پایه صندلی از چوب ساخته شده.  </p><p>ابعاد:عرض 93 سانتی متر، طول:72 سانتی متر، ارتفاع 77 سانتی متر</p>";
                   })
                   .Alter("LC2 DS/23-1", x =>
                   {
                       x.Name = "مبل 3 نفره لوکوربوزیه LC2 (1929)";
                       x.MetaTitle = "مبل 3 نفره لوکوربوزیه LC2 (1929)";
                       x.ShortDescription = "مبل 3 نفره LC 2 ، طراح: لوکوربوزیه ، قاب فولادی کروم ، بالشتک های ساخته شده از کف پلی اورتان و پنبه، بالشتک های صندلی با روکش رویی، روکش: چرم ";
                       x.FullDescription = "<p> مبل 3 نفره LC2، بهترین گزینه برای صندلی معروف Corbusier است. نتیجه استفاده یک نشیمن کاملاً شکل گرفته برای لابی ها، شیروانی ها یا سالن ها با استانداردهای طراحی بالا ارائه می شود. کاناپه های کوربوزیه مخفف اختصار طراح (LC) است، آنها توسط وی طراحی نشده اند. آنها فقط بر اساس مبلمان نشیمن LC2 و LC3 طراحی شده توسط لوکوربوزیه بنا شده اند. با این حال ، این تاثیری در ظاهر و راحتی آن ندارد. قاب این مبل از یک قاب فولادی لوله ای کاملاً خمیده شده در کروم تشکیل شده است. کوسن های چرمی از کف پلی اورتان و پارچه داکرون پر شده اند. برای راحتی بهینه در صندلی مبل نیز با پرهای پایین روکش شده است. </p><p> ابعاد:  180 * 70 * 67 سانتی متر ، ارتفاع صندلی: تقریباً 45 سانتی متر < p>";
                   })
                  .Alter("JH DS/82-1", x =>
                  {
                      x.Name = "مبل یوزف هافمن 2 نفره کوبوس (1910)";
                      x.MetaTitle = "مبل یوزف هافمن 2 نفره کوبوس (1910)";
                      x.ShortDescription = "مبل 2 نفره کوبوس، طراح: Josef Hoffmann ، عرض 166 سانتی متر، طول 72 سانتی متر ، ارتفاع 77 سانتی متر ، قاب پایه: چوب راش ، تودوزی: کف پلی اورتان جامد، پوشش: چرم";
                      x.FullDescription = "<p>مبل دو نفره از سری کوبوس توسط یوزف هافمن، چشم نواز، شیک در فضاهای زندگی و تجاری است. به همراه صندلی کوبوس سه نفره، یک گروه صندلی شیک برای سالن های پذیرایی و اتاق های بزرگ نشیمن ایجاد شده است. مبل یوزف هافمن با روکش چرم و به لطف یک روش خیاطی خاص ، مربع های زیادی را نشان می دهد که یک تصویر کلی را تشکیل می دهد. قاب پایه از چوب راش ساخته شده است. شکل کاملاً هندسی این طرح هافمن نیز برای کوبیسم پیشگام بود ، که در اوایل قرن 20 به اوج خود رسید.</p> <p> ابعاد: عرض 166 سانتی متر ، طول 72 سانتی متر ، ارتفاع 77 سانتی متر ، </p>";
                  })
                   .Alter("LR 556", x =>
                   {
                       x.Name = "میس ون دیر روه بارسلونا (1929)";
                       x.MetaTitle = "میس ون دیر روه بارسلونا (1929)";
                       x.ShortDescription = "صندلی بلند بارسلونا ، طراح:میس ون دیر روه بارسلونا، اندازه  L x D x H: 147 x 75 x 75 سانتی متر ، قاب کرومی ساخته شده از فولاد مخصوص، پوشش: نوارهای چرمی، روکش مبلمان با هسته فوم پلی اورتان، پوشش: چرم";
                       x.FullDescription = "<p> مبل Loveseat بارسلونا یکی از مشهورترین مبلمان مربوط به دوران باهاوس است. توسط میس ون در روهه طراحی و در نمایشگاه جهانی بارسلونا در سال 1929 ارائه شد. میس ون آن را به زوج سلطنتی اسپانیا تقدیم کرد. نسخه بزرگ مبل بارسلونا به عنوان صندلی برای نمایشگاه ها یا محل های تجاری ایده آل است. بهمراه طراحی باریک و یک میز توسط Mies van der Rohe ، یک مکان نشیمن شیک برای اتاق نشیمن نیز ایجاد شده است. مبل Loveseat بارسلونا دارای قاب ساخته شده از استیل مخصوص با کیفیت بالا است. نوارهای چرمی هم به عنوان پوشش هستند. روکش های چرمی در بالا است. مربع های مجزا تصویری شیک و متقارن ایجاد می کنند. </ p><p> ابعاد: طول 147 سانتی متر ، عرض 75 سانتی متر ، ارتفاع 75 سانتی متر</p>";
                   })
                   .Alter("IN 200", x =>
                   {
                       x.Name = "میز قهوه خوری Isamu Noguchi ، میز قهوه (1945) ";
                       x.MetaTitle = "میز قهوه خوری Isamu Noguchi ، میز قهوه (1945) ";
                       x.ShortDescription = "میز قهوه ، طراح: Isamu Noguchi ابعاد: 128 x 40 x 92.5 سانتی متر ، پایه: چوب ، روی میز: شیشه کریستال ، 15 یا 19 میلی متر";
                       x.FullDescription = "<p> میز قهوه Isamu Noguchi زمانی رئیس موزه هنرهای مدرن نیویورک را تحت تأثیر قرار داد. در ابتدا برای او طراحی شده بود. پایه خاکستر خمیده چشم نواز و زیبا است. به نظر می رسد محجوب است و به لطف صفحه شیشه ای سه طرفه شفاف به خودی خود ظاهر می شود. یک مبلمان باهاوس که اکنون به عنوان میز کنار در بسیاری از اتاق ها با مبلمان پیشرفته استفاده می شود. کاملاً در سالن ، اتاق نشیمن و اتاق پذیرایی متناسب است. </ p><p> ابعاد: عرض 128 سانتی متر ، ارتفاع 40 سانتی متر ، طول 92.5 سانتی متر </p>";
                   })
                   .Alter("LM T/98", x =>
                   {
                       x.Name = "میز بارسلونا Ludwig Mies van der Rohe Tisch Barcelona (1930)";
                       x.MetaTitle = "Ludwig Mies van der Rohe Tisch Barcelona (1930)";
                       x.ShortDescription = "میز بارسلونا ، طراح: Mies van der Rohe. ابعاد: عرض 90 سانتی متر ، ارتفاع 46 سانتی متر ، طول 90 سانتی متر ، پایه: فولاد تخت با روکش کروم ، رومیزی میز: شیشه (12 میلی متر)";
                       x.FullDescription = "<p> این میز توسط میس ون روهه با سری صندلی ها و چهارپایه های معروف بارسلونا مطابقت دارد که برای پادشاه اسپانیا طراحی شده و در نمایشگاه جهانی 1929 ارائه شده است. میز قهوه خوری فقط مدتی بعد توسط میس ون روهه برای خانه Tugendhat ساخته شد ، اما همراه با مبلمان از سری بارسلونا یک فضای نشیمن جذاب برای دفاتر و محل زندگی تشکیل می دهد. میز ساخته شده توسط میس ون روهه شامل یک قاب ساخته شده از فولاد تخت و یک صفحه شیشه ای به ضخامت 12 میلی متر است. در زیر صفحه شفاف ،با روکش کروم  می باشد. </ p><p> ابعاد: عرض 90 سانتی متر ، ارتفاع 46 سانتی متر ، طول 90 سانتی متر ، صفحه شیشه ای: 12 میلی متر </p>";
                   });
        }

        protected override void Alter(IList<Product> entities)
        {
            base.Alter(entities);

            try
            {
                //string ps3FullDescription = "<table cellspacing=\"0\" cellpadding=\"1\"><tbody><tr><td>Prozessortyp&nbsp;</td><td>Cell Processor&nbsp;</td></tr><tr><td>Arbeitsspeicher (RAM)nbsp;</td><td>256 MB&nbsp;</td></tr><tr><td>Grafikchipsatz&nbsp;</td><td>nVidia RSX&nbsp;</td></tr><tr><td>Taktfrequenz&nbsp;</td><td>3.200 MHz&nbsp;</td></tr><tr><td>Abmessungen&nbsp;</td><td>290 x 60 x 230 mm&nbsp;</td></tr><tr><td>Gewicht&nbsp;</td><td>2.100 g&nbsp;</td></tr><tr><td>Speichermedium&nbsp;</td><td>Blu-ray&nbsp;</td></tr><tr><td>Stromverbrauch in Betrieb&nbsp;</td><td>190 Watt&nbsp;</td></tr><tr><td>Plattform&nbsp;</td><td>Playstation 3 (PS3)&nbsp;</td></tr><tr><td>Akku-Laufzeit&nbsp;</td><td>0 h&nbsp;</td></tr><tr><td>Anschlüsse&nbsp;</td><td>2x USB 2.0, AV-Ausgang, digitaler optischer Ausgang (SPDIF), HDMI&nbsp;</td></tr><tr><td>Soundmodi&nbsp;</td><td>AAC, Dolby igital, Dolby Digital Plus, Dolby TrueHD, DTS, DTS-HD, LPCM 7.1-Kanal&nbsp;</td></tr><tr><td>Unterstützte Auflösungen&nbsp;</td><td>576i, 576p, 720p, 1080i, 1080p Full HD&nbsp;</td></tr><tr><td>Serie&nbsp;</td><td>Sony Playstation 3&nbsp;</td></tr><tr><td>Veröffentlichungsjahr&nbsp;</td><td>2012&nbsp;</td></tr><tr><td>Mitgelieferte Hardware&nbsp;</td><td>Dual Shock 3-Controller&nbsp;</td></tr><tr><td>Farbe&nbsp;</td><td>schwarz&nbsp;</td></tr><tr><td>USK-Freigabe&nbsp;</td><td>0 Jahre&nbsp;</td></tr><tr><td>PEGI-Freigabe&nbsp;</td><td>3 Jahre&nbsp;</td></tr><tr><td>RAM-Typ&nbsp;</td><td>XDR-DRAM&nbsp;</td></tr><tr><td>Controller-Akku-Laufzeit&nbsp;</td><td>30 h&nbsp;</td></tr><tr><td>WLAN-Standard&nbsp;</td><td>IEEE 802.11 b/g&nbsp;</td></tr><tr><td>LAN-Standard&nbsp;</td><td>Gigabit Ethernet (10/100/1000 Mbit/s)&nbsp;</td></tr><tr><td>Daten-Kommunikation&nbsp;</td><td>Bluetooth 2.0 + EDR, Netzwerk (Ethernet), WLAN (Wi-Fi)&nbsp;</td></tr><tr><td>Controller-Eigenschaften&nbsp;</td><td>Beschleunigungssensor, Lagesensor (Gyrosensor), Headset-nschluss, Vibration&nbsp;</td></tr><tr><td>Spielsteuerungen&nbsp;</td><td>Bewegungssteuerung, Controller&nbsp;</td></tr><tr><td>Spielfunktionen&nbsp;</td><td>Community, Kindersicherung, Plattformübergreifendes Spielen, Remote Gaming, Sony PlayStation Network, Sony PlayStation Plus, Streaming (DLNA), Streaming (PlayStation Now/Gaikai)&nbsp;</td></tr><tr><td>Marketplace&nbsp;</td><td>Sony PlayStation Store&nbsp;</td></tr><tr><td>Internetfunktionen&nbsp;</td><td>Chat, Video Chat, Voice Chat, Webbrowser&nbsp;</td></tr><tr><td>Multimedia-Funktionen&nbsp;</td><td>Audio-CD-Wiedergabe, Blu-ray-Wiedergabe, DVD-Wiedergabe, Internet-Radio, Video-Wiedergabe&nbsp;</td></tr><tr><td>Streaming-ienste&nbsp;</td><td>Animax, Lovefilm, Maxdome, Mubi, Music on Demand, Sony Music Unlimited, Sony Video Unlimited, TuneIn, VidZone, Video on Demand, Watchever, YouTube&nbsp;</td></tr><tr><td>Ausstattung</td><td>onlinefähig/eingebautes Netzteil/3D-Ready</td></tr><tr><td>Sonstiges</td><td>bis zu 7 kabellose lageempfindliche Controller (Bluetooth) / PSP-Connectivity / keine Abwärtskompatibilität zu PlayStation 2-Spielen / Herunterladen von Filmen von Hollywood Studios aus dem Playstation Network, übertragbar auf PSP / Toploader-Laufwerk / Cross-Plattform-Funktionen (PS3 und PS Vita): Remote Play (Zugriff auf kompatible Inhalte auf PS3), Cross Buy (Spiel für anderes System kostenlos oder günstiger (online) dazukaufen), Cross-Goods (In-Game-Objekte für beide Systeme), Cross-Save (gespeichertes Spiel auf anderem System weiterspielen), Cross-Controller (PS Vita als Controller), Cross-Play (PSV vs. PS3), PlayStation Network-Konto erforderlich / 256 MB GDDR3 Grafikspeicher&nbsp;</td></tr></tbody></table>";
                //string ps4FullDescription = "<ul><li>PlayStation 4, die neueste Generation des Entertainment Systems, definiert reichhaltiges und beeindruckendes Gameplay, völlig neu.</li><li>Den Kern der PS4 bilden ein leistungsstarker, eigens entwickelter Chip mit acht x86-Kernen (64 bit) sowie ein hochmoderner optimierter Grafikprozessor.</li><li>Ein neuer, hochsensibler SIXAXIS-Sensor ermöglicht mit dem DualShock 4 Wireless Controller eine erstklassige Bewegungssteuerung.</li><li>Der DualShock 4 bietet als Neuerungen ein Touchpad, eine Share-Taste, einen eingebauten Lautsprecher und einen Headset-Anschluss.</li><li>PS4 integriert Zweitbildschirme, darunter PS Vita, Smartphones und Tablets, damit Spieler ihre Lieblingsinhalte überall hin mitnehmen können.</li></ul>";

                entities.WithKey(x => x.MetaTitle)

                #region Category Sports

                #region Category Golf

                .Alter("Titleist SM6 Tour Chrome", x =>
                {
                    x.Name = "چوب گلف Titleist Sm6";
                    x.ShortDescription = "برای گلف بازانی که حداکثر کنترل و بازخورد را می خواهند.";
                    x.FullDescription = "<p><strong> با الهام از بهترین بازیکنان آهنین جهان </ strong></p><p> چوب های جدید 'Spin Milled 6' کلاس عملکردی جدیدی را در سه قسمت اصلی بازی ایجاد می کنند: طول دقیق مراحل ، تنوع ضربه و حداکثر چرخش. & nbsp؛ </ p><p><br/> مرکز  به صورت جداگانه برای هر انبار تعیین می شود. بنابراین ، SM6 کنترل دقیق طول و منحنی پرواز را در ارتباط با احساس عالی ارائه می دهد. <br /> کف خرد کن های مورد استفاده Bob Vokey به همه گلف بازان اجازه می دهد ضربات بیشتری بزنند، متناسب با مشخصات و شرایط زمینی مربوطه. </p><p><br/> شیار جدیدی کاملا دقیق اضافه شد و کنترل کامل را به شما می دهد،</p><p></p><ul><li> به لطف مرکز ثقل خوب، طول دقیق و کنترل منحنی پرواز وجود دارد. </li><li> شیارهای TX4 به لطف وضوح جدید سطح و لبه چرخش بیشتری ایجاد می کنند. </ li><li> گزینه های متنوع شخصی سازی. <li></ul><p></p><p></p> <p></p> ";
                })
                .Alter("Titleist Pro V1x", x =>
                {
                    x.Name = "توپ گلف Titleist پرو";
                    x.ShortDescription = "توپ گلف با پرواز توپ بالا";
                    x.FullDescription = "<p> بازیکنان برتر به Titleist Pro V1x جدید اعتماد دارند. پرواز با توپ، احساس نرم و چرخش بیشتر در بازی کوتاه از مزایای نسخه V1x است. عملکرد کلی عالی از تولید کننده پیشرو. توپ جدید گلف Titleist Pro V1 دقیقاً تعریف شده است و نوید پرواز نفوذی توپ با احساس بسیار نرم را می دهد. </p>";
                })
                .Alter("Supreme Golfball", x =>
                {
                    x.Name = "توپ عالی گلف";
                    x.ShortDescription = "توپ های آموزشی با ویژگی های پرواز کامل";
                    x.FullDescription = "<p> توپ تمرینی گلف عالی با همان خصوصیات 'اصلی' ، اما در یک طرح ضد شکستگی شیشه. هسته جامد ، یک توپ تمرینی ایده آل برای حیاط و باغ. رنگها: سفید ، زرد ، نارنجی. </p>";
                })
                .Alter("GBB Epic Sub Zero Driver", x =>
                {
                    x.Name = "چوب گلف GBB";
                    x.ShortDescription = "چرخش کم برای گلف خوب!";
                    x.FullDescription = "<p> بازی شما با این توپ برنده می شود. یک باشگاه گلف با چرخش بسیار کم و ویژگی های فوق العاده و سرعت بالا. </p>";
                })

                #endregion Category Golf

                #region Category Soccer

                .Alter("Nike Strike Football", x =>
                {
                    x.Name = "توپ فوتبال نایک";
                    x.ShortDescription = "احساس توپ عالی. دید خوب ";
                    x.FullDescription = "<p> هر روز با استفاده از توپ Nike Strike Football بازی خود را بهبود ببخشید.جنس توپ از لاستیک تقویت شده که شکل خود را برای کنترل مطمئن و سازگار حفظ می کند. یشکل ظاهر توپ که برجسته ، به رنگ سیاه ، سبز و نارنجی می باشد با وجود شرایط تاریک یا نامناسب برای ردیابی توپ بهترین حالت می باشد </ p> <p> </p> <ul> <li>شکل ظاهری توپ باعث می شود که در مسیر پرواز آنرا بخوبی مشاهده کنید. </ li> <li> جنس بافت آن ایجاد لمس فوق العاده ای را ارائه می دهد. </ li> <li> لاستیکی تقویت شده به حفظ هوا و شکل کمک می کند. </ li> <li> 66٪ لاستیک / 15٪ پلی اورتان / 13٪ پلی استر / 7٪ EVA. </li> </ul>";
                })
                .Alter("Evopower 5.3 Trainer HS Ball", x =>
                {
                    x.Name = "توپ اوو پاور تمرینی";
                    x.ShortDescription = "توپ آموزش مبتدیان.";
                    x.FullDescription = "<p> توپ آموزش مبتدیان. <br /> از 32 سطح برابر برای کاهش درزهای کاهش یافته و کاملاً گرد ساخته شده است. <br /> شک گلدوزی دستی ، پشتی چند لایه بافته شده برای پایداری و آیرودینامیک بیشتر. </p>";
                })
                .Alter("Torfabrik official game ball", x =>
                {
                    x.Name = "توپ فروتاب Torfabrik";
                    x.ShortDescription = "توپ آموزش مبتدیان.";
                    x.FullDescription = "<p> توپ آموزش مبتدیان. <br /> از 32 سطح برابر برای کاهش درزهای کاهش یافته و کاملاً گرد ساخته شده است. <br /> شک گلدوزی دستی ، پشتی چند لایه بافته شده برای پایداری و آیرودینامیک بیشتر. </p>";
                })
                .Alter("Adidas TANGO SALA BALL", x =>
                {
                    x.Name = "توپ آدیداس سالا بال";
                    x.ShortDescription = "رنگ سفید / سیاه / قرمز خورشیدی";
                    x.FullDescription = "<h2>توپ سالا بال</h2>توپ سالا بال آدیداس&nbsp; مخصوص تمرینات سخت و درگیری های شدید برای زمین فوتبال ساخته شده است. بهترین امتیاز فیفا رادریافت کرده.در ساخت آن بدنه دست دوز دارد که هیچ تمرین و بازی ای نمی تواند به آن آسیب برساند.< br > ";
                })

                #endregion Category Soccer

                #region Category Basketball

                .Alter("Evolution High School Game Basketball", x =>
                {
                    x.Name = "توپ بسکتبال دبیرستانی";
                    x.ShortDescription = "برای همه موقعیت ها در همه سطوح";
                    x.FullDescription = "<p>توپ بازی بسکتبال Wilson Evolution High School دارای ساخت چرم کامپوزیت میکرو و الیاف با سنگ ریزه های برجسته عمیق است تا بیشترین احساس و کنترل را به شما بدهد. </p><p> فناوری Cushion Core ثبت اختراع باعث افزایش ماندگاری برای بازی طولانی تر می شود. < /p>";
                })
                .Alter("All-Court Basketball", x =>
                {
                    x.Name = "توپ بسکتبال تمام زمین";
                    x.ShortDescription = "بسکتبال با دوام برای تمام سطوح";
                    x.FullDescription = "<p> </p> <div> <h2> توپ بسکتبال تمام زمین </h2> <h4> بسکتبال با دوام برای تمام سطوح </ h4> <div class = 'product-details-description clearfix'> <div class = 'prod-details para-small' itemprop = 'description'> </div> <div class = 'prod-details para-small' itemprop = 'description'> چه روی پارکت باشد و چه روی آسفالت - All-Court آدیداس توپ آماده فقط یک هدف دارد: سبد. این توپ بسکتبال از چرم مصنوعی بادوام ساخته شده است ، که آن را هم برای زمین های داخلی و هم برای بازی های فضای باز ایده آل می کند. </div> <div class = 'prod-details para-small' itemprop = 'description'> </div> <ul class = 'bullets_list para-small'> <li> پوشش کامپوزیت ساخته شده از چرم مصنوعی </ li> <li> مناسب برای داخل و خارج از منزل </ li> <li> بدون تورم تحویل داده می شود </li> </ul> </div> </div>";
                })

                #endregion Category Basketball

                #endregion Category Sports

                #region Category Sunglasses

                .Alter("Radar EV Prizm Sports Sunglasses", x =>
                {
                    x.Name = "عینک آفتابی Radar EV Prizm Sports";
                    x.ShortDescription = "";
                    x.FullDescription = "<p> <strong> RADAR & nbsp؛ مسیر EV & nbsp؛ PRIZM & nbsp؛ راه </ strong> </p> <p> نقطه عطفی جدید در تاریخ طراحی عملکرد: Radar® EV ترکیبی از نوآوری. یک طراحی انقلابی با یک لیوان بزرگتر برای یک میدان دید بزرگ. </ p> <p> <strong> ویژگی ها </ strong> </ p> <ul> <li> PRIZM technology یک فناوری جدید لنز از اوکلی است که دید را برای شرایط خاص ورزشی و محیطی بهینه می کند. </ li> <li> برای حداکثر جریان هوا برای تهویه خنک کننده طراحی شده است </ li> <li> قلاب گوش و بالشتک های بینی ساخته شده از Unobtainium® برای جا دادن ایمن عینک ، که حتی باعث تعریق بیشتر می شود < / li> <li> سیستم لنز قابل تعویض برای تغییر لنزها در چند ثانیه برای تنظیم بهینه دید در هر محیط ورزشی </ li> </ul>";
                })
                .Alter("Custom Flak Sunglasses", x =>
                {
                    x.Name = "عینک آفتابی سفارشی Flak®";
                    x.ShortDescription = "";
                    x.FullDescription = "هر عینک برای شما ساخته شده است.";
                })
                .Alter("Ray-Ban Top Bar RB 3183", x =>
                {
                    x.Name = "عینک Ray-Ban Top Bar RB 3183";
                    x.ShortDescription = "";
                    x.FullDescription = "<p> عینک آفتابی Ray-Ban ® RB3183 با شکل آیرودینامیکی یادآور سرعت است. یک شکل مستطیل و آرم کلاسیک Ray-Ban که روی بازوها چاپ شده است </ p> <p> مشخصه این مدل نیمه لبه سبک است. </ p>";
                })
                .Alter("ORIGINAL WAYFARER AT COLLECTION", x =>
                {
                    x.Name = "عینک ارژینال WAYFARER";
                    x.ShortDescription = "عینک Ray-Ban Original Wayfarer معروف ترین سبک در تاریخ عینک آفتابی است. با طراحی اصلی ان در سال 1952 ، از Wayfarer در بین مشاهیر ، موسیقی دانان ، هنرمندان و متخصصان مد محبوب است.";
                    x.FullDescription = "";
                })

                #endregion Category Sunglasses

                #region Category Gift Cards

                .Alter("$10 Virtual Gift Card", x =>
                {
                    x.Name = "10 کوپن هدیه";
                    x.ShortDescription = "10 ریال گواهی هدیه. ایده ایده آل ایده آل.";
                    x.FullDescription = "<p> اگر هدیه ای در لحظه آخر  میخواهید تهیه نماید و یا نمی دانید چه چیزی هدیه دهید می توانید یک کوپن هدیه برای اون خریداری نمایید. </ p>";
                })
                .Alter("$25 Virtual Gift Card", x =>
                {
                    x.Name = "25 کوپن هدیه";
                    x.ShortDescription = "25 ریال گواهی هدیه. ایده ایده آل ایده آل.";
                    x.FullDescription = "<p> اگر هدیه ای در لحظه آخر  میخواهید تهیه نماید و یا نمی دانید چه چیزی هدیه دهید می توانید یک کوپن هدیه برای اون خریداری نمایید. </ p>";
                })
                .Alter("$50 Virtual Gift Card", x =>
                {
                    x.Name = "50 کوپن هدیه";
                    x.ShortDescription = "50 ریال گواهی هدیه. ایده ایده آل ایده آل.";
                    x.FullDescription = "<p> اگر هدیه ای در لحظه آخر  میخواهید تهیه نماید و یا نمی دانید چه چیزی هدیه دهید می توانید یک کوپن هدیه برای اون خریداری نمایید. </ p>";
                })
                .Alter("$100 Virtual Gift Card", x =>
                {
                    x.Name = "100 کوپن هدیه";
                    x.ShortDescription = "100 ریال گواهی هدیه. ایده ایده آل ایده آل.";
                    x.FullDescription = "<p> اگر هدیه ای در لحظه آخر  میخواهید تهیه نماید و یا نمی دانید چه چیزی هدیه دهید می توانید یک کوپن هدیه برای اون خریداری نمایید. </ p>";
                })

                #endregion

                #region Category Books

                .Alter("Überman: The novel", x =>
                {
                    x.Name = "اوبرمن: رمان";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })
                .Alter("Best Grilling Recipes", x =>
                {
                    x.Name = "بهترین دستور العمل های گریل";
                    x.ShortDescription = "بیش از 100 مورد دستور پخت کباب دلخواه برای آشپزی در فضای باز ";
                    x.FullDescription = "";
                })
                .Alter("Cooking for Two", x =>
                {
                    x.Name = "پخت و پز برای دو نفر";
                    x.ShortDescription = "200+ دستور العمل بی عیب و نقص برای آخر هفته ها ";
                    x.FullDescription = "";
                })
                .Alter("Car of superlatives", x =>
                {
                    x.Name = "اتومبیل های فوق العاده: قویترین ، اولین ، زیباترین ، سریعترین";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })
                .Alter("Picture Atlas Motorcycles", x =>
                {
                    x.Name = "موتورسیکلت های اطلس تصویری: با بیش از 350 تصویر درخشان";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })
                .Alter("The Car Book", x =>
                {
                    x.Name = "کتاب ماشین وقایع نگاری بزرگ با بیش از 1200 مدل";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })
                .Alter("Fast Cars", x =>
                {
                    x.Name = "ماشینهای سریع ، تقویم تصویری 2013";
                    x.ShortDescription = "صحافی مارپیچی";
                    x.FullDescription = "";
                })
                .Alter("Motorcycle Adventures", x =>
                {
                    x.Name = "ماجراجویی موتور سیکلت: تکنیک سواری برای مسافران";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })
                .Alter("The Prisoner of Heaven: A Novel", x =>
                {
                    x.Name = "اسیر بهشت";
                    x.ShortDescription = "نسخه Hardback";
                    x.FullDescription = "";
                })

                #endregion Category Books

                // Category Computer is not implemented in shop yet > Add product(s) to InvariantSeedData.Products
                #region Category Computer

                .Alter("Dell Inspiron One 23", x =>
                {
                    x.ShortDescription = "این رایانه شخصی همه چیز در یک 58 سانتی متری (23 اینچ) با پردازنده های قدرتمند نسل 7 Intel® Core en , و صفحه فول اچ دی با امکان صفحه لمسی می باشدد.";
                    x.FullDescription = "<p> رایانه شخصی ال این وان بسیار قدرتمند با ویندوز 8 ، پردازنده Intel® Core ™ i7 ، دیسک سخت بزرگ 2 ترابایتی و درایو Blu-Ray. </p> <p> پردازنده Intel® Core ™ i7-3770S (حافظه پنهان 3.1 گیگاهرتز ، 6 مگابایت) ویندوز 8 64 بیتی ، آلمانی و 8 گیگابایتی DDR3 SDRAM با سرعت 1600 مگاهرتز - 2 ترابایت هارد سریال ATA (7.200 دور در دقیقه) ) <br> 1 گیگابایت AMD Radeon HD 7650 <br> </p>";
                    x.Price = 589.00M;
                    x.DeliveryTime = _defaultDeliveryTime;
                    x.ManageInventoryMethod = ManageInventoryMethod.DontManageStock;
                    x.OrderMinimumQuantity = 1;
                    x.OrderMaximumQuantity = 10000;
                    x.StockQuantity = 10000;
                    x.NotifyAdminForQuantityBelow = 1;
                    x.AllowBackInStockSubscriptions = false;
                    x.Published = true;
                    x.IsShippingEnabled = true;
                })
                .Alter("Dell Optiplex 3010 DT Base", x =>
                {
                    x.ShortDescription = "پیشنهاد ویژه: 500 هزار ریال تخفیف اضافی برای همه دسکتاپ های Dell OptiPlex  کوپن آنلاین: W8DWQ0ZRKTM1 ، تا آذر سال 99 معتبر است";
                    x.FullDescription = "<p> همچنین این سیستم همراه است </ p> <p> 1 سال خدمات پایه - خدمات بعدی روز کاری بعدی در سایت - </ p> <p> گزینه های زیر گزینه های استاندارد موجود در سفارش شما هستند . </ p>";
                    x.Price = 419.00M;
                    x.DeliveryTime = _defaultDeliveryTime;
                    x.ManageInventoryMethod = ManageInventoryMethod.DontManageStock;
                    x.OrderMinimumQuantity = 1;
                    x.OrderMaximumQuantity = 10000;
                    x.StockQuantity = 10000;
                    x.NotifyAdminForQuantityBelow = 1;
                    x.AllowBackInStockSubscriptions = false;
                    x.Published = true;
                    x.IsShippingEnabled = true;
                })
                .Alter("Acer Aspire One 8.9", x =>
                {
                    x.Name = "کیف مینی نوت بوک Acer Aspire One 8.9 ' - (مشکی)";
                    x.ShortDescription = "ایسر Aspire One ، نت بوک انقلابی و سرگرم کننده در اندازه کوچک 8.9 اینچی";
                    x.FullDescription = "<p> از همان لحظه فشار دکمه روشن / خاموش ، Aspire One فقط در چند ثانیه آماده استفاده است. دیگر ، کار بسیار ساده است: یک دفتر کار خانگی، کار کنید ، بازی کنید و زندگی خود را در حال حرکت سازمان دهید.  </p>";
                    x.Price = 210.60M;
                    x.DeliveryTime = _defaultDeliveryTime;
                    x.ManageInventoryMethod = ManageInventoryMethod.DontManageStock;
                    x.OrderMinimumQuantity = 1;
                    x.OrderMaximumQuantity = 10000;
                    x.StockQuantity = 10000;
                    x.NotifyAdminForQuantityBelow = 1;
                    x.AllowBackInStockSubscriptions = false;
                    x.Published = true;
                    x.IsShippingEnabled = true;
                })

                #endregion Category Computer 

                #region Category Apple

                .Alter("iPhone Plus", x =>
                {
                    x.Name = "آیفون پلاس";
                    x.ShortDescription = "این آیفون است. سیستم های جدید دوربین پیشرفته است. بهترین عملکرد و عمر باتری آیفون که تاکنون وجود داشته است. بلندگوهای استریو چشمگیر و درخشان ترین نمایشگر آیفون. با رنگ های بیشتر. محافظت در برابر آب. و به نظر می رسد بسیار عالی است. این است آیفون.";
                    x.FullDescription = "";
                })
                .Alter("AirPods", x =>
                {
                    x.Name = "ایرپاد";
                    x.ShortDescription = "ساده. بي سيم. جادویی ، آنها را از قاب خارج می کنید و آنها برای همه دستگاه های شما آماده هستند. آنها را در گوش خود قرار می دهید و بلافاصله به هم متصل می شوند. شما در آن صحبت می کنید و صدای شما به وضوح شنیده می شود.";
                    x.FullDescription = "<p> <br /> AirPods برای همیشه نحوه استفاده از هدفون را تغییر می دهد. وقتی AirPods را از قاب شارژ خارج می کنید ، آنها روشن می شوند و به iPhone ، iPad ، Mac یا Apple Watch شما متصل می شوند. </p>";
                })
                .Alter("Ultimate Apple Pro Hipster Bundle", x =>
                {
                    x.Name = "بسته محصولات اپل پرو";
                    x.ShortDescription = "با این مجموعه 5٪ پس انداز کنید!";
                    x.FullDescription = "<p> به عنوان یک طرفدار اپل ، نیاز اصلی شما همیشه داشتن جدیدترین محصولات اپل است. & nbsp؛ </p>";
                })
                .Alter("9,7' iPad", x =>
                {
                    x.ShortDescription = "این فقط سرگرم کننده است. یاد بگیرید ، بازی کنید ، گشت و گذار کنید ، خلاقیت به خرج دهید. با iPad ، صفحه نمایش باورنکردنی ، عملکرد عالی و برنامه هایی برای هر کاری که دوست دارید انجام دهید.";
                    x.FullDescription = "<ul> <li> نمایشگر شبکیه 9.7 اینچی با True Tone و پوشش ضد انعکاس (مورب 24.63 سانتی متر) </li> <li> تراشه A9X نسل سوم با معماری  64 بیتی </ li> <li> حسگر اثر انگشت </ li> </ul>";
                })
                .Alter("Watch Series 2", x =>
                {
                    x.Name = "سری 2 تماشا";
                })

                #endregion Category Apple

                #region Category Digital Goods & Instant Downloads

                .Alter("Antonio Vivaldi: spring", x =>
                {
                    x.Name = "آنتونیو ویوالدی: بهار";
                    x.ShortDescription = "MP3, 320 kbit/s";
                    x.FullDescription = "";
                })
                .Alter("Ludwig van Beethoven: Für Elise", x =>
                {
                    x.Name = "لودویگ ون بتهوون: برای الیز";
                    x.ShortDescription = "لودویگ ون بتهوون: برای الیز. یکی از محبوب ترین ساخته های بتهوون.";
                    x.FullDescription = "<p> بتهوون اولین نسخه از \"کرنملودی\" [5] را که در سال 1973 شناخته شد ، در کتابچه ای برای دامبانی یادداشت کرد. برخی از صفحات حذف شده از کتاب طراحی ، امضای امروز Mus را تشکیل می دهد. </p>";
                })
                .Alter("Ebook 'Stone of the Wise' in 'Lorem ipsum'", x =>
                {
                    x.Name = "لودویگ ون بتهوون: برای الیز";
                    x.ShortDescription = "صفحه 465 کتاب الکترونیک";
                })

                #endregion Category Digital Goods & Instant Downloads

                #region Category Watches

                .Alter("Certina DS Podium Big Size", x =>
                {
                    x.Name = "کرنوگراف مردانه Certina DS Podium Big Size";
                    x.ShortDescription = "کرونوگراف ترانس اقیانوس زیبایی تجارتی کلنوگرافی های کلاسیک دهه های 1950 و 1960 را به سبک کاملا معاصر هماهنگ کرده است.";
                    x.FullDescription = "";
                })
                .Alter("TRANSOCEAN CHRONOGRAPH", x =>
                {
                    x.Name = "کرونوگرافی ترنسسی";
                    x.ShortDescription = "کرونوگراف ترانس اقیانوس زیبایی تجارتی کلنوگرافی های کلاسیک دهه های 1950 و 1960 را به سبک کاملا معاصر هماهنگ کرده است.";
                    x.FullDescription = "<p>کرونوگراف ترانس اقیانوس زیبایی تجارتی کلنوگرافی های کلاسیک دهه های 1950 و 1960 را به سبک کاملا معاصر هماهنگ کرده است.</p>";
                })
                .Alter("Tissot T-Touch Expert Solar", x =>
                {
                    x.Name = "ساعت تسکو تی تاچ";
                    x.ShortDescription = "تاج Tissot T-Touch Expert Solar روی صفحه تضمین می کند که شاخص ها و عقربه های که پوشش داده شده با Super-LumiNova® در تاریکی درخشان شده و باتری ساعت را شارژ می کنند.";
                    x.FullDescription = "<p> T-Touch Expert Solar یک مدل مهم جدید در محدوده Tissot است. </ p> <p> روح پیشگام Tissot همان چیزی است که منجر به ایجاد ساعت های لمسی در سال 1999 شد. </ p> <p> نتیجه این است امروز اولین کسی است که یک ساعت صفحه نمایش لمسی با انرژی خورشیدی را به نمایش می گذارد </ p>";
                })
                .Alter("Seiko Mechanical Automatic SRPA49K1", x =>
                {
                    x.Name = "ساعت اتوماتیک سیکو SRPA49K1";
                    x.ShortDescription = "یک همراه عالی برای زندگی روزمره! ساعت اتوماتیک ظریف با طراحی جذاب خود همه را تحت تأثیر قرار می دهد و تقریباً همه لباس ها را به شکلی کامل تکمیل می  کند.";
                    x.FullDescription = "<p> <strong> ساعت اتوماتیک Seiko 5 Sport SRPA49K1 SRPA49 </strong> </p> <p> </p> <ul> <li> حاشیه چرخشی یک طرفه </ li> <li> نمایش روز و تاریخ </ li> <li> از پشت مورد دیدن کنید </li> <li> مقاومت در برابر آب 100M </li> <li> مورد از جنس استنلس استیل </li> <li> حرکت خودکار </li> <li> 24 جواهرات </ li > <li> کالیبر: 4R36 </li> </ul>";
                })

                #endregion Category Watches

                #region Category Gaming

                .Alter("Playstation 3 Super Slim", x =>
                {
                    x.ShortDescription = "Sony PlayStation 3 کنسول چند رسانه ای برای نسل بعدی سرگرمی های خانگی دیجیتال است. با فناوری Blu-Ray می توانید از فیلم های HD لذت ببرید.";
                    x.FullDescription = "";
                })
                .Alter("DUALSHOCK 3 Wireless Controller", x =>
                {
                    x.ShortDescription = "کنترل کننده بی سیم DUALSHOCK®3 برای PlayStation®3 مجهز به فناوری حسگر حرکت و سنسور فشار SIXAXIS the ، بصری ترین تجربه گیم پلی را ارائه می دهد.";
                    x.FullDescription = "<ul> <li> <h4> وزن و ابعاد </ h4> <ul> <li> اندازه و وزن (تقریباً): 27 .5 23.5 cm 4 سانتی متر ؛ 191 گرم </ li> </ul> </li> </ul>";
                })
                .Alter("Assassin's Creed III", x =>
                {
                    x.ShortDescription = "با داشتن زرادخانه ای گسترده از سلاح ها از جمله تیر و کمان ، تپانچه ، توماهاوک ، و امضای Order of Assassin's Blade مخالفان خود را از بین ببرید. شهرهای پرجمعیت را در امتداد مرز گسترده و خطرناک طبیعت پر کنید.";
                    x.FullDescription = "<p> در پس زمینه انقلاب آمریکا در اواخر قرن هجدهم ، Assassin's Creed III قهرمان جدیدی را ارائه می دهد: Ratohnhaké: ton ، که بخشی آمریکایی است و بخشی دیگر انگلیسی تبار است. او خود را کانر می نامد و صدای جدید عدالت در جنگ باستان بین قاتلان و معبدین می شود. بازیکن در پیچیده ترین و روان ترین تجربه نبرد، در جنگ برای آزادی و علیه استبداد به یک آدم کش تبدیل می شود. Assassin's Creed III شامل انقلاب آمریکا می شود و بازیکن را به سفری در مرزهای پر جنب و جوش ، شهرهای استعماری گذشته ، به میادین نبرد با اختلاف و هرج و مرج می کشاند </ p>";
                })
                .Alter("PlayStation 3 Assassin's Creed III Bundle", x =>
                {
                    x.ShortDescription = "کنسول 500 گیگابایتی PlayStation®3 ، کنترل کننده های بی سیم 2 × DUALSHOCK®3 و Assassin's Creed® III.";
                    x.FullDescription = "";
                    x.BundleTitleText = "مجموعه محصولات متشکل از";
                })
                .Alter("PlayStation 4", x =>
                {
                    x.Name = "پلی استیشن 4";
                    x.ShortDescription = "پلی استیشن 4 با همکاری برخی از خلاق ترین ذهنان صنعت توسعه یافته است و یک بازی خیره کننده و منحصر به فرد را ارائه می دهد.";
                    x.FullDescription = "";
                })
                .Alter("Playstation 4 Pro", x =>
                {
                    x.Name = "پی استیشن 4 پرو";
                    x.ShortDescription = "Sony PlayStation 4 Pro کنسول چند رسانه ای برای نسل بعدی سرگرمی های خانگی دیجیتال است. این فناوری Blu-ray را ارائه می دهد که به شما امکان می دهد از فیلم های با کیفیت بالا لذت ببرید.";
                    x.FullDescription = "";
                })
                .Alter("FIFA 17 - PlayStation 4", x =>
                {
                    x.Name = "پی استیشن 4 به همراه فیفا 17";
                    x.ShortDescription = "طراحی شده توسط Frostbite";
                    x.FullDescription = "<ul> <li> طراحی شده توسط Frostbite: یکی از موتورهای بازی سازی پیشرو در صنعت ، Frostbite. بازیکنان را به دنیای جدید فوتبال می برد و شخصیت های پر از عمق و احساسات را در FIFA 17 به طرفداران معرفی می کند. </li> <li> در FIFA 17 دنیای کاملاً جدیدی را تجربه کنید زیرا در این فراز و نشیب احساسی راه خود را طی می کنید . < li></ul>";
                })
                .Alter("Horizon Zero Dawn - PlayStation 4", x =>
                {
                    x.Name = "پلی استیشن 4 به همراه بازی Horizon Zero Dawn";
                    x.ShortDescription = "جهانی سرسبز و زنده را زندگی کنید که موجودات مکانیکی مرموزی در آن زندگی می کنند";
                    x.FullDescription = "<Ul> <li> جهانی سرسبز پس از آخرالزمانی - ماشین ها چگونه بر این جهان تسلط داشتند و هدف آنها چیست؟ چه بر سر تمدن آمده است؟ برای کشف گذشته خود و کشف بسیاری از اسرار یک سرزمین فراموش شده ، گوشه گوشه امپراتوری مملو از آثار باستانی و بناهای مرموز را کاوش کنید</ ul>";
                })
                .Alter("LEGO Worlds - PlayStation 4", x =>
                {
                    x.Name = "پلی استیشن 4 به همراه بازی LEGO Worlds";
                    x.ShortDescription = "کهکشانی را که کاملاً از آجرهای LEGO ساخته شده است تجربه کنید.";
                    x.FullDescription = "<Ul> <Li> کهکشان جهانی را که کاملاً از آجرهای LEGO ساخته شده است تجربه کنید. </ Li> <Li> LEGO Worlds یک محیط باز از جهانهای فرایند تولید شده است که کاملاً از آجرهای LEGO تشکیل شده است ، که می تواند با استفاده از مدلهای LEGO آزادانه دستکاری شده و به صورت پویا پر شود. </ Li> <Li> هر آنچه می توانید تصور کنید ، با آجر بسازید </ Ul> <P> </ P>";
                })
                .Alter("Minecraft - Playstation 4 Edition", x =>
                {
                    x.Name = "پی استیشن 4 ادیشن به همراه برای ماین کرافت";
                    x.ShortDescription = "مجموعه ای سوم شخص اکشن و ماجراجویی.";
                    x.FullDescription = "<P> ساختار! هنر! کاوش کنید! </p> <p> Minecraft با تحسین منتقدان در حال آمدن به پلی استیشن 4 است و جهان های بزرگتر و مسافت بیشتری را نسبت به نسخه های PS3 و PS Vita ارائه می دهد. </ P> <p> دنیای خود را بسازید ، سپس بسازید. کاوش کنید و تسخیر کنید. وقتی شب فرا می رسد ، هیولاها ظاهر می شوند ، بنابراین قبل از رسیدن آنها حتما یک پناهگاه بسازید. </ P> <p> جهان فقط توسط تخیل شما محدود می شود! دنیای بزرگتر و مسافت بیشتری نسبت به نسخه های PS3 و PS Vita شامل همه ویژگی ها از نسخه PS3 دنیای PS3 و PS Vita خود را به ویرایش PS4 وارد کنید. </ P>";
                })
                .Alter("PlayStation 4 Minecraft Bundle", x =>
                {
                    x.Name = "پلی استیشن 4 با لوازم جانبی و بازی ماین کرافت";
                    x.ShortDescription = "سیستم 100 گیگابایتی PlayStation®4 ، کنترل کننده های بی سیم 2 × DUALSHOCK®4 و Minecraft برای نسخه PS4.";
                    x.FullDescription = "";
                })
                .Alter("DUALSHOCK 4 Wireless Controller", x =>
                {
                    x.Name = "کنترل کنند بی سیسم دوال شاک";
                    x.ShortDescription = "کنترل کننده بی سیم DUALSHOCK ways 4 با ترکیب عناصر کنترل کلاسیک و شیوه های نوآورانه بازی ، کنترل کننده تکاملی برای دوره جدید بازی است.";
                    x.FullDescription = "";
                })
                .Alter("PlayStation 4 Camera", x =>
                {
                    x.Name = "دوربین پلی استیشن 4";
                    x.ShortDescription = "دوربینی که بتواند عمق محیط مقابل خود را درک کند و با کمک نور LED بتواند موقعیت کنترل کننده را در فضای سه بعدی تعیین کند.";
                    x.FullDescription = "<p> دوربین جدید دارای چهار میکروفون است که تشخیص و موقعیت دقیق صدا با آنها امکان پذیر است و از کنترلر حرکت PlayStation Move (شامل نمی شود) با دقت بیشتری نسبت به گذشته پشتیبانی می کند. </ p> <p> <ul> <li> <b> رنگ: </ b> سیاه جت </ li> <li> <b> ابعاد خارجی: </b> تقریباً 186 × 27 × 27 میلی متر (عرض × ارتفاع × عمق) (موقت) </li> <li> <b> وزن: </b> تقریباً 183 گرم (موقت) </li> <li> <b> پیکسل های ویدیو: </b> (حداکثر) 2 × 1280 × 800 پیکسل </ li> < li> <b> نرخ فریم ویدئو: </ b> 1280 × 800 پیکسل با سرعت 60 فریم در ثانیه ، 640 × 400 پیکسل با سرعت 120 فریم در ثانیه ، 320 × 192 پیکسل با سرعت 240 فریم در ثانیه </ li> <li> <b> قالب فیلم: < / b> RAW ، YUV (فشرده نشده) </ li> <li> <b> لنز: </b> دو لنز ، مقدار F / F2.0 کانونی ثابت </ li> <li> <b> منطقه تشخیص </ b> 30 cm ～ ∞ </li> <li> <b> میدان دید </ b> 85 ° </li> <li> <b> میکروفن: </ b> آرایه میکروفن 4 کانال </ چپ> <li> <b> نوع اتصال: </ b> پلاگین ویژه PS4 (پلاگین AUX) </li> <li> <b> طول کابل: </ b> تقریباً 2 متر (موقت) </ li> < p> </p> <p> <i> تولید کننده حق ایجاد تغییرات کوتاه مدت را برای خود محفوظ می داند قدیمی. </i> </p> </ul> </p>";
                })
                .Alter("PlayStation 4 Bundle", x =>
                {
                    x.Name = "پلی استیشن 4 با لوازم جانبی";
                    x.ShortDescription = "کنسول PlayStation®4 ، کنترل کننده بی سیم DUALSHOCK®4 و دوربین PS4.";
                    x.FullDescription = "";
                    x.BundleTitleText = "مجموعه محصولات متشکل از";
                })
                .Alter("Accessories for unlimited gaming experience", x =>
                {
                    x.Name = "لوازم جانبی برای تجربه بازی نامحدود";
                    x.ShortDescription = "آینده بازی اکنون با بازی های پویا ، متصل ، عملکرد و سرعت گرافیکی قوی ، شخصی سازی هوشمند ، مهارت های اجتماعی داخلی و قابلیت های ابتکاری صفحه دوم است. PlayStation® 4 که اوج ابتکاری خلاق ترین ذهن های صنعت است ، یک محیط بازی بی نظیر را ارائه می دهد که نفس شما را می گیرد.";
                    x.FullDescription = " با عملکرد و گرافیک قوی ، خود را در دنیای جدیدی از بازی غرق کنید.";
                })
                .Alter("Watch Dogs", x =>
                {
                    x.ShortDescription = "شهر را هک کنید و آن را سلاح خود قرار دهید. از طریق شهر به صورت پویا حرکت کنید ، از میان ساختمان ها میانبر بروید ، از پشت بام ها عبور کنید و از موانع عبور کنید";
                    x.FullDescription = "";
                })
                .Alter("Prince of Persia", x =>
                {
                    x.Name = "شاهزاده ایرانی \"زمان فراموش شده \"";
                    x.ShortDescription = "با شاهزاده ایرانی: زمان فراموش شده ، داستانی کاملاً جدید از جهان شاهزاده ایرانی ظاهر می شود. ";
                    x.FullDescription = "";
                })
                .Alter("Driver San Francisco", x =>
                {
                    x.ShortDescription = "رئیس اوباش چارلز جریکو دوباره آزاد است و تهدیدی بزرگ برای تمام سانفرانسیسکو محسوب می شود. اکنون فقط یک مرد می تواند جلوی او را بگیرد.";
                    x.FullDescription = "";
                })
                .Alter("Xbox One S 500 GB Konsole", x =>
                {
                    x.Name = "کنسول ایکس باکس وان اس 500 گیگ";
                    x.ShortDescription = "کنسول ایکس باکس وان یک کنسول جدید و پیشرفته می باشد که بعد از ایکس باکس 360 ارائه شده است.";
                    x.FullDescription = "";
                })
                .Alter("PlayStation 3 plus game cheaper", x =>
                {
                    x.Name = "PlayStation 3 plus بازی ارزان تر";
                    x.ShortDescription = "پیشنهاد ویژه ما: پلی استیشن 3 به علاوه یک بازی با انتخاب شما.";
                    x.FullDescription = "";
                });

                #endregion Category Gaming

                AlterFashionProducts(entities);
                AlterFurnitureProducts(entities);
            }
            catch (Exception ex)
            {
                throw new SeedDataException("AlterProduct", ex);
            }
        }

        protected override void Alter(IList<Discount> entities)
        {
            base.Alter(entities);

            var names = new Dictionary<string, string>
            {
                { "10% for certain manufacturers", "تخفیف ده درصدی روی تولید کننده" },
                { "20% order total discount", "تخفیف 20 درصدی" },
                { "20% for certain categories", "تخفیف 20 درصدی برای دسته بندی" },
                { "25% on certain products", "تخفیف 25 درصدی محصولات" },
                { "5% on weekend orders", "تخفیف 5 درصدی روی سفارش این هفته" },
                { "Sample discount with coupon code", "تخفیف با کوپن تخفیف" },
            };

            var alterer = entities.WithKey(x => x.Name);

            foreach (var kvp in names)
            {
                alterer.Alter(kvp.Key, x => x.Name = kvp.Value);
            }
        }

        protected override void Alter(IList<DeliveryTime> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.DisplayOrder)
                .Alter(0, x => x.Name = "ارسال فوری")
                .Alter(1, x => x.Name = "2-5 روز کاری")
                .Alter(2, x => x.Name = "7 روزه");

            _defaultDeliveryTime = entities.First();
        }

        protected override void Alter(IList<QuantityUnit> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.DisplayOrder)
                 .Alter(0, x =>
                 {
                     x.Name = "قطعه";
                     x.NamePlural = "قطعه";
                     x.Description = "قطعه";
                 })
                 .Alter(1, x =>
                 {
                     x.Name = "جعبه";
                     x.NamePlural = "جعبه";
                     x.Description = "جعبه";
                 })
                 .Alter(2, x =>
                 {
                     x.Name = "بسته بندی";
                     x.NamePlural = "بسته بندی";
                     x.Description = "بسته بندی";
                 })
                 .Alter(3, x =>
                 {
                     x.Name = "جعبه رنگ نقاشی";
                     x.NamePlural = "جعبه رنگ نقاشی";
                     x.Description = "جعبه رنگ نقاشی";
                 })
                 .Alter(4, x =>
                 {
                     x.Name = "واحد";
                     x.NamePlural = "واحد";
                     x.Description = "واحد";
                 })
                 .Alter(5, x =>
                 {
                     x.Name = "کیسه";
                     x.NamePlural = "کیسه";
                     x.Description = "کیسه";
                 })
                 .Alter(6, x =>
                 {
                     x.Name = "کیسه";
                     x.NamePlural = "کیسه";
                     x.Description = "کیسه";
                 })
                 .Alter(7, x =>
                 {
                     x.Name = "دوزها";
                     x.NamePlural = "دوزها";
                     x.Description = "دوز";
                 })
                 .Alter(8, x =>
                 {
                     x.Name = "بسته";
                     x.NamePlural = "بسته ها";
                     x.Description = "بسته";
                 })
                 .Alter(9, x =>
                 {
                     x.Name = "میله ها";
                     x.NamePlural = "میله ها";
                     x.Description = "میله ها";
                 })
                 .Alter(10, x =>
                 {
                     x.Name = "بطری";
                     x.NamePlural = "بطری";
                     x.Description = "بطری";
                 })
                 .Alter(11, x =>
                 {
                     x.Name = "شیشه";
                     x.NamePlural = "عینک";
                     x.Description = "شیشه";
                 })
                 .Alter(12, x =>
                 {
                     x.Name = "فدراسیون";
                     x.NamePlural = "فدراسیون";
                     x.Description = "فدراسیون";
                 })
                 .Alter(13, x =>
                 {
                     x.Name = "نقش";
                     x.NamePlural = "نقش";
                     x.Description = "نقش";
                 })
                 .Alter(14, x =>
                 {
                     x.Name = "یک فنجان";
                     x.NamePlural = "یک فنجان";
                     x.Description = "یک فنجان";
                 })
                 .Alter(15, x =>
                 {
                     x.Name = "دسته";
                     x.NamePlural = "دسته";
                     x.Description = "دسته";
                 })
                 .Alter(16, x =>
                 {
                     x.Name = "بشکه";
                     x.NamePlural = "بشکه";
                     x.Description = "بشکه";
                 })
                 .Alter(17, x =>
                 {
                     x.Name = "تنظیم";
                     x.NamePlural = "تنظیم";
                     x.Description = "تنظیم";
                 })
                 .Alter(18, x =>
                 {
                     x.Name = "سطل";
                     x.NamePlural = "سطل";
                     x.Description = "سطل";
                 });
        }

        protected override void Alter(IList<Store> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.DisplayOrder)
                .Alter(1, x =>
                {
                    x.Name = "نام فروشگاه";
                    x.Hosts = "my-shop.ir,www.my-shop.ir";
                });
        }

        protected override void Alter(IList<ProductTag> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Name)
                .Alter("gift", x => { x.Name = "هدیه"; })
                .Alter("book", x => { x.Name = "کتاب"; })
                .Alter("cooking", x => { x.Name = "پختن"; })
                .Alter("cars", x => { x.Name = "ماشین ها"; })
                .Alter("motorbikes", x => { x.Name = "موتور سیکلت"; })
                .Alter("download", x => { x.Name = "دانلود"; })
                .Alter("watches", x => { x.Name = "ساعت"; });
        }

        protected override void Alter(IList<EmailAccount> entities)
        {
            //base.Alter(entities);

            //entities.WithKey(x => x.DisplayName)
            //    .Alter("General contact", x =>
            //    {
            //        x.DisplayName = "Kontakt";
            //        x.Email = "kontakt@meineshopurl.de";
            //        x.Host = "localhost";
            //    })
            //    .Alter("Sales representative", x =>
            //    {
            //        x.DisplayName = "Vertrieb";
            //        x.Email = "vertrieb@meineshopurl.de";
            //        x.Host = "localhost";
            //    })
            //    .Alter("Customer support", x =>
            //    {
            //        x.DisplayName = "Kundendienst / Support";
            //        x.Email = "kundendienst@meineshopurl.de";
            //        x.Host = "localhost";
            //    });
        }

        protected override void Alter(IList<Campaign> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.Name)
                .Alter("Reminder of inactive new customers", x =>
                {
                    x.Name = "یادآوری مشتریان جدید غیرفعال";
                    x.Subject = "محصولات جدید و هیجان انگیز در انتظار شما هستند تا با آنها آشنا شوید.";
                });
        }

        protected override void Alter(IList<RuleSetEntity> entities)
        {
            base.Alter(entities);

            base.Alter(entities);

            entities.WithKey(x => x.Name)
                .Alter("Weekends", x => x.Name = "آخر هفته ها")
                .Alter("Major customers", x =>
                {
                    x.Name = "مشتریان مهم";
                    x.Description = "3 سفارش یا بیشتر و ارزش سفارش فعلی حداقل 200 ریال.";
                })
                .Alter("Sale", x =>
                {
                    x.Name = "فروش";
                    x.Description = "محصولات با تخفیف اعمال شده.";
                })
                .Alter("Inactive new customers", x =>
                {
                    x.Name = "مشتریان جدید غیر فعال";
                    x.Description = "یک سفارش تکمیل شده که حداقل 90 روز از ان گذشته باشد.";
                });
        }

        protected override void Alter(IList<PriceLabel> entities)
        {
            base.Alter(entities);

            entities.WithKey(x => x.ShortName)
                .Alter("MSRP", x =>
                {
                    x.ShortName = "قیمت پیشنهادی";
                    x.Name = "قیت پیشنهادی خرده فروشی";
                    x.Description = "قیمت خرده فروشی پیشنهادی یک پیشنهاد یا پیشنهادی یک محصول است که توسط سازنده تنظیم شده و توسط سازنده، تامین کننده یا فروشنده ارائه می شود.";
                })
                .Alter("Lowest", x =>
                {
                    x.ShortName = "پاینترین";
                    x.Name = "کمترین قیمت اخیر";
                    x.Description = "این کمترین قیمت محصول در 30 روز گذشته قبل از اعمال تخفیف است.";
                })
                .Alter("Regular", x =>
                {
                    x.ShortName = "میانگین";
                    x.Name = "قیمت میانگین";
                    x.Description = "  میانگین قیمت فروش است که توسط مشتریان برای یک محصول پرداخت می شود، به استثنای قیمت های تبلیغاتی";
                });
        }
    }
}