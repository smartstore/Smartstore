using System;
using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.News.Models.Public
{
    public partial class HomepageNewsItemsModel : ModelBase, ICloneable
    {
        public List<PublicNewsItemModel> NewsItems { get; set; } = new();

        public object Clone()
        {
            // We use a shallow copy (deep clone is not required here)
            return this.MemberwiseClone();
        }
    }
}
