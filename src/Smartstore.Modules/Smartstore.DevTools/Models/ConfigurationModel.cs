using Smartstore.IconKit;

namespace Smartstore.DevTools.Models;

[LocalizedDisplay("Plugins.Developer.DevTools.")]
public class ConfigurationModel : ModelBase
{
    /// <summary>
    /// Gets or sets the cached inline drawings shown in the diagnostic grid.
    /// </summary>
    public IReadOnlyList<IconSvg> CachedIcons { get; set; } = [];

    /// <summary>
    /// Gets or sets current kit sprites already present on disk.
    /// </summary>
    public IReadOnlyList<IconKitFileModel> KitFiles { get; set; } = [];

    [LocalizedDisplay("*EnableMiniProfilerInPublicStore")]
    public bool EnableMiniProfilerInPublicStore { get; set; }

    [UIHint("Textarea"), AdditionalMetadata("rows", 2)]
    [LocalizedDisplay("*MiniProfilerIgnorePaths")]
    public string MiniProfilerIgnorePaths { get; set; }

    [LocalizedDisplay("*DisplayWidgetZones")]
    public bool DisplayWidgetZones { get; set; }

    [LocalizedDisplay("*DisplayMachineName")]
    public bool DisplayMachineName { get; set; }
}
/// <summary>
/// Describes an existing kit sprite and its uncompressed file size in bytes.
/// </summary>
public sealed record IconKitFileModel(string Name, string FileName, long Size);
