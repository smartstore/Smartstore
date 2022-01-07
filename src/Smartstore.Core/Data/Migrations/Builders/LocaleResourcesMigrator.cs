using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Data.Migrations
{
    internal class LocaleResourcesMigrator
    {
        private readonly SmartDbContext _db;
        private readonly DbSet<Language> _languages;
        private readonly DbSet<LocaleStringResource> _resources;

        public LocaleResourcesMigrator(SmartDbContext db)
        {
            _db = Guard.NotNull(db, nameof(db));
            _languages = db.Set<Language>();
            _resources = db.Set<LocaleStringResource>();
        }

        public async Task MigrateAsync(IEnumerable<LocaleResourceEntry> entries, bool updateTouchedResources = false)
        {
            Guard.NotNull(entries, nameof(entries));

            if (!entries.Any() || !_languages.Any())
                return;

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, minHookImportance: HookImportance.Essential))
            {
                var langMap = (await _languages
                    .ToListAsync())
                    .ToDictionarySafe(x => x.LanguageCulture.EmptyNull().ToLower());

                var toDelete = new List<LocaleStringResource>();
                var toUpdate = new List<LocaleStringResource>();
                var toAdd = new List<LocaleStringResource>();

                bool IsEntryValid(LocaleResourceEntry entry, Language targetLang)
                {
                    if (entry.Lang == null)
                        return true;

                    var sourceLangCode = entry.Lang.ToLower();

                    if (targetLang != null)
                    {
                        var culture = targetLang.LanguageCulture;
                        if (culture == sourceLangCode || culture.StartsWith(sourceLangCode + "-"))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (langMap.ContainsKey(sourceLangCode))
                            return true;

                        if (langMap.Keys.Any(k => k.StartsWith(sourceLangCode + "-", StringComparison.OrdinalIgnoreCase)))
                            return true;
                    }

                    return false;
                }

                // Remove all entries with invalid lang identifier
                var invalidEntries = entries.Where(x => !IsEntryValid(x, null));
                if (invalidEntries.Any())
                {
                    entries = entries.Except(invalidEntries).ToArray();
                }

                foreach (var lang in langMap)
                {
                    var validEntries = entries.Where(x => IsEntryValid(x, lang.Value)).ToArray();
                    foreach (var entry in validEntries)
                    {
                        var dbRes = GetResource(entry.Key, lang.Value.Id, toAdd, out bool isLocal);

                        if (dbRes == null && entry.Value.HasValue() && !entry.UpdateOnly)
                        {
                            // ADD action
                            toAdd.Add(new LocaleStringResource { LanguageId = lang.Value.Id, ResourceName = entry.Key, ResourceValue = entry.Value });
                        }

                        if (dbRes == null)
                            continue;

                        if (entry.Value == null)
                        {
                            // DELETE action
                            if (isLocal)
                                toAdd.Remove(dbRes);
                            else
                                toDelete.Add(dbRes);
                        }
                        else
                        {
                            if (isLocal)
                            {
                                dbRes.ResourceValue = entry.Value;
                                continue;
                            }

                            // UPDATE action
                            if (updateTouchedResources || !dbRes.IsTouched.GetValueOrDefault())
                            {
                                dbRes.ResourceValue = entry.Value;
                                toUpdate.Add(dbRes);
                                if (toDelete.Contains(dbRes))
                                    toDelete.Remove(dbRes);
                            }
                        }
                    }
                }

                if (toAdd.Any() || toDelete.Any())
                {
                    // add new resources to context
                    _resources.AddRange(toAdd);

                    // remove deleted resources
                    _resources.RemoveRange(toDelete);

                    // save now
                    int affectedRows = await _db.SaveChangesAsync();

                    _db.DetachEntities<Language>();
                    _db.DetachEntities<LocaleStringResource>();
                }
            }
        }

        private LocaleStringResource GetResource(string key, int langId, IList<LocaleStringResource> local, out bool isLocal)
        {
            var res = local.FirstOrDefault(x => x.ResourceName.Equals(key, StringComparison.InvariantCultureIgnoreCase) && x.LanguageId == langId);
            isLocal = res != null;

            if (res == null)
            {
                res = _resources
                    .Where(x => x.ResourceName == key && x.LanguageId == langId)
                    .FirstOrDefault();
            }

            return res;
        }

    }
}
