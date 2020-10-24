using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Common;
using Smartstore.Domain;

namespace Smartstore.Core.Customers
{
    public class CustomerMap : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);
            
            //builder.HasMany(c => c.Addresses)
            //    .WithMany("")
            //    .UsingEntity(m => m.ToTable("CustomerAddresses"));

            builder
                .HasOne(c => c.BillingAddress)
                .WithOne(navigationName: null)
                .HasForeignKey<Customer>(c => c.BillingAddressId);

            builder
                .HasOne(c => c.ShippingAddress)
                .WithOne(navigationName: null)
                .HasForeignKey<Customer>(c => c.ShippingAddressId);
        }
    }

    /// <summary>
    /// Represents a customer
    /// </summary>
    [Index(nameof(Deleted), Name = "IX_Deleted")]
    [Index(nameof(Email), Name = "IX_Customer_Email")]
    [Index(nameof(Username), Name = "IX_Customer_Username")]
    [Index(nameof(CustomerGuid), Name = "IX_Customer_CustomerGuid")]
    [Index(nameof(IsSystemAccount), Name = "IX_IsSystemAccount")]
    [Index(nameof(SystemName), Name = "IX_SystemName")]
    [Index(nameof(LastIpAddress), Name = "IX_Customer_LastIpAddress")]
    [Index(nameof(CreatedOnUtc), Name = "IX_Customer_CreatedOn")]
    [Index(nameof(LastActivityDateUtc), Name = "IX_Customer_LastActivity")]
    [Index(nameof(FullName), Name = "IX_Customer_FullName")]
    [Index(nameof(Company), Name = "IX_Customer_Company")]
    [Index(nameof(CustomerNumber), Name = "IX_Customer_CustomerNumber")]
    [Index(nameof(BirthDate), Name = "IX_Customer_BirthDate")]
    [Index(nameof(Deleted), nameof(IsSystemAccount), Name = "IX_Customer_Deleted_IsSystemAccount")]
    public partial class Customer : BaseEntity, ISoftDeletable
    {
        private readonly ILazyLoader _lazyLoader;

        public Customer()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Customer(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the customer Guid
        /// </summary>
        public Guid CustomerGuid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [StringLength(500)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email
        /// </summary>
        [StringLength(500)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password
        /// </summary>
        [StringLength(500)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password format
        /// </summary>
        public int PasswordFormatId { get; set; }

        /// <summary>
        /// Gets or sets the password format
        /// </summary>
        public PasswordFormat PasswordFormat
        {
            get => (PasswordFormat)PasswordFormatId;
            set => PasswordFormatId = (int)value;
        }

        /// <summary>
        /// Gets or sets the password salt
        /// </summary>
        [StringLength(500)]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer is tax exempt
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets the affiliate identifier
        /// </summary>
        public int AffiliateId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer account is system
        /// </summary>
        public bool IsSystemAccount { get; set; }

        /// <summary>
        /// Gets or sets the customer system name
        /// </summary>
        [StringLength(500)]
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the last IP address
        /// </summary>
        [StringLength(100)]
        public string LastIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last login
        /// </summary>
        public DateTime? LastLoginDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of last activity
        /// </summary>
        public DateTime LastActivityDateUtc { get; set; }

        /// <summary>
        /// For future use
        /// </summary>
        [StringLength(50)]
        public string Salutation { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(225)]
        public string FirstName { get; set; }

        [StringLength(225)]
        public string LastName { get; set; }

        [StringLength(450)]
        public string FullName { get; set; }

        [StringLength(255)]
        public string Company { get; set; }

        [StringLength(100)]
        public string CustomerNumber { get; set; }

        public DateTime? BirthDate { get; set; }

        public string Gender { get; set; }

        public int VatNumberStatusId { get; set; }

        public string TimeZoneId { get; set; }

        public int TaxDisplayTypeId { get; set; }

        public DateTime? LastForumVisit { get; set; }

        public string LastUserAgent { get; set; }

        public string LastUserDeviceType { get; set; }

        [Column("BillingAddress_Id")]
        public int? BillingAddressId { get; set; }

        [Column("ShippingAddress_Id")]
        public int? ShippingAddressId { get; set; }

        #region Navigation properties

        // TODO: (core) Add all navigation properties for Customer entity

        private Address _billingAddress;
        /// <summary>
        /// Default billing address
        /// </summary>
        public Address BillingAddress 
        {
            get => _lazyLoader?.Load(this, ref _billingAddress) ?? _billingAddress;
            set => _billingAddress = value;
        }

        private Address _shippingAddress;
        /// <summary>
        /// Default shipping address
        /// </summary>
        public Address ShippingAddress
        {
            get => _lazyLoader?.Load(this, ref _shippingAddress) ?? _shippingAddress;
            set => _shippingAddress = value;
        }

        //private ICollection<Address> _addresses;
        ///// <summary>
        ///// Gets or sets customer addresses
        ///// </summary>
        //public ICollection<Address> Addresses
        //{
        //    get => _lazyLoader?.Load(this, ref _addresses) ?? (_addresses ??= new HashSet<Address>());
        //    protected set => _addresses = value;
        //}

        #endregion

        #region Utils


        /// <summary>
        /// Gets a string identifier for the customer's roles by joining all role ids
        /// </summary>
        /// <param name="onlyActiveCustomerRoles"><c>true</c> ignores all inactive roles</param>
        /// <returns>The identifier</returns>
        public string GetRolesIdent(bool onlyActiveCustomerRoles = true)
        {
            return string.Join(",", GetRoleIds(onlyActiveCustomerRoles));
        }

        /// <summary>
        /// Get identifiers of assigned customer roles.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles"><c>true</c> ignores all inactive roles</param>
        /// <returns>Customer role identifiers.</returns>
        public int[] GetRoleIds(bool onlyActiveCustomerRoles = true)
        {
            return Array.Empty<int>();
            // TODO: (core) Implement Customer.GetRoleIds()
            //return CustomerRoleMappings
            //    .Select(x => x.CustomerRole)
            //    .Where(x => !onlyActiveCustomerRoles || x.Active)
            //    .Select(x => x.Id)
            //    .ToArray();
        }

        //// // TODO: (core) Implement Customer.RemoveAddress()
        //public virtual void RemoveAddress(Address address)
        //{
        //    if (Addresses.Contains(address))
        //    {
        //        if (BillingAddress == address) BillingAddress = null;
        //        if (ShippingAddress == address) ShippingAddress = null;

        //        Addresses.Remove(address);
        //    }
        //}

        //// TODO: (core) Implement Customer.AddRewardPointsHistoryEntry()
        //public void AddRewardPointsHistoryEntry(
        //    int points,
        //    string message = "",
        //    Order usedWithOrder = null,
        //    decimal usedAmount = 0M)
        //{
        //    int newPointsBalance = this.GetRewardPointsBalance() + points;

        //    var rewardPointsHistory = new RewardPointsHistory()
        //    {
        //        Customer = this,
        //        UsedWithOrder = usedWithOrder,
        //        Points = points,
        //        PointsBalance = newPointsBalance,
        //        UsedAmount = usedAmount,
        //        Message = message,
        //        CreatedOnUtc = DateTime.UtcNow
        //    };

        //    this.RewardPointsHistory.Add(rewardPointsHistory);
        //}

        //// TODO: (core) Implement Customer.GetRewardPointsBalance()
        ///// <summary>
        ///// Gets reward points balance
        ///// </summary>
        //public int GetRewardPointsBalance()
        //{
        //    int result = 0;
        //    if (this.RewardPointsHistory.Count > 0)
        //        result = this.RewardPointsHistory.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id).FirstOrDefault().PointsBalance;
        //    return result;
        //}

        #endregion
    }
}