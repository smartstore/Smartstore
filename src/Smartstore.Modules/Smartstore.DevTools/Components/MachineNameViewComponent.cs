using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Web.Components;

namespace Smartstore.DevTools.Components
{
    public class MachineNameViewComponent : SmartViewComponent
    {
        private readonly ICommonServices _services;
        private readonly ProfilerSettings _profilerSettings;

        public MachineNameViewComponent(ICommonServices services, ProfilerSettings profilerSettings)
        {
            _services = services;
            _profilerSettings = profilerSettings;
        }

        public IViewComponentResult Invoke()
        {
            if (!_profilerSettings.DisplayMachineName)
            {
                return Empty();
            }

            if (!_services.WorkContext.CurrentCustomer.IsAdmin() && !HttpContext.Connection.IsLocal())
            {
                return Empty();
            }

            var css = @"<style>
	            .devtools-machinename {
		            position: fixed;
		            right: 0;
		            bottom: 0;
		            z-index: 999999;
		            background: #333;
		            color: #fff;
		            font-size: 0.9em;
		            font-weight: 600;
		            padding: 0.3em 1em;
		            opacity: 0.92;
		            border: 1px solid #fff;
                    border-right-width: 0;
                    border-bottom-width: 0;
		            border-top-left-radius: 4px;
	            }
            </style>";

            var html = $"<div class='devtools-machinename'>{_services.ApplicationContext.RuntimeInfo.EnvironmentIdentifier}</div>";
            return HtmlContent(new HtmlString(css + html));
        }
    }
}