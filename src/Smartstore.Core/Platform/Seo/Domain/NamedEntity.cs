using System;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    public class NamedEntity : BaseEntity, ISlugSupported
    {
        public string EntityName { get; set; }
        public string DisplayName { get; set; }
        public string Slug { get; set; }
        public DateTime LastMod { get; set; }
        public int? LanguageId { get; set; }

        public string GetDisplayName()
            => DisplayName;

        public string GetDisplayNameMemberName()
            => nameof(DisplayName);

        public override string GetEntityName()
            => EntityName;
    }
}
