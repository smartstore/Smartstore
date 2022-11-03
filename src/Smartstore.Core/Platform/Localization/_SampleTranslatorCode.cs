using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq.Dynamic.Core;
using System.Reflection;
using AngleSharp.Dom;
using Newtonsoft.Json.Linq;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

namespace Smartstore.Core.Localization
{
    //public class MySingletonService
    //{
    //    private IDbContextFactory<SmartDbContext> _dbFactory;

    //    public MySingletonService(IDbContextFactory<SmartDbContext> dbFactory)
    //    {
    //        _dbFactory = dbFactory;
    //    }

    //    public Task<List<LocalizedProperty>> GetMyEntities()
    //    {
    //        using (var db = _dbFactory.CreateDbContext())
    //        {
    //            return db.LocalizedProperties.ToListAsync();
    //        }
    //    }
    //}

    internal class SampleTranslatorCode
    {
        private ILocalizedEntityDescriptorProvider _provider = null;
        private ILocalizedEntityLoader _loader = null;
        private SmartDbContext _db = null;

        /// <summary>
        /// Composite Key für unsere Lookup-Dictionary, die auch Language ID enthält.
        /// </summary>
        class PropertyKey : Tuple<int, string, int>
        {
            public PropertyKey(int entityId, string localeKey, int langId)
                : base(entityId, localeKey, langId)
            {
            }

            public int EntityId => base.Item1;
            public string LocaleKey => base.Item2;
            public int LanguageId => base.Item3;
        }

        public async Task TranslateAsync(CancellationToken cancelToken)
        {
            // Datum der letzten Translation-Session von irgendwoher besorgen (z.B. aus Datei, whatever)
            DateTime? lastExecutedUtc = null;

            // Irgendwie ermitteln, ob CollectDataAsync() bereits gelaufen ist in Vergangenheit.
            // Vielleicht auch in einer Datei merken.
            bool isDataCollectedAlready = false;

            if (!isDataCollectedAlready)
            {
                // Noch nichts gesammelt. Nachholen.
                await CollectDataAsync();
            }

            // ID der Master-Sprache besorgen
            int masterLanguageId = 1;

            foreach (var d in _provider.GetDescriptors().Values)
            {
                // Für schnellere Lookups nach Descriptors gruppieren,
                // damit wir keine KeyGroup-Durchmischung in den geladenen Batches haben.

                // Query für Master-Texte aufbauen
                var query = _db.LocalizedProperties.Where(x => x.LanguageId == masterLanguageId && x.LocaleKeyGroup == d.KeyGroup);

                if (lastExecutedUtc != null)
                {
                    // Filtern nach nicht übersetzten oder veralteten Einträgen.
                    query = query.Where(x => x.TranslatedOnUtc == null || x.TranslatedOnUtc <= lastExecutedUtc);
                }

                var pager = new FastPager<LocalizedProperty>(query, 128);
                while ((await pager.ReadNextPageAsync<LocalizedProperty>(cancelToken)).Out(out var batch))
                {
                    await ProcessTranslationBatchAsync(batch, d, masterLanguageId);
                }
            }
        }

