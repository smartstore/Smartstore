using Smartstore.Collections;

namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPagingFilteringModel : PagedListBase
    {
        public virtual DateTime? GetParsedMonth()
        {
            DateTime? result = null;
            if (this.Month.HasValue())
            {
                string[] tempDate = this.Month.Split(new char[] { '-' });
                if (tempDate.Length == 2)
                {
                    result = new DateTime(Convert.ToInt32(tempDate[0]), Convert.ToInt32(tempDate[1]), 1);
                }
            }
            return result;
        }

        public virtual DateTime? GetFromMonth()
        {
            var filterByMonth = GetParsedMonth();
            if (filterByMonth.HasValue)
                return filterByMonth.Value;
            return null;
        }

        public virtual DateTime? GetToMonth()
        {
            var filterByMonth = GetParsedMonth();
            if (filterByMonth.HasValue)
                return filterByMonth.Value.AddMonths(1).AddSeconds(-1);
            return null;
        }

        public string Month { get; set; }

        public string Tag { get; set; }
    }
}
