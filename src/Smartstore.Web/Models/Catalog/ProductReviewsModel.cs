using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductReviewOverviewModel : ModelBase
    {
        public int ProductId { get; set; }
        public int RatingSum { get; set; }
        public int TotalReviews { get; set; }
        public bool AllowCustomerReviews { get; set; }
    }

    [LocalizedDisplay("Reviews.Fields.")]
    public partial class ProductReviewsModel : ModelBase
    {
        public int ProductId { get; set; }
        public LocalizedValue<string> ProductName { get; set; }
        public string ProductSeName { get; set; }

        public int TotalReviewsCount { get; set; }
        public List<ProductReviewModel> Items { get; set; } = new();

        #region Add

        [Required, StringLength(200, MinimumLength = 1)]
        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [SanitizeHtml, Required]
        [LocalizedDisplay("*ReviewText")]
        public string ReviewText { get; set; }

        [LocalizedDisplay("*Rating")]
        public int Rating { get; set; }

        public bool DisplayCaptcha { get; set; }

        public bool CanCurrentCustomerLeaveReview { get; set; }
        public bool SuccessfullyAdded { get; set; }
        public string Result { get; set; }

        #endregion
    }

    public partial class ProductReviewModel : EntityModelBase
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public bool AllowViewingProfiles { get; set; }
        public string Title { get; set; }
        public string ReviewText { get; set; }
        public int Rating { get; set; }
        public ProductReviewHelpfulnessModel Helpfulness { get; set; }
        public string WrittenOnStr { get; set; }
        public DateTime WrittenOn { get; set; }
    }

    public partial class ProductReviewHelpfulnessModel : ModelBase
    {
        public int ProductReviewId { get; set; }
        public int HelpfulYesTotal { get; set; }
        public int HelpfulNoTotal { get; set; }
    }
}
