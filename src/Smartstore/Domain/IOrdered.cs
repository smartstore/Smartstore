namespace Smartstore.Domain
{
    public interface IOrdered
    {
        // TODO: (MC) Make Nullable!
        int Ordinal { get; }
    }

    public interface IDisplayOrder
    {
        int DisplayOrder { get; }
    }
}
