using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Smart base controller class for OData endpoints.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken]
    public abstract class SmartODataController<TEntity> : ODataController
        where TEntity : BaseEntity, new()
    {
        const string EntityWrapperTypeName = "SelectAllAndExpand`1";
        const string EntityWrapperPropertyName = "Instance";
        const string FulfillKey = "SmApiFulfill";

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

        // INFO: "AsSplitQuery" cannot be used for API requests because there is no guarantee for a unique data order (e.g. order by primary key).
        // See also https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries#split-queries-1
        //protected IQueryable<TEntity> GetQuery(bool tracked = false)
        //{
        //    if (tracked)
        //    {
        //        return Entities;
        //    }
        //    else
        //    {
        //        if (Request?.Query?.Any(x => x.Key == "$expand") ?? false)
        //        {
        //            // Avoid that missing QuerySplittingBehavior warning floods the log list.
        //            return Entities
        //                .AsSplitQuery()
        //                .AsNoTrackingWithIdentityResolution()
        //                .AsNoCaching();
        //        }
        //        else
        //        {
        //            return Entities
        //                .AsNoTracking()
        //                .AsNoCaching();
        //        }
        //    }
        //}

        /// <summary>
        /// Gets an entity by identifier.
        /// </summary>
        /// <remarks>
        /// Navigation properties are always expandable, even if <paramref name="tracked"/> is <c>false</c>.
        /// </remarks>
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
        /// Gets an entity by identifier.
        /// </summary>
        /// <remarks>
        /// The entity is always tracked to ensure that <see cref="ApiQueryableAttribute"/> is applicable 
        /// and navigation properties are expandable via $expand.
        /// </remarks>
        /// <param name="id">Entity identifier.</param>
        /// <exception cref="ODataErrorException">Thrown when the requested entity does not exist.</exception>
        protected async Task<TEntity> GetRequiredById(int id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryModifier = null)
        {           
            var query = Entities.AsQueryable();
            
            if (queryModifier != null)
            {
                query = queryModifier(query);
            }

            var entity = await query.SingleOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new ODataErrorException(new ODataError
                {
                    ErrorCode = StatusCodes.Status404NotFound.ToString(),
                    Message = $"Cannot find {typeof(TEntity).Name} entity with identifier {id}."
                });
            }

            return entity;
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
                return NotFound(id);
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

        /// <summary>
        /// Updates a slug and executes SaveChangesAsync.
        /// </summary>
        protected async Task UpdateSlugAsync<T>(T entity)
            where T : ISlugSupported
        {
            var urlService = HttpContext.RequestServices.GetService<IUrlService>();
            await urlService.SaveSlugAsync(entity, string.Empty, entity.GetDisplayName(), true);
        }

        #region Utilities

        /// <summary>
        /// Gets a value indicating whether the request has a multipart content.
        /// </summary>
        protected bool HasMultipartContent
            => Request.ContentType?.StartsWithNoCase("multipart/") ?? false;

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

        /// <summary>
        /// Sets the identifier property of a foreign relation using a key value in the query string.
        /// Ignores identifier properties where the value is already set.
        /// Avoids extra API requests if the entity ID is unknown.
        /// </summary>
        /// <example>/Addresses(123)?SmApiFulfillCountry=US&SmApiFulfillStateProvince=NY</example>
        /// <param name="entity">Entity instance.</param>
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

                        var prop = entityType.GetProperty(propName);
                        if (prop != null)
                        {
                            var propType = prop.PropertyType;
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

        /// <summary>
        /// Validates <paramref name="options"/> and applies them to <paramref name="source"/>.
        /// </summary>
        /// <exception cref="ODataException">Throws if <paramref name="options"/> are invalid.</exception>
        protected IQueryable Apply(
            ODataQueryOptions<TEntity> options,
            IQueryable<TEntity> source,
            ODataValidationSettings validationSettings = null)
        {
            validationSettings ??= new();

            ODataQuerySettings settings = null;

            if (HttpContext.Items.TryGetValue(MaxApiQueryOptions.Key, out var obj) && obj is MaxApiQueryOptions maxValues)
            {
                validationSettings.MaxTop = maxValues.MaxTop;
                validationSettings.MaxExpansionDepth = maxValues.MaxExpansionDepth;

                if (options.Top == null)
                {
                    // If paging is required and there is no $top sent by client then force the page size specified by merchant.
                    settings = options.Context?.RequestContainer?.GetRequiredService<ODataQuerySettings>() ?? new();
                    settings.PageSize = maxValues.MaxTop;
                }
            }

            options.Validate(validationSettings);

            // The returned IQueryable might not implement IAsyncEnumerable!
            var query = settings != null
                ? options.ApplyTo(source, settings)
                : options.ApplyTo(source);

            return query;
        }

        /// <summary>
        /// Unwraps entities from an <see cref="IQueryable"/> returned by <see cref="ODataQueryOptions.ApplyTo()"/>.
        /// </summary>
        protected IEnumerable<TEntity> UnwrapEntityQuery(IQueryable source)
        {
            Guard.NotNull(source, nameof(source));

            foreach (var item in source)
            {
                if (item is TEntity entity)
                {
                    yield return entity;
                }
                else
                {
                    var type = item.GetType();
                    if (type.Name == EntityWrapperTypeName)
                    {
                        var prop = (TEntity)type.GetProperty(EntityWrapperPropertyName).GetValue(item);
                        yield return prop;
                    }
                }
            }
        }

        /// <summary>
        /// Creates an absolute OData entity URL.
        /// Typically used for <see cref="CreatedODataResult{TEntity}"/> to create the location response header.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <returns>Absolute OData URL.</returns>
        /// <example>https://www.my-store/odata/v1/Addresses(85382)</example>
        protected string BuildUrl(int id)
        {
            Guard.NotZero(id);

            var routePrefix = Request.ODataFeature().RoutePrefix;
            var controller = Request.RouteValues.GetControllerName();
            var url = UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase);

            return $"{url.EnsureEndsWith('/')}{routePrefix.EnsureEndsWith('/')}{controller}({id})";
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

        protected NotFoundODataResult NotFound(int id, string entityName = null)
            => NotFound($"Cannot find {entityName ?? typeof(TEntity).Name} entity with identifier {id}.");

        /// <summary>
        /// Returns <see cref="ODataErrorResult"/> with status <see cref="StatusCodes.Status403Forbidden"/>
        /// and the message that the current operation is not allowed on this endpoint.
        /// </summary>
        /// <param name="extraInfo">Extra info to append to the message.</param>
        protected ODataErrorResult Forbidden(string extraInfo = null)
            => ErrorResult(null, $"{Request.Method} on {Request.Path} is not allowed.".Grow(extraInfo, " "), StatusCodes.Status403Forbidden);

        #endregion
    }
}
