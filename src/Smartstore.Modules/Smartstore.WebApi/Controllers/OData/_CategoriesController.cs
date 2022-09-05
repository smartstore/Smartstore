using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Data;

namespace Smartstore.WebApi.Controllers.OData
{
    // TODO: (mg) (core) add an OData controller base.
    [Produces("application/json")]
    [ODataRouteComponent("odata/v1")]
    public class CategoriesController : ODataController
    {
        private readonly SmartDbContext _db;

        public CategoriesController(SmartDbContext db)
        {
            _db = db;
        }

        [HttpGet, EnableQuery]
        public ActionResult<IQueryable<Category>> Get()
        {
            var query = _db.Categories.AsNoTracking();

            return Ok(query);
        }
    }
}
