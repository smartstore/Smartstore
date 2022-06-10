namespace Smartstore.Admin.Models.Logging
{
    [LocalizedDisplay("Admin.System.Log.Fields.")]
    public class LogModel : EntityModelBase
    {
        public string LogLevelHint { get; set; }

        [LocalizedDisplay("*LogLevel")]
        public string LogLevel { get; set; }

        [LocalizedDisplay("*ShortMessage")]
        public string ShortMessage { get; set; }

        [LocalizedDisplay("*FullMessage")]
        public string FullMessage { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string IpAddress { get; set; }

        [LocalizedDisplay("*Customer")]
        public int? CustomerId { get; set; }

        [LocalizedDisplay("*Customer")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*PageURL")]
        public string PageUrl { get; set; }

        [LocalizedDisplay("*ReferrerURL")]
        public string ReferrerUrl { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*Logger")]
        public string Logger { get; set; }

        [LocalizedDisplay("*Logger")]
        public string LoggerShort { get; set; }

        [LocalizedDisplay("*HttpMethod")]
        public string HttpMethod { get; set; }

        [LocalizedDisplay("*UserName")]
        public string UserName { get; set; }

        public string ViewUrl { get; set; }
    }
}