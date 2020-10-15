using System;
using System.Threading.Tasks;

namespace Smartstore.Events
{
    /// <summary>
    /// Represents a pub/sub message bus provider for inter-server communication between nodes in a web farm.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Posts a message to the specified channel.
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="message">Message</param>
        /// <returns>The number of clients that received the message.</returns>
        long Publish(string channel, string message);

        /// <summary>
        /// Posts a message to the specified channel.
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="message">Message</param>
        /// <returns>The number of clients that received the message.</returns>
        Task<long> PublishAsync(string channel, string message);

        /// <summary>
        /// Subscribes to a posted message in the specified channel.
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="handler">The handler method. First param is channel, second param is message.</param>
        void Subscribe(string channel, Action<string, string> handler);

        /// <summary>
        /// Subscribes to a posted message in the specified channel.
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="handler">The handler method. First param is channel, second param is message.</param>
        Task SubscribeAsync(string channel, Action<string, string> handler);
    }

    public sealed class NullMessageBus : IMessageBus
    {
        public readonly static IMessageBus Instance = new NullMessageBus();

        public long Publish(string channel, string message)
            => 0;

        public Task<long> PublishAsync(string channel, string message)
            => Task.FromResult((long)0);

        public void Subscribe(string channel, Action<string, string> handler)
            { }

        public Task SubscribeAsync(string channel, Action<string, string> handler)
            => Task.CompletedTask;
    }
}