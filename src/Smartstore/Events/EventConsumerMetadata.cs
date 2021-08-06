using System;
using Smartstore.Engine.Modularity;

namespace Smartstore.Events
{
    public class EventConsumerMetadata
    {
        public bool IsActive { get; set; }
        public Type ContainerType { get; set; }
        public IModuleDescriptor ModuleDescriptor { get; set; }
    }
}
