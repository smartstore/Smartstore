namespace Smartstore.Core.Seo
{
    public readonly struct ValidateSlugResult
    {
        private readonly ISlugSupported _source;

        public ValidateSlugResult(ValidateSlugResult copyFrom)
        {
            _source = copyFrom.Source;
            EntityName = _source?.GetEntityName();
            Slug = copyFrom.Slug;
            Found = copyFrom.Found;
            FoundIsSelf = copyFrom.FoundIsSelf;
            LanguageId = copyFrom.LanguageId;
            WasValidated = copyFrom.WasValidated;
        }

        public ISlugSupported Source
        {
            get => _source;
            init
            {
                _source = value;
                EntityName = value?.GetEntityName();
            }
        }

        public string EntityName { get; private init; }
        public string Slug { get; init; }
        public UrlRecord Found { get; init; }
        public bool FoundIsSelf { get; init; }
        public int? LanguageId { get; init; }
        public bool WasValidated { get; init; }
    }
}