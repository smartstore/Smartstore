using System.IO;
using System.Reflection;
using Autofac.Core;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Engine;

namespace Smartstore.Web.Api
{
    internal class DefaultODataModelProvider : IODataModelProvider
    {
        public void Build(ODataModelBuilder builder, int version)
        {
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Discount>("Discounts");
        }

        public string GetXmlCommentsFilePath(IApplicationContext appContext, int version)
        {
            // // TODO: (mg) (core) Why PHYSICAL path?
            return appContext.ModuleCatalog.GetModuleByName(Module.SystemName)?.XmlCommentsPath;
        }
    }
}
