namespace Smartstore.Web.Api.Models
{
    public partial class ProviderInfo<TProvider> where TProvider : new()
    {
        /// <summary>
        /// The provider's system name.
        /// </summary>
        /// <example>Payments.AmazonPay</example>
        public string SystemName { get; set; }

        /// <summary>
        /// The plugin group name.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The provider's friendly name.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The provider's description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// A value that indicates whether the provider is configurable.
        /// </summary>
        public bool IsConfigurable { get; set; }

        /// <summary>
        /// A value that indicates whether the provider is editable by the user, i.e., whether the user can change 
        /// the display order and/or localize the display name.
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// The provider's icon URL.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Provides infos about the module.
        /// </summary>
        public ModuleInfo Module { get; set; } = new();

        /// <summary>
        /// Type specific provider infos.
        /// </summary>
        public TProvider Provider { get; set; } = new TProvider();

        public partial class ModuleInfo
        {
            /// <summary>
            /// Optional friendly name of extension.
            /// </summary>
            public string FriendlyName { get; set; }

            /// <summary>
            /// Optional description of extension.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Optional author of extension.
            /// </summary>
            public string Author { get; set; }

            /// <summary>
            /// Optional project web site/url of extension.
            /// </summary>
            public string ProjectUrl { get; set; }

            /// <summary>
            /// Optional tags of extension.
            /// </summary>
            public string Tags { get; set; }

            /// <summary>
            /// The current version of extension.
            /// </summary>
            public VersionInfo Version { get; set; } = new();

            /// <summary>
            /// The minimum compatible application version.
            /// </summary>
            public VersionInfo MinAppVersion { get; set; } = new();

            /// <summary>
            /// A value that indicates whether the module is incompatible with the current application version.
            /// </summary>
            public bool? Incompatible { get; set; }

            /// <summary>
            /// The module's resource root key.
            /// </summary>
            public string ResourceRootKey { get; set; }

            /// <summary>
            /// The module's brand image URL.
            /// </summary>
            public string BrandImageUrl { get; set; }
        }

        public partial class VersionInfo
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Build { get; set; }
            public int Revision { get; set; }
        }
    }
}
