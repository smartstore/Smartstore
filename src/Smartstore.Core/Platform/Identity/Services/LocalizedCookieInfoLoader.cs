using System.Linq.Dynamic.Core;
using Autofac;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    internal static class LocalizedCookieInfoLoader
    {
        internal static Task<IList<dynamic>> LoadLocalizedCookieInfos(ILifetimeScope scope, SmartDbContext db)
        {
            var task = Task.FromResult<IList<dynamic>>(new List<dynamic>());
            var cookieManager = scope.Resolve<ICookieConsentManager>();
            var cookieInfos = cookieManager.GetUserCookieInfos(false);

            if (cookieInfos.Count == 0)
            {
                return task;
            }

            foreach (var info in cookieInfos)
            {
                // Create dynamic class in the shape: new { Id, KeyGroup = "CookieInfo", Name, Description }
                var dynamicProps = new List<DynamicProperty>
                {
                    new("Id", typeof(int)),
                    new("KeyGroup", typeof(string)),
                    new(nameof(CookieInfo.Name), typeof(string)),
                    new(nameof(CookieInfo.Description), typeof(string))
                };
                var dynamicClassType = DynamicClassFactory.CreateType(dynamicProps, true);

                var obj = (DynamicClass)Activator.CreateInstance(dynamicClassType, new object[]
                {
                    (info as ILocalizedEntity).Id,
                    (info as INamedEntity).GetEntityName(),
                    info.Name,
                    info.Description
                });

                task.Result.Add(obj);
            }

            return task;
        }
    }
}
