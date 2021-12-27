namespace Smartstore.AmazonPay
{
    //public enum AmazonPayRequestType
    //{
    //    None = 0,
    //    ShoppingCart,
    //    Address,
    //    PaymentMethod,
    //    OrderReviewData,
    //    ShippingMethod,
    //    MiniShoppingCart,

    //    /// <summary>
    //    /// Amazon Pay button clicked
    //    /// </summary>
    //    PayButtonHandler,

    //    /// <summary>
    //    /// Display authentication button on login page
    //    /// </summary>
    //    AuthenticationPublicInfo
    //}

    public enum AmazonPayTransactionType
    {
        Authorize = 1,
        AuthorizeAndCapture = 2
    }

    public enum AmazonPayAuthorizeMethod
    {
        Omnichronous = 0,
        Asynchronous,
        Synchronous
    }

    public enum AmazonPaySaveDataType
    {
        None = 0,
        OnlyIfEmpty,
        Always
    }

    //public enum AmazonPayResultType
    //{
    //    None = 0,
    //    PluginView,
    //    Redirect,
    //    Unauthorized
    //}

    public enum AmazonPayMessage
    {
        MessageTyp = 0,
        MessageId,
        AuthorizationID,
        CaptureID,
        RefundID,
        ReferenceID,
        State,
        StateUpdate,
        Fee,
        AuthorizedAmount,
        CapturedAmount,
        RefundedAmount,
        CaptureNow,
        Creation,
        Expiration
    }
}
