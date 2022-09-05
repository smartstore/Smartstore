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
    public class CategoriesController : ODataControllerBase<Category>
    {
        // TODO: (mg) (core) use custom query attribute inherited from EnableQueryAttribute.
        [HttpGet, EnableQuery]
        public ActionResult<IQueryable<Category>> Get()
        {
            var query = Entities.AsNoTracking();

            return Ok(query);
        }
    }
}
