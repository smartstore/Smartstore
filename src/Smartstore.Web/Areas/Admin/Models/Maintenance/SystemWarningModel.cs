namespace Smartstore.Admin.Models.Maintenance
{
    public enum SystemWarningLevel
    {
        Pass,
        Warning,
        Fail
    }

    public class SystemWarningModel : ModelBase
    {
        public SystemWarningLevel Level { get; set; }
        public string Text { get; set; }
    }
}
