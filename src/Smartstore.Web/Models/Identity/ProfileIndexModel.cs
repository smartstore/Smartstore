using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Models.Identity
{
    public partial class ProfileIndexModel : TabbableModel
    {
        public string CustomerName { get; set; }

        public ProfileInfoModel ProfileInfo { get; set; }
    }

    public partial class ProfileInfoModel : EntityModelBase
    {
        public CustomerAvatarModel Avatar { get; set; }

        public bool LocationEnabled { get; set; }
        public string Location { get; set; }

        public bool JoinDateEnabled { get; set; }
        public string JoinDate { get; set; }

        public bool DateOfBirthEnabled { get; set; }
        public string DateOfBirth { get; set; }
    }
}
