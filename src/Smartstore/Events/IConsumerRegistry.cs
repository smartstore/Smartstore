namespace Smartstore.Events
{
    /// <summary>
    /// A registry for fast <see cref="ConsumerDescriptor"/> lookup.
    /// </summary>
    public interface IConsumerRegistry
    {
        IEnumerable<ConsumerDescriptor> GetConsumers(object message);
    }
}