using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using Smartstore.ComponentModel;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Smart base controller class for OData endpoints.
    /// </summary>
    /// <remarks>
    /// - ActionResult<T> vs. IActionResult: IActionResult is used when multiple return types are possible.
    /// For ActionResult<T> ProducesResponseTypeAttribute's type property can be excluded.
    /// - Explicit "From" parameter bindings are required otherwise Swagger will describe them as "query" params by default.
    /// - Accurate examples: https://github.com/dotnet/aspnet-api-versioning/tree/93bd8dc7582ec14c8ec97997c01cfe297b085e17/examples/AspNetCore/OData
    /// - Routing conventions: https://learn.microsoft.com/en-us/odata/webapi/built-in-routing-conventions
    /// - $ref: https://learn.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/entity-relations-in-odata-v4#creating-a-relationship-between-entities
    /// - Swashbuckle: https://github.com/domaindrivendev/Swashbuckle.AspNetCore
    /// </remarks>
    [Authorize(AuthenticationSchemes = "Smartstore.WebApi.Basic")]
    public abstract class SmartODataController<TEntity> : ODataController
        where TEntity : BaseEntity, new()
    {
        internal const string FulfillKey = "SmApiFulfill";

        private SmartDbContext _db;
        private DbSet<TEntity> _dbSet;

        protected SmartDbContext Db
        {
            get => _db ??= HttpContext.RequestServices.GetService<SmartDbContext>();
        }

        protected DbSet<TEntity> Entities
        {
            get => _dbSet ??= Db.Set<TEntity>();
            set => _dbSet = value;
        }

        /// <summary>
        /// Gets an entity by identifier.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="tracked">Applies "AsTracking()" or "AsNoTracking()" according to <paramref name="tracked"/> parameter.</param>
        /// <returns>Returns zero or one entities.</returns>
        protected SingleResult<TEntity> GetById(int id, bool tracked = false)
        {
            var query = Entities
                .ApplyTracking(tracked)
                .Where(x => x.Id == id);

            return SingleResult.Create(query);
        }

        /// <summary>
        /// Gets a related entity via navigation property.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="navigationProperty">Navigation property expression.</param>
        /// <param name="tracked">Applies "AsTracking()" or "AsNoTracking()" according to <paramref name="tracked"/> parameter.</param>
        /// <returns>Returns zero or one entities.</returns>
        protected SingleResult<TProperty> GetRelatedEntity<TProperty>(int id, Expression<Func<TEntity, TProperty>> navigationProperty, bool tracked = false)
        {
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var query = Entities
                .ApplyTracking(tracked)
                .Where(x => x.Id == id)
                .Select(navigationProperty);

            return SingleResult.Create(query);
        }

        /// <summary>
        /// Gets a query of related entities via navigation property.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="navigationProperty">Navigation property expression.</param>
        /// <param name="tracked">Applies "AsTracking()" or "AsNoTracking()" according to <paramref name="tracked"/> parameter.</param>
        /// <returns>Related entities query.</returns>
        protected IQueryable<TProperty> GetRelatedQuery<TProperty>(int id, Expression<Func<TEntity, IEnumerable<TProperty>>> navigationProperty, bool tracked = false)
        {
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var query = Entities
                .ApplyTracking(tracked)
                .Where(x => x.Id == id)
                .SelectMany(navigationProperty);

            return query;
        }

        //protected async Task<IActionResult> GetPropertyValueAsync(int id, string property)
        //{
        //    Guard.NotEmpty(property, nameof(property));

        //    var values = await Entities
        //        .Where(x => x.Id == id)
        //        .Select($"new {{ {property} }}")
        //        .ToDynamicArrayAsync();

        //    if (values.IsNullOrEmpty())
        //    {
        //        return NotFound(default(TEntity));
        //    }

        //    var propertyValue = (values[0] as DynamicClass).GetDynamicPropertyValue(property);

        //    return Ok(propertyValue);
        //}

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entity">Entity to add.</param>
        /// <param name="add">
        /// Function called to save changes. Typically used to call Db.SaveChangesAsync().
        /// <c>null</c> executes SaveChangesAsync internally.
        /// </param>
        /// <returns>CreatedODataResult<TEntity> which leads to status code 201 "Created" including entity content.</returns>
        protected async Task<IActionResult> PostAsync(TEntity entity, Func<Task> add = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                Entities.Add(entity);
                entity = await ApplyRelatedEntityIdsAsync(entity);

                if (add != null)
                {
                    await add();
                }
                else
                {
                    await Db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }

            return Created(entity);
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="model">Model with the data to overwrite the original entity.</param>
        /// <param name="update">
        /// Function called to save changes. Typically used to call Db.SaveChangesAsync().
        /// <c>null</c> executes SaveChangesAsync internally.
        /// </param>
        /// <returns>
        /// UpdatedODataResult<TEntity> which leads to:
        /// status code 204 "No Content" by default or
        /// status code 200 including entity content if "Prefer" header is specified with value "return=representation".
        /// </returns>
        protected Task<IActionResult> PutAsync(int id, Delta<TEntity> model, Func<TEntity, Task> update = null)
            => UpdateInternal(false, id, model, update);

        /// <summary>
        /// Partially updates an entity.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="model">Delta model with the data to overwrite the original entity.</param>
        /// <param name="update">
        /// Function called to save changes. Typically used to call Db.SaveChangesAsync().
        /// <c>null</c> executes SaveChangesAsync internally.
        /// </param>
        /// <returns>
        /// UpdatedODataResult<TEntity> which leads to:
        /// status code 204 "No Content" by default or
        /// status code 200 including entity content if "Prefer" header is specified with value "return=representation".
        /// </returns>
        protected Task<IActionResult> PatchAsync(int id, Delta<TEntity> model, Func<TEntity, Task> update = null)
            => UpdateInternal(true, id, model, update);

        private async Task<IActionResult> UpdateInternal(bool patch, int id, Delta<TEntity> model, Func<TEntity, Task> update)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await Entities.FindByIdAsync(id);
            if (entity == null)
            {
                return NotFound($"Cannot find {typeof(TEntity).Name} entity with identifier {id}.");
            }

            try
            {
                if (patch)
                {
                    model.Patch(entity);
                }
                else
                {
                    model.Put(entity);
                }

                // TODO: (mg) (core) test ApplyRelatedEntityIdsAsync.
                entity = await ApplyRelatedEntityIdsAsync(entity);

                if (update != null)
                {
                    await update(entity);
                }
                else
                {
                    await Db.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var code = await Entities.AnyAsync(x => x.Id == id) ? StatusCodes.Status409Conflict : StatusCodes.Status404NotFound;

                return ErrorResult(ex, null, code);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }

            return Updated(entity);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="update">
        /// Function called to save changes. Typically used for soft deletable entities.
        /// <c>null</c> removes the entity and executes SaveChangesAsync internally.
        /// </param>
        /// <returns>NoContentResult which leads to status code 204.</returns>
        protected async Task<IActionResult> DeleteAsync(int id, Func<TEntity, Task> delete = null)
        {
            var entity = await Entities.FindByIdAsync(id);
            if (entity == null)
            {
                return NotFound(default(TEntity));
            }

            try
            {
                if (delete != null)
                {
                    await delete(entity);
                }
                else
                {
                    Entities.Remove(entity);
                    await Db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }

            return NoContent();
        }

        #region Utilities

        /// <summary>
        /// Gets related keys from an OData Uri.
        /// </summary>
        /// <returns>Dictionary with key property names and values.</returns>
        protected IReadOnlyDictionary<string, object> GetRelatedKeys(Uri uri)
        {
            Guard.NotNull(uri, nameof(uri));

            var feature = HttpContext.ODataFeature();
            //var serviceRoot = new Uri(new Uri(feature.BaseAddress), feature.RoutePrefix);
            var serviceRoot = new Uri(feature.BaseAddress);
            var parser = new ODataUriParser(feature.Model, serviceRoot, uri, feature.Services);

            parser.Resolver ??= new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };
            //parser.UrlKeyDelimiter = ODataUrlKeyDelimiter.Slash;

            var path = parser.ParsePath();
            var segment = path.OfType<KeySegment>().FirstOrDefault();

            if (segment is null)
            {
                return new Dictionary<string, object>(capacity: 0);
            }

            return new Dictionary<string, object>(segment.Keys, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the related key from an OData Uri.
        /// </summary>
        protected T GetRelatedKey<T>(Uri uri, string key = null)
        {
            var keys = GetRelatedKeys(uri);

            if (keys.TryGetValue(key ?? "id", out var value))
            {
                return value.Convert<T>();
            }

            return default;
        }

        protected ODataErrorResult ErrorResult(
            Exception ex = null,
            string message = null,
            int statusCode = StatusCodes.Status422UnprocessableEntity)
        {
            if (ex != null && ex is ODataErrorException oex)
            {
                return ODataErrorResult(oex.Error);
            }

            return ODataErrorResult(new()
            {
                ErrorCode = statusCode.ToString(),
                Message = message ?? ex.Message,
                InnerError = ex != null ? new ODataInnerError(ex) : null
            });
        }

        protected async Task<TEntity> ApplyRelatedEntityIdsAsync(TEntity entity)
        {
            if (entity != null)
            {
                var entityType = entity.GetType();

                foreach (var pair in Request.Query.Where(x => x.Key.StartsWithNoCase(FulfillKey)))
                {
                    var propName = pair.Key[FulfillKey.Length..];
                    var queryValue = pair.Value.ToString();

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

        protected async Task<int> GetRelatedEntityIdAsync(string propertyName, string queryValue)
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
    }
}
