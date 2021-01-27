using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore.Web.Modelling
{
    public sealed class LocalizedDisplayNameAttribute : DisplayNameAttribute, IModelAttribute
    {
        const string AttributeName = nameof(LocalizedDisplayNameAttribute);
        
        public LocalizedDisplayNameAttribute(string resourceKey, [CallerMemberName] string propertyName = null)
            : base(resourceKey)
        {
            ResourceKey = resourceKey;
            CallerPropertyName = propertyName;
        }

        public string ResourceKey { get; internal set; }
        internal string CallerPropertyName { get; init; }

        public override string DisplayName
        {
            get
            {
                var langId = EngineContext.Current.ResolveService<IWorkContext>().WorkingLanguage.Id;
                var value = EngineContext.Current.ResolveService<ILocalizationService>().GetResource(ResourceKey, langId, returnEmptyIfNotFound: true);

                if (value.IsEmpty() && CallerPropertyName.HasValue())
                {
                    value = CallerPropertyName.SplitPascalCase();
                }

                return value;
            }
        }

        public string Name => AttributeName;
    }
}
