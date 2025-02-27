﻿using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.ProductTags.")]
    public class ProductTagListModel : ModelBase
    {
        [LocalizedDisplay("*Fields.Name")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*Published")]
        public bool? SearchPublished { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.ProductTags.")]
    public class ProductTagModel : EntityModelBase, ILocalizedModel<ProductTagLocalizedModel>
    {
        public Type GetEntityType() => typeof(ProductTag);

        [Required]
        [LocalizedDisplay("*Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*Fields.ProductCount")]
        public int ProductCount { get; set; }

        public List<ProductTagLocalizedModel> Locales { get; set; } = new();
    }

    public class ProductTagLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Catalog.ProductTags.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class ProductTagValidator : AbstractValidator<ProductTagModel>
    {
        public ProductTagValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
