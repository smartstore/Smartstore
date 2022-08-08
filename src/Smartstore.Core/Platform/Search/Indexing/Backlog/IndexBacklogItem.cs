namespace Smartstore.Core.Search.Indexing
{
    public class IndexBacklogItem
    {
        public IndexBacklogItem(string scope, int entityId, IndexOperationType operationType = IndexOperationType.Index)
        {
            Guard.NotEmpty(scope, nameof(scope));
            Guard.NotZero(entityId, nameof(entityId));

            Scope = scope;
            EntityId = entityId;
            OperationType = operationType;
        }

        public string Scope { get; }
        public int EntityId { get; }
        public IndexOperationType OperationType { get; }

        public override string ToString()
            => $"IndexBacklogItem (Scope: {Scope}, EntityId: {EntityId}, Operation: {OperationType})";
    }
}
