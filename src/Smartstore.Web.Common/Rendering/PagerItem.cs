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
        Gap
    }

    public class PagerItem
    {
        public PagerItem(
            int index,
            string text, 
            string url, 
            PagerItemType itemType = PagerItemType.Page, 
            PagerItemState state = PagerItemState.Normal)
        {
            Index = index;
            Text = text;
            Url = url;
            Type = itemType;
            State = state;
            ExtraData = string.Empty;
        }

        /// <summary>
        /// One-based index
        /// </summary>
        public int Index { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public string CssClass { get; set; }
        public PagerItemState State { get; set; }
        public PagerItemType Type { get; set; }
        public string ExtraData { get; set; }
        public string DisplayBreakpointUp { get; set; }
        public bool IsNavButton => 
            Type == PagerItemType.FirstPage || 
            Type == PagerItemType.PreviousPage || 
            Type == PagerItemType.NextPage || 
            Type == PagerItemType.LastPage;
    }
}
