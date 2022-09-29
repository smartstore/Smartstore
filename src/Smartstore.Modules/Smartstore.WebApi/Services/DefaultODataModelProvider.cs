using System.IO;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Engine;

namespace Smartstore.Web.Api
{
    internal class DefaultODataModelProvider : ODataModelProviderBase
    {
        public override void Build(ODataModelBuilder builder, int version)
        {
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Discount>("Discounts");
        }

        public override Stream GetXmlCommentsStream(IApplicationContext appContext)
            => GetDefaultXmlCommentsStream(appContext, Module.SystemName);
    }
}
