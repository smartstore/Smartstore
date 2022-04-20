namespace Smartstore.Core.Search.Indexing
{
    public class IndexBacklogItem
    {
        public string Scope { get; set; }
        public int EntityId { get; set; }
        public IndexOperationType OperationType { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public override string ToString()
            => $"IndexBacklogItem (Scope: {Scope}, EntityId: {EntityId}, Operation: {OperationType}, CreatedOnUtc: {CreatedOnUtc})";
    }
}
