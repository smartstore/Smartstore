using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;

namespace Smartstore.Web.Components
{
    public class BreadcrumbViewComponent : SmartViewComponent
    {
        private readonly IBreadcrumb _breadcrumb;

        public BreadcrumbViewComponent(IBreadcrumb breadcrumb)
        {
            _breadcrumb = breadcrumb;
        }

        public IViewComponentResult Invoke(IEnumerable<MenuItem> trail = null)
        {
            trail ??= _breadcrumb.Trail;

            if (trail == null || !trail.Any())
            {
                return Empty();
            }

            return View(trail);
        }
    }
}
