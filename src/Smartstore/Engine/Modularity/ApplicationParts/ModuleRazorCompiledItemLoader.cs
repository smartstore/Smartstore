using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// A custom implementation of <see cref="RazorCompiledItemLoader"/> that
    /// prefixes the unrooted view path with "/Modules/{ModuleName}"
    /// </summary>
    internal class ModuleRazorCompiledItemLoader : RazorCompiledItemLoader
    {
        private readonly string _moduleName;
        private readonly string _modulePath;

        public ModuleRazorCompiledItemLoader(string moduleName)
        {
            _moduleName = moduleName;
            _modulePath = "/Modules/" + _moduleName;
        }

        protected override RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute)
        {
            return new CompiledItem(_modulePath, base.CreateItem(attribute));
        }

        class CompiledItem : RazorCompiledItem
        {
            public CompiledItem(string pathPrefix, RazorCompiledItem other)
            {
                Identifier = pathPrefix + other.Identifier;
                Kind = other.Kind;
                Metadata = other.Metadata;
                Type = other.Type;
            }

            public override string Identifier { get; }
            public override string Kind { get; }
            public override IReadOnlyList<object> Metadata { get; }
            public override Type Type { get; }
        }
    }
}
