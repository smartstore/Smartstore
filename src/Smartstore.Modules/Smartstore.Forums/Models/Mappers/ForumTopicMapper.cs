using System.Dynamic;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Forums.Models.Mappers
{
    public static partial class ForumTopicMappingExtensions
    {
        public static async Task<IPagedList<PublicForumTopicModel>> MapAsync(this IPagedList<ForumTopic> entities, SmartDbContext db)
        {
            Guard.NotNull(entities, nameof(entities));

            dynamic parameters = new ExpandoObject();
            parameters.FirstPost = null;
            parameters.LastPosts = await db.ForumPosts().GetLastForumPostsAsync(entities.Select(x => x.LastPostId));

            var mapper = MapperFactory.GetMapper<ForumTopic, PublicForumTopicModel>();
            var models = await entities
                .SelectAsync(async x =>
                {
                    var model = new PublicForumTopicModel();
                    await mapper.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return new PagedList<PublicForumTopicModel>(models, entities.PageIndex, entities.PageSize, entities.TotalCount);
        }

        public static async Task<PublicForumTopicModel> MapAsync(this ForumTopic entity,
            Dictionary<int, ForumPost> lastPosts,
            ForumPost firstPost)
        {
            var model = new PublicForumTopicModel();
            await entity.MapAsync(model, lastPosts, firstPost);

            return model;
        }

        public static async Task MapAsync(this ForumTopic entity, 
            PublicForumTopicModel model, 
            Dictionary<int, ForumPost> lastPosts,
            ForumPost firstPost)
        {
            Guard.NotNull(lastPosts, nameof(lastPosts));

            dynamic parameters = new ExpandoObject();
            parameters.LastPosts = lastPosts;
            parameters.FirstPost = firstPost;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class ForumTopicMapper : Mapper<ForumTopic, PublicForumTopicModel>
    {
        private readonly IForumService _forumService;
        private readonly ForumSettings _forumSettings;
        private readonly CustomerSettings _customerSettings;

        public ForumTopicMapper(
            IForumService forumService,
            ForumSettings forumSettings,
            CustomerSettings customerSettings)
        {
            _forumService = forumService;
            _forumSettings = forumSettings;
            _customerSettings = customerSettings;
        }

        protected override void Map(ForumTopic from, PublicForumTopicModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ForumTopic from, PublicForumTopicModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from.Customer, nameof(from.Customer));

            var lastPosts = parameters.LastPosts as Dictionary<int, ForumPost>;
            var firstPost = parameters?.FirstPost as ForumPost;

            MiniMapper.Map(from, to);

            to.Slug = _forumService.BuildSlug(from);
            to.FirstPostId = firstPost?.Id ?? from.FirstPostId;
            to.HasCustomerProfile = _customerSettings.AllowViewingProfiles && !from.Customer.IsGuest();
            to.CustomerName = from.Customer.FormatUserName(true);
            to.Avatar = await from.Customer.MapAsync(to.CustomerName);
            to.PostsPages = new Pageable<ForumPost>(0, _forumSettings.PostsPageSize, from.NumPosts);

            if (from.LastPostId != 0 && lastPosts.TryGetValue(from.LastPostId, out var lastPost) && lastPost != null)
            {
                to.LastPost = await lastPost.MapAsync();
            }
        }
    }
}
