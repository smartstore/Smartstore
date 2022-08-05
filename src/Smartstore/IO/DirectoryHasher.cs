using System.Globalization;
using System.Text;
using Smartstore.Data;
using Smartstore.Utilities;

namespace Smartstore.IO
{
    public class DirectoryHasher
    {
        private readonly IDirectory _source;
        private readonly string _searchPattern;
        private readonly bool _deep;
        private readonly IDirectory _storageDir;

        private int? _lastHash;
        private int? _currentHash;
        private string _lookupKey;

        private static readonly IDirectory _defaultStorageDir;

        static DirectoryHasher()
        {
            _defaultStorageDir = DataSettings.Instance.TenantRoot.GetDirectory("Hash");
            _defaultStorageDir.Create();
        }

        internal DirectoryHasher(IDirectory source, IDirectory storageDir, string searchPattern = "*", bool deep = false)
        {
            Guard.NotNull(source, nameof(source));

            _source = source;
            _storageDir = storageDir ?? _defaultStorageDir;
            _searchPattern = searchPattern;
            _deep = deep;
        }

        public bool HasChanged
            => LastHash != CurrentHash;

        public int? LastHash
        {
            get
            {
                if (_lastHash == null)
                {
                    _lastHash = ReadLastHash();
                }

                return _lastHash == -1 ? (int?)null : _lastHash.Value;
            }
        }

        public int CurrentHash
            => ((int?)(_currentHash ??= ComputeHash())).Value;

        public string LookupKey
            => _lookupKey ??= BuildLookupKey();

        public void Refresh()
        {
            _currentHash = null;
        }

        //public void Reset()
        //{
        //    _lastHash = -1;

        //    var path = Path.Combine(_storagePath, LookupKey + ".hash");
        //    if (File.Exists(path))
        //    {
        //        File.Delete(path);
        //    }
        //}

        public void Persist()
        {
            if (LastHash == CurrentHash)
                return;

            var fs = _storageDir.FileSystem;
            var path = PathUtility.Join(_storageDir.SubPath, LookupKey + ".hash");
            fs.WriteAllText(path, CurrentHash.ToStringInvariant(), Encoding.UTF8);
            _lastHash = CurrentHash;
        }

        protected virtual int ComputeHash()
        {
            if (_source.Exists)
            {
                return HashCodeCombiner.Start().Add(_source);
            }

            return 0;
        }

        protected virtual int ReadLastHash()
        {
            var fs = _storageDir.FileSystem;
            var path = PathUtility.Join(_storageDir.SubPath, LookupKey + ".hash");
            var hash = fs.FileExists(path)
                ? ConvertHash(fs.ReadAllText(path, Encoding.UTF8))
                : -1;

            return hash;
        }

        protected virtual string BuildLookupKey()
        {
            var key = PathUtility.MakeRelativePath(CommonHelper.ContentRoot.Root, _source.PhysicalPath, '_').ToLower();

            if (_deep)
                key += "_d";

            if (_searchPattern.HasValue() && _searchPattern != "*")
                key += "_" + PathUtility.SanitizeFileName(_searchPattern.ToLower(), "x");

            return key.Trim(PathUtility.PathSeparators);
        }

        private static int ConvertHash(string val)
        {
            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var outVal))
            {
                return outVal;
            }

            return 0;
        }
    }
}