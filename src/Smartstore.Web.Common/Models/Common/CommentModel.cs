using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Models.Common
{
    public partial class CommentListModel : ModelBase
    {
        public bool AllowComments { get; set; }
        public int NumberOfComments { get; set; }
        public List<CommentModel> Comments { get; set; } = new();
        public bool AllowCustomersToUploadAvatars { get; set; }
    }

    public partial class CommentModel : EntityModelBase
    {
        private readonly WeakReference<CommentListModel> _parent;

        public CommentModel(CommentListModel parent)
        {
            Guard.NotNull(parent, nameof(parent));

            _parent = new WeakReference<CommentListModel>(parent);
        }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CommentTitle { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedOnPretty { get; set; }
        public bool AllowViewingProfiles { get; set; }
        public CustomerAvatarModel Avatar { get; set; } = new();

        public CommentListModel Parent
        {
            get
            {
                _parent.TryGetTarget(out var parent);
                return parent;
            }
        }
    }
}
