#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Contributes icon concepts to kits and collections using existing library sources.
/// Implementations are discovered in installed modules and created in a temporary scope per catalog generation.
/// </summary>
public interface IIconProvider
{
    /// <summary>
    /// Adds deterministic, application-wide definitions when a catalog generation is loaded.
    /// Do not depend on request state, scoped services or the icon service itself.
    /// </summary>
    /// <param name="builder">The builder for this catalog generation. Do not retain it.</param>
    void Configure(IconKitBuilder builder);
}
