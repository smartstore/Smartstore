#nullable enable

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Widgets
{
    public class ComponentWidgetInvoker : WidgetInvoker<ComponentWidget>
    {
        private static readonly ConcurrentDictionary<Type, ViewComponentDescriptor> _componentByTypeCache = new();

        private readonly IViewComponentSelector _componentSelector;
        private readonly IViewComponentDescriptorCollectionProvider _componentDescriptorProvider;
        private readonly IModuleCatalog _moduleCatalog;

        public ComponentWidgetInvoker(
            IViewComponentSelector componentSelector,
            IViewComponentDescriptorCollectionProvider componentDescriptorProvider,
            IModuleCatalog moduleCatalog)
        {
            _componentSelector = componentSelector;
            _componentDescriptorProvider = componentDescriptorProvider;
            _moduleCatalog = moduleCatalog;
        }

        public override Task<IHtmlContent> InvokeAsync(WidgetContext context, ComponentWidget widget)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(widget, nameof(widget));

            if (widget.ComponentType == null && widget.ComponentName == null)
            {
                throw new InvalidOperationException("View component name or type must be set.");
            }

            if (widget.ComponentType != null && widget.Module == null)
            {
                // Check component type location
                var moduleDescriptor = _moduleCatalog.GetModuleByAssembly(widget.ComponentType.Assembly);
                widget.Module = moduleDescriptor?.SystemName;
            }

            widget.Arguments = NormalizeComponentArguments(context, widget);

            var writer = context.Writer;
            if (writer == null)
            {
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                writer = new StringWriter(sb);
            }

            var viewContext = CreateViewContext(context, writer, null, widget.Module);

            // IViewComponentHelper is stateful, we want to make sure to retrieve it every time we need it.
            var viewComponentHelper = context.HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
            (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);
            var result = GetViewComponentResult(widget, viewComponentHelper);
            
            return result;
        }

        private static Task<IHtmlContent> GetViewComponentResult(ComponentWidget widget, IViewComponentHelper viewComponentHelper)
        {
            if (widget.ComponentType == null)
            {
                return viewComponentHelper.InvokeAsync(widget.ComponentName, widget.Arguments);
            }
            else
            {
                return viewComponentHelper.InvokeAsync(widget.ComponentType, widget.Arguments);
            }
        }

        private object? NormalizeComponentArguments(WidgetContext context, ComponentWidget widget)
        {
            if (widget.Arguments is JObject jobj)
            {
                // ConvertUtility.ObjectToDictionary can handle JObject
                widget.Arguments = ConvertUtility.ObjectToDictionary(widget.Arguments, null);
            }

            if (widget.Arguments is IDictionary<string, object?> currentArguments)
            {
                // Check whether input args dictionary has correct types,
                // since JsonConverter is not always able to deserialize as required
                // (e.g. Int64 instead of Int32).
                FixArgumentDictionary(currentArguments, currentArguments);
                return currentArguments;
            }

            // non-null and non-dictionary arguments should return as is.
            if (widget.Arguments != null)
            {
                return widget.Arguments;
            }

            var model = context.Model ?? context.ViewData?.Model;
            if (model == null)
            {
                return null;
            }

            ViewComponentDescriptor descriptor = SelectComponent(widget);
            if (descriptor.Parameters.Count == 0)
            {
                return null;
            }

            if (descriptor.Parameters.Count == 1 && descriptor.Parameters[0].ParameterType.IsAssignableFrom(model.GetType()))
            {
                return model;
            }

            currentArguments = ConvertUtility.ObjectToDictionary(model, null);
            if (currentArguments.Count == 0)
            {
                return null;
            }

            // We gonna select arguments from current args list only if they exist in the 
            // component descriptor's parameter list and the types match.
            var fixedArguments = new Dictionary<string, object?>(currentArguments.Count, StringComparer.OrdinalIgnoreCase);
            FixArgumentDictionary(currentArguments, fixedArguments, descriptor);

            return fixedArguments;

            void FixArgumentDictionary(
                IDictionary<string, object?> currentArgs, 
                IDictionary<string, object?> fixedArgs, 
                ViewComponentDescriptor? descriptor = null)
            {
                descriptor ??= SelectComponent(widget);

                foreach (var para in descriptor.Parameters)
                {
                    if (para.Name is null)
                    {
                        continue;
                    }

                    if (currentArgs.TryGetValue(para.Name, out var value) && value != null)
                    {
                        if (para.ParameterType.IsAssignableFrom(value.GetType()))
                        {
                            fixedArgs[para.Name] = value;
                        }
                        else if (ConvertUtility.TryConvert(value, para.ParameterType, out var convertedValue))
                        {
                            fixedArgs[para.Name] = convertedValue!;
                        }
                    }
                }
            }
        }

        private ViewComponentDescriptor SelectComponent(ComponentWidget widget)
        {
            ViewComponentDescriptor descriptor;

            if (widget.ComponentType != null)
            {
                // Select component by type
                descriptor = _componentByTypeCache.GetOrAdd(widget.ComponentType, componentType =>
                {
                    var descriptors = _componentDescriptorProvider.ViewComponents;
                    for (var i = 0; i < descriptors.Items.Count; i++)
                    {
                        var descriptor = descriptors.Items[i];
                        if (descriptor.TypeInfo == componentType?.GetTypeInfo())
                        {
                            return descriptor;
                        }
                    }

                    return default!;
                });

                if (descriptor == null)
                {
                    throw new InvalidOperationException($"A view component named '{widget.ComponentType.FullName}' could not be found");
                }
            }
            else
            {
                // Select component by name
                descriptor = _componentSelector.SelectComponent(widget.ComponentName);

                if (descriptor == null)
                {
                    throw new InvalidOperationException($"A view component named '{widget.ComponentName}' could not be found");
                }
            }

            return descriptor;
        }
    }
}
