using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class LanguageModel : EntityModelBase
    {
        public string ISOCode { get; set; }
        public string CultureCode { get; set; }
        public string FlagImageFileName { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string LocalizedName { get; set; }
        public string LocalizedShortName { get; set; }
    }
}
