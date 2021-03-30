using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Smartstore.ComponentModel;
using Smartstore.Domain;

namespace Smartstore.Web.Modelling.Validation
{
    public abstract class SmartValidator<TModel> : AbstractValidator<TModel> where TModel : class
    {
        /// <summary>
        /// Copies common validation rules from <typeparamref name="TEntity"/> type over to corresponding <typeparamref name="TModel"/> type.
        /// Common rules are: Required and MaxLength rules on string properties (either fluently mapped or annotated).
        /// </summary>
        /// <typeparam name="TEntity">The type of source entity.</typeparam>
        /// <param name="db">The data context instance to which <typeparamref name="TEntity"/> belongs.</param>
        /// <param name="ignoreProperties">An optional list of property names to ignore.</param>
        protected virtual void CopyFromEntityRules<TEntity>(DbContext db, params string[] ignoreProperties) where TEntity : BaseEntity, new()
        {
            Guard.NotNull(db, nameof(db));

            var entityType = db.Model.GetEntityTypes(typeof(TEntity)).FirstOrDefault();
            if (entityType != null)
            {
                // Get all model properties
                var modelProps = FastProperty.GetProperties(typeof(TModel), PropertyCachingStrategy.EagerCached);

                // Get all entity properties not in exclusion list
                var entityProps = entityType.GetProperties()
                    .Where(x => x.ClrType == typeof(string) && !ignoreProperties.Contains(x.Name))
                    .OfType<IMutableProperty>();

                // Loop thru all entity string properties
                foreach (var entityProp in entityProps)
                {
                    // Check if target model contains property (must be of same name and type)
                    if (modelProps.TryGetValue(entityProp.Name, out var modelProp) && modelProp.Property.PropertyType == typeof(string))
                    {
                        var isNullable = entityProp.IsNullable;
                        var maxLength = entityProp.GetMaxLength();

                        if (!isNullable || maxLength.HasValue)
                        {
                            var expression = DynamicExpressionParser.ParseLambda<TModel, string>(null, false, "@" + modelProp.Name);

                            if (!isNullable)
                            {
                                RuleFor(expression).NotEmpty();
                            }

                            if (maxLength.HasValue)
                            {
                                RuleFor(expression).Length(0, maxLength.Value);
                            }
                        }
                    }
                }
            }
        }
    }
}
