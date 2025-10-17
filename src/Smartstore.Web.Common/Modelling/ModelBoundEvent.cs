using Microsoft.AspNetCore.Http;
using Smartstore.Events;

namespace Smartstore.Web.Modelling
{
    public class ModelBoundEvent : IEventMessage
    {
        public ModelBoundEvent(TabbableModel boundModel, object entityModel, IFormCollection form)
        {
            BoundModel = boundModel;
            EntityModel = entityModel;
            Form = form;
        }

        public TabbableModel BoundModel { get; }
        public object EntityModel { get; }
        public IFormCollection Form { get; }
    }
}
