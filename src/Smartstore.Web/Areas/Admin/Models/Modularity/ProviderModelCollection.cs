namespace Smartstore.Admin.Models.Modularity
{
    public class ProviderModelCollection
    {
        public IEnumerable<ProviderModel> Data { get; set; }

        public List<Func<dynamic, object>> ExtraColumns { get; set; } = new();
    }

    public class ProviderModelCollection<TModel> : ProviderModelCollection
        where TModel : ProviderModel
    {
        public void SetData(IEnumerable<TModel> data)
        {
            Data = data;
        }

        public void SetColumns(IEnumerable<Func<TModel, object>> columns)
        {
            Guard.NotNull(columns, nameof(columns));

            foreach (var col in columns)
            {
                object fn(dynamic x) => col(x);
                ExtraColumns.Add(fn);
            }
        }
    }
}
