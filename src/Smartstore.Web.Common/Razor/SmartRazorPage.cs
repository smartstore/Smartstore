using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Web.Razor
{
    public abstract class SmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class SmartRazorPage<TModel> : RazorPage<TModel>
    {
        [RazorInject]
        protected IDisplayHelper Display { get; set; }

        [RazorInject]
        protected Localizer T { get; set; }

        [RazorInject]
        protected IWorkContext WorkContext { get; set; }

        [RazorInject]
        protected IEventPublisher EventPublisher { get; set; }

        [RazorInject]
        protected IApplicationContext ApplicationContext { get; set; }

        [RazorInject]
        protected IPageAssetBuilder Assets { get; set; }

        [RazorInject]
        protected IUserAgent UserAgent { get; set; }

        [RazorInject]
        protected ILinkResolver LinkResolver { get; set; }

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
