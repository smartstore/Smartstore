using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smartstore.Web.Modelling
{
    [ModelBinder(typeof(CustomPropertiesDictionaryModelBinder))]
    public sealed class CustomPropertiesDictionary : Dictionary<string, object>
    {
    }

    public class CustomPropertiesDictionaryModelBinder : IModelBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IModelBinderFactory _modelBinderFactory;

        public CustomPropertiesDictionaryModelBinder(IModelMetadataProvider modelMetadataProvider, IModelBinderFactory modelBinderFactory)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(BindCore(bindingContext));
            return Task.CompletedTask;
        }

        private CustomPropertiesDictionary BindCore(ModelBindingContext bindingContext)
        {
            var model = bindingContext.Model as CustomPropertiesDictionary ?? new CustomPropertiesDictionary();

            var keys = GetValueProviderKeys(bindingContext.ActionContext, bindingContext.ModelName + "[");
            if (keys.Count == 0)
            {
                return model;
            }

            foreach (var key in keys)
            {
                var keyName = GetKeyName(key);
                if (keyName == null || model.ContainsKey(keyName))
                    continue;

                //var modelMetadata = bindingContext.ModelMetadata.get(type);
                //var valueBinder = this.Binders.DefaultBinder;

                var subPropertyName = GetSubPropertyName(key);
                if (subPropertyName.EqualsNoCase("__Type__"))
                    continue;

                if (subPropertyName == null)
                {
                    var metadata = _modelMetadataProvider.GetMetadataForType(GetValueType(keys, key, bindingContext.ValueProvider));
                    var simpleBindingContext = DefaultModelBindingContext.CreateBindingContext(
                        bindingContext.ActionContext,
                        bindingContext.ValueProvider,
                        metadata,
                        bindingInfo: null,
                        modelName: key);

                    var factoryContext = new ModelBinderFactoryContext { Metadata = metadata };
                    var valueBinder = _modelBinderFactory.CreateBinder(factoryContext);

                    //
                }
                else
                {
                    // Is Complex type
                    var modelName = key.Substring(0, key.Length - subPropertyName.Length - 1);
                    var valueType = GetValueType(keys, modelName, bindingContext.ValueProvider);
                    if (!valueType.HasAttribute<CustomModelPartAttribute>(false))
                    {
                        throw new SecurityException("For security reasons complex types in '{0}' must be decorated with the '{1}' attribute.".FormatInvariant(
                            typeof(CustomPropertiesDictionary).AssemblyQualifiedNameWithoutVersion(),
                            typeof(CustomModelPartAttribute).AssemblyQualifiedNameWithoutVersion()));
                    }

                    var metadata = _modelMetadataProvider.GetMetadataForType(valueType);
                    var complexBindingContext = DefaultModelBindingContext.CreateBindingContext(
                        bindingContext.ActionContext, 
                        bindingContext.ValueProvider,
                        metadata,
                        bindingInfo: null,
                        modelName: key.Substring(0, key.Length - subPropertyName.Length - 1));

                    var factoryContext = new ModelBinderFactoryContext { Metadata = metadata };
                    var valueBinder = _modelBinderFactory.CreateBinder(factoryContext);

                    //
                }
            }

            return model;
        }

        private static HashSet<string> GetValueProviderKeys(ActionContext context, string prefix)
        {
            var keys = context.RouteData.Values.Keys
                .Concat(context.HttpContext.Request.Query.Keys);

            if (context.HttpContext.Request.HasFormContentType)
            {
                keys = context.HttpContext.Request.Form.Keys.Concat(keys);
            }

            keys = keys.Where(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));

            return new HashSet<string>(keys, StringComparer.InvariantCultureIgnoreCase);
        }

        private static string GetKeyName(string key)
        {
            int startBracket = key.IndexOf('[');
            int endBracket = key.IndexOf(']', startBracket);

            if (endBracket == -1)
                return null;

            return key.Substring(startBracket + 1, endBracket - startBracket - 1);
        }

        private static string GetSubPropertyName(string key)
        {
            var parts = key.Split('.');
            if (parts.Length > 1)
            {
                return parts[1];
            }

            return null;
        }

        private static Type GetValueType(HashSet<string> keys, string prefix, IValueProvider valueProvider)
        {
            var typeKey = prefix + ".__Type__";
            if (keys.Contains(typeKey))
            {
                var type = Type.GetType(valueProvider.GetValue(typeKey).FirstValue, true);
                return type;
            }

            return typeof(object);
        }
    }
}
