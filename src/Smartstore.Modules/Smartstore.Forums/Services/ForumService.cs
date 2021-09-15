using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Services
{
    public partial class ForumService : AsyncDbSaveHook<Forum>, IForumService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;

        public ForumService(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(Forum entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Delete topic and forum subscriptions.
            var deletedForumIds = entries
                .Where(x => x.State == Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<Forum>()
                .Select(x => x.Id)
                .ToList();

            // TODO: (mg) (core) Are you sure that Forum.Id is still > 0 after physical deletion?
            if (deletedForumIds.Any())
            {
                var topicIds = await _db.ForumTopics()
                    .AsNoTracking()
                    .Where(x => deletedForumIds.Contains(x.ForumId))
                    .Select(x => x.Id)
                    .ToListAsync(cancelToken);

                if (topicIds.Any())
                {
                    await _db.ForumSubscriptions()
                        .Where(x => topicIds.Contains(x.TopicId))
                        .BatchDeleteAsync(cancelToken);
                }

                await _db.ForumSubscriptions()
                    .Where(x => deletedForumIds.Contains(x.ForumId))
                    .BatchDeleteAsync(cancelToken);
            }
        }

        public virtual string BuildSlug(ForumTopic forumTopic)
        {
            // TODO: (mg) (core) If this is the only method in IForumService: make this an extension method and remove ForumService.
            const int maxLength = 100;

            var slug = SeoHelper.BuildSlug(forumTopic.Subject);

            // Trim SE name to avoid URLs that are too long.
            if (slug.Length > maxLength)
            {
                slug = slug.Substring(0, maxLength);
            }

            return slug;
        }

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
