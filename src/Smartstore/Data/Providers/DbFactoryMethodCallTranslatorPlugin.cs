using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;

namespace Smartstore.Data.Providers
{
    public class DbFactoryMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {
        public DbFactoryMethodCallTranslatorPlugin(IServiceProvider services)
        {
            Translators = new IMethodCallTranslator[]
            {
                new DbFactoryFunctionsTranslator(services),
            };
        }

        public IEnumerable<IMethodCallTranslator> Translators { get; }
    }
}
