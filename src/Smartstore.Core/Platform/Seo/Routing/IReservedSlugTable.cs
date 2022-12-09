#nullable enable

namespace Smartstore.Core.Seo.Routing;

public class ReservedSlug
{
    public string Slug { get; init; } = default!;
    public bool IsPrefix { get; init; }
}

/// <summary>
/// Provides a table with system reserved slugs.
/// </summary>
public interface IReservedSlugTable
{
    bool IsReservedSlug(string slug);
    IEnumerable<ReservedSlug> EnumerateSlugs();
}
