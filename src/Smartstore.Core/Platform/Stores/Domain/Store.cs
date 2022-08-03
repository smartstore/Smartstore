using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Common;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Stores
{
    internal class StoreMap : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> builder)
        {
            builder
                .HasOne(x => x.DefaultCurrency)
                .WithMany()
                .HasForeignKey(x => x.DefaultCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

#pragma warning disable CS0618
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
        public Store()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Store(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the store name
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the store URL
        /// </summary>
        [Required, StringLength(400)]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL is enabled
        /// </summary>
        public bool SslEnabled { get; set; }

        /// <summary>
        /// Gets or sets the store secure URL (HTTPS)
        /// </summary>
        [StringLength(400)]
        public string SecureUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all pages will be forced to use SSL (no matter of a specified [RequireHttpsByConfigAttribute] attribute)
        /// </summary>
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
            if (SslEnabled)
            {
                return Url.StartsWith("https") || (SecureUrl.HasValue() && SecureUrl.StartsWith("https"));
            }

            return false;
        }

        /// <summary>
        /// Gets the store host name (Scheme + Host + /)
        /// </summary>
        /// <param name="secure">
        /// If <c>false</c>, returns the default unsecured url.
        /// If <c>true</c>, returns the secure url, but only if SSL is enabled for the store.
        /// </param>
        /// <returns>The host name</returns>
        public string GetHost(bool secure)
        {
            string host;
            if (secure && SslEnabled)
            {
                if (!string.IsNullOrWhiteSpace(SecureUrl))
                {
                    host = SecureUrl;
                }
                else
                {
                    host = Url.Replace("http:/", "https:/");
                }
            }
            else
            {
                host = Url;
            }

            return host.EnsureEndsWith('/');
        }
    }
}