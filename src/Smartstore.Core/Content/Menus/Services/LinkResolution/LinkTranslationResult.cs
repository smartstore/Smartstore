namespace Smartstore.Core.Content.Menus
{
    public class LinkTranslationResult
    {
        public Type EntityType { get; set; }

        public string EntityName { get; set; }

        public LinkTranslatorEntitySummary EntitySummary { get; set; }

        public LinkStatus Status { get; set; }

        public string Label { get; set; }

        public string Link { get; set; }
    }

    public class LinkTranslatorEntitySummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public bool Deleted { get; set; }
        public bool Published { get; set; }
        public bool SubjectToAcl { get; set; }
        public bool LimitedToStores { get; set; }
        public int? PictureId { get; set; }
        public string[] LocalizedPropertyNames { get; set; } = Array.Empty<string>();
    }
}
