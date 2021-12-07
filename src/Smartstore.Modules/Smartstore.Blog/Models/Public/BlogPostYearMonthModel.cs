namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPostYearModel : ModelBase
    {
        public int Year { get; set; }
        public List<BlogPostMonthModel> Months { get; set; } = new();
    }

    public partial class BlogPostMonthModel : ModelBase
    {
        public int Month { get; set; }

        public int BlogPostCount { get; set; }
    }
}
