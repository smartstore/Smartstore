using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Events;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models;
using Smartstore.Forums.Search.Modelling;
using Smartstore.Forums.Services;
using Smartstore.Templating;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering.Builders;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Forums
{
    public class Events : IConsumer
    {
        public Localizer T { get; set; }

        // Add menu item for ForumSettings to settings menu.
        public async Task HandleEventAsync(MenuBuiltEvent message, IPermissionService permissions)
        {
            if (message.Name.EqualsNoCase("Settings"))
            {
                if (await permissions.AuthorizeAsync(ForumPermissions.Read))
                {
                    var refNode = message.Root.SelectNodeById("dataexchange") ?? message.Root.LastChild;
                    if (refNode != null)
                    {
                        var forumNode = new TreeNode<MenuItem>(new MenuItem().ToBuilder()
                            .Text("Forums")
                            .ResKey("Forum.Forums")
                            .Icon("fa fa-fw fa-users")
                            .PermissionNames(ForumPermissions.Read)
                            .Action("ForumSettings", "Forum", new { area = "Admin" })
                            .AsItem());

                        forumNode.InsertBefore(refNode);
                    }
                }
            }
        }

        // Add tab for ForumSearchSettings to search settings page.
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

        // Save ForumSearchSettings.
        public async Task HandleEventAsync(
            ModelBoundEvent message, 
            ICommonServices services,
            StoreDependingSettingHelper settingHelper,
            Lazy<IForumSearchQueryAliasMapper> forumSearchQueryAliasMapper)
        {
            var model = message.BoundModel.CustomProperties.ContainsKey("ForumSearchSettings")
                ? message.BoundModel.CustomProperties["ForumSearchSettings"] as ForumSearchSettingsModel
                : null;

            if (model == null || !await services.Permissions.AuthorizeAsync(ForumPermissions.Read))
            {
                return;
            }

            var storeId = services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var storeScope = services.StoreContext.GetStoreById(storeId)?.Id ?? 0;    
            var settingsProperties = FastProperty.GetProperties(typeof(ForumSearchSettings)).Values;
            var settings = await services.SettingFactory.LoadSettingsAsync<ForumSearchSettings>(storeScope);

            MiniMapper.Map(model, settings);
            settings.ForumDisabled = model.ForumFacet.Disabled;
            settings.ForumDisplayOrder = model.ForumFacet.DisplayOrder;
            settings.CustomerDisabled = model.CustomerFacet.Disabled;
            settings.CustomerDisplayOrder = model.CustomerFacet.DisplayOrder;
            settings.DateDisabled = model.DateFacet.Disabled;
            settings.DateDisplayOrder = model.DateFacet.DisplayOrder;

            foreach (var prop in settingsProperties)
            {
                await settingHelper.ApplySettingAsync(
                    $"CustomProperties[ForumSearchSettings].{prop.Name}",
                    prop.Name,
                    settings,
                    message.Form,
                    storeScope);
            }

            // Poor validation because ModelBoundEvent comes too late for ModelState.
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

            // We need to save here for subsequent ApplySettingAsync to work correctly.
            await services.DbContext.SaveChangesAsync();

            await services.Settings.ApplySettingAsync(settings, x => x.SearchFields);

            if (storeScope != 0)
            {
                foreach (var prefix in new[] { "Forum", "Customer", "Date" })
                {
                    await settingHelper.ApplySettingAsync($"CustomProperties[ForumSearchSettings].{prefix}Facet.Disabled", prefix + "Disabled", settings, message.Form, storeScope);
                    await settingHelper.ApplySettingAsync($"CustomProperties[ForumSearchSettings].{prefix}Facet.DisplayOrder", prefix + "DisplayOrder", settings, message.Form, storeScope);
                }
            }

            var num = 0;
            num += await ApplyLocalizedFacetSettings(model.ForumFacet, FacetGroupKind.Forum, storeScope, services);
            num += await ApplyLocalizedFacetSettings(model.CustomerFacet, FacetGroupKind.Customer, storeScope, services);
            num += await ApplyLocalizedFacetSettings(model.DateFacet, FacetGroupKind.Date, storeScope, services);

            await services.DbContext.SaveChangesAsync();

            if (num > 0)
            {
                await forumSearchQueryAliasMapper.Value.ClearCommonFacetCacheAsync();
            }
        }

        public async Task HandleEventAsync(MessageModelPartMappingEvent message, ICommonServices services, IForumService forumService)
        {
            // TODO: (mg) (core) handle MessageModelPartMappingEvent.
            if (message.Source is ForumTopic topic)
            {
                var customer = services.WorkContext.CurrentCustomer;
                var pageIndex = message.MessageContext.Model.GetFromBag<int>("TopicPageIndex");
                var firstPost = await services.DbContext.ForumPosts()
                    .AsNoTracking()
                    .ApplyStandardFilter(customer, topic.Id)
                    .FirstOrDefaultAsync();

                // TODO: (mg) (core) build localized route URL for message model part.
                // RE: MH has done this already in Blog module. Please have a look.
                //var url = pageIndex > 0 ?
                //    BuildRouteUrl("TopicSlugPaged", new { id = part.Id, slug = part.GetSeName(), page = pageIndex }, messageContext) :
                //    BuildRouteUrl("TopicSlug", new { id = part.Id, slug = part.GetSeName() }, messageContext);
                string url = null;

                message.Result = new Dictionary<string, object>
                {
                    { "Subject", topic.Subject.NullEmpty() },
                    { "NumReplies", topic.NumReplies },
                    { "NumPosts", topic.NumPosts },
                    { "NumViews", topic.Views },
                    { "Body", forumService.FormatPostText(firstPost).NullEmpty() },
                    { "Url", url }
                };

                await services.EventPublisher.PublishAsync(new MessageModelPartCreatedEvent<ForumTopic>(topic, message.Result));
            }
            else if (message.Source is ForumPost post)
            {
                await services.DbContext.LoadReferenceAsync(post, x => x.Customer);

                message.Result = new Dictionary<string, object>
                {
                    { "Author", post.Customer.FormatUserName().NullEmpty() },
                    { "Body", forumService.FormatPostText(post).NullEmpty() }
                };

                await services.EventPublisher.PublishAsync(new MessageModelPartCreatedEvent<ForumPost>(post, message.Result));
            }
        }

        // Add random forum data for message template preview.
        public async Task HandleEventAsync(PreviewModelResolveEvent message, ITemplateEngine engine, SmartDbContext db)
        {
            switch (message.ModelName)
            {
                case nameof(ForumTopic):
                    message.Result = await GetRandomEntity<ForumTopic>();
                    break;
                case nameof(Forum):
                    message.Result = await GetRandomEntity<Forum>();
                    break;
                case nameof(ForumPost):
                    message.Result = await GetRandomEntity<ForumPost>();
                    break;
                case nameof(ForumPostVote):
                    message.Result = await GetRandomEntity<ForumPostVote>();
                    break;
                case nameof(PrivateMessage):
                    message.Result = await GetRandomEntity<PrivateMessage>();
                    break;
            }

            async Task<object> GetRandomEntity<T>() where T : BaseEntity, new()
            {
                var query = db.Set<T>().AsNoTracking();
                var count = await query.CountAsync();

                if (count > 0)
                {
                    var skip = CommonHelper.GenerateRandomInteger(0, count);
                    return await query.OrderBy(x => x.Id).Skip(skip).FirstOrDefaultAsync();
                }
                else
                {
                    var entity = Activator.CreateInstance<T>();
                    return engine.CreateTestModelFor(entity, entity.GetEntityName());
                }
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
