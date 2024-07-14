﻿using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaTypeResolver(ICacheManager cache, MediaSettings mediaSettings) : AsyncDbSaveHook<Setting>, IMediaTypeResolver
    {
        const string MapCacheKey = "media:exttypemap";

        private readonly ICacheManager _cache = cache;
        private readonly MediaSettings _mediaSettings = mediaSettings;

        private static readonly HashSet<string> _mapInvalidatorSettingKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            TypeHelper.NameOf<MediaSettings>(x => x.ImageTypes, true),
            TypeHelper.NameOf<MediaSettings>(x => x.VideoTypes, true),
            TypeHelper.NameOf<MediaSettings>(x => x.AudioTypes, true),
            TypeHelper.NameOf<MediaSettings>(x => x.DocumentTypes, true),
            TypeHelper.NameOf<MediaSettings>(x => x.TextTypes, true),
            TypeHelper.NameOf<MediaSettings>(x => x.BinTypes, true)
        };

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Invalidation Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var setting = entry.Entity as Setting;
            if (_mapInvalidatorSettingKeys.Contains(setting.Name))
            {
                _cache.Remove(MapCacheKey);
            }

            return Task.FromResult(HookResult.Ok);
        }

        #endregion

        public virtual MediaType Resolve(string extension, string mimeType = null)
        {
            if (extension.IsEmpty() && mimeType.HasValue())
            {
                extension = MimeTypes.MapMimeTypeToExtension(mimeType);
            }

            var map = GetExtensionMediaTypeMap();

            string mediaType = null;
            if (extension.HasValue() && map.TryGetValue(extension.TrimStart('.').ToLower(), out mediaType))
            {
                return mediaType;
            }

            if (mimeType.HasValue())
            {
                // Get first mime token (e.g. IMAGE/png, VIDEO/mp4 etc.)
                var mimeGroup = mimeType.Split('/')[0];
                mediaType = MediaType.GetMediaType(mimeGroup);
            }

            return (MediaType)mediaType ?? MediaType.Binary;
        }

        public virtual IEnumerable<string> ParseTypeFilter(string typeFilter)
        {
            if (typeFilter.IsEmpty() || typeFilter == "*")
            {
                return GetExtensionMediaTypeMap().Keys;
            }
            else
            {
                return ParseTypeFilter(typeFilter.SplitSafe(',').ToArray());
            }
        }

        public virtual IEnumerable<string> ParseTypeFilter(string[] typeFilter)
        {
            if (typeFilter == null || typeFilter.Length == 0)
                return Enumerable.Empty<string>();

            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filter in typeFilter.Select(x => x.Trim()))
            {
                if (filter[0] == '.')
                {
                    extensions.Add(filter[1..]);
                }
                else
                {
                    extensions.AddRange(GetExtensionMediaTypeMap().Where(x => filter == "*" || x.Value == filter).Select(x => x.Key));
                }
            }

            return extensions;
        }

        public IReadOnlyDictionary<string, string> GetExtensionMediaTypeMap()
        {
            return _cache.Get(MapCacheKey, () =>
            {
                var map = new Dictionary<string, string>();

                AddExtensionsToMap(_mediaSettings.ImageTypes, MediaType.Image);
                AddExtensionsToMap(_mediaSettings.VideoTypes, MediaType.Video);
                AddExtensionsToMap(_mediaSettings.AudioTypes, MediaType.Audio);
                AddExtensionsToMap(_mediaSettings.DocumentTypes, MediaType.Document);
                AddExtensionsToMap(_mediaSettings.TextTypes, MediaType.Text);
                AddExtensionsToMap(_mediaSettings.BinTypes, MediaType.Binary);

                return map;

                void AddExtensionsToMap(string extensions, MediaType forType)
                {
                    var arr = extensions.EmptyNull()
                        .Replace(Environment.NewLine, " ")
                        .ToLower()
                        .Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    if (arr.Length == 0)
                    {
                        arr = forType.DefaultExtensions;
                    }

                    foreach (var ext in arr)
                    {
                        if (map.TryGetValue(ext, out var typeName) && typeName != forType.Name)
                        {
                            Logger.Warn($"Cannot assign file extension to type '{forType.Name}' because it is already assigned to '{typeName}'.");
                        }
                        else
                        {
                            map[ext] = forType.Name;
                        }
                    }
                }
            });
        }
    }
}
