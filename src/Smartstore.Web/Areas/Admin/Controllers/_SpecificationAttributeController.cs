using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Areas.Admin.Controllers
{
    public class SpecificationAttributeController : AdminController
    {
        private readonly SmartDbContext _db;

        public SpecificationAttributeController(SmartDbContext db)
        {
            _db = db;
        }

        // Ajax.
        public async Task<IActionResult> GetOptionsByAttributeId(int attributeId)
        {
            var options = await _db.SpecificationAttributeOptions
                .AsNoTracking()
                .Where(x => x.SpecificationAttributeId == attributeId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var result =
                from o in options
                select new { id = o.Id, name = o.Name, text = o.Name };

            return Json(result.ToList());
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.Update)]
        public async Task<IActionResult> SetAttributeValue(string pk, string value, string name)
        {
            var success = false;
            var message = string.Empty;

            // name is the entity id of product specification attribute mapping.
            var attribute = await _db.ProductSpecificationAttributes.FindByIdAsync(Convert.ToInt32(name));

            try
            {
                attribute.SpecificationAttributeOptionId = Convert.ToInt32(value);
                await _db.SaveChangesAsync();
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            // we give back the name to xeditable to overwrite the grid data in success event when a grid element got updated.
            return Json(new { success, message, name = attribute.SpecificationAttributeOption?.Name });
        }
    }
}
