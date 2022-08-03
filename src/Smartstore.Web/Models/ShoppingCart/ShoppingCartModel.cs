using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Web.Models.Cart
{
    public partial class ShoppingCartModel : CartModelBase
    {
        public override IEnumerable<ShoppingCartItemModel> Items { get; } = new List<ShoppingCartItemModel>();

        public void AddItems(params ShoppingCartItemModel[] models)
        {
            ((List<ShoppingCartItemModel>)Items).AddRange(models);
        }

        public string CheckoutAttributeInfo { get; set; }
        public List<CheckoutAttributeModel> CheckoutAttributes { get; set; } = new();

        public bool TermsOfServiceEnabled { get; set; }
        public EstimateShippingModel EstimateShipping { get; set; } = new();
        public DiscountBoxModel DiscountBox { get; set; } = new();
        public GiftCardBoxModel GiftCardBox { get; set; } = new();
        public RewardPointsBoxModel RewardPoints { get; set; } = new();
        public OrderReviewDataModel OrderReviewData { get; set; } = new();
        public int MediaDimensions { get; set; }
        public DeliveryTimesPresentation DeliveryTimesPresentation { get; set; }
        public ButtonPaymentMethodModel ButtonPaymentMethods { get; set; } = new();
        public string CustomerComment { get; set; }
        public string MeasureUnitName { get; set; }
        public bool DisplayWeight { get; set; }
        public bool DisplayBasePrice { get; set; }
        public bool DisplayCommentBox { get; set; }
        public bool DisplayEsdRevocationWaiverBox { get; set; }
        public bool DisplayMoveToWishlistButton { get; set; }

        public partial class ShoppingCartItemModel : CartEntityModelBase
        {
            public decimal Weight { get; set; }

            public bool IsShipEnabled { get; set; }
            public LocalizedValue<string> DeliveryTimeName { get; set; }
            public string DeliveryTimeHexValue { get; set; }
            public string DeliveryTimeDate { get; set; }

            public bool IsDownload { get; set; }
            public bool HasUserAgreement { get; set; }
            public bool IsEsd { get; set; }

            public override IEnumerable<ShoppingCartItemModel> ChildItems { get; } = new List<ShoppingCartItemModel>();

            public void AddChildItems(params ShoppingCartItemModel[] models)
            {
                ((List<ShoppingCartItemModel>)ChildItems).AddRange(models);
            }

            public bool DisableWishlistButton { get; set; }
        }

        public partial class CheckoutAttributeModel : ChoiceModel
        {
            public override string BuildControlId()
                => CheckoutAttributeQueryItem.CreateKey(Id);

            public override string GetFileUploadUrl(IUrlHelper url)
                => url.Action("UploadFileCheckoutAttribute", "ShoppingCart", new { controlId = BuildControlId() });
        }

        public partial class CheckoutAttributeValueModel : ChoiceItemModel
        {
            public override string GetItemLabel()
            {
                var label = Name;

                if (PriceAdjustment.HasValue())
                {
                    label += " ({0})".FormatCurrent(PriceAdjustment);
                }

                return label;
            }
        }

        public partial class DiscountBoxModel : ModelBase
        {
            public bool Display { get; set; }
            public string Message { get; set; }
            public string CurrentCode { get; set; }
            public bool IsWarning { get; set; }
        }

        public partial class GiftCardBoxModel : ModelBase
        {
            public bool Display { get; set; }
            public string Message { get; set; }
            public bool IsWarning { get; set; }
        }

        public partial class RewardPointsBoxModel : ModelBase
        {
            public bool DisplayRewardPoints { get; set; }
            public int RewardPointsBalance { get; set; }
            public string RewardPointsAmount { get; set; }
            public bool UseRewardPoints { get; set; }
        }

        public partial class OrderReviewDataModel : ModelBase
        {
            public bool Display { get; set; }

            public AddressModel BillingAddress { get; set; } = new();
            public AddressModel ShippingAddress { get; set; } = new();
            public bool IsShippable { get; set; }
            public string ShippingMethod { get; set; }
            public bool DisplayShippingMethodChangeOption { get; set; }

            public string PaymentMethod { get; set; }
            public string PaymentSummary { get; set; }
            public bool DisplayPaymentMethodChangeOption { get; set; }
            public bool IsPaymentSelectionSkipped { get; set; }
        }
    }
}