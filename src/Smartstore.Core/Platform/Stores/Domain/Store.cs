using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Common;
using Smartstore.Data.Caching;
using Smartstore.Http;

namespace Smartstore.Core.Stores
{
    internal class StoreMap : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> builder)
        {
#pragma warning disable CS0618
            builder
                .HasOne(x => x.DefaultCurrency)
                .WithMany()
                .HasForeignKey(x => x.DefaultCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(x => x.PrimaryExchangeRateCurrency)
                .WithMany()
                .HasForeignKey(x => x.PrimaryExchangeRateCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
#pragma warning restore CS0618
        }
    }

    /// <summary>
    /// Represents a store
    /// </summary>
    [CacheableEntity]
    public partial class Store : EntityWithAttributes, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the store name
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        private string _url;
        /// <summary>
        /// Gets or sets the store URL
        /// </summary>
        [Required, StringLength(400)]
        public string Url 
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    _baseUri = null;
                }
            }
        }

        private bool _sslEnabled;
        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled
        /// </summary>
        public bool SslEnabled
        {
            get => _sslEnabled;
            set
            {
                if (_sslEnabled != value)
                {
                    _sslEnabled = value;
                    _baseUri = null;
                }
            }
        }

        private int? _sslPort;
        /// <summary>
        /// Gets or sets the SSL port for secure connections.
        /// Should be null if port is default (443).
        /// </summary>
        public int? SslPort
        {
            get => _sslPort;
            set
            {
                if (_sslPort != value)
                {
                    _sslPort = value;
                    _baseUri = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the store secure URL (HTTPS)
        /// </summary>
        [StringLength(400)]
        [Obsolete("Secure URL now is URL + SslPort")]
        public string SecureUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all pages are forced to use SSL.
        /// </summary>
        [Obsolete("SSL applies to all pages by default now (if enabled).")]
        public bool ForceSslForAllPages { get; set; }

        /// <summary>
        /// Gets or sets the comma separated list of possible HTTP_HOST values
        /// </summary>
        [StringLength(1000)]
        public string Hosts { get; set; }

        /// <summary>
        /// Gets or sets the logo media file id
        /// </summary>
        public int LogoMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the png icon media file id 
        /// </summary>
        public int? FavIconMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the png icon media file id 
        /// </summary>
        public int? PngIconMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the apple touch icon media file id
        /// </summary>
        public int? AppleTouchIconMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the ms tile image media file id
        /// </summary>
        public int? MsTileImageMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the ms tile color
        /// </summary>
        public string MsTileColor { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a store specific id for the HTML body
        /// </summary>
        public string HtmlBodyId { get; set; }

        /// <summary>
        /// Gets or sets the CDN host name, if static media content should be served through a CDN.
        /// </summary>
        [StringLength(400)]
        public string ContentDeliveryNetwork { get; set; }

        /// <summary>
        /// Gets or sets the default currency identifier.
        /// </summary>
        [Column("PrimaryStoreCurrencyId")]
        public int DefaultCurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the primary exchange rate currency identifier
        /// </summary>
        public int PrimaryExchangeRateCurrencyId { get; set; }

        private Currency _defaultCurrency;
        /// <summary>
        /// Gets or sets the default currency.
        /// </summary>
        [Obsolete("Use ICurrencyService.PrimaryCurrency")]
        public Currency DefaultCurrency
        {
            get => _defaultCurrency ?? LazyLoader.Load(this, ref _defaultCurrency);
            set => _defaultCurrency = value;
        }

        private Currency _primaryExchangeRateCurrency;
        /// <summary>
        /// Gets or sets the primary exchange rate currency
        /// </summary>
        [Obsolete("Use ICurrencyService.PrimaryExchangeCurrency")]
        public Currency PrimaryExchangeRateCurrency
        {
            get => _primaryExchangeRateCurrency ?? LazyLoader.Load(this, ref _primaryExchangeRateCurrency);
            set => _primaryExchangeRateCurrency = value;
        }


        /// <summary>
        /// Checks whether the store supports HTTPS.
        /// </summary>
        public bool SupportsHttps()
        {
            return SslEnabled || Url.StartsWith("https");
        }

        private Uri _baseUri;
        /// <summary>
        /// Gets the store's base URI (Scheme + Host + PathBase + /).
        /// </summary>
        /// <returns>The store base URI</returns>
        public Uri GetBaseUri()
        {
            LazyInitializer.EnsureInitialized(ref _baseUri, () =>
            {
                var url = Url;
                if (SslEnabled && !url.StartsWith("https"))
                {
                    var httpBaseUri = new Uri(url.EnsureEndsWith('/'));
                    var httpsPort = SslPort ?? WebHelper.GetServerHttpsPort();
                    var host = httpsPort == -1 || httpsPort == 443
                        ? new HostString(httpBaseUri.Host)
                        : new HostString(httpBaseUri.Host, httpsPort);

                    url = "https://" + host + httpBaseUri.AbsolutePath;
                }

                return new Uri(url.EnsureEndsWith('/'));
            });

            return _baseUri;
        }

        /// <summary>
        /// Gets the store's base URL (Scheme + Host + PathBase + /).
        /// </summary>
        /// <returns>The store base URL</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetBaseUrl()
            => GetBaseUri().ToString();
    }
}