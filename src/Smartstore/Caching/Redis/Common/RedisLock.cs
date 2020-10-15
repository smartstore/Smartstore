using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public sealed class RedisLock
    {
        private readonly static TimeSpan DefaultExpiration = TimeSpan.FromSeconds(30);
        private readonly static TimeSpan SleepTime = TimeSpan.FromMilliseconds(50);

        public RedisLock(IDatabase database, string key)
        {
            Database = database;
            Token = Environment.MachineName;
            Key = "lock:" + key;
        }

        public IDatabase Database { get; private set; }
        public string Key { get; private set; }
        public string Token { get; private set; }

        /// <summary>
        /// The defualt expiration time is 30 seconds.
        /// </summary>
        public IDisposable Lock(TimeSpan? expiration = null)
        {
            WaitForLock(expiration ?? DefaultExpiration);
            return new Releaser(this);
        }

        /// <summary>
        /// The defualt expiration time is 30 seconds.
        /// </summary>
        public Task<IDisposable> LockAsync(TimeSpan? expiration = null, CancellationToken cancelToken = default)
        {
            var wait = WaitForLockAsync(expiration ?? DefaultExpiration);
            return wait.IsCompleted
                ? Task.FromResult(new Releaser(this) as IDisposable)
                : wait.ContinueWith((t, s) => new Releaser((RedisLock)s) as IDisposable,
                    this,
                    cancelToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private TimeSpan WaitForLock(TimeSpan expiration)
        {
            var totalTime = TimeSpan.Zero;
            var acquired = false;

            while (!acquired && totalTime < expiration)
            {
                acquired = Database.LockTake(Key, Token, expiration);
                if (acquired)
                {
                    continue;
                }

                Thread.Sleep(SleepTime);
                totalTime += SleepTime;
            }

            return totalTime;
        }

        private async Task<TimeSpan> WaitForLockAsync(TimeSpan expiration, CancellationToken cancelToken = default)
        {
            var totalTime = TimeSpan.Zero;
            var acquired = false;

            while (!acquired && totalTime < expiration)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    await Database.LockReleaseAsync(Key, Token);
                    break;
                }
                
                acquired = await Database.LockTakeAsync(Key, Token, expiration);
                if (acquired)
                {
                    continue;
                }

                await Task.Delay(SleepTime, cancelToken);
                totalTime += SleepTime;
            }

            return totalTime;
        }

        sealed class Releaser : IDisposable
        {
            private RedisLock _locker;

            public Releaser(RedisLock locker)
            {
                _locker = locker;
            }

            public void Dispose()
            {
                _locker.Database.LockRelease(_locker.Key, _locker.Token);
                _locker = null;
            }
        }
    }
}