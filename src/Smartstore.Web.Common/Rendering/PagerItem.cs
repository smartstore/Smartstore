namespace Smartstore.Web.Rendering.Pager
{
    public enum PagerItemState
    {
        Normal,
        Disabled,
        Selected
    }

    public enum PagerItemType
    {
        FirstPage,
        PreviousPage,
        Page,
        Text,
        NextPage,
        LastPage,
        Misc
    }

    public class PagerItem
    {
        public PagerItem(string text)
            : this(text, string.Empty, PagerItemType.Page, PagerItemState.Normal)
        {
        }

        public PagerItem(string text, string url)
            : this(text, url, PagerItemType.Page, PagerItemState.Normal)
        {
        }

        public PagerItem(string text, string url, PagerItemType itemType)
            : this(text, url, itemType, PagerItemState.Normal)
        {
        }

        public PagerItem(string text, string url, PagerItemType itemType, PagerItemState state)
        {
            Text = text;
            Url = url;
            Type = itemType;
            State = state;
            ExtraData = string.Empty;
        }

        public string Text { get; set; }
        public string Url { get; set; }
        public PagerItemState State { get; set; }
        public PagerItemType Type { get; set; }
        public string ExtraData { get; set; }
        public bool IsNavButton => (Type == PagerItemType.FirstPage || Type == PagerItemType.PreviousPage || Type == PagerItemType.NextPage || Type == PagerItemType.LastPage);
    }
}
