using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
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
        [JsonIgnore]
        public string GridId { get; set; }

        [JsonIgnore]
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

    public class GridCommandModelBinderProvider : IModelBinderProvider
    {
        private readonly ComplexObjectModelBinderProvider _workerProvider;

        public GridCommandModelBinderProvider(ComplexObjectModelBinderProvider workerProvider)
        {
            _workerProvider = workerProvider;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            Guard.NotNull(context, nameof(context));

            if (context.Metadata.ModelType == typeof(GridCommand))
            {
                return new GridCommandModelBinder(_workerProvider.GetBinder(context));
            }

            return null;
        }

        /// <summary>
        /// Post-processor for underlying ComplexObjectModelBinder
        /// </summary>
        class GridCommandModelBinder : IModelBinder
        {
            private readonly IModelBinder _workerBinder;

            public GridCommandModelBinder(IModelBinder workerBinder)
            {
                _workerBinder = workerBinder;
            }

            public async Task BindModelAsync(ModelBindingContext bindingContext)
            {
                await _workerBinder.BindModelAsync(bindingContext);
                if (!bindingContext.Result.IsModelSet)
                {
                    return;
                }

                // Preserve command state
                var command = bindingContext.Result.Model as GridCommand;
                if (!command.InitialRequest)
                {
                    var stateStore = bindingContext.HttpContext.RequestServices.GetService<IGridCommandStateStore>();
                    if (stateStore != null)
                    {
                        await stateStore.SaveStateAsync(command);
                    }
                }
            }
        }
    }
}
