#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Discovers local SVG libraries, resolves addresses and implicitly applies library mappings.
/// </summary>
public interface IIconService
{
    /// <summary>
    /// Gets the configured default library.
    /// </summary>
    IconLibrary DefaultLibrary { get; }

    /// <summary>
    /// Finds a library by system name or short name, or returns null.
    /// </summary>
    /// <param name="nameOrShortName">The library directory name or optional short name, matched case-insensitively.</param>
    /// <returns>An immutable library manifest, or null when the selector is unknown.</returns>
    IconLibrary? GetLibrary(string nameOrShortName);

    /// <summary>
    /// Gets locally configured libraries.
    /// </summary>
    /// <returns>Immutable manifests, with one entry per library regardless of its short name.</returns>
    Task<IReadOnlyList<IconLibrary>> GetLibrariesAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Gets an icon after one mapping lookup, or null if unavailable. Parameters fill missing address qualifiers; conflicting qualifiers are rejected.
    /// </summary>
    /// <param name="name">An icon or conceptual name, optionally qualified as library:name@variant. Icon names are case-sensitive.</param>
    /// <param name="library">The library system name or short name. Fills a missing address qualifier; null uses the configured default.</param>
    /// <param name="variant">The variant name or short name. Fills a missing address qualifier; null uses the selected library's effective default.</param>
    /// <returns>The mapped icon with its canonical address, or null when the library, variant or target icon is unavailable.</returns>
    /// <exception cref="FormatException">The address syntax is invalid.</exception>
    /// <exception cref="ArgumentException">An explicit method selector conflicts with a resolved address selector.</exception>
    Task<IconInfo?> GetIconAsync(string name, string? library = null, string? variant = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Gets a cached, validated SVG payload for an already resolved icon, without applying mapping again.
    /// Rendering options are applied separately.
    /// </summary>
    /// <param name="icon">The resolved library, variant and actual icon name. The canonical address is derived from these fields.</param>
    /// <returns>An immutable shared payload, or null when the library, variant or icon is no longer available.</returns>
    /// <exception cref="ArgumentException">The icon identity is incomplete or invalid.</exception>
    /// <exception cref="InvalidDataException">The SVG contains unsupported content or invalid coordinates.</exception>
    /// <exception cref="IOException">A source cannot be opened or changed during preparation. Retry after the catalog reloads.</exception>
    Task<IconSvg?> GetSvgAsync(IconInfo icon, CancellationToken cancelToken = default);

    /// <summary>
    /// Searches available icons by actual name, conceptual name and supplemental tags.
    /// </summary>
    /// <param name="query">The search words, library and variant selectors, and page boundaries. Every word must match a name or tag.</param>
    /// <returns>The requested page in icon-name order and the total matching count before pagination.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Skip is negative or Take is outside the range 1 through 500.</exception>
    Task<IconSearchResult> SearchAsync(IconSearchQuery query, CancellationToken cancelToken = default);
}

public static class IIconServiceExtensions
{
    /// <summary>
    /// Resolves an icon address with one mapping lookup and gets its cached SVG payload.
    /// </summary>
    /// <param name="service">The icon service used for resolution and preparation.</param>
    /// <param name="name">An icon or conceptual name, optionally qualified as library:name@variant.</param>
    /// <param name="library">The library system name or short name. Fills a missing address qualifier; null uses the default.</param>
    /// <param name="variant">The variant name or short name. Fills a missing address qualifier; null uses the effective default.</param>
    /// <returns>An immutable shared SVG payload, or null when the icon is unavailable.</returns>
    /// <exception cref="FormatException">The address syntax is invalid.</exception>
    /// <exception cref="ArgumentException">An explicit selector conflicts with an address selector.</exception>
    public static async Task<IconSvg?> GetSvgAsync(this IIconService service, string name, string? library = null, string? variant = null, CancellationToken cancelToken = default)
    {
        Guard.NotNull(service);

        cancelToken.ThrowIfCancellationRequested();
        var icon = await service.GetIconAsync(name, library, variant, cancelToken);
        return icon == null ? null : await service.GetSvgAsync(icon, cancelToken);
    }
}
