namespace Smartstore.Google.Analytics
{
    /// <summary>
    /// Provides ready to use scripts for plugin settings. 
    /// </summary>
    /// <remarks>
    /// Code formatting (everything is squeezed to the edge) was done intentionally like this. 
    /// Else whitespace would be copied into the setting properties and effect the configuration page in a negative way.
    /// </remarks>
    internal static class AnalyticsScriptUtility
    {
        /// Current information about cookie consent
        /// https://support.google.com/analytics/answer/9976101?hl=de
        internal static string GetTrackingScript()
        {
            return @"<!-- Google code for Analytics tracking -->
<script async src='https://www.googletagmanager.com/gtag/js?id={GOOGLEID}'></script>
<script>
	{OPTOUTCOOKIE}

    window.dataLayer = window.dataLayer || [];
    function gtag(){window.dataLayer.push(arguments);}
    gtag('js', new Date());

    gtag('consent', 'default', {
      'ad_storage': '{STORAGETYPE}',
      'analytics_storage': '{STORAGETYPE}',
      'ad_user_data': '{ADUSERDATA}',
      'ad_personalization': '{ADPERSONALIZATION}',
    });

    gtag('config', '{GOOGLEID}', { 'anonymize_ip': true });

    gtag('config', 'GA_MEASUREMENT_ID', {
      'user_id': '{USERID}'
    });    

	{ECOMMERCE}
</script>";
        }

        internal static string GetEcommerceScript()
        {
            return @"gtag('event', 'purchase', {
  'transaction_id': '{ORDERID}',
  'value': {TOTAL},
  'currency': '{CURRENCY}',
  'tax': {TAX},
  'shipping': {SHIP},
  'items': [{DETAILS}]
});";
        }

        internal static string GetEcommerceDetailScript()
        {
            return @"{
	'id': '{PRODUCTSKU}',
	'name': '{PRODUCTNAME}',
	'category': '{CATEGORYNAME}',
	'quantity': {QUANTITY},
	'price': '{UNITPRICE}'
},";
        }
    }
}
