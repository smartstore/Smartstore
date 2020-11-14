using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Localization
{
    public class LocalizedUrlHelper
    {
        private string _pathBase;
        private string _path;
        private string _seoCode;

        public LocalizedUrlHelper(HttpRequest httpRequest)
            : this(httpRequest.PathBase.Value, httpRequest.Path.Value)
        {
            Guard.NotNull(httpRequest, nameof(httpRequest));
        }

        public LocalizedUrlHelper(string pathBase, string path)
        {
            Guard.NotNull(pathBase, nameof(pathBase));
            Guard.NotNull(path, nameof(path));

            PathBase = pathBase;
            Path = path.TrimStart('/');
        }

        public string PathBase 
        {
            get => _pathBase;
            private set
            {
                _pathBase = value;
            }
        }

        public string Path
        {
            get => _path;
            private set
            {
                _path = value;
                _seoCode = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocalizedUrl()
        {
            return IsLocalizedUrl(out _);
        }

        public bool IsLocalizedUrl(out string seoCode)
        {
            seoCode = _seoCode;

            if (seoCode != null)
            {
                return true;
            }

            string firstPart = Path;

            if (firstPart.IsEmpty())
            {
                return false;
            }

            int firstSlash = firstPart.IndexOf('/');

            if (firstSlash > 0)
            {
                firstPart = firstPart.Substring(0, firstSlash);
            }

            if (CultureHelper.IsValidCultureCode(firstPart))
            {
                seoCode = _seoCode = firstPart;
                return true;
            }

            return false;
        }

        public string StripSeoCode()
        {
            if (IsLocalizedUrl(out var seoCode))
            {
                Path = Path[seoCode.Length..].TrimStart('/');
            }

            return Path;
        }

        public string PrependSeoCode(string seoCode, bool safe = false)
        {
            Guard.NotEmpty(seoCode, nameof(seoCode));

            if (safe)
            {
                if (IsLocalizedUrl(out string currentSeoCode))
                {
                    if (seoCode == currentSeoCode)
                    {
                        return Path;
                    }
                    else
                    {
                        StripSeoCode();
                    }
                }
            }

            Path = (seoCode + '/' + Path).TrimEnd('/');
            return Path;
        }

        public string GetAbsolutePath()
        {
            string absPath = PathBase.EnsureEndsWith('/') + Path;

            if (absPath.Length > 1 && absPath[0] != '/')
            {
                absPath = "/" + absPath;
            }

            return absPath;
        }
    }
}