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
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.WebApi.Controllers.OData
{
    [Produces("application/json")]
    [ODataRouteComponent("odata/v1")]
    public abstract class ODataControllerBase<TEntity> : ODataController
        where TEntity : BaseEntity, new()
    {
        private SmartDbContext _db;
        private DbSet<TEntity> _dbSet;

        protected SmartDbContext Db
        {
            get => _db ??= HttpContext.RequestServices.GetService<SmartDbContext>();
        }

        public virtual DbSet<TEntity> Entities
        {
            get => _dbSet ??= Db.Set<TEntity>();
            set => _dbSet = value;
        }
    }
}
