using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;
using Smartstore.Utilities.Html;

namespace Smartstore.Forums.Services
{
    public partial class ForumService : AsyncDbSaveHook<Forum>, IForumService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly ForumSettings _forumSettings;
        private readonly HashSet<int> _deletingForumIds = new();
        private readonly HashSet<int> _postCountCustomerIds = new();

        public ForumService(
            SmartDbContext db,
            ICommonServices services,
            IGenericAttributeService genericAttributeService,
            Lazy<IUrlHelper> urlHelper,
            ForumSettings forumSettings)
        {
            _db = db;
            _services = services;
            _genericAttributeService = genericAttributeService;
            _urlHelper = urlHelper;
            _forumSettings = forumSettings;
        }

        public static string ForumPostCountKey => "ForumPostCount";
        public static string NotifiedAboutNewPrivateMessagesKey => "NotifiedAboutNewPrivateMessages";
        public static string SignatureKey => "Signature";

        #region Hook

        protected override Task<HookResult> OnDeletingAsync(Forum entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _deletingForumIds.Add(entity.Id);

            return Task.FromResult(HookResult.Ok);
        }            

        protected override Task<HookResult> OnDeletedAsync(Forum entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_deletingForumIds.Any())
            {
                // Delete forum subscriptions.
                await DeleteForumSubscriptionsAsync(_deletingForumIds.ToArray(), cancelToken);

                // Keep related customerIDs (could be a lot of IDs loaded here).
                _postCountCustomerIds.AddRange(await _db.ForumPosts()
                    .Where(x => _deletingForumIds.Contains(x.ForumTopic.ForumId))
                    .Select(x => x.CustomerId)
                    .Distinct()
                    .ToListAsync(cancelToken));
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update post counts of customers.
            await UpdateForumPostCountsAsync(_postCountCustomerIds.ToArray(), cancelToken);
            _postCountCustomerIds.Clear();
        }

        #endregion

        public virtual async Task<int> DeleteForumSubscriptionsAsync(int[] forumIds, CancellationToken cancelToken = default)
        {
            var num = 0;

            if (forumIds?.Any() ?? false)
            {
                var topicIds = await _db.ForumTopics()
                    .Where(x => forumIds.Contains(x.ForumId))
                    .Select(x => x.Id)
                    .ToArrayAsync(cancelToken);

                // Delete topic subscriptions.
                num += await DeleteForumTopicSubscriptionsAsync(topicIds, cancelToken);

                // Delete forum subscriptions.
                num += await _db.ForumSubscriptions()
                    .Where(x => forumIds.Contains(x.ForumId))
                    .BatchDeleteAsync(cancelToken);
            }

            return num;
        }

        public virtual async Task<int> DeleteForumTopicSubscriptionsAsync(int[] forumTopicIds, CancellationToken cancelToken = default)
        {
            var num = 0;

            if (forumTopicIds?.Any() ?? false)
            {
                num = await _db.ForumSubscriptions()
                    .Where(x => forumTopicIds.Contains(x.TopicId))
                    .BatchDeleteAsync(cancelToken);
            }

            return num;
        }

        public virtual async Task<int> UpdateForumPostCountsAsync(int[] customerIds, CancellationToken cancelToken = default)
        {
            var num = 0;

            if (customerIds?.Any() ?? false)
            {
                var entityName = nameof(Customer);

                foreach (var chunk in customerIds.Slice(500))
                {
                    var customerIdsChunk = chunk.ToArray();
                    var numPostsByCustomer = await _db.ForumPosts().CountByCustomerIdsAsync(customerIdsChunk, cancelToken);

                    if (numPostsByCustomer.Any())
                    {
                        await _genericAttributeService.PrefetchAttributesAsync(entityName, customerIdsChunk);

                        foreach (var pair in numPostsByCustomer)
                        {
                            var attributes = _genericAttributeService.GetAttributesForEntity(entityName, pair.Key);

                            // This should actually be stored per store and retrieved as well if the related forum group
                            // is limited to a store. For the sake of simplicity, we'll leave it that way.
                            attributes.Set(ForumPostCountKey, pair.Value);
                        }

                        num += await _db.SaveChangesAsync(cancelToken);
                    }
                }
            }

            return num;
        }

