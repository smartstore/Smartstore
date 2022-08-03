namespace Smartstore.Core.Localization
{
    public static partial class LocalizedPropertyQueryExtensions
    {
        public static IQueryable<LocalizedProperty> ApplyStandardFilter(this IQueryable<LocalizedProperty> query,
            int languageId,
            int entityId,
            string localeKeyGroup,
            string localeKey)
        {
            Guard.NotNull(query, nameof(query));

            if (languageId > 0)
                query = query.Where(x => x.LanguageId == languageId);

            if (entityId > 0)
                query = query.Where(x => x.EntityId == entityId);

            if (localeKeyGroup.HasValue())
                query = query.Where(x => x.LocaleKeyGroup == localeKeyGroup);

            if (localeKey.HasValue())
                query = query.Where(x => x.LocaleKey == localeKey);

            return query;
        }
    }
}