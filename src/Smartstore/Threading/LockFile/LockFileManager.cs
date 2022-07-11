using System.Globalization;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Threading
{
    public class LockFileManager : ILockFileManager
    {
        // TODO: (core) Remove all lock files on app shutdown

        private readonly IFileSystem _root;
        private readonly AsyncLock _asyncLock = new();

        static LockFileManager()
        {
            Expiration = TimeSpan.FromMinutes(10);
        }

        public LockFileManager(IApplicationContext appContext)
        {
            _root = appContext.TenantRoot;
        }

        public static TimeSpan Expiration
        {
            get;
            private set;
        }

        public bool TryAcquireLock(string path, out ILockHandle lockHandle)
        {
            lockHandle = null;

            try
            {
                if (IsLockedInternal(path))
                {
                    return false;
                }

                lockHandle = new AsyncLockFile(_root, path, DateTime.UtcNow.ToString("u"), _asyncLock);
                return true;
            }
            catch
            {
                // An error occured while reading/creating the lock file
                return false;
            }
        }

        public async Task<AsyncOut<ILockHandle>> TryAcquireLockAsync(string path)
        {
            try
            {
                if (await IsLockedInternalAsync(path))
                {
                    return AsyncOut<ILockHandle>.Empty;
                }

                var lockFile = new AsyncLockFile(_root, path, DateTime.UtcNow.ToString("u"), _asyncLock);
                return new AsyncOut<ILockHandle>(true, lockFile);
            }
            catch
            {
                // An error occured while reading/creating the lock file
                return AsyncOut<ILockHandle>.Empty;
            }
        }

        public bool IsLocked(string subpath)
        {
            using (_asyncLock.Lock())
            {
                try
                {
                    return IsLockedInternal(subpath);
                }
                catch
                {
                    // An error occured while reading the file
                    return true;
                }
            }
        }

        public async Task<bool> IsLockedAsync(string subpath)
        {
            using (await _asyncLock.LockAsync())
            {
                try
                {
                    return await IsLockedInternalAsync(subpath);
                }
                catch
                {
                    // An error occured while reading the file
                    return true;
                }
            }
        }

        private bool IsLockedInternal(string path)
        {
            if (_root.FileExists(path))
            {
                var content = _root.ReadAllText(path);

                if (DateTime.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var creationUtc))
                {
                    // if expired the file is not removed
                    // it should be automatically as there is a finalizer in LockFile
                    // or the next taker can do it, unless it also fails, again
                    return creationUtc.ToUniversalTime().Add(Expiration) > DateTime.UtcNow;
                }
            }

            return false;
        }

        private async Task<bool> IsLockedInternalAsync(string path)
        {
            if (await _root.FileExistsAsync(path))
            {
                var content = await _root.ReadAllTextAsync(path);

                if (DateTime.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var creationUtc))
                {
                    // if expired the file is not removed
                    // it should be automatically as there is a finalizer in LockFile
                    // or the next taker can do it, unless it also fails, again
                    return creationUtc.ToUniversalTime().Add(Expiration) > DateTime.UtcNow;
                }
            }

            return false;
        }
    }
}