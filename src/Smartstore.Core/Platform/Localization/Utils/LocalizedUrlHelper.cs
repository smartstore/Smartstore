using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Localization
{
    public class LocalizedUrlHelper
    {
        private string _pathBase;
        private string _path;
        private string _cultureCode;

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
                _cultureCode = null;
            }
        }

        /// <summary>
        /// Full path: PathBase + Path
        /// </summary>
        /// <returns></returns>
        public string FullPath
        {
            get
            {
                string absPath = PathBase.EnsureEndsWith('/') + Path;

                if (absPath.Length > 1 && absPath[0] != '/')
                {
                    absPath = "/" + absPath;
                }

                return absPath;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocalizedUrl()
        {
            return IsLocalizedUrl(out _);
        }

        public bool IsLocalizedUrl(out string cultureCode)
        {
            cultureCode = _cultureCode;

            if (cultureCode != null)
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
                cultureCode = _cultureCode = firstPart;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string StripCultureCode()
        {
            return StripCultureCode(out _);
        }

        public string StripCultureCode(out string cultureCode)
        {
            if (IsLocalizedUrl(out cultureCode))
            {
                Path = Path[cultureCode.Length..].TrimStart('/');
            }

            return Path;
        }

        public string PrependCultureCode(string cultureCode, bool safe = false)
        {
            Guard.NotEmpty(cultureCode, nameof(cultureCode));

            if (safe)
            {
                if (IsLocalizedUrl(out string currentCultureCode))
                {
                    if (cultureCode == currentCultureCode)
                    {
                        return Path;
                    }
                    else
                    {
                        StripCultureCode(out _);
                    }
                }
            }

            Path = (cultureCode + '/' + Path).TrimEnd('/');
            return Path;
        }
    }
}