        private async Task ProcessTranslationBatchAsync(IList<LocalizedProperty> masterProps, LocalizedEntityDescriptor d, int masterLanguageId)
        {
            var entityIds = masterProps.Select(x => x.Id).Distinct().ToArray();

            // Language IDs aller Zielsprachen ermitteln
            var languageIds = new int[] { 2, 3, 4 };

            // Übersetzungen für aktuellen Batch besorgen (alle außer MasterLanguageId)
            var locPropLookup = await _db.LocalizedProperties
                .Where(x => x.LanguageId != masterLanguageId && x.LocaleKeyGroup == d.KeyGroup && entityIds.Contains(x.EntityId))
                .ToDictionaryAsync(x => new PropertyKey(x.EntityId, x.LocaleKey, x.LanguageId));

            foreach (var masterProp in masterProps)
            {
                // XxHash ist superschnell!
                var masterHash = XxHashUnsafe.ComputeHash(masterProp.LocaleValue).ToStringInvariant();

                foreach (var langId in languageIds)
                {
                    // Eintrag in Zielsprache besorgen...
                    var locProp = locPropLookup.Get(new PropertyKey(masterProp.EntityId, masterProp.LocaleKey, langId));

                    if (locProp == null)
                    {
                        // ...existiert noch nicht.
                        locProp = new LocalizedProperty
                        {
                            LocaleKeyGroup = masterProp.LocaleKeyGroup,
                            LanguageId = langId,
                            LocaleKey = masterProp.LocaleKey,
                            EntityId = masterProp.EntityId,
                            CreatedOnUtc = DateTime.UtcNow,
                            CreatedBy = "CRS.TranslationService",
                            MasterChecksum = masterHash
                        };

                        _db.LocalizedProperties.Add(locProp);

                        await TranslatePropertyAsync(masterProp, locProp);
                    }
                    else
                    {
                        // ...existiert schon. Aktualisieren.

                        // Ermitteln, ob sich der Master-Content seit der letzten Übersetzung geändert hat.
                        if (locProp.MasterChecksum == null || locProp.MasterChecksum != masterHash)
                        {
                            // TODO: Theoretisch müssten wir hier zusätzlich prüfen,
                            // ob der User einen zuvor übersetzten Eintrag per Backend 
                            // explizit geändert hat. Oder?... Denn dann dürften wir
                            // nix übersetzen.

                            //var author = locProp.UpdatedBy ?? locProp.CreatedBy;
                            //var lastUpdated = locProp.UpdatedOnUtc ?? locProp.CreatedOnUtc;
                            
                            await TranslatePropertyAsync(masterProp, locProp);

                            // Hash merken
                            locProp.MasterChecksum = masterHash;
                            locProp.UpdatedOnUtc = DateTime.UtcNow;
                            locProp.UpdatedBy = "CRS.TranslationService";
                        }
                    }
                }

                // Für das nächste mal merken, wann wir das übersetzt haben.
                // TODO: DateTime.UtcNow ist eine Scheiß-Idee. Besser machen.
                masterProp.TranslatedOnUtc = DateTime.UtcNow;
            }

            // Speichern
            await _db.SaveChangesAsync();

            // Atmen
            _db.DetachEntities<LocalizedProperty>();
        }

        private async Task TranslatePropertyAsync(LocalizedProperty masterProp, LocalizedProperty locProp)
        {
            // TODO: Übersetzungen sollten definitiv gebatched ablaufen, also mehrere 
            // Einträge senden und empfangen. Das habe ich hier nicht berücksichtigt.
            
            // Do something
            await Task.Delay(0);
        }

        public async Task CollectDataAsync() 
        {
            // ID der Master-Sprache besorgen
            int masterLanguageId = 1;
            
            // Alle ILocalizedEntity Descriptors iterieren
            foreach (var d in _provider.GetDescriptors().Values)
            {
                // Pager besorgen. Für Batch-Operationen hat sich 128 als
                // perfekte PageSize etabliert, weil damit
                // SELECT...IN[,] Abfragen noch sehr performant laufen.
                var pager = _loader.LoadGroupPaged(d, pageSize: 128);

                // Alle Pages einlesen (jeweils max. 128 Entities)
                while ((await pager.ReadNextPageAsync()).Out(out var list))
                {
                    // Batch verarbeiten
                    await ProcessCollectionBatchAsync(masterLanguageId, d.KeyGroup, list);
                }
            }

            // Nicht normierbare Sonderfälle verarbeiten, wie CookieInfo und lokalisierbare Settings
            foreach (var d in _provider.GetDelegates())
            {
                var list = await _loader.LoadByDelegateAsync(d);

                // ... Dafür habe ich jetzt keinen Sample-Code, weil wir
                // das untere ProcessCollectionBatchAsync() an dieser Stelle
                // leider nicht direkt verwenden können. Denn per Delegat geladene
                // Entries können gemischte KeyGroups aufweisen, wir müssten hier 
                // also noch irgendwie nach KeyGroup gruppieren und
                // ProcessCollectionBatchAsync() für jede Gruppe dezidiert aufrufen.
            }

            // Ab jetzt können wir mit LocalizedProperty in Mastersprache als Quelle weiterarbeiten
            // und müssen den ILocalizedEntityLoader nicht mehr bemühen, um Daten einzusammeln.

            // Wichtig ist nur, dass wir ab jetzt per Hook auf Änderungen in ILocalizedEntity-
            // und LocalizedProperty-Instanzen reagieren müssen. Siehe Code weiter unten.
            // Täten wir dies nicht, müssten wir bei jeder Translation-Session diesen potentiell
            // langsamen Code wiederholt ausführen. Und genau das wollen wir nicht, weil
            // wir ja schließlich entkoppeln möchten.
        }

