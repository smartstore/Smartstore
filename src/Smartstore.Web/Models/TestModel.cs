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


        [LocalizedDisplay("Account.Fields.DateOfBirth")]
        public int? DateOfBirthDay { get; set; } = 5;

        [LocalizedDisplay("Account.Fields.DateOfBirth")]
        public int? DateOfBirthMonth { get; set; } = 5;

        [LocalizedDisplay("Account.Fields.DateOfBirth")]
        public int? DateOfBirthYear { get; set; } = 1979;

        public string TestColor { get; set; } = "#ff99cc";

        [DataType(DataType.Password)]
        [LocalizedDisplay("Account.Fields.Password")]
        public string Password { get; set; }

        [DataType(DataType.PhoneNumber)]
        [LocalizedDisplay("Account.Fields.Phone")]
        public string TestPhone { get; set; }

        [LocalizedDisplay("Account.Fields.Email")]
        [DataType(DataType.EmailAddress)]
        public string TestEmail { get; set; }

        [LocalizedDisplay("Account.Fields.Newsletter")]
        public bool TestSubscribeToNewsletter { get; set; }
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
