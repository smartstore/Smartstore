using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models
{
    // TODO: (core) Remove TestModel later
    [LocalizedDisplayName("Admin.Catalog.Products.Fields.")]
    public class TestModel : ModelBase
    {
        [LocalizedDisplayName("*AssociatedToProductName")]
        [Required]
        public string TestProp1 { get; set; }

        [LocalizedDisplayName("*ShortDescription")]
        public string TestProp2 { get; set; }

        [LocalizedDisplayName("*ShowOnHomePage")]
        public bool TestProp3 { get; set; }

        [LocalizedDisplayName("*AllowCustomerReviews")]
        public bool TestProp4 { get; set; }
    }
}