        public virtual async Task<int> UpdateForumStatisticsAsync(int[] forumIds, CancellationToken cancelToken = default)
        {
            var num = 0;

            if (forumIds?.Any() ?? false)
            {
                var topicSet = _db.ForumTopics();
                var postSet = _db.ForumPosts();
                var forums = await _db.Forums().GetManyAsync(forumIds, true);
                var numTopicsByForum = await topicSet.CountByForumIdsAsync(forumIds, cancelToken);
                var numPostsByForum = await postSet.CountByForumIdsAsync(forumIds, cancelToken);

                foreach (var forum in forums)
                {
                    var lastValuesQuery =
                        from ft in topicSet
                        join fp in postSet on ft.Id equals fp.TopicId
                        where ft.ForumId == forum.Id && ft.Published && fp.Published
                        orderby fp.CreatedOnUtc descending, ft.CreatedOnUtc descending
                        select new
                        {
                            LastTopicId = ft.Id,
                            LastPostId = fp.Id,
                            LastPostCustomerId = fp.CustomerId,
                            LastPostTime = fp.CreatedOnUtc
                        };

                    var lastValues = await lastValuesQuery.FirstOrDefaultAsync(cancelToken);

                    forum.LastTopicId = lastValues?.LastTopicId ?? 0;
                    forum.LastPostId = lastValues?.LastPostId ?? 0;
                    forum.LastPostCustomerId = lastValues?.LastPostCustomerId ?? 0;
                    forum.LastPostTime = lastValues?.LastPostTime;

                    forum.NumTopics = numTopicsByForum.TryGetValue(forum.Id, out var numTopics)
                        ? numTopics
                        : 0;

                    forum.NumPosts = numPostsByForum.TryGetValue(forum.Id, out var numPosts)
                        ? numPosts
                        : 0;
                }

                num = await _db.SaveChangesAsync(cancelToken);
            }

            return num;
        }

        public virtual async Task<int> UpdateForumTopicStatisticsAsync(int[] forumTopicIds, CancellationToken cancelToken = default)
        {
            var num = 0;

            if (forumTopicIds?.Any() ?? false)
            {
                var postSet = _db.ForumPosts();
                var lastPosts = await postSet
                    .ApplyLastPostFilter(forumTopicIds)
                    .ToListAsync(cancelToken);

                if (lastPosts.Any())
                {
                    var numPostsByTopic = await postSet.CountByForumTopicIdsAsync(forumTopicIds, cancelToken);

                    foreach (var lastPost in lastPosts)
                    {
                        lastPost.ForumTopic.LastPostId = lastPost.Id;
                        lastPost.ForumTopic.LastPostCustomerId = lastPost.CustomerId;
                        lastPost.ForumTopic.LastPostTime = lastPost.CreatedOnUtc;

                        lastPost.ForumTopic.NumPosts = numPostsByTopic.TryGetValue(lastPost.TopicId, out var numPosts)
                            ? numPosts
                            : 0;
                    }

                    num = await _db.SaveChangesAsync(cancelToken);
                }
            }

            return num;
        }


        public virtual string BuildSlug(ForumTopic topic)
        {
            if (topic != null)
            {
                const int maxLength = 100;

                var slug = SeoHelper.BuildSlug(topic.Subject);

                // Trim SE name to avoid URLs that are too long.
                if (slug.Length > maxLength)
                {
                    slug = slug.Substring(0, maxLength);
                }

                return slug;
            }

            return string.Empty;
        }

        public virtual string StripSubject(ForumTopic topic)
        {
            var subject = topic?.Subject;
            if (subject.IsEmpty())
            {
                return subject;
            }

            var maxLength = _forumSettings.StrippedTopicMaxLength;
            if (maxLength > 0 && subject.Length > maxLength)
            {
                var index = subject.IndexOf(" ", maxLength);
                if (index > 0)
                {
                    subject = subject.Substring(0, index);
                    subject += "…";
                }
            }

            return subject;
        }

        public virtual string FormatPostText(ForumPost post)
        {
            var text = post?.Text;
            if (text.IsEmpty())
            {
                return string.Empty;
            }

            text = HtmlUtility.ConvertPlainTextToHtml(text.HtmlEncode());

            if (_forumSettings.ForumEditor == EditorType.BBCodeEditor)
            {
                text = BBCodeHelper.ToHtml(text);
            }

            return text;
        }

        public virtual string FormatPrivateMessage(PrivateMessage message)
        {
            var text = message?.Text;
            if (text.IsEmpty())
            {
                return string.Empty;
            }

            text = HtmlUtility.ConvertPlainTextToHtml(text.HtmlEncode());

            return BBCodeHelper.ToHtml(text);
        }

        public virtual ForumModerationPermissionFlags GetModerationPermissions(ForumTopic topic = null, ForumPost post = null, Customer customer = null)
        {
            customer ??= _services.WorkContext.CurrentCustomer;

            var permits = ForumModerationPermissionFlags.None;

            if (customer.IsForumModerator())
            {
                permits = ForumModerationPermissionFlags.All;
            }
            else
            {
                var isGuest = customer.IsGuest();

                if (!(isGuest && !_forumSettings.AllowGuestsToCreateTopics))
                    permits |= ForumModerationPermissionFlags.CanCreateTopics;

                if (!(isGuest && !_forumSettings.AllowGuestsToCreatePosts))
                    permits |= ForumModerationPermissionFlags.CanCreatePosts;

                if (_forumSettings.AllowPrivateMessages && !isGuest)
                    permits |= ForumModerationPermissionFlags.CanCreatePrivateMessages;

                if (topic != null && topic.Published && topic.CustomerId == customer.Id)
                {
                    if (_forumSettings.AllowCustomersToEditPosts)
                        permits |= ForumModerationPermissionFlags.CanEditTopic;

                    if (_forumSettings.AllowCustomersToDeletePosts)
                        permits |= ForumModerationPermissionFlags.CanDeleteTopic;
                }

                if (post != null && post.Published && post.CustomerId == customer.Id)
                {
                    if (_forumSettings.AllowCustomersToEditPosts)
                        permits |= ForumModerationPermissionFlags.CanEditPost;

                    if (_forumSettings.AllowCustomersToDeletePosts)
                        permits |= ForumModerationPermissionFlags.CanDeletePost;
                }

                // ...
            }

            return permits;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesForum || !context.LoadSettings<ForumSettings>().ForumsEnabled)
            {
                return null;
            }

