#nullable enable

using System.ComponentModel;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Content.Media
{
    [TypeConverter(typeof(StringBackedTypeConverter<MediaType>))]
    public readonly partial struct MediaType : IStringBacked<MediaType>, IEquatable<MediaType>
    {
        private readonly static Dictionary<string, string[]> _defaultExtensionsMap = new()
        {
            ["image"] = ["png", "jpg", "jpeg", "jfif", "gif", "webp", "bmp", "avif", "svg", "ico"],
            ["video"] = ["mp4", "m4v", "mkv", "wmv", "avi", "asf", "mpg", "mpeg", "webm", "flv", "ogv", "mov", "3gp"],
            ["audio"] = ["mp3", "wav", "wma", "aac", "flac", "oga", "wav", "m4a", "ogg"],
            ["document"] = ["pdf", "doc", "docx", "ppt", "pptx", "pps", "ppsx", "docm", "odt", "ods", "dot", "dotx", "dotm", "psd", "xls", "xlsx", "rtf"],
            ["text"] = ["txt", "xml", "csv", "htm", "html", "json", "css", "js"],
            ["bin"] = []
        };

        private readonly static Dictionary<string, MediaType> _map = new(StringComparer.OrdinalIgnoreCase);

        public readonly static MediaType Image = new("image", _defaultExtensionsMap["image"]);
        public readonly static MediaType Video = new("video", _defaultExtensionsMap["video"]);
        public readonly static MediaType Audio = new("audio", _defaultExtensionsMap["audio"]);
        public readonly static MediaType Document = new("document", _defaultExtensionsMap["document"]);
        public readonly static MediaType Text = new("text", _defaultExtensionsMap["text"]);
        public readonly static MediaType Binary = new("bin", _defaultExtensionsMap["bin"]);

        internal MediaType(string name, params string[] defaultExtensions)
        {
            Guard.NotEmpty(name);

            Name = name;
            DefaultExtensions = defaultExtensions.OrderBy(x => x).ToArray();

            _map[name] = this;
        }

        public string Name { get; }

        public string[] DefaultExtensions { get; }

        public static IEnumerable<string> AllExtensions
            => _defaultExtensionsMap.SelectMany(x => x.Value);

        public override string? ToString()
            => Name;

        public static implicit operator string?(MediaType obj)
            => obj.Name;

        public static implicit operator MediaType?(string? obj)
            => FromString(obj);

        public static MediaType? FromString(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_map.TryGetValue(name, out var instance))
            {
                return instance;
            }

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
            => obj is MediaType other && Equals(other);

        public bool Equals(MediaType other)
            => Name?.Equals(other.Name) ?? false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
            => Name?.GetHashCode() ?? 0;
    }
}
