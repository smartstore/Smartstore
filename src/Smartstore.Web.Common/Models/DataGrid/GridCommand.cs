using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.DataGrid
{
    public class SortDescriptor
    {
        [JsonPropertyName("member")]
        public string Member { get; set; }

        [JsonPropertyName("descending")]
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
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 25;

        [JsonPropertyName("sorting")]
        public List<SortDescriptor> Sorting { get; } = new();

        [JsonPropertyName("filters")]
        public List<object> Filters { get; } = new();

        [JsonPropertyName("groups")]
        public List<object> Groups { get; } = new();

        [JsonPropertyName("aggregates")]
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
