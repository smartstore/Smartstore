using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;

namespace Smartstore.Forums.Models.Mappers
{
    public static partial class ForumGroupMappingExtensions
    {
        public static async Task<PublicForumGroupModel> MapAsync(this ForumGroup entity)
        {
            var model = new PublicForumGroupModel();
            await entity.MapAsync(model);

            return model;
        }

        public static async Task MapAsync(this ForumGroup entity, PublicForumGroupModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    internal class ForumGroupMapper : Mapper<ForumGroup, PublicForumGroupModel>
    {
        private readonly SmartDbContext _db;

        public ForumGroupMapper(SmartDbContext db)
        {
            _db = db;
        }

        protected override void Map(ForumGroup from, PublicForumGroupModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ForumGroup from, PublicForumGroupModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from.Forums, nameof(from.Forums));

            var lastPostIds = from.Forums
                .Where(x => x.LastPostId != 0)
                .Select(x => x.LastPostId)
                .Distinct()
                .ToArray();

            var lastPosts = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .Include(x => x.Customer)
                .AsNoTracking()
                .Where(x => lastPostIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            to.Id = from.Id;
            to.Name = from.GetLocalized(x => x.Name);
            to.Description = from.GetLocalized(x => x.Description);
            to.Slug = from.GetActiveSlug();

            to.Forums = await from.Forums
                .OrderBy(x => x.DisplayOrder)
                .SelectAsync(async x => await x.MapAsync(lastPosts))
                .AsyncToList();
        }
    }
}
