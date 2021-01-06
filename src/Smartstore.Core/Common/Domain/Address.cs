using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Common
{
    public class AddressMap : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.HasOne(a => a.Country)
                .WithMany()
                .HasForeignKey(a => a.CountryId)
                .OnDelete(DeleteBehavior.SetNull); // TODO: (core) Is DeleteBehavior.SetNull the correct equivalent to WillCascadeOnDelete(false)

            builder.HasOne(a => a.StateProvince)
                .WithMany()
                .HasForeignKey(a => a.StateProvinceId)
                .OnDelete(DeleteBehavior.SetNull); // TODO: (core) Is DeleteBehavior.SetNull the correct equivalent to WillCascadeOnDelete(false)
        }
    }

    public partial class Address : EntityWithAttributes, ICloneable
    {
        public Address()
        {
        }

        private readonly ILazyLoader _lazyLoader;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Address(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the first name
        /// </summary>
        public string Salutation { get; set; }

        /// <summary>
        /// Gets or sets the first name
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the company
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public int? StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the address 1
        /// </summary>
        public string Address1 { get; set; }

        /// <summary>
        /// Gets or sets the address 2
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// Gets or sets the zip/postal code
        /// </summary>
        public string ZipPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the fax number
        /// </summary>
        public string FaxNumber { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        private Country _country;
        /// <summary>
        /// Gets or sets the country
        /// </summary>
        public Country Country
        {
            get => _lazyLoader?.Load(this, ref _country) ?? _country;
            set => _country = value;
        }

        private StateProvince _stateProvince;
        /// <summary>
        /// Gets or sets the state/province
        /// </summary>
        public StateProvince StateProvince
        {
            get => _lazyLoader?.Load(this, ref _stateProvince) ?? _stateProvince;
            set => _stateProvince = value;
        }

        public object Clone()
        {
            var addr = new Address()
            {
                Salutation = this.Salutation,
                Title = this.Title,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Email = this.Email,
                Company = this.Company,
                Country = this.Country,
                CountryId = this.CountryId,
                StateProvince = this.StateProvince,
                StateProvinceId = this.StateProvinceId,
                City = this.City,
                Address1 = this.Address1,
                Address2 = this.Address2,
                ZipPostalCode = this.ZipPostalCode,
                PhoneNumber = this.PhoneNumber,
                FaxNumber = this.FaxNumber,
                CreatedOnUtc = this.CreatedOnUtc,
            };
            return addr;
        }

        public static string DefaultAddressFormat => @"{{ Salutation }} {{ Title }} {{ FirstName }} {{ LastName }}
{{ Company }}
{{ Street1 }}
{{ Street2 }}
{{ ZipCode }} {{ City }}
{{ Country | Upcase }}";
    }
}