namespace Smartstore.Core.Content.Menus
{
    public enum LinkStatus
    {
        Ok,
        Forbidden,
        NotFound,
        Hidden
    }

    public class LinkResolutionResult
    {
        internal LinkResolutionResult(LinkExpression expression, LinkStatus status)
        {
            Expression = expression;
            Status = status;
        }

        public LinkResolutionResult(LinkExpression expression, LinkTranslationResult translationResult, LinkStatus status)
        {
            Guard.NotNull(expression);
            Guard.NotNull(translationResult);

            Expression = Guard.NotNull(expression, nameof(expression));
            Status = status;
            Label = translationResult.Label;
            EntityName = translationResult.EntityName;
            EntityId = translationResult.EntitySummary?.Id;
            PictureId = translationResult.EntitySummary?.PictureId;

            var link = translationResult.Link;
            if (link.HasValue() && !link.Contains('?'))
            {
                link += expression.Query;
            }

            Link = link;
        }

        public LinkExpression Expression { get; }
        public LinkStatus Status { get; }
        public string Label { get; }
        public string Link { get; }

        public string EntityName { get; }
        public int? EntityId { get; }
        public int? PictureId { get; }

        public override string ToString()
            => Link.EmptyNull();
    }
}
