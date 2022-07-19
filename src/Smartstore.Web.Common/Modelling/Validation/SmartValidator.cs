using System.Linq.Dynamic.Core;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.EntityFrameworkCore.Metadata;
using Smartstore;
using Smartstore.ComponentModel;

namespace FluentValidation
{
    public abstract class SmartValidator<TModel> : AbstractValidator<TModel> where TModel : class
    {
        /// <summary>
        /// Adds "NotEmpty" rules to non-nullable intrinsic model properties to bypass MVC's non-localized RequiredAttributeAdapter.
        /// </summary>
        /// <param name="ignoreProperties">An optional list of property names to ignore.</param>
        protected virtual void ApplyNonNullableValueTypeRules(params string[] ignoreProperties)
        {
            // Get all model properties
            var modelProps = FastProperty.GetProperties(typeof(TModel), PropertyCachingStrategy.EagerCached);

            foreach (var modelProp in modelProps.Values.Where(x => !x.Property.PropertyType.IsNullableType(out _) && x.Property.PropertyType.IsValueType))
            {
                // If the model property is a non-nullable value type, then MVC will have already generated a non-localized Required rule.
                // We should provide our own localized required rule and rely on FV to remove the MVC one. 

                if (ignoreProperties.Contains(modelProp.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rule = CreatePropertyRule(modelProp);
                rule.AddValidator(new NotEmptyValidator(Activator.CreateInstance(modelProp.Property.PropertyType)));
                AddRule(rule);
            }
        }
        
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
            Guard.NotNull(db, nameof(db));

            // Get all model properties
            var modelProps = FastProperty.GetProperties(typeof(TModel), PropertyCachingStrategy.EagerCached);

            var entityType = db.Model.FindEntityTypes(typeof(TEntity)).FirstOrDefault();
            if (entityType == null)
                return;

            var scalarProps = entityType.GetProperties().ToArray();
            var declaredProps = entityType.GetDeclaredProperties().ToArray();

            // Get all entity properties not in exclusion list
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
                        var rule = CreatePropertyRule(modelProp);

                        if (!isNullable)
                        {
                            rule.AddValidator(new NotEmptyValidator(null));
                        }

                        if (maxLength.HasValue)
                        {
                            rule.AddValidator(new LengthValidator(0, maxLength.Value));
                        }

                        AddRule(rule);
                    }
                }
            }
        }

        private static PropertyRule CreatePropertyRule(FastProperty modelProp)
        {
            var rule = new PropertyRule(
                modelProp.Property,
                modelProp.GetValue,
                DynamicExpressionParser.ParseLambda(typeof(TModel), modelProp.Property.PropertyType, "@" + modelProp.Property.Name),
                () => ValidatorOptions.Global.CascadeMode,
                modelProp.Property.PropertyType,
                typeof(TModel));

            return rule;
        }
    }
}
