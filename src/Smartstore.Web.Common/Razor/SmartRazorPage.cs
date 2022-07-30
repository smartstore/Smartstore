using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Events;

namespace Smartstore.Web.Razor
{
    public abstract class SmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class SmartRazorPage<TModel> : RazorPage<TModel>
    {
        private IDisplayHelper _display;
        private Localizer _localizer;
        private IWorkContext _workContext;
        private IEventPublisher _eventPublisher;
        private IApplicationContext _appContext;
        private IPageAssetBuilder _assets;
        private IUserAgent _userAgent;
        private ILinkResolver _linkResolver;
        private ICommonServices _services;

        public SmartRazorPage()
        {
        }

        protected HttpRequest Request
        {
            get => Context.Request;
        }

        protected IDisplayHelper Display
        {
            get => _display ??= base.Context.RequestServices.GetRequiredService<IDisplayHelper>();
        }

        protected Localizer T
        {
            get => _localizer ??= base.Context.RequestServices.GetRequiredService<Localizer>();
        }

        protected IWorkContext WorkContext
        {
            get => _workContext ??= base.Context.RequestServices.GetRequiredService<IWorkContext>();
        }

        protected IEventPublisher EventPublisher
        {
            get => _eventPublisher ??= base.Context.RequestServices.GetRequiredService<IEventPublisher>();
        }

        protected IApplicationContext ApplicationContext
        {
            get => _appContext ??= base.Context.RequestServices.GetRequiredService<IApplicationContext>();
        }

        protected IPageAssetBuilder Assets
        {
            get => _assets ??= base.Context.RequestServices.GetRequiredService<IPageAssetBuilder>();
        }

        protected IUserAgent UserAgent
        {
            get => _userAgent ??= base.Context.RequestServices.GetRequiredService<IUserAgent>();
        }

        protected ILinkResolver LinkResolver
        {
            get => _linkResolver ??= base.Context.RequestServices.GetRequiredService<ILinkResolver>();
        }

        protected ICommonServices CommonServices
        {
            get => _services ??= base.Context.RequestServices.GetRequiredService<ICommonServices>();
        }

        /// <summary>
        /// Resolves a service from scoped service container.
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        protected T Resolve<T>() where T : notnull
        {
            return Context.RequestServices.GetService<T>();
        }

        #region Metadata

        public bool HasMetadata(string name)
        {
            return TryGetMetadata<object>(name, out _);
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <returns>Result</returns>
        public T GetMetadata<T>(string name)
        {
            TryGetMetadata<T>(name, out var value);
            return value;
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <param name="defaultValue">The default value to return if item does not exist.</param>
        /// <returns>Result</returns>
        public T GetMetadata<T>(string name, T defaultValue)
        {
            if (TryGetMetadata<T>(name, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <returns><c>true</c> if the entry exists in any of the dictionaries, <c>false</c> otherwise</returns>
        public bool TryGetMetadata<T>(string name, out T value)
        {
            value = default;

            var exists = ViewData.TryGetValue(name, out var raw);
            if (!exists)
            {
                exists = ViewData.ModelMetadata?.AdditionalValues?.TryGetValue(name, out raw) == true;
            }

            if (raw != null)
            {
                value = raw.Convert<T>();
            }

            return exists;
        }

        #endregion
    }
}
