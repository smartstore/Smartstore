using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.DataGrid;

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

    [JsonPropertyName("page"), DefaultValue(1)]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize"), DefaultValue(25)]
    public int PageSize { get; set; } = 25;

    [JsonPropertyName("sorting"), DefaultValue("[]")]
    public List<SortDescriptor> Sorting { get; set; } = [];

    [JsonPropertyName("filters"), DefaultValue("[]")]
    public List<object> Filters { get; set; } = [];

    [JsonPropertyName("groups"), DefaultValue("[]")]
    public List<object> Groups { get; set; } = [];

    [JsonPropertyName("aggregates"), DefaultValue("[]")]
    public List<object> Aggregates { get; set; } = [];
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