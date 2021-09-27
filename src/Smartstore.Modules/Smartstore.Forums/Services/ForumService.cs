using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
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
        private readonly ForumSettings _forumSettings;

        public ForumService(
            SmartDbContext db,
            ICommonServices services,
            ForumSettings forumSettings)
        {
            _db = db;
            _services = services;
            _forumSettings = forumSettings;
        }

        protected override Task<HookResult> OnDeletingAsync(Forum entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletingForumIds = entries
                .Where(x => x.State == Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<Forum>()
                .Select(x => x.Id)
                .ToArray();

            await DeleteSubscriptionsByForumIdsAsync(deletingForumIds, cancelToken);
        }

        public virtual async Task<int> DeleteSubscriptionsByForumIdsAsync(int[] forumIds, CancellationToken cancelToken = default)
        {
            if (!forumIds.Any())
            {
                return 0;
            }

            var numDeleted = 0;
            var topicIds = await _db.ForumTopics()
                .AsNoTracking()
                .Where(x => forumIds.Contains(x.ForumId))
                .Select(x => x.Id)
                .ToListAsync(cancelToken);

            // Delete topic subscriptions.
            if (topicIds.Any())
            {
                numDeleted += await _db.ForumSubscriptions()
                    .Where(x => topicIds.Contains(x.TopicId))
                    .BatchDeleteAsync(cancelToken);
            }

            // Delete forum subscriptions.
            numDeleted += await _db.ForumSubscriptions()
                .Where(x => forumIds.Contains(x.ForumId))
                .BatchDeleteAsync(cancelToken);

            return numDeleted;
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

            text = HtmlUtils.ConvertPlainTextToHtml(text.HtmlEncode());

            if (_forumSettings.ForumEditor == EditorType.BBCodeEditor)
            {
                text = BBCodeHelper.ToHtml(text);
            }

            return text;
        }

        public virtual ForumModerationPermits GetModerationPermits(ForumTopic topic = null, ForumPost post = null, Customer customer = null)
        {
            customer ??= _services.WorkContext.CurrentCustomer;

            var permits = ForumModerationPermits.None;

            if (customer.IsForumModerator())
            {
                permits = ForumModerationPermits.All;
            }
            else
            {
                var isGuest = customer.IsGuest();

                if (!(isGuest && !_forumSettings.AllowGuestsToCreateTopics))
                    permits |= ForumModerationPermits.CanCreateTopics;

                if (!(isGuest && !_forumSettings.AllowGuestsToCreatePosts))
                    permits |= ForumModerationPermits.CanCreatePosts;

                if (_forumSettings.AllowPrivateMessages && !isGuest)
                    permits |= ForumModerationPermits.CanCreatePrivateMessages;

                if (topic != null && topic.Published && topic.CustomerId == customer.Id)
                {
                    if (_forumSettings.AllowCustomersToEditPosts)
                        permits |= ForumModerationPermits.CanEditTopic;

                    if (_forumSettings.AllowCustomersToDeletePosts)
                        permits |= ForumModerationPermits.CanDeleteTopic;
                }

                if (post != null && post.Published && post.CustomerId == customer.Id)
                {
                    if (_forumSettings.AllowCustomersToEditPosts)
                        permits |= ForumModerationPermits.CanEditPost;

                    if (_forumSettings.AllowCustomersToDeletePosts)
                        permits |= ForumModerationPermits.CanDeletePost;
                }

                // ...
            }

            return permits;
        }

        // TODO: (mg) (core) Hook and hook and hook.
        //public virtual async Task ApplyForumStatisticsAsync(Forum forum)
        //{
        //    if (forum == null)
        //    {
        //        return;
        //    }

        //    var query = 
        //        from ft in _db.ForumTopics()
        //        join fp in _db.ForumPosts() on ft.Id equals fp.TopicId
        //        where ft.ForumId == forum.Id && ft.Published && fp.Published
        //        orderby fp.CreatedOnUtc descending, ft.CreatedOnUtc descending
        //        select new
        //        {
        //            LastTopicId = ft.Id,
        //            LastPostId = fp.Id,
        //            LastPostCustomerId = fp.CustomerId,
        //            LastPostTime = fp.CreatedOnUtc
        //        };

        //    var lastValues = await query.FirstOrDefaultAsync();

        //    forum.LastTopicId = lastValues?.LastTopicId ?? 0;
        //    forum.LastPostId = lastValues?.LastPostId ?? 0;
        //    forum.LastPostCustomerId = lastValues?.LastPostCustomerId ?? 0;
        //    forum.LastPostTime = lastValues?.LastPostTime;

        //    forum.NumTopics = await _db.ForumTopics().CountAsync(x => x.ForumId == forum.Id && x.Published);

        //    var numPostsQuery =
        //        from ft in _db.ForumTopics()
        //        join fp in _db.ForumPosts() on ft.Id equals fp.TopicId
        //        where ft.ForumId == forum.Id && ft.Published && fp.Published
        //        select fp.Id;

        //    forum.NumPosts = await numPostsQuery.CountAsync();
        //}

        //public virtual async Task ApplyTopicStatisticsAsync(ForumTopic topic)
        //{
        //    if (topic == null)
        //    {
        //        return;
        //    }

        //    var lastValues = await _db.ForumPosts()
        //        .Where(x => x.TopicId == topic.Id && x.Published)
        //        .OrderByDescending(x => x.CreatedOnUtc)
        //        .Select(x => new
        //        {
        //            LastPostId = x.Id,
        //            LastPostCustomerId = x.CustomerId,
        //            LastPostTime = x.CreatedOnUtc
        //        })
        //        .FirstOrDefaultAsync();

        //    topic.LastPostId = lastValues?.LastPostId ?? 0;
        //    topic.LastPostCustomerId = lastValues?.LastPostCustomerId ?? 0;
        //    topic.LastPostTime = lastValues?.LastPostTime;

        //    topic.NumPosts = topic.Published
        //        ? await _db.ForumPosts().CountAsync(x => x.TopicId == topic.Id && x.Published)
        //        : 0;
        //}

        //public virtual async Task ApplyCustomerStatistics(Customer customer)
        //{
        //    if (customer != null)
        //    {
        //        var numPosts = await _db.ForumPosts().CountAsync(x => x.CustomerId == customer.Id && x.ForumTopic.Published && x.Published);

        //        customer.GenericAttributes.Set("ForumPostCount", numPosts);
        //    }
        //}

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesForum || !context.LoadSettings<ForumSettings>().ForumsEnabled)
            {
                return null;
            }

            return null;
        }

        class ForumXmlSitemapResult : XmlSitemapProvider
        {
            private readonly XmlSitemapBuildContext _context;
            private readonly SmartDbContext _db;
            private readonly IWorkContext _workContext;
            private readonly IForumService _forumService;

            private List<ForumGroup> _groups;
            private List<Forum> _forums;
            private readonly IQueryable<ForumTopic> _topicsQuery;

            public ForumXmlSitemapResult(
                XmlSitemapBuildContext context,
                SmartDbContext db,
                IWorkContext workContext,
                IForumService forumService)
            {
                _context = context;
                _db = db;
                _workContext = workContext;
                _forumService = forumService;

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

                // TODO: (mg) (core) add localized routes to ForumXmlSitemapResult.
                switch (entity.EntityName)
                {
                    case nameof(ForumGroup):
                        break;

                    case nameof(Forum):
                        break;

                    case nameof(ForumTopic):
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
