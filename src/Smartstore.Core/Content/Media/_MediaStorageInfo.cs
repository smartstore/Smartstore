using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.Core.Content.Media
{
    public interface IMediaStorageInfo
    {
        /// <summary>
        /// Gets the public base path to the media storage used to generate URLs for output HTML.
        /// e.g.: "media" (default), "static", "storage/files" etc. 
        /// </summary>
        string PublicPath { get; }

        /// <summary>
        /// Gets the storage path for media files
        /// either as app local relative path or as a fully qualified physical path to a shared location. E.g.:
        /// <list type="bullet">
        ///     <item>"Media" points to the subfolder named "Media" in the application root.</item>
        ///     <item>"F:\SharedMedia" points to a (mapped network) drive.</item>
        ///     <item>"\\Server1\SharedMedia" points to a network drive.</item>
        /// </list>
        /// <para>Default is <c>App_Data/Tenants/{Tenant}/Media</c></para>
        /// </summary>
        string StoragePath { get; }

        /// <summary>
        /// Gets the storage root physical path for media files.
        /// </summary>
        string RootPath { get; }

        bool IsStoragePathRooted { get; }

        bool IsCloudStorage { get; }
    }
    
    public class MediaStorageInfo : IMediaStorageInfo
    {
        //// /myshop
        //private string _pathBase;

        //// When public URL is outside of current app
        //private bool _isCloudStorage;

        private readonly IApplicationContext _appContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MediaStorageInfo(IApplicationContext appContext, IHttpContextAccessor httpContextAccessor)
        {
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;

            PublicPath = "media/";
            StoragePath = "/App_Data/Tenants/" + DataSettings.Instance.TenantName + "/Media";
            RootPath = Path.Combine(appContext.ContentRoot.Root, StoragePath.Replace('/', '\\'));
            IsStoragePathRooted = false;
            IsCloudStorage = false;
        }

        public string PublicPath { get; private set; }
        public string StoragePath { get; private set; }
        public string RootPath { get; private set; }
        public bool IsStoragePathRooted { get; private set; }
        public bool IsCloudStorage { get; private set; }

        //private static string GetRootPath(IApplicationContext appContext)
        //{
        //    var storagePath =
        //        appContext.AppConfiguration.MediaStoragePath.NullEmpty() ??
        //        "\\App_Data\\Tenants\\" + DataSettings.Instance.TenantName + "\\Media";

        //    if (storagePath == null)
        //    {
        //        return Path.Combine(appContext.TenantRoot.Root, "Media");
        //    }

        //    return storagePath;
        //}
    }
}
