using System;
using System.Linq.Dynamic.Core;
using System.Web;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.WebApi.Controllers.OData
{
    [ODataRouteComponent("odata/v1")]
    [Route("odata/v1")]
    //[Produces("application/json")]
    public abstract class SmartODataController<TEntity> : ODataController
        where TEntity : BaseEntity, new()
    {
        internal const string FulfillKey = "SmNetFulfill";
        internal const string PropertyNotFound = "Entity does not own property '{0}'.";

        private SmartDbContext _db;
        private DbSet<TEntity> _dbSet;

        protected SmartDbContext Db
        {
            get => _db ??= HttpContext.RequestServices.GetService<SmartDbContext>();
        }

        protected virtual DbSet<TEntity> Entities
        {
            get => _dbSet ??= Db.Set<TEntity>();
            set => _dbSet = value;
        }

        protected virtual async Task<IActionResult> GetByKeyAsync(int key, bool tracked = false)
        {
            var entity = await Entities.FindByIdAsync(key, tracked);

            // INFO: "NotFound" without object parameter produces unwanted HTML response.
            return entity != null
                ? Ok(entity)
                : NotFound(default(TEntity));
        }

        protected virtual async Task<IActionResult> GetPropertyValueAsync(int key, string property)
        {
            Guard.NotEmpty(property, nameof(property));

            var values = await Entities
                .Where(x => x.Id == key)
                .Select($"new {{ {property} }}")
                .ToDynamicArrayAsync();

            if (values.IsNullOrEmpty())
            {
                return NotFound(default(TEntity));
            }

            var propertyValue = (values[0] as DynamicClass).GetDynamicPropertyValue(property);

            return Ok(propertyValue);
        }

        /// <summary>
        /// Partially updates an entity.
        /// </summary>
        /// <param name="key">Key (ID) of the entity.</param>
        /// <param name="model">Delta model with the data to overwrite the original entity.</param>
        /// <param name="update">Update action. Typically used to call Db.SaveChangesAsync().</param>
        /// <returns>
        /// UpdatedODataResult<TEntity> which leads to:
        /// status code 204 "No Content" by default or
        /// status code 200 including entity content if "Prefer" header is specified with value "return=representation".
        /// </returns>
        protected virtual async Task<IActionResult> PatchAsync(int key, Delta<TEntity> model, Func<TEntity, Task> update)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await Entities.FindByIdAsync(key);
            if (entity == null)
            {
                return NotFound(default(TEntity));
            }

            try
            {
                model?.Patch(entity);
                // TODO: (mg) (core) test this:
                entity = await ApplyRelatedEntityIdsAsync(entity);

                await update(entity);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (null == await Entities.FindByIdAsync(key, false))
                {
                    return NotFound(default(TEntity));
                }
                else
                {
                    return UnprocessableEntity(ex.Message);
                }
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(ex.Message);
            }

            return Updated(entity);
        }

        #region Utilities

        protected virtual async Task<TEntity> ApplyRelatedEntityIdsAsync(TEntity entity)
        {
            if (entity != null)
            {
                var entityType = entity.GetType();
                // TODO: (mg) (core) RawUrl() does NOT contain scheme and host part. This will fail.
                var uri = new Uri(Request.RawUrl());
                var queries = HttpUtility.ParseQueryString(uri.Query);

                foreach (var key in queries?.AllKeys?.Where(x => x.StartsWith(FulfillKey)))
                {
                    var propName = key[FulfillKey.Length..];
                    var queryValue = queries.Get(key);

                    if (propName.HasValue() && queryValue.HasValue())
                    {
                        propName = propName.EnsureEndsWith("Id", StringComparison.OrdinalIgnoreCase);

                        var prop = FastProperty.GetProperty(entityType, propName);
                        if (prop != null)
                        {
                            var propType = prop?.Property?.PropertyType;
                            if (propType == typeof(int) || propType == typeof(int?))
                            {
                                var propValue = prop.GetValue(entity);
                                if (propValue == null || propValue.Equals(0))
                                {
                                    var relatedEntityId = await GetRelatedEntityIdAsync(propName, queryValue);
                                    if (relatedEntityId != 0)
                                    {
                                        prop.SetValue(entity, relatedEntityId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return entity;
        }

        protected virtual async Task<int> GetRelatedEntityIdAsync(string propertyName, string queryValue)
        {
            if (propertyName.EqualsNoCase("CountryId"))
            {
                return await Db.Countries
                    .ApplyIsoCodeFilter(queryValue)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }
            else if (propertyName.EqualsNoCase("StateProvinceId"))
            {
                return await Db.StateProvinces
                    .Where(x => x.Abbreviation == queryValue)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }
            else if (propertyName.EqualsNoCase("LanguageId"))
            {
                return await Db.Languages
                    .Where(x => x.LanguageCulture == queryValue)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }
            else if (propertyName.EqualsNoCase("CurrencyId"))
            {
                return await Db.Currencies
                    .Where(x => x.CurrencyCode == queryValue)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            return 0;
        }

        #endregion

        #region Probably obsolete (not required anymore)

        //protected internal virtual IQueryable<TCollection> GetRelatedCollection<TCollection>(
        //    int key,
        //    Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty)
        //{
        //    Guard.NotNull(navigationProperty, nameof(navigationProperty));

        //    var query = Entities.Where(x => x.Id.Equals(key));

        //    return query.SelectMany(navigationProperty);
        //}

        #endregion
    }
}