        protected async Task ProcessCollectionBatchAsync(int masterLanguageId, string keyGroup, IList<dynamic> source)
        {
            // Alle enthaltenen Entity-Ids extrahieren
            var allEntityIds = source
                // Weil wir DynamicLinq nutzen, wissen wir, dass der Typ intern DynamicClass ist
                .Cast<DynamicClass>()
                // Id property ist IMMER vorhanden in dynamic entity
                .Select(x => (int)x["Id"])
                .ToArray();

            // Bereits existierende Master LocalizedProperty Entities für aktuellen Batch ermitteln
            var existingLocalizedProperties = await _db.LocalizedProperties
                // Sicherstellen, dass nix aus dem 2nd Level Cache kommt
                .AsNoCaching()
                // KeyGroup steckt im Descriptor
                .Where(x => x.LocaleKeyGroup == keyGroup
                    // masterLanguageId haben wir übergeben
                    && x.LanguageId == masterLanguageId
                    // Haben wir aus "source" extrahiert
                    && allEntityIds.Contains(x.EntityId))
                // Composite PropertyKey als Key benutzen für einfache und schnelle Lookups später
                .ToDictionaryAsync(x => new PropertyKey(x.EntityId, x.LocaleKey, masterLanguageId));

            // Alle Standard-Texte iterieren (jene, die in Entities stecken, z.B. Product, Category etc.)
            foreach (var item in source.Cast<DynamicClass>())
            {
                // Dynamische Properties ermitteln (z.B. Name, Description, Title etc.)
                foreach (var propName in item.GetDynamicMemberNames())
                {
                    if (propName == "Id" || propName == "KeyGroup")
                    {
                        // Id und KeyGroup sind nur Meta-Properties. Brauchen wir nicht, also weitermachen...
                        continue;
                    }

                    // Key für Lookup erzeugen. propName ist unser LocalizedProperty.LocaleKey
                    var entityId = (int)item["Id"];
                    var propertyKey = new PropertyKey(entityId, propName, masterLanguageId);

                    if (!existingLocalizedProperties.ContainsKey(propertyKey))
                    {
                        // Key existiert nicht in Dictionary, demzufolge auch nicht
                        // in Datenbank. Müssen wir anlegen.
                        _db.LocalizedProperties.Add(new LocalizedProperty 
                        {
                            LocaleKeyGroup = keyGroup,
                            LanguageId = masterLanguageId,
                            LocaleKey = propName,
                            EntityId = entityId,
                            LocaleValue = item[propName].Convert<string>(),
                            // SEHR WICHTIG! Wollen wir nicht in UI sehen.
                            IsHidden = true,
                            // Befüllen der Audit-Daten nicht dem Hook überlassen, selber zuweisen.
                            CreatedOnUtc = DateTime.UtcNow,
                            CreatedBy = "CRS.TranslationService"
                        });
                    }
                }
            }

            // Alle neu angelegten LocalizedProperty Instanzen in DB speichern
            await _db.SaveChangesAsync();

            // ChangeTracker leeren, damit er sich nicht vollsaugt nach jedem Batch.
            // So erreichen wir stabile Performance. Sonst nimmt Performance nach jedem
            // Batch radikal ab.
            _db.DetachEntities<LocalizedProperty>();
        }

