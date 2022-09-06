namespace Smartstore.WebApi.Models
{
    public partial class WebApiState
    {
        public bool IsActive { get; init; }
        public bool LogUnauthorized { get; init; }
        public string ModuleVersion { get; init; }
        public int MaxTop { get; init; }
        public int MaxExpansionDepth { get; init; }

        public string Version => $"1 {ModuleVersion}";
    }
}
