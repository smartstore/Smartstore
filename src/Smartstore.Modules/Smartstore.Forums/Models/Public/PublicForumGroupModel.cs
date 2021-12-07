namespace Smartstore.Forums.Models.Public
{
    public partial class PublicForumGroupListModel
    {
        public DateTime CurrentTime { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }

        public List<PublicForumGroupModel> ForumGroups { get; set; }
    }

    public partial class PublicForumGroupModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public string Slug { get; set; }

        public List<PublicForumModel> Forums { get; set; }
    }
}
