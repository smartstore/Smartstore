using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Locale resources of the installed language, used to resolve <c>"@resource:{key}"</c> values in seed records.
/// </summary>
public sealed class SeedResources
{
    private readonly IReadOnlyDictionary<string, string> _resources;

    public SeedResources(string culture, IReadOnlyDictionary<string, string> resources)
    {
        Guard.NotEmpty(culture);
        Guard.NotNull(resources);

        Culture = culture;
        _resources = resources;
    }

    /// <summary>
    /// Culture of the language the resources belong to.
    /// </summary>
    public string Culture { get; }

    public bool TryGetValue(string key, out string value)
        => _resources.TryGetValue(key, out value);

    /// <summary>
    /// Loads the seeded resources of <paramref name="language"/>.
    /// </summary>
    /// <exception cref="SeedDataFormatException">No resources have been seeded for the language yet.</exception>
    public static async Task<SeedResources> LoadAsync(SmartDbContext db, Language language, CancellationToken cancelToken = default)
    {
        return await TryLoadAsync(db, language, cancelToken)
            ?? throw new SeedDataFormatException(
                $"No locale resources have been seeded for language '{language.LanguageCulture}'. Seed them before records that use resource references.");
    }

    /// <summary>
    /// Loads the seeded resources of <paramref name="language"/>, or returns null if none have been seeded yet.
    /// </summary>
    public static async Task<SeedResources> TryLoadAsync(SmartDbContext db, Language language, CancellationToken cancelToken = default)
    {
        Guard.NotNull(db);
        Guard.NotNull(language);

        var resources = await db.LocaleStringResources
            .AsNoTracking()
            .Where(x => x.LanguageId == language.Id)
            .Select(x => new { x.ResourceName, x.ResourceValue })
            .ToListAsync(cancelToken);

        if (resources.Count == 0)
        {
            return null;
        }

        var map = resources
            .GroupBy(x => x.ResourceName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().ResourceValue, StringComparer.OrdinalIgnoreCase);

        return new SeedResources(language.LanguageCulture, map);
    }
}
