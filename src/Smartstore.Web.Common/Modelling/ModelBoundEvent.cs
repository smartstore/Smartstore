using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Modelling
{
    public class ModelBoundEvent
    {
        public ModelBoundEvent(TabbableModel boundModel, object entityModel, IFormCollection form)
        {
            BoundModel = boundModel;
            EntityModel = entityModel;
            Form = form;
        }

        public TabbableModel BoundModel { get; private set; }
        public object EntityModel { get; private set; }
        public IFormCollection Form { get; private set; }
    }
}
