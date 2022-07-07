using Smartstore.IO;

namespace Smartstore.Threading
{
    public class AsyncLockFile : Disposable, ILockHandle
    {
        private readonly string _path;
        private readonly string _content;
        private readonly IFileSystem _fs;
        private readonly AsyncLock _asyncLock;

        private bool _released;

        public AsyncLockFile(IFileSystem fs, string path, string content, AsyncLock asyncLock)
        {
            _fs = fs;
            _content = content;
            _asyncLock = asyncLock;
            _path = path;

            // Create the physical lock file
            _fs.WriteAllText(_path, content);
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
            if (_released)
            {
                return;
            }

            using (_asyncLock.Lock())
            {
                var lockFile = _fs.GetFile(_path);

                if (_released || !lockFile.Exists)
                {
                    return;
                }

                _released = true;

                // Check it has not been granted in the meantime
                var current = lockFile.ReadAllText();
                if (current == _content)
                {
                    lockFile.Delete();
                }
            }
        }

        public async Task ReleaseAsync()
        {
            if (_released)
            {
                return;
            }

            using (await AsyncLock.KeyedAsync($"AsyncLockFile.{_path}"))
            {
                var lockFile = await _fs.GetFileAsync(_path);

                if (_released || !lockFile.Exists)
                {
                    return;
                }

                _released = true;

                // Check it has not been granted in the meantime
                var current = await lockFile.ReadAllTextAsync();
                if (current == _content)
                {
                    await lockFile.DeleteAsync();
                }
            }
        }
    }
}