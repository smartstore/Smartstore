namespace Smartstore.Forums.Models.Public
{
    [CustomModelPart]
    public partial class ForumCustomerInfoModel : ModelBase
    {
        [LocalizedDisplay("Account.Fields.Signature")]
        [SanitizeHtml]
        public string Signature { get; set; }
    }
}
