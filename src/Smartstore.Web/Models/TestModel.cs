using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