        ///// <summary>
        ///// Verfolgt Änderungen an lokalisierbaren Standardtexten. 
        ///// Ist als "Important" markiert, damit er auch während 
        ///// eines Import-Vorganges ausgeführt wird. Muss im Plugin
        ///// implementiert werden.
        ///// </summary>
        //[Important]
        //internal class LocalizedEntityChangeHook : AsyncDbSaveHook<ILocalizedEntity>
        //{
        //    private readonly SmartDbContext _db;
        //    private readonly ILocalizedEntityDescriptorProvider _provider;

        //    public LocalizedEntityChangeHook(SmartDbContext db, ILocalizedEntityDescriptorProvider provider)
        //    {
        //        _db = db;
        //        _provider = provider;
        //    }

        //    protected override Task<HookResult> OnInsertedAsync(ILocalizedEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        //    {
        //        // (perf) Wir hätten auch alles hier erledigen können. Aber in Long-Running Processes,
        //        // die stapelweise massenhaft Daten importieren, ist das langsam.
        //        // Nur EINMAL SaveChangesAsync ausführen ist deutlich schneller,
        //        // siehe OnAfterSaveCompletedAsync().

        //        // Signalisiere dem HookHandler, dass er diesen Hook künftig nur noch für
        //        // ILocalizedEntity Typen aufrufen soll, die auch einen Descriptor haben.
        //        // Der Batch-Methode unten (OnAfterSaveCompletedAsync) werden nur Entries
        //        // übergeben, die hier mit HookResult.Ok quittiert wurden.
        //        return Task.FromResult(
        //            _provider.GetDescriptorByEntityType(entity.GetType()) != null
        //                ? HookResult.Ok
        //                : HookResult.Void);
        //    }

        //    protected override Task<HookResult> OnUpdatingAsync(ILocalizedEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        //    {
        //        // Muss ein PRESave-Hook sein, weil wir nach dem Speichern Infos über geänderte Props verlieren.
        //        // Weitere Erläuterungen siehe OnInsertedAsync().
        //        return Task.FromResult(
        //            _provider.GetDescriptorByEntityType(entity.GetType()) != null
        //                ? HookResult.Ok
        //                : HookResult.Void);
        //    }

        //    public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        //    {
        //        // ID der Mastersprache besorgen
        //        var masterLanguageId = 1;

        //        // (perf) Alle Entries nach KeyGroup gruppieren,
        //        // um weniger DB-Lookups durchführen zu müssen.
        //        var updatedEntitiesGrouped = entries
        //            // SoftDeleted Entities interessieren uns nicht, die werden eh von einem anderen Hook abgeräumt.
        //            .Where(x => !x.IsSoftDeleted.GetValueOrDefault())
        //            .Select(x => x.Entity)
        //            .GroupBy(x => x.GetEntityName());

        //        foreach (var group in updatedEntitiesGrouped)
        //        {
        //            // Alle Entity-Ids in Gruppe extrahieren
        //            var entityIds = group.Select(x => x.Id).ToArray();
        //            // Descriptor für Gruppe besorgen
        //            var descriptor = _provider.GetDescriptorByEntityType(group.First().GetType());
        //            var keyGroup = group.Key;

        //            var existingLocProps = await _db.LocalizedProperties
        //                .Where(x => x.LocaleKeyGroup == keyGroup && x.LanguageId == masterLanguageId && entityIds.Contains(x.EntityId))
        //                // Composite PropertyKey als Key benutzen für einfache und schnelle Lookups später
        //                .ToDictionaryAsync(x => new PropertyKey(x.EntityId, x.LocaleKey, x.LanguageId));

        //            foreach (var updatedEntity in group)
        //            {
        //                // Alle lokalisierbaren Props iterieren
        //                foreach (var prop in descriptor.Properties)
        //                {
        //                    // Evtl. existierende LocProp per Lookup ermitteln
        //                    var existingLocProp = existingLocProps.Get(new PropertyKey(updatedEntity.Id, prop.Name, masterLanguageId));
                            
        //                    if (existingLocProp == null)
        //                    {
        //                        // LocProp existiert noch nicht in DB, daher versuchen anzulegen.
        //                        TryInsertLocalizedProperty(updatedEntity, masterLanguageId, prop);
        //                    }
        //                    else
        //                    {
        //                        // (perf) FastProperty für updatedEntity.Prop besorgen, z.B. "Product.Name"
        //                        var fastProp = FastProperty.GetProperty(prop, PropertyCachingStrategy.EagerCached);

