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
            Guard.NotNull(expression, nameof(expression));
            Guard.NotNull(translationResult, nameof(translationResult));

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

    //public class LinkResolverResult
    //{
    //    private string _link;

    //    /// <summary>
    //    /// The raw expression without query string.
    //    /// </summary>
    //    public string Expression { get; set; }

    //    /// <summary>
    //    /// The query string part.
    //    /// </summary>
    //    public string QueryString { get; set; }

    //    public LinkType Type { get; set; }
    //    public object Value { get; set; }

    //    public LinkStatus Status { get; set; }
    //    public string Label { get; set; }
    //    public int Id { get; set; }
    //    public int? PictureId { get; set; }
    //    public string Slug { get; set; }

    //    public string Link
    //    {
    //        get
    //        {
    //            if (Type != LinkType.Url && !string.IsNullOrWhiteSpace(_link) && !string.IsNullOrWhiteSpace(QueryString))
    //            {
    //                return string.Concat(_link, "?", QueryString);
    //            }

    //            return _link;
    //        }
    //        set
    //        {
    //            _link = value;

    //            if (_link != null && Type != LinkType.Url)
    //            {
    //                var index = _link.IndexOf('?');
    //                if (index != -1)
    //                {
    //                    QueryString = _link[(index + 1)..];
    //                    _link = _link.Substring(0, index);
    //                }
    //            }
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return this.Link.EmptyNull();
    //    }
    //}

    //[Serializable]
    //public partial class LinkResolverData : LinkResolverResult, ICloneable<LinkResolverData>
    //{
    //    public bool SubjectToAcl { get; set; }
    //    public bool LimitedToStores { get; set; }
    //    public bool CheckLimitedToStores { get; set; } = true;

    //    public LinkResolverData Clone()
    //    {
    //        return (LinkResolverData)this.MemberwiseClone();
    //    }

    //    object ICloneable.Clone()
    //    {
    //        return this.MemberwiseClone();
    //    }
    //}

    //public static class LinkResolverExtensions
    //{
    //    public static (string Icon, string ResKey) GetLinkTypeInfo(this LinkType type)
    //    {
    //        return type switch
    //        {
    //            LinkType.Product => ("fa fa-cube", "Common.Entity.Product"),
    //            LinkType.Category => ("fa fa-sitemap", "Common.Entity.Category"),
    //            LinkType.Manufacturer => ("far fa-building", "Common.Entity.Manufacturer"),
    //            LinkType.Topic => ("far fa-file-alt", "Common.Entity.Topic"),
    //            LinkType.BlogPost => ("fa fa-blog", "Common.Entity.BlogPost"),
    //            LinkType.NewsItem => ("far fa-newspaper", "Common.Entity.NewsItem"),
    //            LinkType.Url => ("fa fa-link", "Common.Url"),
    //            LinkType.File => ("far fa-folder-open", "Common.File"),
    //            _ => throw new SmartException("Unknown link builder type."),
    //        };
    //    }

    //    /// <summary>
    //    /// Creates the full link expression including type, value and query string.
    //    /// </summary>
    //    /// <param name="includeQueryString">Whether to include the query string.</param>
    //    /// <returns>Link expression.</returns>
    //    public static string CreateExpression(this LinkResolverResult data, bool includeQueryString = true)
    //    {
    //        if (data?.Value == null)
    //        {
    //            return string.Empty;
    //        }

    //        var result = data.Type == LinkType.Url
    //            ? data.Value.ToString()
    //            : string.Concat(data.Type.ToString().ToLower(), ":", data.Value.ToString());

    //        if (includeQueryString && data.Type != LinkType.Url && !string.IsNullOrWhiteSpace(data.QueryString))
    //        {
    //            return string.Concat(result, "?", data.QueryString);
    //        }

    //        return result;
    //    }
    //}
}
