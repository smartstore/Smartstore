using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.WebApi.Controllers.OData
{
    [Produces("application/json")]
    [ODataRouteComponent("odata/v1")]
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

        protected virtual async Task<IActionResult> GetPropertyValueAsync(int key, string propertyName)
        {
            var entity = await Entities.FindByIdAsync(key, false);
            if (entity == null)
            {
                return NotFound();
            }

            var prop = FastProperty.GetProperty(entity.GetType(), propertyName);
            if (prop == null)
            {
                return BadRequest(PropertyNotFound.FormatInvariant(propertyName.EmptyNull()));
            }

            var propertyValue = prop.GetValue(entity);

            return Ok(propertyValue);
        }
    }
}
