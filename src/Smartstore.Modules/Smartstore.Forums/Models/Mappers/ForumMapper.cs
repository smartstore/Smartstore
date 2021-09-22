using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;

namespace Smartstore.Forums.Models.Mappers
{
    public static partial class ForumMappingExtensions
    {
        public static async Task<PublicForumModel> MapAsync(this Forum entity, Dictionary<int, ForumPost> lastPosts)
        {
            var model = new PublicForumModel();
            await entity.MapAsync(model, lastPosts);

            return model;
        }

        public static async Task MapAsync(this Forum entity,
            PublicForumModel model,
            Dictionary<int, ForumPost> lastPosts)
        {
            Guard.NotNull(lastPosts, nameof(lastPosts));

            dynamic parameters = new ExpandoObject();
            parameters.LastPosts = lastPosts;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class ForumMapper : Mapper<Forum, PublicForumModel>
    {
        protected override void Map(Forum from, PublicForumModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(Forum from, PublicForumModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var lastPosts = parameters.LastPosts as Dictionary<int, ForumPost>;

            MiniMapper.Map(from, to);

            to.Name = from.GetLocalized(x => x.Name);
            to.Slug = await from.GetActiveSlugAsync();
            to.Description = from.GetLocalized(x => x.Description);

            if (from.LastPostId != 0 && lastPosts.TryGetValue(from.LastPostId, out var lastPost) && lastPost != null)
            {
                to.LastPost = await lastPost.MapAsync();
            }
        }
    }
}
