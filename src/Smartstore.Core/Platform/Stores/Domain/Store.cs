using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Common;
using Smartstore.Core.Security;
using Smartstore.Data.Caching;

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
                    _httpUri = null;
                    _httpsUri = null;
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
                    _httpsUri = null;
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
                    _httpsUri = null;
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
        /// Gets or sets a value indicating whether all pages are forced to use SSL 
        /// (regardless of any specified <see cref="RequireSslAttribute"/> attribute)
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

        private Uri _httpUri;
        private Uri _httpsUri;
        /// <summary>
        /// Gets the store root URI (Scheme + Host + PathBase + /)
        /// </summary>
        /// <param name="secure">
        /// If <c>false</c>, returns the default unsecured URI.
        /// If <c>true</c>, returns the secure URI, but only if SSL is enabled for the store.
        /// </param>
        /// <returns>The store root URI</returns>
        public Uri GetUri(bool secure)
        {
            Uri result;
            if (secure && SupportsHttps())
            {
                LazyInitializer.EnsureInitialized(ref _httpsUri, () =>
                {
                    string url;
                    if (Url.StartsWith("https"))
                    {
                        url = Url;
                    }
                    else
                    {
                        if (SslPort == null || SslPort == 443)
                        {
                            url = Url.Replace("http:/", "https:/");
                        }
                        else
                        {
                            var uri = new Uri(Url);
                            url = "https://" + new HostString(uri.Host, SslPort.Value) + uri.AbsolutePath;
                        }
                    }

                    return new Uri(url.EnsureEndsWith('/'));
                });

                result = _httpsUri;
            }
            else
            {
                LazyInitializer.EnsureInitialized(ref _httpUri, () => new Uri(Url.EnsureEndsWith('/')));
                result = _httpUri;
            }

            return result;
        }

        /// <inheritdoc cref="GetUri(bool)" />
        /// <summary>
        /// Gets the store root URL (Scheme + Host + PathBase + /)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetHost(bool secure)
            => GetUri(secure).ToString();
    }
}