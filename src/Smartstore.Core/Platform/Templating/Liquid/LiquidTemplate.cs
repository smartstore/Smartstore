using DotLiquid;
using Smartstore.ComponentModel;
using Smartstore.Utilities;

namespace Smartstore.Templating.Liquid
{
    internal class LiquidTemplate : ITemplate
    {
        public LiquidTemplate(Template template, string source)
        {
            Template = Guard.NotNull(template, nameof(template));
            Source = Guard.NotNull(source, nameof(source));
        }

        public string Source { get; internal set; }

        public Template Template { get; internal set; }

        public Task<string> RenderAsync(object model, IFormatProvider formatProvider)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(formatProvider, nameof(formatProvider));

            var p = CreateParameters(model, formatProvider);
            return Task.FromResult(Template.Render(p));
        }

        private static RenderParameters CreateParameters(object data, IFormatProvider formatProvider)
        {
            var p = new RenderParameters(formatProvider);

            Hash hash = null;

            if (data is ISafeObject so)
            {
                if (so.GetWrappedObject() is IDictionary<string, object> soDict)
                {
                    hash = Hash.FromDictionary(soDict);
                }
                else
                {
                    data = so.GetWrappedObject();
                }
            }

            if (hash == null)
            {
                hash = new Hash();

                if (data is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        hash[kvp.Key] = LiquidUtility.CreateSafeObject(kvp.Value);
                    }
                }
                else
                {
                    var props = FastProperty.GetProperties(data.GetType());
                    foreach (var prop in props)
                    {
                        hash[prop.Key] = LiquidUtility.CreateSafeObject(prop.Value.GetValue(data));
                    }
                }
            }

            p.LocalVariables = hash;
            p.ErrorsOutputMode = CommonHelper.IsHosted ? ErrorsOutputMode.Display : ErrorsOutputMode.Rethrow;

            return p;
        }
    }
}
