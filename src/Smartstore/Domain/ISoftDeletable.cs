namespace Smartstore.Domain
{
    public interface ISoftDeletable
    {
        bool Deleted { get; set; }
    }
}