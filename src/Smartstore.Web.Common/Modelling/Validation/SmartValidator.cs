using System.Linq.Dynamic.Core;
using System.Reflection;
using FluentValidation.Validators;
using Smartstore;
using Smartstore.ComponentModel;

namespace FluentValidation
{
    public abstract class SmartValidator<TModel> : AbstractValidator<TModel> where TModel : class
    {
        //  DynamicExpressionParser
        //  --> public static Expression<Func<T, TResult>> ParseLambda<T, TResult>(ParsingConfig parsingConfig, bool createParameterCtor, string expression, params object[] values)
        static readonly MethodInfo ParseLambdaMethod = typeof(DynamicExpressionParser).GetMethod("ParseLambda",
            genericParameterCount: 2,
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(ParsingConfig), typeof(bool), typeof(string), typeof(object[]) },
            modifiers: null);

        //  AbstractValidator<T>
        //  --> public IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        static readonly MethodInfo RuleForMethod = typeof(AbstractValidator<TModel>).GetMethod("RuleFor");

        /// <summary>
        /// Copies common validation rules from <typeparamref name="TEntity"/> type over to corresponding <typeparamref name="TModel"/> type.
        /// Common rules are: Required and MaxLength rules on string properties (either fluently mapped or annotated).
        /// Also: adds Required rule to non-nullable intrinsic model property type to bypass MVC's non-localized RequiredAttributeAdapter.
        /// </summary>
        /// <typeparam name="TEntity">The type of source entity.</typeparam>
        /// <param name="db">The data context instance to which <typeparamref name="TEntity"/> belongs.</param>
        /// <param name="ignoreProperties">An optional list of property names to ignore.</param>
        protected virtual void ApplyEntityRules<TEntity>(DbContext db, params string[] ignoreProperties) where TEntity : BaseEntity, new()
        {
            Guard.NotNull(db);

            // Get all model properties
            var modelProps = FastProperty.GetProperties(typeof(TModel));

            var entityType = db.Model.FindEntityTypes(typeof(TEntity)).FirstOrDefault();
            if (entityType == null)
                return;

            var scalarProps = entityType.GetProperties().ToArray();
            var declaredProps = entityType.GetDeclaredProperties().ToArray();

            // Get all entity string properties not in exclusion list
            var entityProps = entityType.GetProperties()
                .Where(x => x.ClrType == typeof(string) && !ignoreProperties.Contains(x.Name))
                .ToArray();

            // Loop thru all entity string properties
            foreach (var entityProp in entityProps)
            {
                // Check if target model contains property (must be of same name and type)
                if (modelProps.TryGetValue(entityProp.Name, out var modelProp))
                {
                    if (modelProp.Property.PropertyType != typeof(string))
                    {
                        continue;
                    }

                    var isNullable = entityProp.IsNullable;
                    var maxLength = entityProp.GetMaxLength();

                    if (!isNullable || maxLength.HasValue)
                    {
                        var expression = DynamicExpressionParser.ParseLambda<TModel, string>(null, false, "@" + modelProp.Property.Name);
                        var rule = RuleFor(expression);
                        
                        if (!isNullable)
                        {
                            rule.NotEmpty();
                        }

                        if (maxLength.HasValue)
                        {
                            rule.Length(0, maxLength.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds "NotNull" rules to non-nullable intrinsic model properties to bypass MVC's non-localized RequiredAttributeAdapter.
        /// </summary>
        /// <param name="ignoreProperties">An optional list of property names to ignore.</param>
        protected virtual void ApplyNonNullableValueTypeRules(params string[] ignoreProperties)
        {
            // Get all model properties
            var modelProps = FastProperty.GetProperties(typeof(TModel));

            foreach (var modelProp in modelProps.Values.Where(x => !x.Property.PropertyType.IsNullableType(out _) && x.Property.PropertyType.IsValueType))
            {
                // If the model property is a non-nullable value type, then MVC will have already generated a non-localized Required rule.
                // We should provide our own localized required rule and rely on FV to remove the MVC one. 

                if (ignoreProperties.Contains(modelProp.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddNotEmptyValidatorForPrimitiveProperty(modelProp);
            }
        }

        private void AddNotEmptyValidatorForPrimitiveProperty(FastProperty modelProp)
        {
            // WHAT A F...ING MESS! The FluentValidator devs decided - in a fit of insanity - that from version 10 on
            // everything should be generic. Which is totally hostile to dynamic code.
            // We don't know the generic type signatures, because we use reflection here!!!
            // Therefore we have to generate the generic definition by reflection which looks really ugly.
            // But there's no alternative to it, unfortunately.

            // Make DynamicExpressionParser.ParseLambda<TModel, TProperty>(...)
            var parseLambdaMethod = FastInvoker.GetInvoker(ParseLambdaMethod.MakeGenericMethod(typeof(TModel), modelProp.Property.PropertyType));

            // Call ParseLambda<<TModel, TPropType>() method
            var expression = parseLambdaMethod.Invoke(null, null, false, "@" + modelProp.Property.Name, null);

            // Create rule: first make AbstractValidator<T>.RuleFor<TProperty>(...) method
            var ruleForMethod = FastInvoker.GetInvoker(RuleForMethod.MakeGenericMethod(modelProp.Property.PropertyType));

            // Then call .RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
            var rule = ruleForMethod.Invoke(this, expression);

            // Create Validator instance
            var validatorType = typeof(NotNullValidator<,>).MakeGenericType(typeof(TModel), modelProp.Property.PropertyType);
            var validator = Activator.CreateInstance(validatorType);

            // Make IRuleBuilder<TModel, TProperty>.SetValidator(IPropertyValidator<T, TProperty> validator)
            var propertyValidatorType = typeof(IPropertyValidator<,>).MakeGenericType(typeof(TModel), modelProp.Property.PropertyType);
            var setValidatorMethod = rule.GetType().GetMethod("SetValidator", new[] { propertyValidatorType });

            // Call SetValidator method of property rule builder
            setValidatorMethod.Invoke(rule, new object[] { validator });
        }
    }
}
