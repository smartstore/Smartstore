using System.Reflection;
using Autofac;

namespace Smartstore.Events
{
    /// <summary>
    /// Resolves the concrete <see cref="IConsumer"/> class instance which contains
    /// the requested consumer.
    /// </summary>
    public interface IConsumerResolver
    {
        IConsumer Resolve(ConsumerDescriptor descriptor);
        object ResolveParameter(ParameterInfo p, IComponentContext c = null);
    }
}