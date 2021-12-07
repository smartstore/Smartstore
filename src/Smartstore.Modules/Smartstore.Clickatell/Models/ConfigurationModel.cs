namespace Smartstore.Clickatell.Models
{
    [LocalizedDisplay("Plugins.Sms.Clickatell.Fields.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*PhoneNumber")]
        public string PhoneNumber { get; set; }

        [LocalizedDisplay("*ApiId")]
        public string ApiId { get; set; }

        [LocalizedDisplay("*TestMessage")]
        public string TestMessage { get; set; }

        public bool TestSucceeded { get; set; }
        public string TestSmsResult { get; set; }
        public string TestSmsDetailResult { get; set; }
    }
}