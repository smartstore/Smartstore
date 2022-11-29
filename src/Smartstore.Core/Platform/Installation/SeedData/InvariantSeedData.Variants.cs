using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Installation
{
    public abstract partial class InvariantSeedData
    {
        public IList<ProductAttribute> ProductAttributes()
        {
            var entities = new List<ProductAttribute>
            {
                new ProductAttribute
                {
                    Name = "Color",
                    Alias = "color"
                },
                new ProductAttribute
                {
                    Name = "Custom Text",
                    Alias = "custom-text"
                },
                new ProductAttribute
                {
                    Name = "HDD",
                    Alias = "hdd"
                },
                new ProductAttribute
                {
                    Name = "OS",
                    Alias = "os"
                },
                new ProductAttribute
                {
                    Name = "Processor",
                    Alias = "processor"
                },
                new ProductAttribute
                {
                    Name = "RAM",
                    Alias = "ram",
                },
                new ProductAttribute
                {
                    Name = "Size",
                    Alias = "size"
                },
                new ProductAttribute
                {
                    Name = "Software",
                    Alias = "software"
                },
                new ProductAttribute
                {
                    Name = "Game",
                    Alias = "game"
                },
                new ProductAttribute
                {
                    Name = "Color",
                    Alias = "iphone-color"
                },
                new ProductAttribute
                {
                    Name = "Color",
                    Alias = "ipad-color"
                },
                new ProductAttribute
                {
                    Name = "Memory capacity",
                    Alias = "memory-capacity"
                },
                new ProductAttribute
                {
                    Name = "Width",
                    Alias = "width"
                },
                new ProductAttribute
                {
                    Name = "Length",
                    Alias = "length"
                },
                new ProductAttribute
                {
                    Name = "Plate",
                    Alias = "plate"
                },
                new ProductAttribute
                {
                    Name = "Plate Thickness",
                    Alias = "plate-thickness"
                },
                new ProductAttribute
                {
                    Name = "Ballsize",
                    Alias = "ballsize"
                },
                new ProductAttribute
                {
                    Name = "Leather color",
                    Alias = "leather-color"
                },
                new ProductAttribute
                {
                    Name = "Seat Shell",
                    Alias = "seat-shell"
                },
                new ProductAttribute
                {
                    Name = "Base",
                    Alias = "base"
                },
                new ProductAttribute
                {
                    Name = "Material",
                    Alias = "material"
                },
                new ProductAttribute
                {
                    Name = "Style",
                    Alias = "style"
                },
                new ProductAttribute
                {
                    Name = "Controller",
                    Alias = "controller"
                },
                new ProductAttribute
                {
                    Name = "Framecolor",
                    Alias = "framecolor"
                },
                new ProductAttribute
                {
                    Name = "Lenscolor",
                    Alias = "lenscolor"
                },
                new ProductAttribute
                {
                    Name = "Lenstype",
                    Alias = "lenstype"
                },
                new ProductAttribute
                {
                    Name = "Lenscolor",
                    Alias = "wayfarerlenscolor"
                },
                new ProductAttribute
                {
                    Name = "Framecolor",
                    Alias = "wayfarerframecolor"
                }
            };

            Alter(entities);
            return entities;
        }

        public IList<ProductAttributeOptionsSet> ProductAttributeOptionsSets()
        {
            var entities = new List<ProductAttributeOptionsSet>();
            var colorAttribute = _db.ProductAttributes.First(x => x.Alias == "color");

            entities.Add(new ProductAttributeOptionsSet
            {
                Name = "General colors",
                ProductAttributeId = colorAttribute.Id
            });

            Alter(entities);
            return entities;
        }

        public IList<ProductAttributeOption> ProductAttributeOptions()
        {
            var entities = new List<ProductAttributeOption>();
            var colorAttribute = _db.ProductAttributes.First(x => x.Alias == "color");
            var sets = _db.ProductAttributeOptionsSets.ToList();

            var generalColors = new[]
            {
                new { Color = "Red", Code = "#ff0000" },
                new { Color = "Green", Code = "#008000" },
                new { Color = "Blue", Code = "#0000ff" },
                new { Color = "Yellow", Code = "#ffff00" },
                new { Color = "Black", Code = "#000000" },
                new { Color = "White", Code = "#ffffff" },
                new { Color = "Gray", Code = "#808080" },
                new { Color = "Silver", Code = "#dddfde" },
                new { Color = "Brown", Code = "#a52a2a" },
            };

            for (var i = 0; i < generalColors.Length; ++i)
            {
                entities.Add(new ProductAttributeOption
                {
                    ProductAttributeOptionsSetId = sets[0].Id,
                    Alias = generalColors[i].Color.ToLower(),
                    Name = generalColors[i].Color,
                    Quantity = 1,
                    DisplayOrder = i + 1,
                    Color = generalColors[i].Code
                });
            }

            Alter(entities);
            return entities;
        }

        public IList<ProductVariantAttribute> ProductVariantAttributes()
        {
            var entities = new List<ProductVariantAttribute>();
            var attrColor = _db.ProductAttributes.First(x => x.Alias == "color");
            var attrController = _db.ProductAttributes.First(x => x.Alias == "controller");
            var attrSize = _db.ProductAttributes.First(x => x.Alias == "size");
            var attrGames = _db.ProductAttributes.First(x => x.Alias == "game");
            var attrBallsize = _db.ProductAttributes.First(x => x.Alias == "ballsize");
            var attrMemoryCapacity = _db.ProductAttributes.First(x => x.Alias == "memory-capacity");
            var attrLensType = _db.ProductAttributes.First(x => x.Alias == "lenstype");
            var attrFramecolor = _db.ProductAttributes.First(x => x.Alias == "framecolor");
            var attrLenscolor = _db.ProductAttributes.First(x => x.Alias == "lenscolor");
            var attrIphoneColor = _db.ProductAttributes.First(x => x.Alias == "iphone-color");
            var attr97iPadColor = _db.ProductAttributes.First(x => x.Alias == "ipad-color");
            var attrWidth = _db.ProductAttributes.First(x => x.Alias == "width");
            var attrLength = _db.ProductAttributes.First(x => x.Alias == "length");
            var attrPlate = _db.ProductAttributes.First(x => x.Alias == "plate");
            var attrPlateThickness = _db.ProductAttributes.First(x => x.Alias == "plate-thickness");
            var attrLeatherColor = _db.ProductAttributes.First(x => x.Alias == "leather-color");
            var attrSeatShell = _db.ProductAttributes.First(x => x.Alias == "seat-shell");
            var attrBase = _db.ProductAttributes.First(x => x.Alias == "base");
            var attrMaterial = _db.ProductAttributes.First(x => x.Alias == "material");
            var attrWayfarerLenscolor = _db.ProductAttributes.First(x => x.Alias == "wayfarerlenscolor");
            var attrWayfarerFramecolor = _db.ProductAttributes.First(x => x.Alias == "wayfarerframecolor");

            var generalColors = new[]
            {
                new { Name = "Black", Color = "#000000" },
                new { Name = "White", Color = "#ffffff" },
                new { Name = "Anthracite", Color = "#32312f" },
                new { Name = "Fuliginous", Color = "#5F5B5C" },
                new { Name = "Light grey", Color = "#e3e3e5" },
                new { Name = "Natural", Color = "#BBB98B" },
                new { Name = "Biscuit", Color = "#e0ccab" },
                new { Name = "Beige", Color = "#d1bc8a" },
                new { Name = "Hazelnut", Color = "#94703e" },
                new { Name = "Brown", Color = "#755232" },
                new { Name = "Dark brown", Color = "#27160F" },
                new { Name = "Dark green", Color = "#0a3210" },
                new { Name = "Blue", Color = "#0000ff" },
                new { Name = "Cognac", Color = "#e9aa1b" },
                new { Name = "Yellow", Color = "#e6e60c" },
                new { Name = "Orange", Color = "#ff6501" },
                new { Name = "Tomato red", Color = "#b10101" },
                new { Name = "Red", Color = "#fe0000" },
                new { Name = "Dark red", Color = "#5e0000" }
            };

            var products = (_db.Products.ToList()).ToDictionarySafe(x => x.Sku, x => x);

            #region Oakley custom flak

            var productCustomFlak = products["P-3002"];
            var attributeLensType = new ProductVariantAttribute()
            {
                Product = productCustomFlak,
                ProductAttribute = attrLensType,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.RadioList
            };

            attributeLensType.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Standard",
                Alias = "standard",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 100.0M
            });

            attributeLensType.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Polarized",
                Alias = "polarized",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 200.0M
            });
            attributeLensType.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Prizm",
                Alias = "prizm",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 200.0M
            });

            entities.Add(attributeLensType);


            var attributeFramecolor = new ProductVariantAttribute()
            {
                Product = productCustomFlak,
                ProductAttribute = attrFramecolor,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            attributeFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Matte Black",
                Alias = "matteblack",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#2d2d2d"
            });

            attributeFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Polishedwhite",
                Alias = "polishedwhite",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#f5f5f5"
            });

            attributeFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Sky Blue",
                Alias = "skyblue",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#4187f6"
            });

            attributeFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Orange Flare",
                Alias = "orangeflare",
                DisplayOrder = 4,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#f55700"
            });

            attributeFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Redline",
                Alias = "redline",
                DisplayOrder = 5,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#cf0a02"
            });

            entities.Add(attributeFramecolor);

            var attributeLenscolor = new ProductVariantAttribute()
            {
                Product = productCustomFlak,
                ProductAttribute = attrLenscolor,
                IsRequired = true,
                DisplayOrder = 3,
                AttributeControlType = AttributeControlType.Boxes
            };

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Gray",
                Alias = "gray",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#7A798B"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Sapphire Iridium",
                Alias = "sapphireiridium",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#4460BB"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Violet Iridium",
                Alias = "violetiridium",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#5C5A89"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Jade Iridium",
                Alias = "jadeiridium",
                DisplayOrder = 4,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#376559"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Ruby Iridium",
                Alias = "rubyiridium",
                DisplayOrder = 5,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#CCAD12"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "24K Iridium",
                Alias = "24kiridium",
                DisplayOrder = 6,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#CE9D12"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Positive Red Iridium",
                Alias = "positiverediridium",
                DisplayOrder = 7,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#764CDC"
            });

            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Clear",
                Alias = "clear",
                DisplayOrder = 7,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#e2e2e3"
            });
            attributeLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Fire Iridium",
                Alias = "fireiridium",
                DisplayOrder = 7,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#E2C724"
            });

            entities.Add(attributeLenscolor);

            #endregion Oakley custom flak

            #region Wayfarer

            var productWayfarer = products["P-3003"];
            var wayfarerFramePictures = _db.MediaFiles.Where(x => x.Name.StartsWith("wayfarer-")).ToList();

            var attributeWayfarerLenscolor = new ProductVariantAttribute()
            {
                Product = productWayfarer,
                ProductAttribute = attrWayfarerLenscolor,
                IsRequired = true,
                DisplayOrder = 3,
                AttributeControlType = AttributeControlType.Boxes
            };

            attributeWayfarerLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Blue-Gray classic",
                Alias = "blue-gray-classic",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#3e4659"
            });

            attributeWayfarerLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Brown course",
                Alias = "brown-course",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#3e4659"
            });

            attributeWayfarerLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Gray course",
                Alias = "gray-course",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#727377"
            });

            attributeWayfarerLenscolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Green classic",
                Alias = "green-classic",
                DisplayOrder = 4,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#3c432e"
            });

            entities.Add(attributeWayfarerLenscolor);

            var attributeWayfarerFramecolor = new ProductVariantAttribute()
            {
                Product = productWayfarer,
                ProductAttribute = attrWayfarerFramecolor,
                IsRequired = true,
                DisplayOrder = 3,
                AttributeControlType = AttributeControlType.Boxes
            };

            var wayfarerFramePicture = wayfarerFramePictures.First(x => x.Name.Contains("-rayban-black"));

            attributeWayfarerFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Black",
                Alias = "rayban-black",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                //Color = "#3e4659"
                MediaFileId = wayfarerFramePicture.Id
            });

            wayfarerFramePicture = wayfarerFramePictures.First(x => x.Name.Contains("-havana-black"));
            attributeWayfarerFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Havana; Black",
                Alias = "havana-black",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                //Color = "#3e4659"
                MediaFileId = wayfarerFramePicture.Id
            });

            wayfarerFramePicture = wayfarerFramePictures.First(x => x.Name.Contains("-havana"));
            attributeWayfarerFramecolor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Havana",
                Alias = "havana",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                //Color = "#727377",
                MediaFileId = wayfarerFramePicture.Id
            });

            entities.Add(attributeWayfarerFramecolor);

            #endregion wayfarer

            #region 9,7 iPad

            var product97iPad = products["P-2004"];
            var attribute97iPadMemoryCapacity = new ProductVariantAttribute()
            {
                Product = product97iPad,
                ProductAttribute = attrMemoryCapacity,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.RadioList
            };

            attribute97iPadMemoryCapacity.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "64 GB",
                Alias = "64gb",
                IsPreSelected = true,
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 100.0M
            });

            attribute97iPadMemoryCapacity.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "128 GB",
                Alias = "128gb",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 200.0M
            });

            entities.Add(attribute97iPadMemoryCapacity);

            var attribute97iPadColor = new ProductVariantAttribute()
            {
                Product = product97iPad,
                ProductAttribute = attr97iPadColor,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Silver",
                Alias = "silver",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#dddfde"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Gold",
                Alias = "gold",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#e3d0ba"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Spacegray",
                Alias = "spacegray",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#abaeb1"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Rose",
                Alias = "rose",
                DisplayOrder = 4,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#d9a6ad"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Mint",
                Alias = "mint",
                DisplayOrder = 5,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#a6dbb1"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Purple",
                Alias = "purple",
                DisplayOrder = 6,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#dba5d7"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Lightblue",
                Alias = "lightblue",
                DisplayOrder = 7,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#a6b9df"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Turquoise",
                Alias = "turquoise",
                DisplayOrder = 8,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#a4dbde"
            });

            attribute97iPadColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Yellow",
                Alias = "yellow",
                DisplayOrder = 7,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#dfddb6"
            });

            entities.Add(attribute97iPadColor);

            #endregion 9,7 iPad

            #region iPhone 7 plus

            var productIphone7Plus = products["P-2001"];
            var attributeIphone7PlusMemoryCapacity = new ProductVariantAttribute()
            {
                Product = productIphone7Plus,
                ProductAttribute = attrMemoryCapacity,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.RadioList
            };

            attributeIphone7PlusMemoryCapacity.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "64 GB",
                Alias = "64gb",
                IsPreSelected = true,
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 100.0M
            });

            attributeIphone7PlusMemoryCapacity.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "128 GB",
                Alias = "128gb",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 200.0M
            });

            entities.Add(attributeIphone7PlusMemoryCapacity);


            var attributeIphone7PlusColor = new ProductVariantAttribute()
            {
                Product = productIphone7Plus,
                ProductAttribute = attrIphoneColor,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            attributeIphone7PlusColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Silver",
                Alias = "silver",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#dddfde"
            });

            attributeIphone7PlusColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Gold",
                Alias = "gold",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#e3d0ba"
            });

            attributeIphone7PlusColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Red",
                Alias = "red",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#af1e2d"
            });

            attributeIphone7PlusColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Rose",
                Alias = "rose",
                DisplayOrder = 4,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#d9a6ad"
            });

            attributeIphone7PlusColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Black",
                Alias = "black",
                DisplayOrder = 5,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                Color = "#000000"
            });

            entities.Add(attributeIphone7PlusColor);

            #endregion iPhone 7 plus

            #region Dualshock3ControllerColor

            var productPs3 = products["Sony-PS399000"];
            var attributeDualshock3ControllerColor = new ProductVariantAttribute()
            {
                Product = productPs3,
                ProductAttribute = attrController,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.RadioList
            };

            attributeDualshock3ControllerColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "without controller",
                Alias = "without_controller",
                IsPreSelected = true,
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple
            });

            attributeDualshock3ControllerColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "with controller",
                Alias = "with_controller",
                PriceAdjustment = 60.0M,
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple
            });

            entities.Add(attributeDualshock3ControllerColor);

            #endregion Dualshock3ControllerColor

            #region Apple Airpod

            var productAirpod = products["P-2003"];
            var attributeAirpod = new ProductVariantAttribute()
            {
                Product = productAirpod,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Gold",
                Alias = "gold",
                DisplayOrder = 1,
                Quantity = 1,
                Color = "#e3d0ba",
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 5.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-acreed3").Id
            });

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Rose",
                Alias = "rose",
                DisplayOrder = 2,
                Quantity = 1,
                Color = "#d9a6ad",
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 10.00M,
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-watchdogs").Id
            });

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Mint",
                Alias = "mint",
                DisplayOrder = 3,
                Quantity = 1,
                Color = "#a6dbb1",
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 15.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-princepersia").Id
            });

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Lightblue",
                Alias = "lightblue",
                DisplayOrder = 3,
                Quantity = 1,
                Color = "#a6b9df",
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 15.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-princepersia").Id
            });

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "Turquoise",
                Alias = "turquoise",
                DisplayOrder = 3,
                Quantity = 1,
                Color = "#a4dbde",
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 15.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-princepersia").Id
            });

            attributeAirpod.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "White",
                Alias = "white",
                DisplayOrder = 3,
                Quantity = 1,
                Color = "#ffffff",
                IsPreSelected = true,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 15.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-princepersia").Id
            });

            entities.Add(attributeAirpod);

            #endregion Apple Airpod

            #region Evopower 5.3 Trainer HS Ball

            var productEvopower = products["P-5003"];
            var attributeEvopower = new ProductVariantAttribute()
            {
                Product = productEvopower,
                ProductAttribute = attrBallsize,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.RadioList
            };

            attributeEvopower.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "3",
                Alias = "ballsize-3",
                DisplayOrder = 1,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 5.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-acreed3").Id
            });

            attributeEvopower.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "4",
                Alias = "ballsize-4",
                DisplayOrder = 2,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 10.00M,
                IsPreSelected = true
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-watchdogs").Id
            });

            attributeEvopower.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            {
                Name = "5",
                Alias = "ballsize-5",
                DisplayOrder = 3,
                Quantity = 1,
                ValueType = ProductVariantAttributeValueType.Simple,
                PriceAdjustment = 15.00M
                //LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-princepersia").Id
            });

            entities.Add(attributeEvopower);

            #endregion Evopower 5.3 Trainer HS Ball

            #region Ps3OneGameFree

            //var productPs3OneGameFree = await _db.Products.FirstAsync(x => x.Sku == "Sony-PS310111");

            //var attributePs3OneGameFree = new ProductVariantAttribute()
            //{
            //	Product = productPs3OneGameFree,
            //	ProductAttribute = attrGames,
            //	IsRequired = true,
            //	DisplayOrder = 1,
            //	AttributeControlType = AttributeControlType.DropdownList
            //};

            //attributePs3OneGameFree.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            //{
            //	Name = "Minecraft - Playstation 4 Edition",
            //	Alias = "minecraft-playstation4edition",
            //	DisplayOrder = 1,
            //	Quantity = 1,
            //	ValueType = ProductVariantAttributeValueType.ProductLinkage,
            //	LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "PD-Minecraft4ps4").Id
            //});

            //attributePs3OneGameFree.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            //{
            //	Name = "Watch Dogs",
            //	Alias = "watch-dogs",
            //	DisplayOrder = 2,
            //	Quantity = 1,
            //	ValueType = ProductVariantAttributeValueType.ProductLinkage,
            //	LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Ubi-watchdogs").Id
            //});

            //attributePs3OneGameFree.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            //{
            //	Name = "Horizon Zero Dawn - PlayStation 4",
            //	Alias = "horizon-zero-dawn-playStation-4",
            //	DisplayOrder = 3,
            //	Quantity = 1,
            //	ValueType = ProductVariantAttributeValueType.ProductLinkage,
            //	LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "PD-ZeroDown4PS4").Id
            //});

            //attributePs3OneGameFree.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue()
            //{
            //	Name = "LEGO Worlds - PlayStation 4",
            //             Alias = "lego-worlds-playstation_4",
            //	DisplayOrder = 4,
            //	Quantity = 1,
            //	ValueType = ProductVariantAttributeValueType.ProductLinkage,
            //	LinkedProductId = await _db.Products.FirstAsync(x => x.Sku == "Gaming-Lego-001").Id
            //});

            //entities.Add(attributePs3OneGameFree);

            #endregion Ps3OneGameFree

            #region Fashion - Converse All Star

            var productAllStar = products["Fashion-112355"];
            var allStarColors = new string[] { "Charcoal", "Maroon", "Navy", "Purple", "White" };
            var allStarPictures = _db.MediaFiles.Where(x => x.Name.StartsWith("allstar-")).ToList();

            var attrAllStarColor = new ProductVariantAttribute
            {
                Product = productAllStar,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < allStarColors.Length; ++i)
            {
                var allStarPicture = allStarPictures.First(x => x.Name.Contains(allStarColors[i].ToLower()));
                attrAllStarColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = allStarColors[i],
                    Alias = allStarColors[i].ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    MediaFileId = allStarPicture.Id
                });
            }
            entities.Add(attrAllStarColor);

            var attrAllStarSize = new ProductVariantAttribute
            {
                Product = productAllStar,
                ProductAttribute = attrSize,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };
            attrAllStarSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "42",
                Alias = "42",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrAllStarSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "43",
                Alias = "43",
                DisplayOrder = 2,
                Quantity = 1
            });
            attrAllStarSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "44",
                Alias = "44",
                DisplayOrder = 3,
                Quantity = 1
            });
            entities.Add(attrAllStarSize);

            #endregion

            #region Fashion - Shirt Meccanica

            var productShirtMeccanica = products["Fashion-987693502"];
            var shirtMeccanicaSizes = new string[] { "XS", "S", "M", "L", "XL" };
            var shirtMeccanicaColors = new[]
            {
                new { Color = "Red", Code = "#fe0000" },
                new { Color = "Black", Code = "#000000" }
            };

            var attrShirtMeccanicaColor = new ProductVariantAttribute
            {
                Product = productShirtMeccanica,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < shirtMeccanicaColors.Length; ++i)
            {
                attrShirtMeccanicaColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = shirtMeccanicaColors[i].Color,
                    Alias = shirtMeccanicaColors[i].Color.ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = shirtMeccanicaColors[i].Code,
                    IsPreSelected = shirtMeccanicaColors[i].Color == "Red"
                });
            }
            entities.Add(attrShirtMeccanicaColor);

            var attrShirtMeccanicaSize = new ProductVariantAttribute
            {
                Product = productShirtMeccanica,
                ProductAttribute = attrSize,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < shirtMeccanicaSizes.Length; ++i)
            {
                attrShirtMeccanicaSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = shirtMeccanicaSizes[i],
                    Alias = shirtMeccanicaSizes[i].ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = shirtMeccanicaSizes[i] == "XS"
                });
            }
            entities.Add(attrShirtMeccanicaSize);

            #endregion

            #region Fashion - Ladies Jacket

            var productLadiesJacket = products["Fashion-JN1107"];
            var ladiesJacketSizes = new string[] { "XS", "S", "M", "L", "XL" };
            var ladiesJacketColors = new[]
            {
                new { Color = "Red", Code = "#CE1F1C" },
                new { Color = "Orange", Code = "#EB7F01" },
                new { Color = "Green", Code = "#24B87E" },
                new { Color = "Blue", Code = "#0F8CCE" },
                new { Color = "Navy", Code = "#525671" },
                new { Color = "Silver", Code = "#ABB0B3" },
                new { Color = "Black", Code = "#404040" }
            };

            var attrLadiesJacketColor = new ProductVariantAttribute
            {
                Product = productLadiesJacket,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < ladiesJacketColors.Length; ++i)
            {
                attrLadiesJacketColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = ladiesJacketColors[i].Color,
                    Alias = ladiesJacketColors[i].Color.ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = ladiesJacketColors[i].Code,
                    IsPreSelected = ladiesJacketColors[i].Color == "Red"
                });
            }
            entities.Add(attrLadiesJacketColor);

            var attrLadiesJacketSize = new ProductVariantAttribute
            {
                Product = productLadiesJacket,
                ProductAttribute = attrSize,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.RadioList
            };

            for (var i = 0; i < ladiesJacketSizes.Length; ++i)
            {
                attrLadiesJacketSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = ladiesJacketSizes[i],
                    Alias = ladiesJacketSizes[i].ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = ladiesJacketSizes[i] == "XS"
                });
            }
            entities.Add(attrLadiesJacketSize);

            #endregion

            #region Fashion - Clark Jeans

            var productClarkJeans = products["Fashion-65986524"];
            var clarkJeansWidth = new string[] { "31", "32", "33", "34", "35", "36", "38", "40", "42", "44", "46" };
            var clarkJeansLength = new string[] { "30", "32", "34" };

            var attrClarkJeansWidth = new ProductVariantAttribute
            {
                Product = productClarkJeans,
                ProductAttribute = attrWidth,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < clarkJeansWidth.Length; ++i)
            {
                attrClarkJeansWidth.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = clarkJeansWidth[i],
                    Alias = clarkJeansWidth[i],
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = clarkJeansWidth[i] == "31"
                });
            }
            entities.Add(attrClarkJeansWidth);

            var attrClarkJeansLength = new ProductVariantAttribute
            {
                Product = productClarkJeans,
                ProductAttribute = attrLength,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < clarkJeansLength.Length; ++i)
            {
                attrClarkJeansLength.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = clarkJeansLength[i],
                    Alias = clarkJeansLength[i],
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = clarkJeansLength[i] == "30"
                });
            }
            entities.Add(attrClarkJeansLength);

            #endregion Fashion - Clark Jeans

            #region Furniture - Le Corbusier LC 6 table

            var productCorbusierTable = products["Furniture-lc6"];
            var attrCorbusierTablePlate = new ProductVariantAttribute
            {
                Product = productCorbusierTable,
                ProductAttribute = attrPlate,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };
            attrCorbusierTablePlate.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Clear glass",
                Alias = "clear-glass",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrCorbusierTablePlate.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Sandblasted glass",
                Alias = "sandblasted-glass",
                DisplayOrder = 2,
                Quantity = 1
            });
            entities.Add(attrCorbusierTablePlate);

            var attrCorbusierTableThickness = new ProductVariantAttribute
            {
                Product = productCorbusierTable,
                ProductAttribute = attrPlateThickness,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };
            attrCorbusierTableThickness.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "15 mm",
                Alias = "15mm",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrCorbusierTableThickness.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "19 mm",
                Alias = "19mm",
                DisplayOrder = 2,
                Quantity = 1
            });
            entities.Add(attrCorbusierTableThickness);

            #endregion

            #region Soccer Adidas TANGO SALA BALL

            var productAdidasTANGOSALABALL = products["P-5001"];
            var productAdidasTANGOSALABALLSizes = new string[] { "3", "4", "5" };
            var productAdidasTANGOSALABALLColors = new[]
            {
                new { Color = "Red", Code = "#ff0000" },
                new { Color = "Yellow", Code = " #ffff00" },
                new { Color = "Green", Code = "#008000" },
                new { Color = "Blue", Code = "#0000ff" },
                new { Color = "Gray", Code = "#808080" },
                new { Color = "White", Code = "#ffffff" },
                new { Color = "Brown", Code = "#a52a2a" }
            };

            var attrAdidasTANGOSALABALLColor = new ProductVariantAttribute
            {
                Product = productAdidasTANGOSALABALL,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < productAdidasTANGOSALABALLColors.Length; ++i)
            {
                attrAdidasTANGOSALABALLColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = productAdidasTANGOSALABALLColors[i].Color,
                    Alias = productAdidasTANGOSALABALLColors[i].Color.ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = productAdidasTANGOSALABALLColors[i].Code,
                    IsPreSelected = productAdidasTANGOSALABALLColors[i].Color == "White"
                });
            }
            entities.Add(attrAdidasTANGOSALABALLColor);

            var attrAdidasTANGOSALABALLSize = new ProductVariantAttribute
            {
                Product = productAdidasTANGOSALABALL,
                ProductAttribute = attrSize,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.RadioList
            };

            for (var i = 0; i < productAdidasTANGOSALABALLSizes.Length; ++i)
            {
                attrAdidasTANGOSALABALLSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = productAdidasTANGOSALABALLSizes[i],
                    Alias = productAdidasTANGOSALABALLSizes[i].ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = productAdidasTANGOSALABALLSizes[i] == "5"
                });
            }
            entities.Add(attrAdidasTANGOSALABALLSize);

            #endregion Soccer Adidas TANGO SALA BALL

            #region Torfabrik official game ball

            var productTorfabrikBall = products["P-5002"];
            var productTorfabrikBallSizes = new string[] { "3", "4", "5" };
            var productTorfabrikBallColors = new[]
            {
                new { Color = "Red", Code = "#ff0000" },
                new { Color = "Yellow", Code = " #ffff00" },
                new { Color = "Green", Code = "#008000" },
                new { Color = "Blue", Code = "#0000ff" },
                new { Color = "White", Code = "#ffffff" },
            };

            var attrTorfabrikBallColor = new ProductVariantAttribute
            {
                Product = productTorfabrikBall,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < productTorfabrikBallColors.Length; ++i)
            {
                attrTorfabrikBallColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = productTorfabrikBallColors[i].Color,
                    Alias = productTorfabrikBallColors[i].Color.ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = productTorfabrikBallColors[i].Code,
                    IsPreSelected = productTorfabrikBallColors[i].Color == "White"
                });
            }
            entities.Add(attrTorfabrikBallColor);

            var attrTorfabrikSize = new ProductVariantAttribute
            {
                Product = productTorfabrikBall,
                ProductAttribute = attrSize,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.RadioList
            };

            for (var i = 0; i < productTorfabrikBallSizes.Length; ++i)
            {
                attrTorfabrikSize.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = productTorfabrikBallSizes[i],
                    Alias = productTorfabrikBallSizes[i].ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = productTorfabrikBallSizes[i] == "5"
                });
            }
            entities.Add(attrTorfabrikSize);

            #endregion Soccer Torfabrik official game ball

            #region Furniture - Ball chair

            var productBallChair = products["Furniture-ball-chair"];
            var attrBallChairMaterial = new ProductVariantAttribute
            {
                Product = productBallChair,
                ProductAttribute = attrMaterial,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.DropdownList
            };
            attrBallChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Special",
                Alias = "leather-special",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrBallChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Aniline",
                Alias = "leather-aniline",
                DisplayOrder = 2,
                Quantity = 1
            });
            attrBallChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Mixed Linen",
                Alias = "mixed-linen",
                DisplayOrder = 3,
                Quantity = 1
            });
            entities.Add(attrBallChairMaterial);

            var attrBallChairColor = new ProductVariantAttribute
            {
                Product = productBallChair,
                ProductAttribute = attrColor,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };
            attrBallChairColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "White",
                Alias = "white",
                Color = "#ffffff",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrBallChairColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Black",
                Alias = "black",
                Color = "#000000",
                DisplayOrder = 2,
                Quantity = 1
            });
            entities.Add(attrBallChairColor);

            var attrBallChairLeatherColor = new ProductVariantAttribute
            {
                Product = productBallChair,
                ProductAttribute = attrLeatherColor,
                IsRequired = true,
                DisplayOrder = 3,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < generalColors.Length; ++i)
            {
                attrBallChairLeatherColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = generalColors[i].Name,
                    Alias = generalColors[i].Name.Replace(" ", "-").ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = generalColors[i].Color,
                    IsPreSelected = (generalColors[i].Name == "Tomato red")
                });
            }
            entities.Add(attrBallChairLeatherColor);

            #endregion

            #region Furniture - Lounge chair

            var productLoungeChair = products["Furniture-lounge-chair"];
            var attrLoungeChairMaterial = new ProductVariantAttribute
            {
                Product = productLoungeChair,
                ProductAttribute = attrMaterial,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.DropdownList
            };
            attrLoungeChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Special",
                Alias = "leather-special",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrLoungeChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Aniline",
                Alias = "leather-aniline",
                DisplayOrder = 2,
                Quantity = 1
            });
            entities.Add(attrLoungeChairMaterial);

            var loungeChairSeatShells = new string[] { "Palisander", "Cherry", "Walnut", "Wooden black lacquered" };
            var attrLoungeChairSeatShell = new ProductVariantAttribute
            {
                Product = productLoungeChair,
                ProductAttribute = attrSeatShell,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.DropdownList
            };

            for (var i = 0; i < loungeChairSeatShells.Length; ++i)
            {
                attrLoungeChairSeatShell.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = loungeChairSeatShells[i],
                    Alias = loungeChairSeatShells[i].Replace(" ", "-").ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    IsPreSelected = (i == 0),
                    PriceAdjustment = (loungeChairSeatShells[i] == "Wooden black lacquered" ? 100.00M : decimal.Zero)
                });
            }
            entities.Add(attrLoungeChairSeatShell);

            var attrLoungeChairBase = new ProductVariantAttribute
            {
                Product = productLoungeChair,
                ProductAttribute = attrBase,
                IsRequired = true,
                DisplayOrder = 3,
                AttributeControlType = AttributeControlType.DropdownList
            };
            attrLoungeChairBase.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Top edge polished",
                Alias = "top-edge-polished",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrLoungeChairBase.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Completely polished",
                Alias = "completely-polished",
                DisplayOrder = 2,
                Quantity = 1,
                PriceAdjustment = 150.00M
            });
            entities.Add(attrLoungeChairBase);

            var attrLoungeChairLeatherColor = new ProductVariantAttribute
            {
                Product = productLoungeChair,
                ProductAttribute = attrLeatherColor,
                IsRequired = true,
                DisplayOrder = 4,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < generalColors.Length; ++i)
            {
                attrLoungeChairLeatherColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = generalColors[i].Name,
                    Alias = generalColors[i].Name.Replace(" ", "-").ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = generalColors[i].Color,
                    IsPreSelected = (generalColors[i].Name == "White")
                });
            }
            entities.Add(attrLoungeChairLeatherColor);

            #endregion

            #region Furniture - Cube chair

            var productCubeChair = products["Furniture-cube-chair"];
            var attrCubeChairMaterial = new ProductVariantAttribute
            {
                Product = productCubeChair,
                ProductAttribute = attrMaterial,
                IsRequired = true,
                DisplayOrder = 1,
                AttributeControlType = AttributeControlType.DropdownList
            };
            attrCubeChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Special",
                Alias = "leather-special",
                DisplayOrder = 1,
                Quantity = 1,
                IsPreSelected = true
            });
            attrCubeChairMaterial.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
            {
                Name = "Leather Aniline",
                Alias = "leather-aniline",
                DisplayOrder = 2,
                Quantity = 1,
                PriceAdjustment = 400.00M
            });
            entities.Add(attrCubeChairMaterial);

            var attrCubeChairLeatherColor = new ProductVariantAttribute
            {
                Product = productCubeChair,
                ProductAttribute = attrLeatherColor,
                IsRequired = true,
                DisplayOrder = 2,
                AttributeControlType = AttributeControlType.Boxes
            };

            for (var i = 0; i < generalColors.Length; ++i)
            {
                attrCubeChairLeatherColor.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    Name = generalColors[i].Name,
                    Alias = generalColors[i].Name.Replace(" ", "-").ToLower(),
                    DisplayOrder = i + 1,
                    Quantity = 1,
                    Color = generalColors[i].Color,
                    IsPreSelected = (generalColors[i].Name == "Black")
                });
            }
            entities.Add(attrCubeChairLeatherColor);

            #endregion

            Alter(entities);
            return entities;
        }

        public IList<ProductVariantAttributeCombination> ProductVariantAttributeCombinations()
        {
            var entities = new List<ProductVariantAttributeCombination>();
            var attrColor = _db.ProductAttributes.First(x => x.Alias == "color");
            var attrController = _db.ProductAttributes.First(x => x.Alias == "controller");
            var attrSize = _db.ProductAttributes.First(x => x.Alias == "size");
            var attrMemoryCapacity = _db.ProductAttributes.First(x => x.Alias == "memory-capacity");
            var attrColorIphoneColors = _db.ProductAttributes.First(x => x.Alias == "iphone-color");
            var attr97iPadColors = _db.ProductAttributes.First(x => x.Alias == "ipad-color");
            var attrPlate = _db.ProductAttributes.First(x => x.Alias == "plate");
            var attrPlateThickness = _db.ProductAttributes.First(x => x.Alias == "plate-thickness");
            var attrMaterial = _db.ProductAttributes.First(x => x.Alias == "material");
            var attrLeatherColor = _db.ProductAttributes.First(x => x.Alias == "leather-color");
            var attrSeatShell = _db.ProductAttributes.First(x => x.Alias == "seat-shell");
            var attrBase = _db.ProductAttributes.First(x => x.Alias == "base");
            var attrFlakLenstype = _db.ProductAttributes.First(x => x.Alias == "lenstype");
            var attrFlakFramecolor = _db.ProductAttributes.First(x => x.Alias == "framecolor");
            var attrFlakLenscolor = _db.ProductAttributes.First(x => x.Alias == "lenscolor");
            var attrWayfarerLenscolor = _db.ProductAttributes.First(x => x.Alias == "wayfarerlenscolor");
            var attrWayfarerFramecolor = _db.ProductAttributes.First(x => x.Alias == "wayfarerframecolor");

            #region ORIGINAL WAYFARER AT COLLECTION

            var productWayfarer = _db.Products.First(x => x.Sku == "P-3003");
            var wayfarerPictureIds = productWayfarer.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesWayfarer = _db.MediaFiles.Where(x => wayfarerPictureIds.Contains(x.Id)).ToList();

            var wayfarerLenscolor = _db.ProductVariantAttributes.First(x => x.ProductId == productWayfarer.Id && x.ProductAttributeId == attrWayfarerLenscolor.Id);
            var wayfarerLenscolorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == wayfarerLenscolor.Id).ToList();

            var wayfarerFramecolor = _db.ProductVariantAttributes.First(x => x.ProductId == productWayfarer.Id && x.ProductAttributeId == attrWayfarerFramecolor.Id);
            var wayfarerFramecolorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == wayfarerFramecolor.Id).ToList();

            #region blue-gray-classic-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_blue-gray-classic-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "blue-gray-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "rayban-black").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-blue-gray-classic-black-1")).Id.ToString()
            });

            #endregion blue-gray-classic-black

            #region gray-course-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_gray-course-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "gray-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "rayban-black").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-gray-course-black")).Id.ToString()
            });

            #endregion gray-course-black

            #region brown-course-havana

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_brown-course-havana",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "brown-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-brown-course-havana")).Id.ToString()
            });

            #endregion brown-course-havana

            #region green-classic-havana-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_green-classic-havana-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "green-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana-black").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-green-classic-havana-black")).Id.ToString()
            });

            #endregion green-classic-havana-black

            #region blue-gray-classic-havana-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_blue-gray-classic-havana-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "blue-gray-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana-black").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-blue-gray-classic-black-1")).Id.ToString()
            });

            #endregion green-classic-havana-black

            #region blue-gray-classic-havana

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_blue-gray-classic-havana",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "blue-gray-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-blue-gray-classic-black-1")).Id.ToString()
            });

            #endregion green-classic-rayban-black

            #region gray-course-havana-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_gray-course-havana-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "gray-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana-black").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = true,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-gray-course-black")).Id.ToString()
            });

            #endregion gray-course-havana-black

            #region gray-course-havana

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_gray-course-havana",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "gray-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-gray-course-black")).Id.ToString()
            });

            #endregion gray-course-rayban-black

            #region green-classic-rayban-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_green-classic-rayban-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "green-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "rayban-black").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-green-classic-havana-black")).Id.ToString()
            });

            #endregion green-classic-rayban-black

            #region green-classic-havana

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_green-classic-havana",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "green-classic").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-green-classic-havana-black")).Id.ToString()
            });

            #endregion gray-course-rayban-black

            #region brown-course-havana-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_brown-course-havana-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "brown-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "havana-black").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-brown-course-havana")).Id.ToString()
            });

            #endregion brown-course-havana-black

            #region brown-course-rayban-black

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productWayfarer,
                Sku = productWayfarer.Sku + "_brown-course-rayban-black",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(wayfarerLenscolor.Id, wayfarerLenscolorValues.First(x => x.Alias == "brown-course").Id ),
                    new(wayfarerFramecolor.Id, wayfarerFramecolorValues.First(x => x.Alias == "rayban-black").Id )
                }),
                StockQuantity = 0,
                AllowOutOfStockOrders = true,
                IsActive = false,
                //Price = 299M,
                AssignedMediaFileIds = picturesWayfarer.First(x => x.Name.StartsWith("wayfarer-brown-course-havana")).Id.ToString()
            });

            #endregion brown-course-rayban-black

            #endregion ORIGINAL WAYFARER AT COLLECTION

            #region Custom Flak

            var productFlak = _db.Products.First(x => x.Sku == "P-3002");
            var flakPictureIds = productFlak.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesFlak = _db.MediaFiles.Where(x => flakPictureIds.Contains(x.Id)).ToList();

            //var attributeColorIphone7Plus = await _db.ProductVariantAttributes.FirstAsync(x => x.ProductId == productIphone7Plus.Id && x.ProductAttributeId == attrColor.Id);

            var flakLenscolor = _db.ProductVariantAttributes.First(x => x.ProductId == productFlak.Id && x.ProductAttributeId == attrFlakLenscolor.Id);
            var flakLenscolorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == flakLenscolor.Id).ToList();

            var flakLenstype = _db.ProductVariantAttributes.First(x => x.ProductId == productFlak.Id && x.ProductAttributeId == attrFlakLenstype.Id);
            var flakLenstypeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == flakLenstype.Id).ToList();

            var flakFramecolor = _db.ProductVariantAttributes.First(x => x.ProductId == productFlak.Id && x.ProductAttributeId == attrFlakFramecolor.Id);
            var flakFramecolorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == flakFramecolor.Id).ToList();

            //#region matteblack-gray-standard

            foreach (var lenscolorValue in flakLenscolorValues)
            {
                foreach (var framecolorValue in flakFramecolorValues)
                {

                    foreach (var lenstypeValue in flakLenstypeValues)
                    {
                        try
                        {
                            entities.Add(new ProductVariantAttributeCombination
                            {
                                Product = productFlak,
                                Sku = productFlak.Sku + string.Concat("-", framecolorValue.Alias, "-", lenscolorValue.Alias, "-", lenstypeValue.Alias),
                                RawAttributes = FormatAttributeJson(new List<(int, object)>
                                {
                                    new(flakLenscolor.Id, lenscolorValue.Id ),
                                    new(flakLenstype.Id, lenstypeValue.Id ),
                                    new(flakFramecolor.Id, framecolorValue.Id )
                                }),
                                StockQuantity = 10000,
                                AllowOutOfStockOrders = true,
                                IsActive = true,

                                AssignedMediaFileIds = picturesFlak.First(x => x.Name.Contains(framecolorValue.Alias + "_" + lenscolorValue.Alias)).Id.ToString(),

                                //Price = ballChairPrice
                            });
                        }
                        catch
                        {
                            Console.WriteLine("An error occurred: '{0}'", framecolorValue.Alias + "_" + lenscolorValue.Alias);
                        }
                    }
                }
            }

            #endregion Custom Flak

            #region ps3

            var productPs3 = _db.Products.First(x => x.Sku == "Sony-PS399000");
            var ps3PictureIds = productPs3.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesPs3 = _db.MediaFiles.Where(x => ps3PictureIds.Contains(x.Id)).ToList();

            var productAttributeColor = _db.ProductVariantAttributes.First(x => x.ProductId == productPs3.Id && x.ProductAttributeId == attrController.Id);
            var attributeColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == productAttributeColor.Id).ToList();

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productPs3,
                Sku = productPs3.Sku + "-B",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(productAttributeColor.Id, attributeColorValues.First(x => x.Alias == "with_controller").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesPs3.First(x => x.Name.Contains("-controller")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productPs3,
                Sku = productPs3.Sku + "-W",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(productAttributeColor.Id, attributeColorValues.First(x => x.Alias == "without_controller").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesPs3.First(x => x.Name.Contains("-single")).Id.ToString()
            });

            #endregion ps3

            #region Apple Airpod

            var productAirpod = _db.Products.First(x => x.Sku == "P-2003");
            var airpodPictureIds = productAirpod.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesAirpod = _db.MediaFiles.Where(x => airpodPictureIds.Contains(x.Id)).ToList();

            var airpodAttributeColor = _db.ProductVariantAttributes.First(x => x.ProductId == productAirpod.Id && x.ProductAttributeId == attrColor.Id);
            var airpodAttributeColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == airpodAttributeColor.Id).ToList();

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-gold",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "gold").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-gold")).Id.ToString()
            });
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-rose",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "rose").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-rose")).Id.ToString()
            });
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-mint",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "mint").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-mint")).Id.ToString()
            });
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-lightblue",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "lightblue").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-lightblue")).Id.ToString()
            });
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-turquoise",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "turquoise").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-turquoise")).Id.ToString()
            });
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productAirpod,
                Sku = productAirpod.Sku + "-white",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(airpodAttributeColor.Id, airpodAttributeColorValues.First(x => x.Alias == "white").Id ),
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesAirpod.First(x => x.Name.Contains("-white")).Id.ToString()
            });

            #endregion Apple Airpod

            #region 9,7 Ipad

            var productiPad97 = _db.Products.First(x => x.Sku == "P-2004");
            var iPad97PictureIds = productiPad97.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesiPad97 = _db.MediaFiles.Where(x => iPad97PictureIds.Contains(x.Id)).ToList();

            //var attributeColorIphone7Plus = await _db.ProductVariantAttributes.FirstAsync(x => x.ProductId == productIphone7Plus.Id && x.ProductAttributeId == attrColor.Id);

            var iPad97Color = _db.ProductVariantAttributes.First(x => x.ProductId == productiPad97.Id && x.ProductAttributeId == attr97iPadColors.Id);
            var iPad97ColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == iPad97Color.Id).ToList();

            var ipad97Capacity = _db.ProductVariantAttributes.First(x => x.ProductId == productiPad97.Id && x.ProductAttributeId == attrMemoryCapacity.Id);
            var iPad97CapacityValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ipad97Capacity.Id).ToList();

            #region silver
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-silver-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "silver").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                Price = 299M,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-silver")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "silver-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "silver").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-silver")).Id.ToString()
            });

            #endregion silver

            #region gold
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-gold-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "gold").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                Price = 279M,
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-gold")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "gold-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "gold").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-gold")).Id.ToString()
            });
            #endregion gold

            #region spacegray
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-spacegray-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "spacegray").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-spacegray")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "spacegray-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "spacegray").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-spacegray")).Id.ToString()
            });
            #endregion spacegray

            #region rose
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-rose-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "rose").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-rose")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "rose-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "rose").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-rose")).Id.ToString()
            });
            #endregion rose

            #region mint
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-mint-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "mint").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-mint")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "mint-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "mint").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-mint")).Id.ToString()
            });
            #endregion mint

            #region purple
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-purple-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "purple").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-purple")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "purple-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "purple").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-purple")).Id.ToString()
            });
            #endregion purple

            #region lightblue
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-lightblue-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "lightblue").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-lightblue")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "lightblue-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "lightblue").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-lightblue")).Id.ToString()
            });
            #endregion lightblue

            #region yellow
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-yellow-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "yellow").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-yellow")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "yellow-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "yellow").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-yellow")).Id.ToString()
            });
            #endregion yellow

            #region turquoise
            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "-turquoise-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "turquoise").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-turquoise")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productiPad97,
                Sku = productiPad97.Sku + "turquoise-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(iPad97Color.Id, iPad97ColorValues.First(x => x.Alias == "turquoise").Id ),
                    new(ipad97Capacity.Id, iPad97CapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesiPad97.First(x => x.Name.Contains("-turquoise")).Id.ToString()
            });
            #endregion turquoise

            #endregion 9,7 Ipad

            #region Iphone 7 plus

            var productIphone7Plus = _db.Products.First(x => x.Sku == "P-2001");
            var Iphone7PlusPictureIds = productIphone7Plus.ProductMediaFiles.Select(pp => pp.MediaFileId).ToList();
            var picturesIphone7Plus = _db.MediaFiles.Where(x => Iphone7PlusPictureIds.Contains(x.Id)).ToList();

            //var attributeColorIphone7Plus = await _db.ProductVariantAttributes.FirstAsync(x => x.ProductId == productIphone7Plus.Id && x.ProductAttributeId == attrColor.Id);

            var Iphone7PlusColor = _db.ProductVariantAttributes.First(x => x.ProductId == productIphone7Plus.Id && x.ProductAttributeId == attrColorIphoneColors.Id);
            var Iphone7PlusColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == Iphone7PlusColor.Id).ToList();

            var Iphone7PlusCapacity = _db.ProductVariantAttributes.First(x => x.ProductId == productIphone7Plus.Id && x.ProductAttributeId == attrMemoryCapacity.Id);
            var Iphone7PlusCapacityValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == Iphone7PlusCapacity.Id).ToList();


            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-black-64gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "black").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-black")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-black-128gb",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "black").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-black")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-red-64",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "red").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-red")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-red-128",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "red").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-red")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-silver-64",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "silver").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-silver")).Id.ToString()
            });


            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-silver-128",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "silver").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-silver")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-rose-64",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "rose").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-rose")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-rose-128",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "rose").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-rose")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-gold-64",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "gold").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "64gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-gold")).Id.ToString()
            });

            entities.Add(new ProductVariantAttributeCombination()
            {
                Product = productIphone7Plus,
                Sku = productIphone7Plus.Sku + "-gold-128",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(Iphone7PlusColor.Id, Iphone7PlusColorValues.First(x => x.Alias == "gold").Id ),
                    new(Iphone7PlusCapacity.Id, Iphone7PlusCapacityValues.First(x => x.Alias == "128gb").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                AssignedMediaFileIds = picturesIphone7Plus.First(x => x.Name.Contains("-gold")).Id.ToString()
            });

            #endregion Iphone 7 plus

            #region Fashion - Converse All Star

            var productAllStar = _db.Products.First(x => x.Sku == "Fashion-112355");
            var allStarPictureIds = productAllStar.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var allStarPictures = _db.MediaFiles.Where(x => allStarPictureIds.Contains(x.Id)).ToList();

            var allStarColor = _db.ProductVariantAttributes.First(x => x.ProductId == productAllStar.Id && x.ProductAttributeId == attrColor.Id);
            var allStarColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == allStarColor.Id).ToList();

            var allStarSize = _db.ProductVariantAttributes.First(x => x.ProductId == productAllStar.Id && x.ProductAttributeId == attrSize.Id);
            var allStarSizeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == allStarSize.Id).ToList();

            var allStarCombinations = new[]
            {
                new { Color = "Charcoal", Size = "42" },
                new { Color = "Charcoal", Size = "43" },
                new { Color = "Charcoal", Size = "44" },
                new { Color = "Maroon", Size = "42" },
                new { Color = "Maroon", Size = "43" },
                new { Color = "Maroon", Size = "44" },
                new { Color = "Navy", Size = "42" },
                new { Color = "Navy", Size = "43" },
                new { Color = "Navy", Size = "44" },
                new { Color = "Purple", Size = "42" },
                new { Color = "Purple", Size = "43" },
                new { Color = "Purple", Size = "44" },
                new { Color = "White", Size = "42" },
                new { Color = "White", Size = "43" },
                new { Color = "White", Size = "44" },
            };

            foreach (var comb in allStarCombinations)
            {
                var lowerColor = comb.Color.ToLower();
                entities.Add(new ProductVariantAttributeCombination
                {
                    Product = productAllStar,
                    Sku = productAllStar.Sku + string.Concat("-", lowerColor, "-", comb.Size),
                    RawAttributes = FormatAttributeJson(new List<(int, object)>
                    {
                        new(allStarColor.Id, allStarColorValues.First(x => x.Alias == lowerColor).Id ),
                        new(allStarSize.Id, allStarSizeValues.First(x => x.Alias == comb.Size).Id )
                    }),
                    StockQuantity = 10000,
                    AllowOutOfStockOrders = true,
                    IsActive = true,
                    AssignedMediaFileIds = allStarPictures.First(x => x.Name.Contains(lowerColor)).Id.ToString()
                });
            }

            #endregion

            #region Fashion - Shirt Meccanica

            var productShirtMeccanica = _db.Products.First(x => x.Sku == "Fashion-987693502");
            var shirtMeccanicaPictureIds = productShirtMeccanica.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var shirtMeccanicaPictures = _db.MediaFiles.Where(x => shirtMeccanicaPictureIds.Contains(x.Id)).ToList();

            var shirtMeccanicaColor = _db.ProductVariantAttributes.First(x => x.ProductId == productShirtMeccanica.Id && x.ProductAttributeId == attrColor.Id);
            var shirtMeccanicaColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == shirtMeccanicaColor.Id).ToList();

            var shirtMeccanicaSize = _db.ProductVariantAttributes.First(x => x.ProductId == productShirtMeccanica.Id && x.ProductAttributeId == attrSize.Id);
            var shirtMeccanicaSizeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == shirtMeccanicaSize.Id).ToList();

            var shirtMeccanicaCombinations = new[]
            {
                new { Color = "Red", Size = "XS" },
                new { Color = "Red", Size = "S" },
                new { Color = "Red", Size = "M" },
                new { Color = "Red", Size = "L" },
                new { Color = "Red", Size = "XL" },
                new { Color = "Black", Size = "XS" },
                new { Color = "Black", Size = "S" },
                new { Color = "Black", Size = "M" },
                new { Color = "Black", Size = "L" },
                new { Color = "Black", Size = "XL" }
            };

            foreach (var comb in shirtMeccanicaCombinations)
            {
                var lowerColor = comb.Color.ToLower();
                var lowerSize = comb.Size.ToLower();
                var pictureIds = shirtMeccanicaPictures.Where(x => x.Name.Contains($"_{lowerColor}_")).Select(x => x.Id);

                entities.Add(new ProductVariantAttributeCombination
                {
                    Product = productShirtMeccanica,
                    Sku = productShirtMeccanica.Sku + string.Concat("-", lowerColor, "-", lowerSize),
                    RawAttributes = FormatAttributeJson(new List<(int, object)>
                    {
                        new(shirtMeccanicaColor.Id, shirtMeccanicaColorValues.First(x => x.Alias == lowerColor).Id ),
                        new(shirtMeccanicaSize.Id, shirtMeccanicaSizeValues.First(x => x.Alias == lowerSize).Id )
                    }),
                    StockQuantity = 10000,
                    AllowOutOfStockOrders = true,
                    IsActive = true,
                    AssignedMediaFileIds = string.Join(",", pictureIds)
                });
            }

            #endregion

            #region Fashion - Ladies Jacket

            var productLadiesJacket = _db.Products.First(x => x.Sku == "Fashion-JN1107");
            var ladiesJacketPictureIds = productLadiesJacket.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var ladiesJacketPictures = _db.MediaFiles.Where(x => ladiesJacketPictureIds.Contains(x.Id)).ToList();

            var ladiesJacketColor = _db.ProductVariantAttributes.First(x => x.ProductId == productLadiesJacket.Id && x.ProductAttributeId == attrColor.Id);
            var ladiesJacketColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ladiesJacketColor.Id).ToList();

            var ladiesJacketSize = _db.ProductVariantAttributes.First(x => x.ProductId == productLadiesJacket.Id && x.ProductAttributeId == attrSize.Id);
            var ladiesJacketSizeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ladiesJacketSize.Id).ToList();

            var ladiesJacketCombinations = new[]
            {
                new { Color = "Red", Size = "XS" },
                new { Color = "Red", Size = "S" },
                new { Color = "Red", Size = "M" },
                new { Color = "Red", Size = "L" },
                new { Color = "Red", Size = "XL" },
                new { Color = "Orange", Size = "XS" },
                new { Color = "Orange", Size = "S" },
                new { Color = "Orange", Size = "M" },
                new { Color = "Orange", Size = "L" },
                new { Color = "Orange", Size = "XL" },
                new { Color = "Green", Size = "XS" },
                new { Color = "Green", Size = "S" },
                new { Color = "Green", Size = "M" },
                new { Color = "Green", Size = "L" },
                new { Color = "Green", Size = "XL" },
                new { Color = "Blue", Size = "XS" },
                new { Color = "Blue", Size = "S" },
                new { Color = "Blue", Size = "M" },
                new { Color = "Blue", Size = "L" },
                new { Color = "Blue", Size = "XL" },
                new { Color = "Navy", Size = "XS" },
                new { Color = "Navy", Size = "S" },
                new { Color = "Navy", Size = "M" },
                new { Color = "Navy", Size = "L" },
                new { Color = "Navy", Size = "XL" },
                new { Color = "Silver", Size = "XS" },
                new { Color = "Silver", Size = "S" },
                new { Color = "Silver", Size = "M" },
                new { Color = "Silver", Size = "L" },
                new { Color = "Silver", Size = "XL" },
                new { Color = "Black", Size = "XS" },
                new { Color = "Black", Size = "S" },
                new { Color = "Black", Size = "M" },
                new { Color = "Black", Size = "L" },
                new { Color = "Black", Size = "XL" }
            };

            foreach (var comb in ladiesJacketCombinations)
            {
                var lowerColor = comb.Color.ToLower();
                var lowerSize = comb.Size.ToLower();

                entities.Add(new ProductVariantAttributeCombination
                {
                    Product = productLadiesJacket,
                    Sku = productLadiesJacket.Sku + string.Concat("-", lowerColor, "-", lowerSize),
                    RawAttributes = FormatAttributeJson(new List<(int, object)>
                    {
                        new(ladiesJacketColor.Id, ladiesJacketColorValues.First(x => x.Alias == lowerColor).Id ),
                        new(ladiesJacketSize.Id, ladiesJacketSizeValues.First(x => x.Alias == lowerSize).Id )
                    }),
                    StockQuantity = 10000,
                    AllowOutOfStockOrders = true,
                    IsActive = true,
                    AssignedMediaFileIds = ladiesJacketPictures.First(x => x.Name.Contains(lowerColor)).Id.ToString()
                });
            }

            #endregion

            #region Furniture - Le Corbusier LC 6 table

            var productCorbusierTable = _db.Products.First(x => x.Sku == "Furniture-lc6");

            var corbusierTablePlate = _db.ProductVariantAttributes.First(x => x.ProductId == productCorbusierTable.Id && x.ProductAttributeId == attrPlate.Id);
            var corbusierTablePlateValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == corbusierTablePlate.Id).ToList();

            var corbusierTablePlateThickness = _db.ProductVariantAttributes.First(x => x.ProductId == productCorbusierTable.Id && x.ProductAttributeId == attrPlateThickness.Id);
            var corbusierTablePlateThicknessValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == corbusierTablePlateThickness.Id).ToList();

            entities.Add(new ProductVariantAttributeCombination
            {
                Product = productCorbusierTable,
                Sku = productCorbusierTable.Sku + "-clear-15",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(corbusierTablePlate.Id, corbusierTablePlateValues.First(x => x.Alias == "clear-glass").Id ),
                    new(corbusierTablePlateThickness.Id, corbusierTablePlateThicknessValues.First(x => x.Alias == "15mm").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                Price = 749.00M
            });
            entities.Add(new ProductVariantAttributeCombination
            {
                Product = productCorbusierTable,
                Sku = productCorbusierTable.Sku + "-clear-19",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(corbusierTablePlate.Id, corbusierTablePlateValues.First(x => x.Alias == "clear-glass").Id ),
                    new(corbusierTablePlateThickness.Id, corbusierTablePlateThicknessValues.First(x => x.Alias == "19mm").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                Price = 899.00M
            });
            entities.Add(new ProductVariantAttributeCombination
            {
                Product = productCorbusierTable,
                Sku = productCorbusierTable.Sku + "-sandblasted-15",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(corbusierTablePlate.Id, corbusierTablePlateValues.First(x => x.Alias == "sandblasted-glass").Id ),
                    new(corbusierTablePlateThickness.Id, corbusierTablePlateThicknessValues.First(x => x.Alias == "15mm").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                Price = 849.00M
            });
            entities.Add(new ProductVariantAttributeCombination
            {
                Product = productCorbusierTable,
                Sku = productCorbusierTable.Sku + "-sandblasted-19",
                RawAttributes = FormatAttributeJson(new List<(int, object)>
                {
                    new(corbusierTablePlate.Id, corbusierTablePlateValues.First(x => x.Alias == "sandblasted-glass").Id ),
                    new(corbusierTablePlateThickness.Id, corbusierTablePlateThicknessValues.First(x => x.Alias == "19mm").Id )
                }),
                StockQuantity = 10000,
                AllowOutOfStockOrders = true,
                IsActive = true,
                Price = 999.00M
            });

            #endregion

            #region Soccer Adidas TANGO SALA BALL

            var productAdidasTANGOSALABALL = _db.Products.First(x => x.Sku == "P-5001");
            var adidasTANGOSALABALLPictureIds = productAdidasTANGOSALABALL.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var adidasTANGOSALABALLJacketPictures = _db.MediaFiles.Where(x => adidasTANGOSALABALLPictureIds.Contains(x.Id)).ToList();

            var adidasTANGOSALABALLColor = _db.ProductVariantAttributes.First(x => x.ProductId == productAdidasTANGOSALABALL.Id && x.ProductAttributeId == attrColor.Id);
            var adidasTANGOSALABALLColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == adidasTANGOSALABALLColor.Id).ToList();

            var adidasTANGOSALABALLSize = _db.ProductVariantAttributes.First(x => x.ProductId == productAdidasTANGOSALABALL.Id && x.ProductAttributeId == attrSize.Id);
            var adidasTANGOSALABALLSizeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == adidasTANGOSALABALLSize.Id).ToList();

            var adidasTANGOSALABALLCombinations = new[]
            {
                new { Color = "Red", Size = "3" },
                new { Color = "Red", Size = "4" },
                new { Color = "Red", Size = "5" },

                new { Color = "Yellow", Size = "3" },
                new { Color = "Yellow", Size = "4" },
                new { Color = "Yellow", Size = "5" },

                new { Color = "Green", Size = "3" },
                new { Color = "Green", Size = "4" },
                new { Color = "Green", Size = "5" },

                new { Color = "Blue", Size = "3" },
                new { Color = "Blue", Size = "4" },
                new { Color = "Blue", Size = "5" },

                new { Color = "Gray", Size = "3" },
                new { Color = "Gray", Size = "4" },
                new { Color = "Gray", Size = "5" },

                new { Color = "White", Size = "3" },
                new { Color = "White", Size = "4" },
                new { Color = "White", Size = "5" },

                new { Color = "Brown", Size = "3" },
                new { Color = "Brown", Size = "4" },
                new { Color = "Brown", Size = "5" },

            };

            foreach (var comb in adidasTANGOSALABALLCombinations)
            {
                var lowerColor = comb.Color.ToLower();
                var lowerSize = comb.Size.ToLower();

                entities.Add(new ProductVariantAttributeCombination
                {
                    Product = productAdidasTANGOSALABALL,
                    Sku = productAdidasTANGOSALABALL.Sku + string.Concat("-", lowerColor, "-", lowerSize),
                    RawAttributes = FormatAttributeJson(new List<(int, object)>
                    {
                        new(adidasTANGOSALABALLColor.Id, adidasTANGOSALABALLColorValues.First(x => x.Alias == lowerColor).Id ),
                        new(adidasTANGOSALABALLSize.Id, adidasTANGOSALABALLSizeValues.First(x => x.Alias == lowerSize).Id )
                    }),
                    StockQuantity = 10000,
                    AllowOutOfStockOrders = true,
                    IsActive = true,
                    AssignedMediaFileIds = adidasTANGOSALABALLJacketPictures.First(x => x.Name.Contains(lowerColor)).Id.ToString()
                });
            }

            #endregion Soccer Adidas TANGO SALA BALL

            #region Soccer Torfabrik official game ball

            var productTorfabrikBall = _db.Products.First(x => x.Sku == "P-5002");
            var torfabrikBallPictureIds = productTorfabrikBall.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var torfabrikBallPictures = _db.MediaFiles.Where(x => torfabrikBallPictureIds.Contains(x.Id)).ToList();

            var torfabrikBallColor = _db.ProductVariantAttributes.First(x => x.ProductId == productTorfabrikBall.Id && x.ProductAttributeId == attrColor.Id);
            var torfabrikBallColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == torfabrikBallColor.Id).ToList();

            var torfabrikBallSize = _db.ProductVariantAttributes.First(x => x.ProductId == productTorfabrikBall.Id && x.ProductAttributeId == attrSize.Id);
            var torfabrikBallSizeValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == torfabrikBallSize.Id).ToList();

            var torfabrikBallSizeCombinations = new[]
            {
                new { Color = "Red", Size = "3" },
                new { Color = "Red", Size = "4" },
                new { Color = "Red", Size = "5" },

                new { Color = "Yellow", Size = "3" },
                new { Color = "Yellow", Size = "4" },
                new { Color = "Yellow", Size = "5" },

                new { Color = "Green", Size = "3" },
                new { Color = "Green", Size = "4" },
                new { Color = "Green", Size = "5" },

                new { Color = "Blue", Size = "3" },
                new { Color = "Blue", Size = "4" },
                new { Color = "Blue", Size = "5" },

                new { Color = "White", Size = "3" },
                new { Color = "White", Size = "4" },
                new { Color = "White", Size = "5" },

            };

            foreach (var comb in torfabrikBallSizeCombinations)
            {
                var lowerColor = comb.Color.ToLower();
                var lowerSize = comb.Size.ToLower();

                entities.Add(new ProductVariantAttributeCombination
                {
                    Product = productTorfabrikBall,
                    Sku = productTorfabrikBall.Sku + string.Concat("-", lowerColor, "-", lowerSize),
                    RawAttributes = FormatAttributeJson(new List<(int, object)>
                    {
                        new(torfabrikBallColor.Id, torfabrikBallColorValues.First(x => x.Alias == lowerColor).Id ),
                        new(torfabrikBallSize.Id, torfabrikBallSizeValues.First(x => x.Alias == lowerSize).Id )
                    }),
                    StockQuantity = 10000,
                    AllowOutOfStockOrders = true,
                    IsActive = true,
                    AssignedMediaFileIds = torfabrikBallPictures.First(x => x.Name.Contains(lowerColor)).Id.ToString()
                });
            }

            #endregion Soccer Torfabrik official game ball

            #region Furniture - Ball chair

            var productBallChair = _db.Products.First(x => x.Sku == "Furniture-ball-chair");
            var ballChairPictureIds = productBallChair.ProductMediaFiles.Select(x => x.MediaFileId).ToList();
            var ballChairPictures = _db.MediaFiles.Where(x => ballChairPictureIds.Contains(x.Id)).ToList();

            var ballChairMaterial = _db.ProductVariantAttributes.First(x => x.ProductId == productBallChair.Id && x.ProductAttributeId == attrMaterial.Id);
            var ballChairMaterialValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ballChairMaterial.Id).ToList();

            var ballChairColor = _db.ProductVariantAttributes.First(x => x.ProductId == productBallChair.Id && x.ProductAttributeId == attrColor.Id);
            var ballChairColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ballChairColor.Id).ToList();

            var ballChairLeatherColor = _db.ProductVariantAttributes.First(x => x.ProductId == productBallChair.Id && x.ProductAttributeId == attrLeatherColor.Id);
            var ballChairLeatherColorValues = _db.ProductVariantAttributeValues.Where(x => x.ProductVariantAttributeId == ballChairLeatherColor.Id).ToList();

            foreach (var materialValue in ballChairMaterialValues)
            {
                foreach (var colorValue in ballChairColorValues)
                {
                    decimal ballChairPrice = 2199.00M;

                    if (materialValue.Alias.StartsWith("leather-special"))
                    {
                        ballChairPrice = 2599.00M;
                    }
                    else if (materialValue.Alias.StartsWith("leather-aniline"))
                    {
                        ballChairPrice = 2999.00M;
                    }

                    foreach (var leatherColorValue in ballChairLeatherColorValues)
                    {
                        entities.Add(new ProductVariantAttributeCombination
                        {
                            Product = productBallChair,
                            Sku = productBallChair.Sku + string.Concat("-", colorValue.Alias, "-", materialValue.Alias),
                            RawAttributes = FormatAttributeJson(new List<(int, object)>
                            {
                                new(ballChairMaterial.Id, materialValue.Id ),
                                new(ballChairColor.Id, colorValue.Id ),
                                new(ballChairLeatherColor.Id, leatherColorValue.Id )
                            }),
                            StockQuantity = 10000,
                            AllowOutOfStockOrders = true,
                            IsActive = true,
                            AssignedMediaFileIds = ballChairPictures.First(x => x.Name.Contains(colorValue.Alias)).Id.ToString(),
                            Price = ballChairPrice
                        });
                    }
                }
            }

            #endregion

            return entities;
        }
    }
}