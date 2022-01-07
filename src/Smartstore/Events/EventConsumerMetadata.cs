using Smartstore.Engine.Modularity;

namespace Smartstore.Events
{
    public class EventConsumerMetadata
    {
        public Type ContainerType { get; set; }
        public IModuleDescriptor ModuleDescriptor { get; set; }
    }
}
