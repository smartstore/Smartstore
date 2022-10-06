using System.Collections;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Smartstore.Web.Models.DataGrid
{
    public interface IGridModel
    {
        IEnumerable Rows { get; }
        int Total { get; }
        object Aggregates { get; }
    }

    public class GridModel<T> : IGridModel
    {
        public GridModel()
        {
        }

        public GridModel(IEnumerable<T> rows)
        {
            Rows = Guard.NotNull(rows, nameof(rows));
        }

        [JsonProperty("rows")]
        public IEnumerable<T> Rows { get; set; }

        [IgnoreDataMember]
        IEnumerable IGridModel.Rows
            => this.Rows;

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("aggregates")]
        public object Aggregates { get; set; }
    }
}
