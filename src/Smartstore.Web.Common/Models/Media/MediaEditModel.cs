#nullable enable

using System.Text.Json;
using Smartstore.Json;

namespace Smartstore.Web.Models.Media;

/// <summary>
/// Represents commands to edit a media file such as an image or video.
/// </summary>
public partial class MediaEditModel
{
    public required List<MediaEditCommand> Commands { get; set; }

    public virtual string? ToJson()
    {
        if (IsDefault())
        {
            return null;
        }

        return JsonSerializer.Serialize(this, SmartJsonOptions.CamelCased);
    }

    private bool IsDefault()
    {
        return Commands.IsNullOrEmpty();
    }
}

public partial class MediaEditCommand
{
    public required string Name { get; set; }
    public string? Value { get; set; }
}
