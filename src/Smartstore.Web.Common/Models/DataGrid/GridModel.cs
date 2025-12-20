using System.Collections;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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
            Rows = Guard.NotNull(rows);
        }

        [JsonPropertyName("rows")]
        public IEnumerable<T> Rows { get; set; }

        [IgnoreDataMember]
        IEnumerable IGridModel.Rows
            => this.Rows;

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("aggregates")]
        public object Aggregates { get; set; }
    }
}
