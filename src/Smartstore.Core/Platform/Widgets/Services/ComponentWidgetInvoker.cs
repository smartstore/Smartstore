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
        private static readonly ConcurrentDictionary<Type, ViewComponentDescriptor?> _componentByTypeCache = new();
        private static readonly ConcurrentDictionary<string, Type?> _componentTypeByNameCache = new();

        private readonly IViewComponentSelector _componentSelector;
        private readonly IViewComponentDescriptorCollectionProvider _componentDescriptorProvider;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ITypeScanner _typeScanner;

        public ComponentWidgetInvoker(
            IViewComponentSelector componentSelector,
            IViewComponentDescriptorCollectionProvider componentDescriptorProvider,
            IModuleCatalog moduleCatalog,
            ITypeScanner typeScanner)
        {
            _componentSelector = componentSelector;
            _componentDescriptorProvider = componentDescriptorProvider;
            _moduleCatalog = moduleCatalog;
            _typeScanner = typeScanner;
        }

        public override Task<IHtmlContent> InvokeAsync(WidgetContext context, ComponentWidget widget)
        {
            Guard.NotNull(context);
            Guard.NotNull(widget);

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

            var arguments = GetComponentArguments(context, widget);

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
            var result = GetViewComponentResult(widget, arguments, viewComponentHelper);
            
            return result;
        }

        private static Task<IHtmlContent> GetViewComponentResult(ComponentWidget widget, object? arguments, IViewComponentHelper viewComponentHelper)
        {
            if (widget.ComponentType == null)
            {
                return viewComponentHelper.InvokeAsync(widget.ComponentName, arguments);
            }
            else
            {
                return viewComponentHelper.InvokeAsync(widget.ComponentType, arguments);
            }
        }

        private object? GetComponentArguments(WidgetContext context, ComponentWidget widget)
        {
            var arguments = widget.Arguments;
            
            if (arguments is JObject jobj)
            {
                // ConvertUtility.ObjectToDictionary can handle JObject
                arguments = ConvertUtility.ObjectToDictionary(arguments, null);
            }

            if (arguments is IDictionary<string, object?> currentArguments)
            {
                // Check whether input args dictionary has correct types,
                // since JsonConverter is not always able to deserialize as required
                // (e.g. Int64 instead of Int32).
                FixArgumentDictionary(currentArguments, currentArguments);
                return currentArguments;
            }

            if (arguments != null)
            {
                return arguments;
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
            ViewComponentDescriptor? descriptor;

            var componentType = GetComponentType(widget);

            if (componentType != null)
            {
                // Select component by type
                descriptor = _componentByTypeCache.GetOrAdd(componentType, type =>
                {
                    var descriptors = _componentDescriptorProvider.ViewComponents;
                    for (var i = 0; i < descriptors.Items.Count; i++)
                    {
                        var descriptor = descriptors.Items[i];
                        if (descriptor.TypeInfo == type?.GetTypeInfo())
                        {
                            return descriptor;
                        }
                    }

                    return null;
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

        /// <summary>
        /// Tries to find the component type by name in a module assembly.
        /// </summary>
        private Type? GetComponentType(ComponentWidget widget)
        {
            if (widget.ComponentType == null && widget.Module.HasValue())
            {
                var key = $"{widget.Module}/{widget.ComponentName}";
                var componentType = _componentTypeByNameCache.GetOrAdd(key, _ =>
                {
                    var assembly = _moduleCatalog.GetModuleByName(widget.Module)?.Module?.Assembly;
                    if (assembly != null)
                    {
                        var componentName = widget.ComponentName.EnsureEndsWith("ViewComponent");
                        var matchingType = _typeScanner.FindTypes<ViewComponent>([assembly]).FirstOrDefault(t => t.Name == componentName);

                        return matchingType;
                    }

                    return null;
                });

                if (componentType != null)
                {
                    return componentType;
                }
            }
            
            return widget.ComponentType;
        }
    }
}
