using System.Threading;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore.Threading
{
    public class AsyncLockFile : Disposable, ILockFile
    {
        private readonly string _path;
        private readonly string _content;
        private readonly IFileSystem _directory;
        private readonly AsyncLock _asyncLock;

        private bool _released;

        public AsyncLockFile(IFileSystem directory, string path, string content, AsyncLock asyncLock)
        {
            _directory = directory;
            _content = content;
            _asyncLock = asyncLock;
            _path = path;

            // Create the physical lock file
            _directory.WriteAllText(_path, content);
        }

        protected override void OnDispose(bool disposing)
        {
            Release();
        }

        protected override ValueTask OnDisposeAsync(bool disposing)
        {
            return new ValueTask(ReleaseAsync());
        }

        public void Release()
        {
            using (_asyncLock.Lock())
            {
                var lockFile = _directory.GetFile(_path);

                if (_released || !lockFile.Exists)
                {
                    return;
                }

                _released = true;

                // Check it has not been granted in the meantime
                var current = _directory.ReadAllText(_path);
                if (current == _content)
                {
                    _directory.TryDeleteFile(_path);
                }
            }
        }

        public async Task ReleaseAsync()
        {
            using (await AsyncLock.KeyedAsync($"AsyncLockFile.{_path}").ConfigureAwait(false))
            {
                var lockFile = await _directory.GetFileAsync(_path);

                if (_released || !lockFile.Exists)
                {
                    return;
                }

                _released = true;

                // Check it has not been granted in the meantime
                var current = await _directory.ReadAllTextAsync(_path).ConfigureAwait(false);
                if (current == _content)
                {
                    await _directory.TryDeleteFileAsync(_path).ConfigureAwait(false);
                }
            }
        }
    }
}