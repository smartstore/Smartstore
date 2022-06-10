using Smartstore.Core.Configuration;

namespace Smartstore.DevTools
{
    public class ProfilerSettings : ISettings
    {
        public bool EnableMiniProfilerInPublicStore { get; set; }

        public string[] MiniProfilerIgnorePaths { get; set; } = new[] { "/admin/", "/themes/", "/bundle/", "/media/", "/components/", "/lib/", "/js/", "/css/", "/images/", "/taskscheduler/" };

        public bool DisplayWidgetZones { get; set; }
        public bool DisplayMachineName { get; set; }
    }
}
