using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Smartstore.Web.Modelling
{
    public interface ISmartModelBinder : IModelBinder
    {
    }

    public abstract class SmartModelBinder<T> : ISmartModelBinder where T : class
    {
        // Model contains only properties that are expected to bind from value providers and no value provider has
        // matching data.
        internal const int NoDataAvailable = 0;

        // If model contains properties that are expected to bind from value providers, no value provider has matching
        // data. Remaining (greedy) properties might bind successfully.
        internal const int GreedyPropertiesMayHaveData = 1;

        // Model contains at least one property that is expected to bind from value providers and a value provider has
        // matching data.
        internal const int ValueProviderDataAvailable = 2;

        private readonly Dictionary<ModelMetadata, IModelBinder> _propertyBinders = new();
        private Func<T> _modelCreator;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        protected IModelMetadataProvider MetadataProvider { get; private set; }
        protected IModelBinderFactory BinderFactory { get; private set; }

        public virtual Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            var propertyData = CanCreateModel(bindingContext);
            if (propertyData == NoDataAvailable)
            {
                return Task.CompletedTask;
            }

            var services = bindingContext.HttpContext.RequestServices;

            if (MetadataProvider == null)
            {
                MetadataProvider = services.GetRequiredService<IModelMetadataProvider>();
            }

            if (BinderFactory == null)
            {
                BinderFactory = services.GetRequiredService<IModelBinderFactory>();
            }

            return BindModelCoreAsync(bindingContext, propertyData);
        }

        protected virtual IModelBinder GetPropertyBinder(ModelMetadata propertyMetadata)
        {
            Guard.NotNull(propertyMetadata, nameof(propertyMetadata));

            if (!_propertyBinders.TryGetValue(propertyMetadata, out var binder))
            {
                binder = BinderFactory.CreateBinder(new ModelBinderFactoryContext
                {
                    Metadata = propertyMetadata,
                    CacheToken = propertyMetadata
                });

                _propertyBinders[propertyMetadata] = binder;
            }

            return binder;
        }

        internal async Task BindModelCoreAsync(ModelBindingContext bindingContext, int propertyData)
        {
            Debug.Assert(propertyData == GreedyPropertiesMayHaveData || propertyData == ValueProviderDataAvailable);

            var isPolymorphicBind = false;

            if (bindingContext.Model == null)
            {
                var instance = CreateModel(bindingContext);
                bindingContext.Model = instance;

                var modelType = instance?.GetType();
                if (modelType != null && modelType != bindingContext.ModelMetadata.ModelType)
                {
                    // Fix metadata for polymorphic binding scenarios.
                    bindingContext.ModelMetadata = MetadataProvider.GetMetadataForType(modelType);
                    isPolymorphicBind = true;
                }
            }

            var modelMetadata = bindingContext.ModelMetadata;
            var attemptedPropertyBinding = false;
            var propertyBindingSucceeded = false;
            var postponePlaceholderBinding = false;

            for (var i = 0; i < modelMetadata.Properties.Count; i++)
            {
                var property = modelMetadata.Properties[i];
                if (!CanBindProperty(bindingContext, property))
                {
                    continue;
                }

                var propertyBinder = GetPropertyBinder(property);
                var isPlaceholderBinder = propertyBinder.GetType().Name == "PlaceholderBinder";

                if (isPlaceholderBinder)
                {
                    if (postponePlaceholderBinding)
                    {
                        // Decided to postpone binding properties that complete a loop in the model types when handling
                        // an earlier loop-completing property. Postpone binding this property too.
                        continue;
                    }
                    else if (!bindingContext.IsTopLevelObject &&
                        !propertyBindingSucceeded &&
                        propertyData == GreedyPropertiesMayHaveData)
                    {
                        // Have no confirmation of data for the current instance. Postpone completing the loop until
                        // we _know_ the current instance is useful. Recursion would otherwise occur prior to the
                        // block with a similar condition after the loop.
                        //
                        // Example cases include an Employee class containing
                        // 1. a Manager property of type Employee
                        // 2. an Employees property of type IList<Employee>
                        postponePlaceholderBinding = true;
                        continue;
                    }
                }

                var fieldName = property.BinderModelName ?? property.PropertyName;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                var result = await BindPropertyCoreAsync(bindingContext, property, fieldName, modelName);

                if (result.IsModelSet)
                {
                    attemptedPropertyBinding = true;
                    propertyBindingSucceeded = true;
                }
                else if (property.IsBindingRequired)
                {
                    attemptedPropertyBinding = true;
                }
            }

            if (postponePlaceholderBinding && propertyBindingSucceeded)
            {
                // Have some data for this instance. Continue with the model type loop.
                for (var i = 0; i < modelMetadata.Properties.Count; i++)
                {
                    var property = modelMetadata.Properties[i];
                    if (!CanBindProperty(bindingContext, property))
                    {
                        continue;
                    }

                    var propertyBinder = GetPropertyBinder(property);
                    var isPlaceholderBinder = propertyBinder.GetType().Name == "PlaceholderBinder";

                    if (isPlaceholderBinder)
                    {
                        var fieldName = property.BinderModelName ?? property.PropertyName;
                        var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                        await BindPropertyCoreAsync(bindingContext, property, fieldName, modelName);
                    }
                }
            }

            // Have we created a top-level model despite an inability to bind anything in said model and a lack of
            // other IsBindingRequired errors? Does that violate [BindRequired] on the model? This case occurs when
            // 1. The top-level model has no public settable properties.
            // 2. All properties in a [BindRequired] model have [BindNever] or are otherwise excluded from binding.
            // 3. No data exists for any property.
            if (!attemptedPropertyBinding &&
                bindingContext.IsTopLevelObject &&
                modelMetadata.IsBindingRequired)
            {
                var messageProvider = modelMetadata.ModelBindingMessageProvider;
                var message = messageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
            }

            // Have all binders failed because no data was available?
            //
            // If CanCreateModel determined a property has data, failures are likely due to conversion errors. For
            // example, user may submit ?[0].id=twenty&[1].id=twenty-one&[2].id=22 for a collection of a complex type
            // with an int id property. In that case, the bound model should be [ {}, {}, { id = 22 }] and
            // ModelState should contain errors about both [0].id and [1].id. Do not inform higher-level binders of the
            // failure in this and similar cases.
            //
            // If CanCreateModel could not find data for non-greedy properties, failures indicate greedy binders were
            // unsuccessful. For example, user may submit file attachments [0].File and [1].File but not [2].File for
            // a collection of a complex type containing an IFormFile property. In that case, we have exhausted the
            // attached files and checking for [3].File is likely be pointless. (And, if it had a point, would we stop
            // after 10 failures, 100, or more -- all adding redundant errors to ModelState?) Inform higher-level
            // binders of the failure.
            //
            // Required properties do not change the logic below. Missed required properties cause ModelState errors
            // but do not necessarily prevent further attempts to bind.
            //
            // This logic is intended to maximize correctness but does not avoid infinite loops or recursion when a
            // greedy model binder succeeds unconditionally.
            if (!bindingContext.IsTopLevelObject && !propertyBindingSucceeded && propertyData == GreedyPropertiesMayHaveData)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            }

            if (isPolymorphicBind && bindingContext.Result.IsModelSet)
            {
                // Setting the ValidationState ensures properties on derived types are correctly validated.
                bindingContext.ValidationState[bindingContext.Result.Model] = new ValidationStateEntry
                {
                    Metadata = modelMetadata,
                };
            }

            await OnModelBoundAsync(bindingContext, (T)bindingContext.Model);
        }

        protected virtual Task OnModelBoundAsync(ModelBindingContext bindingContext, T model)
        {
            return Task.CompletedTask;
        }

        protected virtual T CreateModel(ModelBindingContext bindingContext)
        {
            Guard.NotNull(bindingContext, nameof(bindingContext));

            if (_modelCreator == null)
            {
                var modelType = bindingContext.ModelType;
                if (modelType.IsAbstract || !modelType.HasDefaultConstructor())
                {
                    var metadata = bindingContext.ModelMetadata;
                    switch (metadata.MetadataKind)
                    {
                        case ModelMetadataKind.Parameter:
                            throw new InvalidOperationException(
                                "Could not create an instance of type '{0}'. Model bound complex types must not be abstract and must have a parameterless constructor. Alternatively, give the '{1}' parameter a non-null default value.".FormatInvariant(
                                    modelType.FullName,
                                    metadata.ParameterName));
                        case ModelMetadataKind.Property:
                            throw new InvalidOperationException(
                                "Could not create an instance of type '{0}'. Model bound complex types must not be abstract and must have a parameterless constructor. Alternatively, set the '{1}' property to a non-null value in the '{2}' constructor.".FormatInvariant(
                                    modelType.FullName,
                                    metadata.PropertyName,
                                    bindingContext.ModelMetadata.ContainerType.FullName));
                        case ModelMetadataKind.Type:
                            throw new InvalidOperationException(
                                "Could not create an instance of type '{0}'. Model bound complex types must not be abstract and must have a parameterless constructor.".FormatInvariant(
                                    modelType.FullName));
                    }

                }

                _modelCreator = Expression
                    .Lambda<Func<T>>(Expression.New(bindingContext.ModelType))
                    .Compile();
            }

            return _modelCreator();
        }

        /// <summary>
        /// Attempts to bind a property of the model.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/> for the model property.</param>
        /// <returns>
        /// A <see cref="Task"/> that when completed will set <see cref="ModelBindingContext.Result"/> to the
        /// result of model binding.
        /// </returns>
        protected virtual Task BindPropertyAsync(ModelBindingContext bindingContext)
        {
            var binder = GetPropertyBinder(bindingContext.ModelMetadata);
            return binder.BindModelAsync(bindingContext);
        }

        private async ValueTask<ModelBindingResult> BindPropertyCoreAsync(
            ModelBindingContext bindingContext,
            ModelMetadata property,
            string fieldName,
            string modelName)
        {
            Debug.Assert(property.MetadataKind == ModelMetadataKind.Property);

            // Pass complex (including collection) values down so that binding system does not unnecessarily
            // recreate instances or overwrite inner properties that are not bound. No need for this with simple
            // values because they will be overwritten if binding succeeds. Arrays are never reused because they
            // cannot be resized.
            object propertyModel = null;
            if (property.PropertyGetter != null &&
                property.IsComplexType &&
                !property.ModelType.IsArray)
            {
                propertyModel = property.PropertyGetter(bindingContext.Model);
            }

            ModelMetadata polymorphPropertyMetadata = null;
            ModelBindingResult result;
            using (bindingContext.EnterNestedScope(
                modelMetadata: property,
                fieldName: fieldName,
                modelName: modelName,
                model: propertyModel))
            {
                //if (property.IsComplexType && 
                //    //!property.ModelType.IsArray &&
                //    property.BindingSource != null &&
                //    property.BindingSource == BindingSource.Custom)
                //{
                //    bindingContext.BindingSource = BindingSource.ModelBinding;
                //}   

                await BindPropertyAsync(bindingContext);
                result = bindingContext.Result;

                if (property.ModelType != bindingContext.ModelMetadata.ModelType)
                {
                    // Fix metadata for polymorphic binding scenarios
                    polymorphPropertyMetadata = bindingContext.ModelMetadata;
                }
            }

            if (result.IsModelSet)
            {
                SetProperty(bindingContext, modelName, property, result);

                if (polymorphPropertyMetadata != null)
                {
                    // Setting the ValidationState ensures properties on derived types are correctly validated.
                    bindingContext.ValidationState[result.Model] = new ValidationStateEntry
                    {
                        Metadata = polymorphPropertyMetadata,
                    };
                }
            }
            else if (property.IsBindingRequired)
            {
                var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
                bindingContext.ModelState.TryAddModelError(modelName, message);
            }

            return result;
        }

        internal void SetProperty(
            ModelBindingContext bindingContext,
            string modelName,
            ModelMetadata propertyMetadata,
            ModelBindingResult result)
        {
            if (!result.IsModelSet)
            {
                // If we don't have a value, don't set it on the model and trounce a pre-initialized value.
                return;
            }

            if (propertyMetadata.IsReadOnly)
            {
                // The property should have already been set when we called BindPropertyAsync, so there's
                // nothing to do here.
                return;
            }

            var value = result.Model;
            try
            {
                propertyMetadata.PropertySetter(bindingContext.Model, value);
            }
            catch (Exception exception)
            {
                AddModelError(exception, modelName, bindingContext);
            }
        }

        private static void AddModelError(
            Exception exception,
            string modelName,
            ModelBindingContext bindingContext)
        {
            var targetInvocationException = exception as TargetInvocationException;
            if (targetInvocationException?.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
            }

            // Do not add an error message if a binding error has already occurred for this property.
            var modelState = bindingContext.ModelState;
            var validationState = modelState.GetFieldValidationState(modelName);
            if (validationState == ModelValidationState.Unvalidated)
            {
                modelState.AddModelError(modelName, exception, bindingContext.ModelMetadata);
            }
        }

        #region CanCreateModel

        internal int CanCreateModel(ModelBindingContext bindingContext)
        {
            // Other than the inbuilt ComplextObjectModelBinder we don't check for BindingSource
            // in sublevel objects.
            return bindingContext.IsTopLevelObject
                ? ValueProviderDataAvailable
                : CanBindAnyModelProperties(bindingContext);
        }

        private int CanBindAnyModelProperties(ModelBindingContext bindingContext)
        {
            // If there are no properties on the model, and no constructor parameters, there is nothing to bind. We are here means this is not a top
            // level object. So we return false.
            var modelMetadata = bindingContext.ModelMetadata;

            if (modelMetadata.Properties.Count == 0)
            {
                return NoDataAvailable;
            }

            // We want to check to see if any of the properties of the model can be bound using the value providers or
            // a greedy binder.
            //
            // Because a property might specify a custom binding source ([FromForm]), it's not correct
            // for us to just try bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName);
            // that may include other value providers - that would lead us to mistakenly create the model
            // when the data is coming from a source we should use (ex: value found in query string, but the
            // model has [FromForm]).
            //
            // To do this we need to enumerate the properties, and see which of them provide a binding source
            // through metadata, then we decide what to do.
            //
            //      If a property has a binding source, and it's a greedy source, then it's always bound.
            //
            //      If a property has a binding source, and it's a non-greedy source, then we'll filter the
            //      the value providers to just that source, and see if we can find a matching prefix
            //      (see CanBindValue).
            //
            //      If a property does not have a binding source, then it's fair game for any value provider.
            //
            // Bottom line, if any property meets the above conditions and has a value from ValueProviders, then we'll
            // create the model and try to bind it. Of, if ANY properties of the model have a greedy source,
            // then we go ahead and create it.
            var hasGreedyBinders = false;
            for (var i = 0; i < bindingContext.ModelMetadata.Properties.Count; i++)
            {
                var propertyMetadata = bindingContext.ModelMetadata.Properties[i];
                if (!CanBindProperty(bindingContext, propertyMetadata))
                {
                    continue;
                }

                // If any property can be bound from a greedy binding source, then success.
                var bindingSource = propertyMetadata.BindingSource;
                if (bindingSource != null && bindingSource.IsGreedy)
                {
                    hasGreedyBinders = true;
                    continue;
                }

                // Otherwise, check whether the (perhaps filtered) value providers have a match.
                var fieldName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName!;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                using (bindingContext.EnterNestedScope(
                    modelMetadata: propertyMetadata,
                    fieldName: fieldName,
                    modelName: modelName,
                    model: null))
                {
                    // If any property can be bound from a value provider, then success.
                    if (bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
                    {
                        return ValueProviderDataAvailable;
                    }
                }
            }

            if (hasGreedyBinders)
            {
                return GreedyPropertiesMayHaveData;
            }

            return NoDataAvailable;
        }

        protected virtual bool CanBindProperty(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
        {
            var metadataProviderFilter = bindingContext.ModelMetadata.PropertyFilterProvider?.PropertyFilter;
            if (metadataProviderFilter?.Invoke(propertyMetadata) == false)
            {
                return false;
            }

            if (bindingContext.PropertyFilter?.Invoke(propertyMetadata) == false)
            {
                return false;
            }

            if (!propertyMetadata.IsBindingAllowed)
            {
                return false;
            }

            if (propertyMetadata.MetadataKind == ModelMetadataKind.Property && propertyMetadata.IsReadOnly)
            {
                // Determine if we can update a readonly property (such as a collection).
                return CanUpdateReadOnlyProperty(propertyMetadata.ModelType);
            }

            return true;
        }

        internal static bool CanUpdateReadOnlyProperty(Type propertyType)
        {
            // Value types have copy-by-value semantics, which prevents us from updating
            // properties that are marked readonly.
            if (propertyType.IsValueType)
            {
                return false;
            }

            // Arrays are strange beasts since their contents are mutable but their sizes aren't.
            // Therefore we shouldn't even try to update these. Further reading:
            // http://blogs.msdn.com/ericlippert/archive/2008/09/22/arrays-considered-somewhat-harmful.aspx
            if (propertyType.IsArray)
            {
                return false;
            }

            // Special-case known immutable reference types
            if (propertyType == typeof(string))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
