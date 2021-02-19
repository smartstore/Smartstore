using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models
{
    // TODO: (core) Remove TestModel later
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class TestModel : ModelBase, ILocalizedModel<LocalizedTestModel>
    {
        public List<LocalizedTestModel> Locales { get; set; } = new();

        [LocalizedDisplay("*AssociatedToProductName")]
        [Required]
        public string TestProp1 { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string TestProp2 { get; set; }

        [LocalizedDisplay("*ShowOnHomePage")]
        public bool TestProp3 { get; set; }

        [LocalizedDisplay("*AllowCustomerReviews")]
        public bool TestProp4 { get; set; }

        public string TestColor { get; set; } = "#ff99cc";
    }

    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class LocalizedTestModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*AssociatedToProductName")]
        [Required]
        public string TestProp1 { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string TestProp2 { get; set; }
    }
}
