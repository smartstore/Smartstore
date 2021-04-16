using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Models.Profiles
{
    public partial class ProfileIndexModel : EntityModelBase
    {
        public string ProfileTitle { get; set; }

        public ProfileInfoModel ProfileInfo { get; set; } = new();

        // TODO: (mh) (core) Implement in forum module.
        //public int PostsPage { get; set; }
        //public bool PagingPosts { get; set; }
        //public bool ForumsEnabled { get; set; }
    }

    public partial class ProfileInfoModel : EntityModelBase
    {
        public CustomerAvatarModel Avatar { get; set; }

        public bool LocationEnabled { get; set; }
        public string Location { get; set; }

        // TODO: (mh) (core) Implement in forum module.
        //public bool PMEnabled { get; set; }

        //public bool TotalPostsEnabled { get; set; }
        //public int TotalPosts { get; set; }

        public bool JoinDateEnabled { get; set; }
        public string JoinDate { get; set; }

        public bool DateOfBirthEnabled { get; set; }
        public string DateOfBirth { get; set; }
    }
}
