using Smartstore.ComponentModel;

namespace Smartstore.Core.Messaging
{
    public class TemplateModel : HybridExpando
    {
        public T GetFromBag<T>(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            if (base.Contains("Bag") && base["Bag"] is IDictionary<string, object> bag)
            {
                if (bag.TryGetValue(key, out var value) && value is T result)
                {
                    return result;
                }
            }

            return default;
        }
    }
}
