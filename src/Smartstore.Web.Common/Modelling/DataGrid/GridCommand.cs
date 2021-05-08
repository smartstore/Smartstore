using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Smartstore.Web.Modelling.DataGrid
{
    public class SortDescriptor
    {
        [JsonProperty("member")]
        public string Member { get; set; }

        [JsonProperty("descending")]
        public bool Descending { get; set; }
    }
    
    public class GridCommand
    {
        [JsonProperty("page")]
        public int Page { get; set; } = 1;

        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 25;

        [JsonProperty("sorting")]
        public List<SortDescriptor> Sorting { get; } = new();

        [JsonProperty("filters")]
        public List<object> Filters { get; } = new();

        [JsonProperty("groups")]
        public List<object> Groups { get; } = new();

        [JsonProperty("aggregates")]
        public List<object> Aggregates { get; } = new();
    }
}
