using Microsoft.AspNetCore.Http;
using Smartstore.Collections;
using Smartstore.Net;

namespace Smartstore.Core.Web
{
    public class PreviewModeCookie : IPreviewModeCookie
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private MutableQueryCollection _currentValues;
        private bool _changed;

        public PreviewModeCookie(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            httpContextAccessor.HttpContext?.Response?.OnStarting(FlushCookie);
        }

        public string GetOverride(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            return GetCookie()[key].ToString().NullEmpty();
        }

        public void SetOverride(string key, string value)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(value, nameof(value));

            GetCookie().Add(key, value, true);
            _changed = true;
        }

        public bool RemoveOverride(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            var cookie = GetCookie();
            var exists = cookie.ContainsKey(key);
            if (exists)
            {
                cookie.Remove(key);
                _changed = true;
            }

            return exists;
        }

        public ICollection<string> AllOverrideKeys
        {
            get => GetCookie().Keys;
        }

        private MutableQueryCollection GetCookie()
        {
            if (_currentValues == null)
            {
                var cookieValue = _httpContextAccessor.HttpContext?.Request?.Cookies[CookieNames.PreviewModeOverride].NullEmpty();
                _currentValues = cookieValue == null
                    ? new MutableQueryCollection()
                    : new MutableQueryCollection('?' + cookieValue);
            }

            return _currentValues;
        }

        private Task FlushCookie()
        {
            if (_changed)
            {
                var cookieName = CookieNames.PreviewModeOverride;
                var response = _httpContextAccessor.HttpContext.Response;

                response.Cookies.Delete(cookieName);

                if (_currentValues != null && _currentValues.Count > 0)
                {
                    response.Cookies.Append(cookieName, _currentValues.ToString().TrimStart('?'), new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddMinutes(20),
                        HttpOnly = true,
                        IsEssential = true
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}
