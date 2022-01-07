using Smartstore.ComponentModel;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Used to transfer miscellaneous data to the template engine
    /// which is merged with the generic "Bag" part.
    /// </summary>
    public interface IModelPart : IDictionary<string, object>
    {
    }

    public interface INamedModelPart : IDictionary<string, object>
    {
        string ModelPartName { get; }
    }


    #region Impl

    public class ModelPart : HybridExpando, IModelPart
    {
    }

    public class NamedModelPart : HybridExpando, INamedModelPart
    {
        public NamedModelPart(string modelPartName)
            : base(true)
        {
            Guard.NotEmpty(modelPartName, nameof(modelPartName));
            ModelPartName = modelPartName;
        }

        public string ModelPartName
        {
            get;
            private set;
        }
    }

    #endregion
}
