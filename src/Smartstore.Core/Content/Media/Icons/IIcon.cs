#nullable enable

using Smartstore.IconKit;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>Exposes an icon address together with its presentation.</summary>
public interface IIcon
{
    /// <summary>Gets the icon expression; null represents no icon.</summary>
    IconExpression? Icon { get; }
}
