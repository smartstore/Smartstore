using Newtonsoft.Json;

namespace Smartstore.Web.Models.DataGrid
{
    public class GridSelection
    {
        [JsonProperty("selectedKeys")]
        public string[] SelectedKeys { get; set; }

        public IEnumerable<int> GetEntityIds()
        {
            return SelectedKeys
                .Select(x => x.ToInt())
                .Where(x => x > 0)
                .Distinct();
        }
    }
}
