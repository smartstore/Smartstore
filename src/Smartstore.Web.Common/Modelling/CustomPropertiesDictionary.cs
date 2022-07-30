using System.Security;
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

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(await BindCore(bindingContext));
        }

        private async Task<CustomPropertiesDictionary> BindCore(ModelBindingContext bindingContext)
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

                var subPropertyName = GetSubPropertyName(key);
                if (subPropertyName.EqualsNoCase("__Type__"))
                    continue;

                ModelMetadata valueMetadata;
                ModelBindingContext valueBindingContext;

                if (subPropertyName == null)
                {
                    valueMetadata = _modelMetadataProvider.GetMetadataForType(GetValueType(keys, key, bindingContext.ValueProvider));
                    valueBindingContext = DefaultModelBindingContext.CreateBindingContext(
                        bindingContext.ActionContext,
                        bindingContext.ValueProvider,
                        valueMetadata,
                        bindingInfo: null,
                        modelName: key);
                }
                else
                {
                    // Is Complex type
                    var modelName = key[..(key.Length - subPropertyName.Length - 1)];
                    var valueType = GetValueType(keys, modelName, bindingContext.ValueProvider);
                    if (!valueType.HasAttribute<CustomModelPartAttribute>(false))
                    {
                        throw new SecurityException("For security reasons complex types in '{0}' must be decorated with the '{1}' attribute.".FormatInvariant(
                            typeof(CustomPropertiesDictionary).AssemblyQualifiedNameWithoutVersion(),
                            typeof(CustomModelPartAttribute).AssemblyQualifiedNameWithoutVersion()));
                    }

                    valueMetadata = _modelMetadataProvider.GetMetadataForType(valueType);
                    valueBindingContext = DefaultModelBindingContext.CreateBindingContext(
                        bindingContext.ActionContext,
                        bindingContext.ValueProvider,
                        valueMetadata,
                        bindingInfo: null,
                        modelName: key.Substring(0, key.Length - subPropertyName.Length - 1));
                }

                if (valueBindingContext != null)
                {
                    valueBindingContext.PropertyFilter = bindingContext.PropertyFilter;

                    var factoryContext = new ModelBinderFactoryContext { Metadata = valueMetadata };
                    var valueBinder = _modelBinderFactory.CreateBinder(factoryContext);

                    if (valueBinder != null)
                    {
                        await valueBinder.BindModelAsync(valueBindingContext);
                        if (valueBindingContext.Result.IsModelSet)
                        {
                            model[keyName] = valueBindingContext.Result.Model;
                        }
                    }
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

            return typeof(string);
        }
    }
}
