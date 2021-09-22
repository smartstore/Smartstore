using System;
using System.Threading.Tasks;
using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;

namespace Smartstore.Forums.Models.Mappers
{
    public static partial class ForumPostMappingExtensions
    {
        public static async Task<PublicForumPostModel> MapAsync(this ForumPost entity)
        {
            var model = new PublicForumPostModel();
            await entity.MapAsync(model);

            return model;
        }

        public static async Task MapAsync(this ForumPost entity, PublicForumPostModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    internal class ForumPostMapper : Mapper<ForumPost, PublicForumPostModel>
    {
        private readonly IForumService _forumService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ForumSettings _forumSettings;
        private readonly CustomerSettings _customerSettings;

        public ForumPostMapper(
            IForumService forumService,
            IDateTimeHelper dateTimeHelper,
            ForumSettings forumSettings,
            CustomerSettings customerSettings)
        {
            _forumService = forumService;
            _dateTimeHelper = dateTimeHelper;
            _forumSettings = forumSettings;
            _customerSettings = customerSettings;
        }

        protected override void Map(ForumPost from, PublicForumPostModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override Task MapAsync(ForumPost from, PublicForumPostModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from.Customer, nameof(from.Customer));
            Guard.NotNull(from.ForumTopic, nameof(from.ForumTopic));

            var createdOn = _dateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
            var isGuest = from.Customer.IsGuest();

            MiniMapper.Map(from, to);

            to.ForumTopicId = from.TopicId;
            to.ForumTopicSlug = _forumService.BuildSlug(from.ForumTopic);
            to.ForumTopicSubject = _forumService.StripSubject(from.ForumTopic);
            to.CustomerName = from.Customer.FormatUserName(true);
            to.IsCustomerGuest = isGuest;
            to.HasCustomerProfile = _customerSettings.AllowViewingProfiles && !isGuest;
            to.PostCreatedOnStr = _forumSettings.RelativeDateTimeFormattingEnabled
                ? createdOn.Humanize(false)
                : createdOn.ToString("f");

            return Task.CompletedTask;
        }
    }
}
