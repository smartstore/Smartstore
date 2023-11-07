using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Models.Customers
{
    public static class CustomerAvatarMappingExtensions
    {
        /// <summary>
        /// Creates a new <see cref="CustomerAvatarModel"/> and maps an <see cref="Customer"/> entity to it.
        /// </summary>
        /// <param name="entity"><see cref="Customer"/> entity.</param>
        /// <param name="userName">The user name which should be displayed for this avatar.</param>
        /// <param name="largeAvatar">A value indicating whether to display a large avatar.</param>
        /// <returns><see cref="CustomerAvatarModel"/>.</returns>
        public static async Task<CustomerAvatarModel> MapAsync(this Customer entity,
            string userName = null,
            bool largeAvatar = false,
            bool displayRing = false)
        {
            var model = new CustomerAvatarModel();
            await MapAsync(entity, model, userName, largeAvatar, displayRing);

            return model;
        }

        /// <summary>
        ///  Maps a <see cref="Customer"/> entity to a <see cref="CustomerAvatarModel"/>.
        /// </summary>
        /// <param name="entity"><see cref="Customer"/> entity.</param>
        /// <param name="model"><see cref="CustomerAvatarModel"/>.</param>
        /// <param name="userName">The user name which should be displayed for this avatar.</param>
        /// <param name="largeAvatar">A value indicating whether to display a large avatar.</param>
        public static async Task MapAsync(this Customer entity,
            CustomerAvatarModel model,
            string userName = null,
            bool largeAvatar = false,
            bool displayRing = false)
        {
            dynamic parameters = new ExpandoObject();
            parameters.UserName = userName;
            parameters.LargeAvatar = largeAvatar;
            parameters.DisplayRing = displayRing;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class CustomerAvatarMapper : Mapper<Customer, CustomerAvatarModel>
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;
        private readonly MediaSettings _mediaSettings;

        public CustomerAvatarMapper(SmartDbContext db, CustomerSettings customerSettings, MediaSettings mediaSettings)
        {
            _db = db;
            _customerSettings = customerSettings;
            _mediaSettings = mediaSettings;
        }

        protected override void Map(Customer from, CustomerAvatarModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(Customer from, CustomerAvatarModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            await _db.LoadCollectionAsync(from, x => x.CustomerRoleMappings, false, x => x.Include(y => y.CustomerRole));

            to.Id = from.Id;
            to.Large = (bool)(parameters.LargeAvatar == true);
            to.DisplayRing = (bool)(parameters.DisplayRing == true);
            to.UserName = parameters.UserName as string;
            to.AvatarPictureSize = _mediaSettings.AvatarPictureSize;

            if (from.IsGuest())
            {
                to.AvatarLetter = 'G';
                to.AvatarColor = "light";
            }
            else
            {
                to.AllowViewingProfiles = _customerSettings.AllowViewingProfiles;

                var userName = "?";

                if (from.FirstName.HasValue())
                {
                    userName = from.FirstName;
                }
                else if (from.LastName.HasValue())
                {
                    userName = from.LastName;
                }
                else if (from.FullName.HasValue())
                {
                    userName = from.FullName;
                }
                else if (from.Username.HasValue())
                {
                    userName = from.Username;
                }
                else if (to.UserName.HasValue())
                {
                    userName = to.UserName;
                }

                to.AvatarLetter = userName.ToUpper().TrimStart()[0];

                if (_customerSettings.AllowCustomersToUploadAvatars)
                {
                    to.FileId = from.GenericAttributes.AvatarPictureId;
                }

                if (!to.FileId.HasValue)
                {
                    to.AvatarColor = from.GenericAttributes.AvatarColor;
                }
            }
        }
    }
}
