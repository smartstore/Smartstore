using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore.Web.Common.Mvc.Razor
{
    public abstract class SmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class SmartRazorPage<TModel> : RazorPage<TModel>
    {
        [RazorInject]
        public Localizer T { get; set; }

        [RazorInject]
        public IWorkContext WorkContext { get; set; }

        [RazorInject]
        public IApplicationContext ApplicationContext { get; set; }
    }
}
