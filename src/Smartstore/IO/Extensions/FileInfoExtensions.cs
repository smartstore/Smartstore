namespace Smartstore
{
    public static class FileInfoExtensions
    {
        public static void WaitForUnlockAndExecute(this FileInfo file, Action<FileInfo> action)
        {
            Guard.NotNull(file, nameof(file));

            try
            {
                action(file);
            }
            catch (IOException)
            {
                if (!WaitForUnlock(file, 250))
                {
                    throw;
                }

                action(file);
            }
        }

        public static async Task WaitForUnlockAndExecuteAsync(this FileInfo file, Action<FileInfo> action)
        {
            Guard.NotNull(file, nameof(file));

            try
            {
                action(file);
            }
            catch (IOException)
            {
                if (!await WaitForUnlockAsync(file, 250))
                {
                    throw;
                }

                action(file);
            }
        }

        public static bool WaitForUnlock(this FileInfo file, int timeoutMs = 1000)
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

                    Task.Delay(wait).Wait();
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> WaitForUnlockAsync(this FileInfo file, int timeoutMs = 1000)
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

                    await Task.Delay(wait);
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
