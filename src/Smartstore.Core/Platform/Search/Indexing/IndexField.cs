using Smartstore.Core.Localization;

namespace Smartstore.Core.Search.Indexing
{
    public class IndexField
    {
        public IndexField(string name, bool value)
            : this(name, value, IndexTypeCode.Boolean)
        {
        }

        public IndexField(string name, int value)
            : this(name, value, IndexTypeCode.Int32)
        {
        }

        public IndexField(string name, double value)
            : this(name, value, IndexTypeCode.Double)
        {
        }

        public IndexField(string name, DateTime value)
            : this(name, value, IndexTypeCode.DateTime)
        {
        }

        public IndexField(string name, string value)
            : this(name, value, IndexTypeCode.String)
        {
        }

        public IndexField(Language language, string name, string value)
            : this(CreateName(name, language?.LanguageCulture), value, IndexTypeCode.String)
        {
        }

        private IndexField(string name, object value, IndexTypeCode typeCode)
        {
            Guard.NotEmpty(name, nameof(name));

            Name = name;
            Value = value;
            TypeCode = typeCode;
        }

        /// <summary>
        /// Creates the name for a localized index field.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="languageCulture">Language culture, e.g. "en-US".</param>
        /// <returns>Name of a localized field.</returns>
        public static string CreateName(string name, string languageCulture)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(languageCulture, nameof(languageCulture));

            return $"{name}_l-{languageCulture.EmptyNull().ToLowerInvariant()}";
        }

        /// <summary>
        /// Gets the language culture from a localized field name, e.g. "en-us".
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <returns>Language culture in lower case. <c>null</c> if it does not exist.</returns>
        public static string GetLanguageCulture(string name)
        {
            Guard.NotEmpty(name, nameof(name));

            var index = name.IndexOf("_l-");

            if (index != -1)
            {
                var namePart = name[..index];
                var languageCulture = name[(index + 3)..];

                if (namePart.HasValue() && languageCulture.HasValue())
                {
                    return languageCulture;
                }
            }

            return null;
        }

        public IndexField Store(bool store = true)
        {
            Stored = store;
            return this;
        }

        public IndexField Analyze(bool analyze = true)
        {
            Analyzed = analyze;
            return this;
        }

        public IndexField RemoveTags(bool removeTags = true)
        {
            ShouldRemoveTags = removeTags;
            return this;
        }

        public IndexField Boost(float? factor)
        {
            Boosted = factor;
            return this;
        }

        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }

        public IndexTypeCode TypeCode
        {
            get;
            private set;
        }

        public bool Stored
        {
            get;
            private set;
        }

        public bool Analyzed
        {
            get;
            private set;
        }

        public bool ShouldRemoveTags
        {
            get;
            private set;
        }

        public float? Boosted
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Name + ": " + Value.ToString().NaIfEmpty();
        }
    }
}
