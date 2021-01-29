using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models
{
    // TODO: (core) Remove TestModel later
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class TestModel : ModelBase
    {
        [LocalizedDisplay("*AssociatedToProductName")]
        [Required]
        public string TestProp1 { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        public string TestProp2 { get; set; }

        [LocalizedDisplay("*ShowOnHomePage")]
        public bool TestProp3 { get; set; }

        [LocalizedDisplay("*AllowCustomerReviews")]
        public bool TestProp4 { get; set; }
    }
}
