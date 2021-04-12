using System.Linq;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Engine;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Customers
{
    public static class CustomerMappingExtensions
    {
        /// <summary>
        /// TODO: (mh) (core) Find proper place & describe
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="userName"></param>
        /// <param name="large"></param>
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

        public string CurrentFileName { get; set; } // TODO: (mh) (core) What's this for?

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
