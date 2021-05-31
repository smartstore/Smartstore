using System;

namespace Smartstore.Admin.Models.Messages
{
    public class SearchEmailsQuery
    {
        public string From { get; set; }
        public string To { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool UnsentOnly { get; set; }
        public int MaxSendTries { get; set; }
        public bool OrderByLatest { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public bool? SendManually { get; set; }

        /// <summary>
        /// Navigation properties to eager load (comma separataed)
        /// </summary>
        public string Expand { get; set; }
    }
}