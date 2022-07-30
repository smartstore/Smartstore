namespace Smartstore
{
    public static class FileInfoExtensions
    {
        public static void WaitForUnlockAndExecute(this FileInfo file, Action<FileInfo> action)
            => WaitForUnlockAndExecuteInternal(file, action, false).Await();

        public static Task WaitForUnlockAndExecuteAsync(this FileInfo file, Action<FileInfo> action)
            => WaitForUnlockAndExecuteInternal(file, action, true);

        private static async Task WaitForUnlockAndExecuteInternal(FileInfo file, Action<FileInfo> action, bool async)
        {
            Guard.NotNull(file, nameof(file));

            try
            {
                action(file);
            }
            catch (IOException)
            {
                var succeeded = async
                    ? await WaitForUnlockInternal(file, 250, true)
                    : WaitForUnlockInternal(file, 250, false).Await();

                if (!succeeded)
                {
                    throw;
                }

                action(file);
            }
        }


        public static bool WaitForUnlock(this FileInfo file, int timeoutMs = 1000)
            => WaitForUnlockInternal(file, timeoutMs, false).Await();

        public static Task<bool> WaitForUnlockAsync(this FileInfo file, int timeoutMs = 1000)
            => WaitForUnlockInternal(file, timeoutMs, true);

        private static async Task<bool> WaitForUnlockInternal(FileInfo file, int timeoutMs, bool async)
        {
            Guard.NotNull(file, nameof(file));

            var wait = TimeSpan.FromMilliseconds(50);
            var attempts = Math.Floor(timeoutMs / wait.TotalMilliseconds);

            try
            {
                for (var i = 0; i < attempts; i++)
                {
                    if (!IsFileLocked(file))
                    {
                        return true;
                    }

                    if (async)
                    {
                        await Task.Delay(wait);
                    }
                    else
                    {
                        Task.Delay(wait).Wait();
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }


        public static bool IsFileLocked(this FileInfo file)
        {
            if (file == null)
                return false;

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                // The file is unavailable because it is:
                // still being written to
                // or being processed by another thread
                // or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            // File is not locked
            return false;
        }
    }
}
