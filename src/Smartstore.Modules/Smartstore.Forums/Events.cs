using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;
using Smartstore.Events;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models;
using Smartstore.Forums.Search.Modelling;
using Smartstore.Forums.Services;
using Smartstore.Templating;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Builders;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Forums
{
    public class Events : IConsumer
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        // Add menu item for ForumSettings to settings menu.
        public async Task HandleEventAsync(MenuBuiltEvent message,
            ICommonServices services,
            IUrlHelper urlHelper,
            ForumSettings forumSettings)
        {
            if (message.Name.EqualsNoCase("Settings"))
            {
                if (await services.Permissions.AuthorizeAsync(ForumPermissions.Read))
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
            else if (message.Name.EqualsNoCase("MyAccount"))
            {
                if (forumSettings.ForumsEnabled && forumSettings.AllowCustomersToManageSubscriptions)
                {
                    message.Root.Append(new MenuItem
                    {
                        Id = "forumsubscriptions",
                        Text = T("Account.ForumSubscriptions"),
                        Icon = "fal fa-bell",
                        Url = urlHelper.Action("CustomerSubscriptions", "Boards", new { area = string.Empty })
                    });
                }

                if (forumSettings.AllowPrivateMessages)
                {
                    var numUnreadMessages = 0;
                    var customer = services.WorkContext.CurrentCustomer;

                    if (!customer.IsGuest())
                    {
                        numUnreadMessages = await services.DbContext.PrivateMessages()
                            .ApplyStatusFilter(false, null, false)
                            .ApplyStandardFilter(null, customer.Id, services.StoreContext.CurrentStore.Id)
                            .CountAsync();
                    }

                    // TODO: (mg) (core) verify private message route URL.
                    message.Root.Append(new MenuItem
                    {
                        Id = "privatemessages",
                        Text = T("PrivateMessages.Inbox"),
                        Icon = "fal fa-envelope",
                        Url = urlHelper.RouteUrl("PrivateMessages", new { tab = "inbox" }),
                        BadgeText = numUnreadMessages > 0 ? numUnreadMessages.ToString() : null,
                        BadgeStyle = (int)BadgeStyle.Warning
                    });
                }
            }
        }

        // Add tab for ForumSearchSettings to search settings page.
        public async Task HandleEventAsync(TabStripCreated message, 
            IPermissionService permissions)
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
        public async Task HandleEventAsync(ModelBoundEvent message, 
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

        // Add forum message model parts.
        public async Task HandleEventAsync(MessageModelPartMappingEvent message,
            SmartDbContext db,
            IForumService forumService,
            IUrlHelper urlHelper,
            IDateTimeHelper dtHelper,
            MessageModelHelper messageModelHelper)
        {
            var ctx = message.MessageContext;

            if (message.Source is ForumTopic topic)
            {
                var firstPost = await db.ForumPosts()
                    .AsNoTracking()
                    .ApplyStandardFilter(ctx.Customer, topic.Id)
                    .FirstOrDefaultAsync();

                var pageIndex = ctx.Model.GetFromBag<int>("TopicPageIndex");
                var url = pageIndex > 0 ?
                    urlHelper.RouteUrl("ForumTopicBySlugPaged", new { id = topic.Id, slug = forumService.BuildSlug(topic), page = pageIndex }) :
                    urlHelper.RouteUrl("ForumTopicBySlug", new { id = topic.Id, slug = forumService.BuildSlug(topic) });

                message.Result = new Dictionary<string, object>
                {
                    { "Subject", topic.Subject.NullEmpty() },
                    { "NumReplies", topic.NumReplies },
                    { "NumPosts", topic.NumPosts },
                    { "NumViews", topic.Views },
                    { "Body", forumService.FormatPostText(firstPost).NullEmpty() },
                    { "Url", messageModelHelper.BuildUrl(url, ctx) }
                };

                await PublishEvent(topic);
            }
            else if (message.Source is Forum forum)
            {
                await db.LoadReferenceAsync(forum, x => x.ForumGroup);

                var url = urlHelper.RouteUrl("ForumBySlug", new { id = forum.Id, slug = await forum.GetActiveSlugAsync(ctx.Language.Id) });

                message.Result = new Dictionary<string, object>
                {
                    { "Name", forum.GetLocalized(x => x.Name, ctx.Language).Value.NullEmpty() },
                    { "GroupName", forum.ForumGroup?.GetLocalized(x => x.Name, ctx.Language)?.Value.NullEmpty() },
                    { "NumPosts", forum.NumPosts },
                    { "NumTopics", forum.NumTopics },
                    { "Url", messageModelHelper.BuildUrl(url, ctx) }
                };

                await PublishEvent(forum);
            }
            else if (message.Source is ForumPost post)
            {
                await db.LoadReferenceAsync(post, x => x.Customer);

                message.Result = new Dictionary<string, object>
                {
                    { "Author", post.Customer.FormatUserName().NullEmpty() },
                    { "Body", forumService.FormatPostText(post).NullEmpty() }
                };

                await PublishEvent(post);
            }
            else if (message.Source is ForumPostVote vote)
            {
                await db.LoadReferenceAsync(vote, x => x.ForumPost, false, x => x.Include(y => y.ForumTopic));

                var timeZone = dtHelper.GetCustomerTimeZone(ctx.Customer);

                message.Result = new Dictionary<string, object>
                {
                    { "ForumPostId", vote.ForumPostId },
                    { "Vote", vote.Vote },
                    { "TopicId", vote.ForumPost?.TopicId },
                    { "TopicSubject", vote.ForumPost?.ForumTopic?.Subject.NullEmpty() },
                    { "CustomerId", vote.CustomerId },
                    { "IpAddress", vote.IpAddress },
                    { "CreatedOn", dtHelper.ConvertToUserTime(vote.CreatedOnUtc, TimeZoneInfo.Utc, timeZone) },
                    { "UpdatedOn", dtHelper.ConvertToUserTime(vote.UpdatedOnUtc, TimeZoneInfo.Utc, timeZone) }
                };

                await PublishEvent(vote);
            }
            else if (message.Source is ForumSubscription subscription)
            {
                var timeZone = dtHelper.GetCustomerTimeZone(ctx.Customer);

                message.Result = new Dictionary<string, object>
                {
                    { "SubscriptionGuid", subscription.SubscriptionGuid },
                    { "CustomerId", subscription.CustomerId },
                    { "ForumId", subscription.ForumId },
                    { "TopicId", subscription.TopicId },
                    { "CreatedOn", dtHelper.ConvertToUserTime(subscription.CreatedOnUtc, TimeZoneInfo.Utc, timeZone) }
                };

                await PublishEvent(subscription);
            }
            else if (message.Source is PrivateMessage pm)
            {
                await db.LoadReferenceAsync(pm, x => x.FromCustomer);
                await db.LoadReferenceAsync(pm, x => x.ToCustomer);

                // TODO: (mg) (core) verify private message frontend URL.
                var url = urlHelper.Action("View", "PrivateMessages", new { id = pm.Id, area = string.Empty });

                message.Result = new Dictionary<string, object>
                {
                    { "Subject", pm.Subject.NullEmpty() },
                    { "Text", pm.FormatPrivateMessageText().NullEmpty() },
                    { "FromEmail", pm.FromCustomer?.FindEmail().NullEmpty() },
                    { "ToEmail", pm.ToCustomer?.FindEmail().NullEmpty() },
                    { "FromName", pm.FromCustomer?.GetFullName().NullEmpty() },
                    { "ToName", pm.ToCustomer?.GetFullName().NullEmpty() },
                    { "Url", messageModelHelper.BuildUrl(url, ctx) }
                };

                await PublishEvent(pm);
            }

            Task PublishEvent<T>(T source) where T : class
            {
                return messageModelHelper.PublishModelPartCreatedEventAsync(source, message.Result);
            }
        }

        // Add random forum data for message template preview.
        public async Task HandleEventAsync(PreviewModelResolveEvent message, 
            ITemplateEngine engine, 
            SmartDbContext db)
        {
            if (message.ModelName.EqualsNoCase(nameof(ForumPost)))
            {
                var count = await db.ForumPosts().CountAsync();
                var skip = CommonHelper.GenerateRandomInteger(0, count);

                if (count > 0)
                {
                    message.Result = await db.ForumPosts()
                        .Include(x => x.ForumTopic)
                        .ThenInclude(x => x.Forum)
                        .Include(x => x.ForumPostVotes)
                        .Include(x => x.Customer)
                        .AsNoTracking()
                        .OrderBy(x => x.Id)
                        .Skip(skip)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    var post = new ForumPost
                    {
                        Text = "Seamlessly plagiarize intermandated 'outside the box' leading-edge process improvements. Collaboratively optimize markets vis-a-vis resource-leveling.",
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    message.Result = engine.CreateTestModelFor(post, post.GetEntityName());
                }
            }
            else if (message.ModelName.EqualsNoCase(nameof(ForumTopic)))
            {
                var count = await db.ForumTopics().CountAsync();
                var skip = CommonHelper.GenerateRandomInteger(0, count);

                if (count > 0)
                {
                    message.Result = await db.ForumTopics()
                        .Include(x => x.Forum)
                        .Include(x => x.Customer)
                        .AsNoTracking()
                        .OrderBy(x => x.Id)
                        .Skip(skip)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    var topic = new ForumTopic
                    {
                        Subject = "Synergistically whiteboard timely action items before parallel collaboration and idea-sharing. Enthusiastically deliver.",
                        CreatedOnUtc = DateTime.UtcNow,
                        Views = 254
                    };

                    message.Result = engine.CreateTestModelFor(topic, topic.GetEntityName());
                }
            }
            else if (message.ModelName.EqualsNoCase(nameof(PrivateMessage)))
            {
                var count = await db.PrivateMessages().CountAsync();
                var skip = CommonHelper.GenerateRandomInteger(0, count);

                if (count > 0)
                {
                    message.Result = await db.PrivateMessages()
                        .Include(x => x.FromCustomer)
                        .Include(x => x.ToCustomer)
                        .AsNoTracking()
                        .OrderBy(x => x.Id)
                        .Skip(skip)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    var pm = new PrivateMessage
                    {
                        Subject = "Efficiently synergize cross-unit vortals via user friendly markets.",
                        Text = "Holisticly matrix maintainable supply chains for strategic synergy. Uniquely maintain cross-platform. Dynamically provide access to holistic initiatives after.",
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    message.Result = engine.CreateTestModelFor(pm, pm.GetEntityName());
                }
            }
        }

        // Anonymize customer forum data (GDPR).
        public async Task HandleEventAsync(CustomerAnonymizedEvent message,
            SmartDbContext db)
        {
            var tool = message.GdprTool;
            var customer = message.Customer;
            var language = message.Language;

            await db.ForumSubscriptions()
                .Where(x => x.CustomerId == customer.Id)
                .BatchDeleteAsync();

            var posts = await db.ForumPosts()
                .Where(x => x.CustomerId == customer.Id)
                .ToListAsync();

            foreach (var post in posts)
            {
                tool.AnonymizeData(post, x => x.IPAddress, IdentifierDataType.IpAddress, language);

                if (message.PseudomyzeContent)
                {
                    tool.AnonymizeData(post, x => x.Text, IdentifierDataType.LongText, language);
                }
            }

            if (message.PseudomyzeContent)
            {
                var privateMessages = await db.PrivateMessages()
                    .Where(x => x.FromCustomerId == customer.Id)
                    .ToListAsync();

                foreach (var pm in privateMessages)
                {
                    tool.AnonymizeData(pm, x => x.Subject, IdentifierDataType.Text, language);
                    tool.AnonymizeData(pm, x => x.Text, IdentifierDataType.LongText, language);
                }

                var topics = await db.ForumTopics()
                    .Where(x => x.CustomerId == customer.Id)
                    .ToListAsync();

                foreach (var topic in topics)
                {
                    tool.AnonymizeData(topic, x => x.Subject, IdentifierDataType.Text, language);
                }
            }
        }

        // Download customer forum data for "GDPR Data Portability".
        public async Task HandleEventAsync(GdprCustomerDataExportedEvent message,
            SmartDbContext db,
            IForumService forumService,
            IDateTimeHelper dtHelper)
        {
            // INFO: we're not going to export private messages.
            // It doesn't feel right and GDPR rules are not very clear about this. Let's wait and see :-)

            var customer = message.Customer;
            var timeZone = dtHelper.GetCustomerTimeZone(customer);

            var posts = await db.ForumPosts()
                .AsNoTracking()
                .ApplyStandardFilter(customer, null, true)
                .ToListAsync();

            var topics = await db.ForumTopics()
                .AsNoTracking()
                .ApplyStandardFilter(customer, null, true)
                .ToListAsync();

            if (topics.Any())
            {
                var postsMap = posts.ToMultimap(x => x.TopicId, x => x);

                message.Result["ForumTopics"] = topics.Select(x =>
                {
                    postsMap.TryGetValues(x.Id, out var topicPosts);

                    return new Dictionary<string, object>
                    {
                        { "Subject", x.Subject.NullEmpty() },
                        { "NumReplies", x.NumReplies },
                        { "NumPosts", x.NumPosts },
                        { "NumViews", x.Views },
                        { "Body", forumService.FormatPostText(topicPosts?.FirstOrDefault()).NullEmpty() }
                    };
                })
                .ToList();
            }

            if (posts.Any())
            {
                var author = customer.FormatUserName().NullEmpty();

                message.Result["ForumPosts"] = posts.Select(x => new Dictionary<string, object>
                {
                    { "Author", author },
                    { "Body", forumService.FormatPostText(x).NullEmpty() }
                })
                .ToList();
            }

            var votes = await db.CustomerContent
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OfType<ForumPostVote>()
                .Include(x => x.ForumPost)
                .ThenInclude(x => x.ForumTopic)
                .ToListAsync();

            if (votes.Any())
            {
                message.Result["ForumPostVotes"] = votes.Select(x => new Dictionary<string, object>
                {
                    { "ForumPostId", x.ForumPostId },
                    { "Vote", x.Vote },
                    { "TopicId", x.ForumPost?.TopicId },
                    { "TopicSubject", x.ForumPost?.ForumTopic?.Subject.NullEmpty() },
                    { "IpAddress", x.IpAddress },
                    { "CreatedOn", dtHelper.ConvertToUserTime(x.CreatedOnUtc, TimeZoneInfo.Utc, timeZone) }
                })
                .ToList();
            }

            var subscriptions = await db.ForumSubscriptions()
                .AsNoTracking()
                .ApplyStandardFilter(customer.Id)
                .ToListAsync();

            if (subscriptions.Any())
            {
                message.Result["ForumSubscriptions"] = subscriptions.Select(x => new Dictionary<string, object>
                {
                    { "SubscriptionGuid", x.SubscriptionGuid },
                    { "ForumId", x.ForumId },
                    { "TopicId", x.TopicId },
                    { "CreatedOn", dtHelper.ConvertToUserTime(x.CreatedOnUtc, TimeZoneInfo.Utc, timeZone) }
                })
                .ToList();
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
