using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Test.Common;
using Smartstore.Threading;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class AsyncLockTests : ServiceTestBase
    {
        [Test]
        public async Task BasicTest()
        {
            var locks = 5000;
            var concurrency = 50;
            var concurrentQueue = new ConcurrentQueue<(bool entered, string key)>();

            var tasks = Enumerable.Range(1, locks * concurrency)
                .Select(async i =>
                {
                    var key = Convert.ToInt32(Math.Ceiling((double)i / concurrency)).ToString();
                    using (await AsyncLock.KeyedAsync(key))
                    {
                        concurrentQueue.Enqueue((true, key));
                        await Task.Delay(100);
                        concurrentQueue.Enqueue((false, key));
                    }
                });
            await Task.WhenAll(tasks.AsParallel());

            bool valid = concurrentQueue.Count == locks * concurrency * 2;

            var entered = new HashSet<string>();

            while (valid && !concurrentQueue.IsEmpty)
            {
                concurrentQueue.TryDequeue(out var result);
                if (result.entered)
                {
                    if (entered.Contains(result.key))
                    {
                        valid = false;
                        break;
                    }
                    entered.Add(result.key);
                }
                else
                {
                    if (!entered.Contains(result.key))
                    {
                        valid = false;
                        break;
                    }
                    entered.Remove(result.key);
                }
            }

            valid.ShouldBeTrue();
        }

        [Test]
        public async Task KeylessLock()
        {
            var myLock = new AsyncLock();

            using (await myLock.LockAsync())
            {
                await Task.Delay(100);
            }

            myLock.ShouldNotBeNull();
        }
    }
}
