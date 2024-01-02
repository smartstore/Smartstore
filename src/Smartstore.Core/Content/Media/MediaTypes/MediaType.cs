namespace Smartstore.Core.Content.Media
{
    public class MediaType : IEquatable<MediaType>
    {
        private readonly static IDictionary<string, string[]> _defaultExtensionsMap = new Dictionary<string, string[]>
        {
            ["image"] = ["png", "jpg", "jpeg", "jfif", "gif", "webp", "bmp", "svg", "ico"],
            ["video"] = ["mp4", "m4v", "mkv", "wmv", "avi", "asf", "mpg", "mpeg", "webm", "flv", "ogv", "mov", "3gp"],
            ["audio"] = ["mp3", "wav", "wma", "aac", "flac", "oga", "wav", "m4a", "ogg"],
            ["document"] = ["pdf", "doc", "docx", "ppt", "pptx", "pps", "ppsx", "docm", "odt", "ods", "dot", "dotx", "dotm", "psd", "xls", "xlsx", "rtf"],
            ["text"] = ["txt", "xml", "csv", "htm", "html", "json", "css", "js"],
            ["bin"] = []
        };

        private readonly static IDictionary<string, MediaType> _map = new Dictionary<string, MediaType>(StringComparer.OrdinalIgnoreCase);

        public readonly static MediaType Image = new("image", _defaultExtensionsMap["image"]);
        public readonly static MediaType Video = new("video", _defaultExtensionsMap["video"]);
        public readonly static MediaType Audio = new("audio", _defaultExtensionsMap["audio"]);
        public readonly static MediaType Document = new("document", _defaultExtensionsMap["document"]);
        public readonly static MediaType Text = new("text", _defaultExtensionsMap["text"]);
        public readonly static MediaType Binary = new("bin", _defaultExtensionsMap["bin"]);

        protected MediaType(string name, params string[] defaultExtensions)
        {
            Guard.NotEmpty(name);

            Name = name;
            DefaultExtensions = defaultExtensions.OrderBy(x => x).ToArray();

            _map[name] = this;
        }

        public string Name { get; private set; }

        public string[] DefaultExtensions { get; private set; }

        public static IEnumerable<string> AllExtensions
            => _defaultExtensionsMap.SelectMany(x => x.Value);

        public override string ToString()
            => Name;

        public static implicit operator string(MediaType obj)
            => obj?.Name;

        public static implicit operator MediaType(string obj)
            => GetMediaType(obj);

        internal static MediaType GetMediaType(string name)
        {
            if (name.IsEmpty())
            {
                return null;
            }

            if (_map.TryGetValue(name, out var instance))
            {
                return instance;
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((MediaType)obj);
        }

        public bool Equals(MediaType other)
            => string.Equals(Name, other.Name);

        public override int GetHashCode()
            => Name?.GetHashCode() ?? 0;
    }
}
