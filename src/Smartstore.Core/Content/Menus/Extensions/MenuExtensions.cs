using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Content.Menus
{
    public static class MenuExtensions
    {
        public static IEnumerable<string> GetWidgetZones(this Menu menu)
        {
            Guard.NotNull(menu, nameof(menu));

            if (menu.WidgetZone.IsEmpty())
            {
                return Enumerable.Empty<string>();
            }

            return menu.WidgetZone.EmptyNull().Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
        }
    }
}
