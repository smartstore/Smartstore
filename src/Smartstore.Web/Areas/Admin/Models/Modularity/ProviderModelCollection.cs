#nullable enable

namespace Smartstore.Admin.Models.Modularity
{
    public class ProviderModelCollection
    {
        public IEnumerable<ProviderModel> Data { get; set; } = default!;

        public Func<dynamic, object>? ButtonTemplate { get; set; }
        public Func<dynamic, object>? InfoTemplate { get; set; }
    }

    public class ProviderModelCollection<TModel> : ProviderModelCollection
        where TModel : ProviderModel
    {
        public ProviderModelCollection(IEnumerable<TModel> data)
        {
            Data = Guard.NotNull(data);
        }

        public void SetTemplates(Func<TModel, object>? buttonTemplate, Func<TModel, object>? infoTemplate)
        {
            if (buttonTemplate != null)
            {
                ButtonTemplate = Cast(buttonTemplate);
            }

            if (infoTemplate != null)
            {
                InfoTemplate = Cast(infoTemplate);
            }

            Func<dynamic, object> Cast(Func<TModel, object> template)
            {
                object fn(dynamic x) => template(x);
                return fn;
            }
        }
    }
}