            return new ForumXmlSitemapResult(context, _db, _services.WorkContext, this, _urlHelper.Value);
        }

        class ForumXmlSitemapResult : XmlSitemapProvider
        {
            private readonly XmlSitemapBuildContext _context;
            private readonly SmartDbContext _db;
            private readonly IWorkContext _workContext;
            private readonly IForumService _forumService;
            private readonly IUrlHelper _urlHelper;

            private List<ForumGroup> _groups;
            private List<Forum> _forums;
            private readonly IQueryable<ForumTopic> _topicsQuery;

            public ForumXmlSitemapResult(
                XmlSitemapBuildContext context,
                SmartDbContext db,
                IWorkContext workContext,
                IForumService forumService,
                IUrlHelper urlHelper)
            {
                _context = context;
                _db = db;
                _workContext = workContext;
                _forumService = forumService;
                _urlHelper = urlHelper;

                _topicsQuery = _db.ForumTopics()
                    .AsNoTracking()
                    .ApplyStandardFilter(_workContext.CurrentCustomer);
            }

            public override int Order => int.MinValue + 101;

            public override async Task<int> GetTotalCountAsync()
            {
                await EnsureDataLoaded();

                // INFO: we gonna create nodes for all groups, forums within groups and all topics.
                return _groups.Count + _forums.Count + await _topicsQuery.CountAsync(_context.CancellationToken);
            }

            public override XmlSitemapNode CreateNode(LinkGenerator linkGenerator, string baseUrl, NamedEntity entity, UrlRecordCollection slugs, Language language)
            {
                string path = null;

                switch (entity.EntityName)
                {
                    case nameof(ForumGroup):
                        path = _urlHelper.RouteUrl("ForumGroupBySlug", new { id = entity.Id, slug = slugs.GetSlug(language.Id, entity.Id, true) });
                        break;

                    case nameof(Forum):
                        path = _urlHelper.RouteUrl("ForumBySlug", new { id = entity.Id, slug = slugs.GetSlug(language.Id, entity.Id, true) });
                        break;

                    case nameof(ForumTopic):
                        path = _urlHelper.RouteUrl("ForumTopicBySlug", new { id = entity.Id, slug = entity.Slug });
                        break;
                }

                if (path.HasValue())
                {
                    return new XmlSitemapNode
                    {
                        LastMod = entity.LastMod,
                        Loc = baseUrl + path.TrimStart('/')
                    };
                }

                return null;
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                await EnsureDataLoaded(cancelToken);

                // Enlist forum groups.
                foreach (var group in _groups)
                {
                    yield return new NamedEntity { EntityName = nameof(ForumGroup), Id = group.Id, LastMod = group.UpdatedOnUtc };
                }

                // Enlist forums.
                foreach (var forum in _forums)
                {
                    yield return new NamedEntity { EntityName = nameof(Forum), Id = forum.Id, LastMod = forum.UpdatedOnUtc };
                }

                // Enlist topics.
                var topicsPager = new FastPager<ForumTopic>(_topicsQuery, _context.MaximumNodeCount);

                while ((await topicsPager.ReadNextPageAsync<ForumTopic>(cancelToken)).Out(out var topics))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    foreach (var topic in topics)
                    {
                        yield return new NamedEntity
                        {
                            Id = topic.Id,
                            EntityName = nameof(ForumTopic),
                            Slug = _forumService.BuildSlug(new ForumTopic { Subject = topic.Subject }),
                            LastMod = topic.UpdatedOnUtc
                        };
                    }
                }
            }

            private async Task EnsureDataLoaded(CancellationToken cancelToken = default)
            {
                if (_groups == null)
                {
                    _groups = await _db.ForumGroups()
                        .Include(x => x.Forums)
                        .AsNoTracking()
                        .ApplyStoreFilter(_context.RequestStoreId)
                        .ApplyAclFilter(_workContext.CurrentCustomer)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync(cancelToken);

                    _forums = _groups.SelectMany(x => x.Forums).ToList();
                }
            }
        }
    }
}
