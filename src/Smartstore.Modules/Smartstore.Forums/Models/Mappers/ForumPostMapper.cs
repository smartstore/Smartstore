using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Forums.Models.Mappers
{
    public static partial class ForumPostMappingExtensions
    {
        public static async Task<PublicForumPostModel> MapAsync(this ForumPost entity,
            bool stripLongUserNames = true,
            int page = 1)
        {
            var model = new PublicForumPostModel();
            await entity.MapAsync(model, stripLongUserNames, page);

            return model;
        }

        public static async Task MapAsync(this ForumPost entity, 
            PublicForumPostModel model,
            bool stripLongUserNames = true,
            int page = 1)
        {
            dynamic parameters = new ExpandoObject();
            parameters.StripLongUserNames = stripLongUserNames;
            parameters.Page = page;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class ForumPostMapper : Mapper<ForumPost, PublicForumPostModel>
    {
        private readonly IForumService _forumService;
        private readonly ICommonServices _services;
        private readonly ForumSettings _forumSettings;
        private readonly CustomerSettings _customerSettings;

        public ForumPostMapper(
            IForumService forumService,
            ICommonServices services,
            ForumSettings forumSettings,
            CustomerSettings customerSettings)
        {
            _forumService = forumService;
            _services = services;
            _forumSettings = forumSettings;
            _customerSettings = customerSettings;
        }

        protected override void Map(ForumPost from, PublicForumPostModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ForumPost from, PublicForumPostModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from.Customer, nameof(from.Customer));
            Guard.NotNull(from.ForumTopic, nameof(from.ForumTopic));

            var stripLongUserNames = (bool)parameters.StripLongUserNames;
            var page = (int)parameters.Page;
            var createdOn = _services.DateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
            var currentCustomer = _services.WorkContext.CurrentCustomer;

            MiniMapper.Map(from, to);

            to.ForumTopicId = from.TopicId;
            to.ForumTopicSlug = _forumService.BuildSlug(from.ForumTopic);
            to.ForumTopicSubject = _forumService.StripSubject(from.ForumTopic);
            to.FormattedText = _forumService.FormatPostText(from);
            to.CustomerName = from.Customer.FormatUserName(stripLongUserNames);
            to.IsGuest = from.Customer.IsGuest();
            to.IsForumModerator = from.Customer.IsForumModerator();
            to.ModerationPermissions = _forumService.GetModerationPermissions(from.ForumTopic, from, currentCustomer);
            to.ShowCustomersPostCount = _forumSettings.ShowCustomersPostCount;
            to.ForumPostCount = from.Customer.GenericAttributes.Get<int>("ForumPostCount");
            to.ShowCustomersJoinDate = _customerSettings.ShowCustomersJoinDate;
            to.CustomerJoinDate = from.Customer.CreatedOnUtc;
            to.SignaturesEnabled = _forumSettings.SignaturesEnabled;
            to.FormattedSignature = HtmlUtility.ConvertPlainTextToHtml(from.Customer.GenericAttributes.Signature.HtmlEncode());
            to.HasCustomerProfile = _customerSettings.AllowViewingProfiles && !to.IsGuest;
            to.ShowCustomersLocation = _customerSettings.ShowCustomersLocation;
            to.CurrentTopicPage = page;

            to.PostCreatedOnStr = _forumSettings.RelativeDateTimeFormattingEnabled
                ? createdOn.Humanize(false)
                : createdOn.ToString("f");

            to.Avatar = from.Customer.ToAvatarModel(to.CustomerName, true);

            if (_forumSettings.AllowCustomersToVoteOnPosts && from.CustomerId != currentCustomer.Id)
            {
                if (!_forumSettings.AllowGuestsToVoteOnPosts && currentCustomer.IsGuest())
                {
                    to.CanVote = false;
                }
                else
                {
                    await _services.DbContext.LoadCollectionAsync(from, x => x.ForumPostVotes);

                    to.CanVote = true;
                    to.Vote = from.ForumPostVotes.FirstOrDefault(x => x.CustomerId == currentCustomer.Id)?.Vote ?? false;
                    to.VoteCount = from.ForumPostVotes.Count;
                }
            }

            if (_customerSettings.ShowCustomersLocation)
            {
                var country = await _services.DbContext.Countries.FindByIdAsync(from.Customer.GenericAttributes.CountryId ?? 0, false);
                to.CustomerLocation = country?.GetLocalized(x => x.Name) ?? string.Empty;
            }
        }
    }
}
