using Microsoft.AspNetCore.Http;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Modelling;

public class ModelBoundEvent : IEventMessage, IStoreScoped
{
    public ModelBoundEvent(TabbableModel boundModel, object entityModel, ManageController controller)
        : this(boundModel, entityModel, controller.Request.Form, controller.GetActiveStoreScopeConfiguration())
    {
    }

    public ModelBoundEvent(TabbableModel boundModel, object entityModel, IFormCollection form, int storeScope = 0)
    {
        BoundModel = boundModel;
        EntityModel = entityModel;
        Form = form;
        StoreScope = storeScope;
    }

    public TabbableModel BoundModel { get; }
    public object EntityModel { get; }
    public IFormCollection Form { get; }
    public int StoreScope { get; }
}