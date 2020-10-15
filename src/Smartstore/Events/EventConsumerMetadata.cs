using System;
using Smartstore.Engine;

namespace Smartstore.Events
{
    public class EventConsumerMetadata
    {
        public bool IsActive { get; set; }
        public Type ContainerType { get; set; }
        public ModuleDescriptor ModuleDescriptor { get; set; }
    }
}
