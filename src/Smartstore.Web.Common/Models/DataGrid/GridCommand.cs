using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.DataGrid
{
    public class SortDescriptor
    {
        [JsonProperty("member")]
        public string Member { get; set; }

        [JsonProperty("descending")]
        public bool Descending { get; set; }
    }

    [ModelBinder(BinderType = typeof(GridCommandModelBinder))]
    public class GridCommand
    {
        [IgnoreDataMember]
        public string GridId { get; set; }

        [IgnoreDataMember]
        public bool InitialRequest { get; set; }

        /// <summary>
        /// Required for state preservation.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

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

    /// <summary>
    /// Post-processor for GridCommand model binder.
    /// </summary>
    public class GridCommandModelBinder : SmartModelBinder<GridCommand>
    {
        protected override async Task OnModelBoundAsync(ModelBindingContext bindingContext, GridCommand model)
        {
            if (model == null)
            {
                return;
            }

            // Preserve command state
            if (!model.InitialRequest)
            {
                var stateStore = bindingContext.HttpContext.RequestServices.GetService<IGridCommandStateStore>();
                if (stateStore != null)
                {
                    await stateStore.SaveStateAsync(model);
                }
            }
        }
    }
}
