using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    public interface IFilterConstraint : IFilterFactory
    {
        bool Match(ActionContext context);
    }
}
