namespace Smartstore.Core.Localization
{
    public static class LocaleStringResourceQueryExtensions
    {
        public static IQueryable<LocaleStringResource> ApplyLanguageFilter(this IQueryable<LocaleStringResource> query, int languageId)
        {
            Guard.NotNull(query, nameof(query));

            if (languageId > 0)
                query = query.Where(x => x.LanguageId == languageId);

            return query;
        }

        public static IQueryable<LocaleStringResource> ApplyPatternFilter(this IQueryable<LocaleStringResource> query, string pattern)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotEmpty(pattern, nameof(pattern));

            query = query.Where(x => x.ResourceName.StartsWith(pattern));

            return query;
        }
    }
}