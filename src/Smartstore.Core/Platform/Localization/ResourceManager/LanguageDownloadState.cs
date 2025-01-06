namespace Smartstore.Core.Localization
{
    public enum LanguageDownloadStep
    {
        DownloadResources,
        ImportResources
    }

    public class LanguageDownloadState
    {
        public int Id { get; set; }
        public int ProgressPercent { get; set; }
        public LanguageDownloadStep Step { get; set; }
    }
}
