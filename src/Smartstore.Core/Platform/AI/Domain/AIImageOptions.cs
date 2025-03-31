#nullable enable

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents AI image creation or editing options supported by an AI provider.
    /// </summary>
    public class AIImageOptions
    {
        /// <summary>
        /// Gets or sets supported image styles.
        /// </summary>
        public List<AIImageOption>? Styles { get; set; }

        public class AIImageOption
        {
            /// <summary>
            /// Gets or sets the displayed name of the option.
            /// </summary>
            public string? Name { get; set; }

            /// <summary>
            /// Gets or sets the option value.
            /// </summary>
            /// <example>photograph or watercolor etc.</example>
            public required string Value { get; set; }
        }
    }
}
