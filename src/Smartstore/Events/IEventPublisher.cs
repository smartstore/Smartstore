using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T message, CancellationToken cancelToken = default) where T : class;
    }

    public class NullEventPublisher : IEventPublisher
    {
        private readonly static IEventPublisher s_instance = new NullEventPublisher();

        public static IEventPublisher Instance => s_instance;

        public Task PublishAsync<T>(T eventMessage, CancellationToken cancelToken = default) where T : class
        {
            return Task.CompletedTask;
        }
    }
}