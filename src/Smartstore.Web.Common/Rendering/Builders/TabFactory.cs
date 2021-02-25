using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering.Builders
{
    public class TabFactory
    {
        public TabFactory(TabStripTagHelper tabStrip)
        {
            Guard.NotNull(tabStrip, nameof(tabStrip));
            TabStrip = tabStrip;
        }

        internal TabStripTagHelper TabStrip { get; }
    }
}