        //                        // Wert per Reflection besorgen
        //                        var value = fastProp.GetValue(updatedEntity);
        //                        var valueStr = value.Convert<string>();

        //                        if (valueStr != existingLocProp.LocaleValue)
        //                        {
        //                            // Entity-Wert ist ungleich LocalizedProperty.LocaleValue und wurde somit geändert.

        //                            // Autor ermitteln
        //                            var author = existingLocProp.UpdatedBy ?? existingLocProp.CreatedBy;

        //                            // Wenn der Autor dieses Eintrags NICHT der Translator war,
        //                            // hat der User zuvor im Backend explizit was geändert.
        //                            // I.d.F. machen wir lieber gar nichts.
        //                            if (author == null || author == "CRS.TranslationService")
        //                            {
        //                                // Neuen Value übertragen
        //                                existingLocProp.LocaleValue = valueStr;
                                        
        //                                // Bei der nächsten Translation-Session muss dieser Eintrag also neu übersetzt werden.
        //                                existingLocProp.TranslatedOnUtc = null;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        // INFO: SaveChanges an dieser Stelle nicht notwendig, da wir in einem PreSave-Hook.
        //        // Commit steht also kurz bevor.
        //        // SaveChanges wird hier eh unterdrückt, um Zirkularitätsprobleme zu vermeiden.
        //    }

        //    public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        //    {
        //        // ID der Mastersprache besorgen
        //        var masterLanguageId = 1;

        //        // Im PostSave-Szenario interessieren uns nur NEUE Entities,
        //        // die über mind. eine lokalisierbare Prop verfügen.
        //        // INFO: Das Löschen verwaister LocalizedProperties nimmt bereits ein
        //        // anderer Hook im Core vor.
        //        var newEntities = entries
        //            .Select(x => x.Entity)
        //            .ToList();

        //        var isDirty = false;

        //        foreach (var newEntity in newEntities)
        //        {
        //            var descriptor = _provider.GetDescriptorByEntityType(newEntity.GetType());

        //            // Alle lokalisierbaren Props iterieren
        //            foreach (var prop in descriptor.Properties)
        //            {
        //                isDirty = TryInsertLocalizedProperty(newEntity, masterLanguageId, prop) || isDirty;
        //            }
        //        }

        //        // Alle neuen LocalizedProperty Entities speichern
        //        if (isDirty)
        //        {
        //            await _db.SaveChangesAsync(cancelToken);
        //        }     
        //    }

        //    /// <summary>
        //    /// Fügt eine neue LocalizedProperty Instanz hinzu, wenn prop nicht leer ist.
        //    /// </summary>
        //    private bool TryInsertLocalizedProperty(BaseEntity forEntity, int langId, PropertyInfo prop)
        //    {
        //        // (perf) FastProperty für entity.Prop besorgen, z.B. "Product.Name"
        //        var fastProp = FastProperty.GetProperty(prop, PropertyCachingStrategy.EagerCached);

        //        // Wert per Reflection besorgen
        //        var value = fastProp.GetValue(forEntity);
        //        var hasValue = value != null && (value is not string str || str.HasValue());

        //        if (hasValue)
        //        {
        //            // Lokalisierbare Property hat einen Wert:
        //            // müssen wir also als LocalizedProperty abbilden und ablegen.
        //            _db.LocalizedProperties.Add(new LocalizedProperty
        //            {
        //                LocaleKeyGroup = forEntity.GetEntityName(),
        //                LanguageId = langId,
        //                LocaleKey = prop.Name,
        //                EntityId = forEntity.Id,
        //                LocaleValue = value.Convert<string>(),
        //                // SEHR WICHTIG! Wollen wir nicht in UI sehen.
        //                IsHidden = true,
        //                // Befüllen der Audit-Daten nicht dem Hook überlassen, selber zuweisen.
        //                CreatedOnUtc = DateTime.UtcNow,
        //                CreatedBy = "CRS.TranslationService"
        //            });

        //            return true;
        //        }

        //        return false;
        //    }
        //}
    }
}
