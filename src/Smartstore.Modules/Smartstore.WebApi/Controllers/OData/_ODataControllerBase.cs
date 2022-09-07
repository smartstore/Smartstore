using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.WebApi.Controllers.OData
{
    [ODataRouteComponent("odata/v1")]
    [Route("odata/v1")]
    //[Produces("application/json")]
    public abstract class ODataControllerBase<TEntity> : ODataController
        where TEntity : BaseEntity, new()
    {
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

        protected virtual async Task<IActionResult> GetPropertyValueAsync(int key, string property)
        {
            Guard.NotEmpty(property, nameof(property));

            var values = await Entities
                .Where(x => x.Id == key)
                .Select($"new {{ {property} }}")
                .ToDynamicArrayAsync();

            if (values.IsNullOrEmpty())
            {
                return NotFound();
            }

            var propertyValue = (values[0] as DynamicClass).GetDynamicPropertyValue(property);

            return Ok(propertyValue);
        }

        protected virtual async Task<IActionResult> PatchAsync(int key, Delta<TEntity> model, Func<TEntity, Task> update)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await Entities.FindByIdAsync(key, false);
            if (entity == null)
            {
                return NotFound();
            }

            model?.Patch(entity);
            // TODO:
            //entity = FulfillPropertiesOn(entity);

            try
            {
                await update(entity);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (null == await Entities.FindByIdAsync(key, false))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            if (Request?.Headers?.ContainsKey("Prefer") ?? false)
            {
                return Updated(entity);
            }

            // Avoid HTTP 204 No Content by default.
            return Ok(entity);
        }

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
