using System.Linq;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Engine;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Customers
{
    public static class CustomerMappingExtensions
    {
        // TODO: (mh) (core) Find proper place => Maybe a CustomerMapper with a method MapToAvatar?
        /// <summary>
        ///  Maps a <see cref="Customer"/> to a <see cref="CustomerAvatarModel"/>.
        /// </summary>
        /// <param name="customer">The <see cref="Customer"/> entity which should be mapped to <see cref="CustomerAvatarModel"/></param>
        /// <param name="userName">The username which should be displayed for this avatar.</param>
        /// <param name="large">Specifies the size of the displayed avatar.</param>
        public static CustomerAvatarModel ToAvatarModel(this Customer customer, string userName = null, bool large = false)
        {
            Guard.NotNull(customer, nameof(customer));

            var engine = EngineContext.Current.Scope;
            var customerSettings = engine.Resolve<CustomerSettings>();
            var mediaSettings = engine.Resolve<MediaSettings>();

            var model = new CustomerAvatarModel
            {
                Id = customer.Id,
                Large = large,
                UserName = userName,
                AllowViewingProfiles = customerSettings.AllowViewingProfiles,
                AvatarPictureSize = mediaSettings.AvatarPictureSize
            };

            if (customer.IsGuest())
            {
                model.AvatarLetter = 'G';
                model.AvatarColor = "light";
            }
            else
            {
                if (customer.FirstName.HasValue())
                {
                    model.AvatarLetter = customer.FirstName.First();
                }
                else if (customer.LastName.HasValue())
                {
                    model.AvatarLetter = customer.LastName.First();
                }
                else if (customer.FullName.HasValue())
                {
                    model.AvatarLetter = customer.FullName.First();
                }
                else if (customer.Username.HasValue())
                {
                    model.AvatarLetter = customer.Username.First();
                }
                else if (userName.HasValue())
                {
                    model.AvatarLetter = userName.First();
                }
                else
                {
                    model.AvatarLetter = '?';
                }

                if (customerSettings.AllowCustomersToUploadAvatars)
                {
                    model.FileId = customer.GenericAttributes.AvatarPictureId;
                }

                if (!model.FileId.HasValue)
                {
                    model.AvatarColor = customer.GenericAttributes.AvatarColor;
                }
            }

            return model;
        }
    }

    public partial class CustomerAvatarModel : EntityModelBase
    {
        public bool Large { get; set; }
        public int? FileId { get; set; }
        public string AvatarColor { get; set; }
        public char AvatarLetter { get; set; }
        public string UserName { get; set; }
        public bool AllowViewingProfiles { get; set; }
        public int AvatarPictureSize { get; set; }
    }

    public partial class CustomerAvatarEditModel : ModelBase
    {
        public string MaxFileSize { get; set; }
        public CustomerAvatarModel Avatar { get; set; }
    }
}
