using System;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Forums.Models;
using Smartstore.Forums.Settings;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Forums
{
    public class Events : IConsumer
    {
        public Localizer T { get; set; }

        public async Task HandleEventAsync(TabStripCreated message, IPermissionService permissions)
        {
            // Render tab with forum search settings.
            if (message.TabStripName.EqualsNoCase("searchsettings-edit"))
            {
                if (await permissions.AuthorizeAsync(ForumPermissions.Read))
                {
                    await message.TabFactory.AddAsync(builder => builder
                        .Text(T("Forum.Forum"))
                        .Name("tab-search-forum")
                        .LinkHtmlAttributes(new { data_tab_name = "ForumSearchSettings" })
                        .Action("ForumSearchSettings", "Forum", new { area = "Admin" })
                        .Ajax());
                }
            }
        }

        public async Task HandleEventAsync(
            ModelBoundEvent message, 
            ICommonServices services,
            StoreDependingSettingHelper storeDependingSettingHelper
            /*Lazy<IForumSearchQueryAliasMapper> forumSearchQueryAliasMapper*/)
        {
            var model = message.BoundModel.CustomProperties.ContainsKey("ForumSearchSettings")
                ? message.BoundModel.CustomProperties["ForumSearchSettings"] as ForumSearchSettingsModel
                : null;

            if (model == null || !await services.Permissions.AuthorizeAsync(ForumPermissions.Read))
            {
                return;
            }

            var storeId = services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var store = services.StoreContext.GetStoreById(storeId);
            var storeScope = store?.Id ?? 0;    
            var settings = await services.SettingFactory.LoadSettingsAsync<ForumSearchSettings>(storeScope);

            $"-- Bound ForumSearchSettingsModel: {storeScope}".Dump();

            MiniMapper.Map(model, settings);
            settings.ForumDisabled = model.ForumFacet.Disabled;
            settings.ForumDisplayOrder = model.ForumFacet.DisplayOrder;
            settings.CustomerDisabled = model.CustomerFacet.Disabled;
            settings.CustomerDisplayOrder = model.CustomerFacet.DisplayOrder;
            settings.DateDisabled = model.DateFacet.Disabled;
            settings.DateDisplayOrder = model.DateFacet.DisplayOrder;

            // TODO: (mg) (core) no validation of ForumSearchSettingsModel? Too late for ModelBoundEvent.
            if (settings.InstantSearchEnabled)
            {
                if (settings.InstantSearchNumberOfHits < 1)
                {
                    settings.InstantSearchNumberOfHits = 1;
                }
                else if (settings.InstantSearchNumberOfHits > 16)
                {
                    settings.InstantSearchNumberOfHits = 16;
                }
            }

            // TODO: (mg) (core) storeDependingSettingHelper cannot work for models binded by CustomProperties (was never designed for it).
            // Actual key:   CustomProperties[ForumSearchSettings].SearchMode_OverrideForStore
            // Expected key: ForumSearchSettings.SearchMode_OverrideForStore
            await storeDependingSettingHelper.UpdateSettingsAsync(settings, message.Form, storeScope);
            
            await services.Settings.ApplySettingAsync(settings, x => x.SearchFields);

            if (storeScope != 0)
            {
                foreach (var prefix in new[] { "Forum", "Customer", "Date" })
                {
                    await storeDependingSettingHelper.ApplySettingAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings, message.Form, storeScope);
                    await storeDependingSettingHelper.ApplySettingAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings, message.Form, storeScope);
                }
            }

            var num = 0;
            num += await ApplyLocalizedFacetSettings(model.ForumFacet, FacetGroupKind.Forum, storeScope, services);
            num += await ApplyLocalizedFacetSettings(model.CustomerFacet, FacetGroupKind.Customer, storeScope, services);
            num += await ApplyLocalizedFacetSettings(model.DateFacet, FacetGroupKind.Date, storeScope, services);

            await services.DbContext.SaveChangesAsync();

            if (num > 0)
            {
                // TODO: (mg) (core) add IForumSearchQueryAliasMapper.
                //await forumSearchQueryAliasMapper.Value.ClearCommonFacetCacheAsync();
            }
        }

        private static async Task<int> ApplyLocalizedFacetSettings(
            ForumFacetSettingsModel model, 
            FacetGroupKind kind, 
            int storeId,
            ICommonServices services)
        {
            var num = 0;

            foreach (var localized in model.Locales)
            {
                var key = FacetUtility.GetFacetAliasSettingKey(kind, localized.LanguageId, "Forum");
                var existingAlias = services.Settings.GetSettingByKey<string>(key, storeId: storeId);

                if (existingAlias.EqualsNoCase(localized.Alias))
                {
                    continue;
                }

                if (localized.Alias.HasValue())
                {
                    await services.Settings.ApplySettingAsync(key, localized.Alias, storeId);
                }
                else
                {
                    await services.Settings.RemoveSettingAsync(key, storeId);
                }

                ++num;
            }

            return num;
        }
    }
}
