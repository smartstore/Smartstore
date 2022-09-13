namespace Smartstore.WebApi.Models
{
    public class WebApiState
    {
        // TODO: (mg) (core) add new setting for WebApiState.IsActive.
        public bool IsActive { get; init; }
        public string ModuleVersion { get; init; }
        public int MaxTop { get; init; }
        public int MaxExpansionDepth { get; init; }

        public string Version => $"1 {ModuleVersion}";
    }
}
