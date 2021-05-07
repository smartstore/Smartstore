using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Areas.Admin.Models
{
    // TODO: (mh) (core) Remove model when not used anymore.
    public class EditorTemplatesTestModel : ModelBase, IQuantityInput
    {
        public bool? BooleanNullable { get; set; }
        public bool BooleanNotNullable { get; set; }

        [UIHint("ButtonType")]
        public string ButtonType { get; set; }
        public byte? Byte { get; set; }

        [UIHint("Color")]
        public string Color { get; set; }
        public DateTime? DateTime { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }

        [UIHint("Html")]
        public string Html { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }

        [UIHint("Link")]
        public string Link { get; set; }

        [UIHint("Liquid")]
        public string Liquid { get; set; }

        [UIHint("Media")]
        public int? Media { get; set; }

        [UIHint("Range")]
        public int Range { get; set; }

        [UIHint("Time")]
        public DateTime? Time { get; set; }

        [UIHint("QtyInput")]
        public int QtyInput { get; set; }

        public int EnteredQuantity { get; set; } = 1;

        public int MinOrderAmount { get; set; } = 1;

        public int MaxOrderAmount { get; set; } = 100;

        public int QuantityStep { get; set; } = 1;

        public LocalizedValue<string> QuantityUnitName { get; set; }

        public List<SelectListItem> AllowedQuantities { get; set; } = new();

        public QuantityControlType QuantityControlType { get; set; } = QuantityControlType.Spinner;
    }
}
