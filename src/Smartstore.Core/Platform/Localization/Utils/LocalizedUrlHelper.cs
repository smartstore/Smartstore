#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Localization
{
    public class LocalizedUrlHelper
    {
        private readonly string _pathBase;
        private string _path;
        private string? _cultureCode;

        public LocalizedUrlHelper(HttpRequest httpRequest)
            : this(httpRequest.PathBase.Value!, httpRequest.Path.Value!)
        {
            Guard.NotNull(httpRequest);
        }

        public LocalizedUrlHelper(string pathBase, string path)
        {
            Guard.NotNull(pathBase);
            Guard.NotNull(path);

            _pathBase = pathBase;
            _path = path.TrimStart('/');
        }

        public string PathBase
        {
            get => _pathBase;
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
                var absPath = PathBase.EnsureEndsWith('/') + Path;

                if (absPath.Length > 1 && absPath[0] != '/')
                {
                    absPath = "/" + absPath;
                }

                return absPath;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLocalizedUrl()
            => IsLocalizedUrl(out _);

        public bool IsLocalizedUrl([MaybeNullWhen(false)] out string? cultureCode)
        {
            cultureCode = _cultureCode;

            if (cultureCode != null)
            {
                return true;
            }

            var firstPart = _path;

            if (firstPart.IsEmpty())
            {
                return false;
            }

            int firstSlash = firstPart.IndexOf('/');

            if (firstSlash > 0)
            {
                firstPart = firstPart[..firstSlash];
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
            => StripCultureCode(out _);

        public string StripCultureCode(out string? cultureCode)
        {
            if (IsLocalizedUrl(out cultureCode))
            {
                Path = Path[cultureCode!.Length..].TrimStart('/');
            }

            return Path;
        }

        public string PrependCultureCode(string cultureCode, bool safe = false)
        {
            Guard.NotEmpty(cultureCode);

            if (safe)
            {
                if (IsLocalizedUrl(out var currentCultureCode))
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