using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.News.Domain;
using Smartstore.News.Models.Public;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Media;

namespace Smartstore.News.Models.Mappers
{
    public class NewsItemMapper : Mapper<NewsItem, PublicNewsItemModel>
    {
        private readonly ICommonServices _services;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;

        public NewsItemMapper(ICommonServices services, IDateTimeHelper dateTimeHelper, CustomerSettings customerSettings, CaptchaSettings captchaSettings)
        {
            _services = services;
            _dateTimeHelper = dateTimeHelper;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(NewsItem from, PublicNewsItemModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(NewsItem from, PublicNewsItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var prepareComments = Convert.ToBoolean(parameters?.PrepareComments as bool?);

            _services.DisplayControl.Announce(from);

            MiniMapper.Map(from, to);

            to.Title = from.GetLocalized(x => x.Title);
            to.Short = from.GetLocalized(x => x.Short);
            to.Full = from.GetLocalized(x => x.Full, true);
            to.MetaTitle = from.GetLocalized(x => x.MetaTitle);
            to.MetaDescription = from.GetLocalized(x => x.MetaDescription);
            to.MetaKeywords = from.GetLocalized(x => x.MetaKeywords);
            to.SeName = await from.GetActiveSlugAsync(ensureTwoPublishedLanguages: false);
            to.CreatedOn = _dateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
            to.CreatedOnUTC = from.CreatedOnUtc;
            to.AddNewComment.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnNewsCommentPage;
            to.DisplayAdminLink = _services.Permissions.Authorize(Permissions.System.AccessBackend, _services.WorkContext.CurrentCustomer);

            var mapper = MapperFactory.GetMapper<NewsItem, ImageModel>();
            to.PictureModel = await mapper.MapAsync(from, new { FileId = from.MediaFileId });
            to.PreviewPictureModel = await mapper.MapAsync(from, new { FileId = from.PreviewMediaFileId });
            to.Comments.AllowComments = from.AllowComments;
            to.Comments.NumberOfComments = from.ApprovedCommentCount;
            to.Comments.AllowCustomersToUploadAvatars = _customerSettings.AllowCustomersToUploadAvatars;

            if (prepareComments)
            {
                var newsComments = from.NewsComments.Where(n => n.IsApproved).OrderBy(pr => pr.CreatedOnUtc);
                foreach (var nc in newsComments)
                {
                    var isGuest = nc.Customer.IsGuest();

                    var commentModel = new CommentModel(to.Comments)
                    {
                        Id = nc.Id,
                        CustomerId = nc.CustomerId,
                        CustomerName = nc.Customer.FormatUserName(_customerSettings, T, false),
                        CommentTitle = nc.CommentTitle,
                        CommentText = nc.CommentText,
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc),
                        CreatedOnPretty = _services.DateTimeHelper.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc).Humanize(false),
                        AllowViewingProfiles = _customerSettings.AllowViewingProfiles && !isGuest,
                    };

                    commentModel.Avatar = nc.Customer.ToAvatarModel(null, false);

                    to.Comments.Comments.Add(commentModel);
                }
            }
        }
    }
}
