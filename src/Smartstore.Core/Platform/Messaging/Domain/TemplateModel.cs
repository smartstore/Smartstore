using Smartstore.ComponentModel;

namespace Smartstore.Core.Messaging
{
    public class TemplateModel : HybridExpando
    {
        public T GetFromBag<T>(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            if (TryGetValue("Bag", out var obj) && obj is IDictionary<string, object> bag)
            {
                if (bag.TryGetValueAs<T>(key, out var result))
                {
                    return result;
                }
            }

            return default;
        }

        /// <summary>
        /// Tries to resolve an object by splitting the given <paramref name="expression"/>
        /// by dot ('.') and traversing the model deeply.
        /// </summary>
        /// <param name="expression">The key expression</param>
        /// <returns>The found result object or <c>null</c></returns>
        public object Evaluate(string expression)
        {
            Guard.NotEmpty(expression, nameof(expression));

            if (!expression.Contains('.'))
            {
                return this.Get(expression);
            }

            object currentValue = this;
            var keys = expression.Split('.');

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                if (currentValue is null)
                {
                    break;
                }
                else if (currentValue is IDictionary<string, object> dict)
                {
                    currentValue = dict.Get(key);
                }
                else
                {
                    currentValue = currentValue.GetType().GetProperty(key)?.GetValue(currentValue);
                }
            }

            return currentValue;
        }
    }
}
