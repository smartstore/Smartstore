using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;

namespace Smartstore.Core.Identity
{
    internal class CustomerMap : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Globally exclude soft-deleted entities from all queries.
            builder.HasQueryFilter(c => !c.Deleted);

            builder
                .HasMany(c => c.Addresses)
                .WithMany("Customers")  // Refers to private Address.Customers.
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerAddresses",
                    c => c
                        .HasOne<Address>()
                        .WithMany()
                        .HasForeignKey("Address_Id")
                        .HasConstraintName("FK_dbo.CustomerAddresses_dbo.Address_Address_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c => c
                        .HasOne<Customer>()
                        .WithMany()
                        .HasForeignKey("Customer_Id")
                        .HasConstraintName("FK_dbo.CustomerAddresses_dbo.Customer_Customer_Id")
                        .OnDelete(DeleteBehavior.Cascade),
                    c =>
                    {
                        c.HasIndex("Customer_Id");
                        c.HasKey("Customer_Id", "Address_Id");
                    });

            // INFO: we cannot set both addresses to DeleteBehavior.SetNull. It would produce cycles or multiple cascade paths.
            builder
                .HasOne(c => c.BillingAddress)
                .WithOne(navigationName: null)
                .HasForeignKey<Customer>(c => c.BillingAddressId);

            builder
                .HasOne(c => c.ShippingAddress)
                .WithOne(navigationName: null)
                .HasForeignKey<Customer>(c => c.ShippingAddressId)
                .OnDelete(DeleteBehavior.SetNull);
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
    public partial class Customer : EntityWithAttributes, ISoftDeletable
    {
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
        [IgnoreDataMember]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password format
        /// </summary>
        [IgnoreDataMember]
        public int PasswordFormatId { get; set; }

        /// <summary>
        /// Gets or sets the password format
        /// </summary>
        [NotMapped]
        [IgnoreDataMember]
        public PasswordFormat PasswordFormat
        {
            get => (PasswordFormat)PasswordFormatId;
            set => PasswordFormatId = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the customer was detected
        /// cookie-less by evaluating the ClientIdent (IP+UserAgent).
        /// </summary>
        [NotMapped]
        [IgnoreDataMember]
        public bool DetectedByClientIdent { get; set; }

        /// <summary>
        /// Gets or sets the password salt
        /// </summary>
        [StringLength(500)]
        [IgnoreDataMember]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        [StringLength(4000)]
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
        [IgnoreDataMember]
        public bool Deleted { get; set; }

        bool ISoftDeletable.ForceDeletion
        {
            // We don't want to soft-delete ordinary guest customer accounts.
            get => !IsSystemAccount && Email == null && Username == null;
        }

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

        // TODO: customer string properties Gender, TimeZoneId, LastUserAgent, LastUserDeviceType should not set to maximum length (migration required).

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

        [NotMapped, IgnoreDataMember]
        public override CustomerAttributeCollection GenericAttributes
            => new(base.GenericAttributes);

        #region Navigation properties

        private Address _billingAddress;
        /// <summary>
        /// Default billing address
        /// </summary>
        public Address BillingAddress
        {
            get => _billingAddress ?? LazyLoader.Load(this, ref _billingAddress);
            set => _billingAddress = value;
        }

        private Address _shippingAddress;
        /// <summary>
        /// Default shipping address
        /// </summary>
        public Address ShippingAddress
        {
            get => _shippingAddress ?? LazyLoader.Load(this, ref _shippingAddress);
            set => _shippingAddress = value;
        }

        private ICollection<Address> _addresses;
        /// <summary>
        /// Gets or sets customer addresses
        /// </summary>
        public ICollection<Address> Addresses
        {
            get => _addresses ?? LazyLoader.Load(this, ref _addresses) ?? (_addresses ??= new HashSet<Address>());
            protected set => _addresses = value;
        }

        private ICollection<ExternalAuthenticationRecord> _externalAuthenticationRecords;
        /// <summary>
        /// Gets or sets external authentication records.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<ExternalAuthenticationRecord> ExternalAuthenticationRecords
        {
            get => _externalAuthenticationRecords ?? LazyLoader.Load(this, ref _externalAuthenticationRecords) ?? (_externalAuthenticationRecords ??= new HashSet<ExternalAuthenticationRecord>());
            protected set => _externalAuthenticationRecords = value;
        }

        private ICollection<CustomerContent> _customerContent;
        /// <summary>
        /// Gets or sets customer generated content.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<CustomerContent> CustomerContent
        {
            get => _customerContent ?? LazyLoader.Load(this, ref _customerContent) ?? (_customerContent ??= new HashSet<CustomerContent>());
            protected set => _customerContent = value;
        }

        private ICollection<CustomerRoleMapping> _customerRoleMappings;
        /// <summary>
        /// Gets or sets the customer role mappings.
        /// </summary>
        public ICollection<CustomerRoleMapping> CustomerRoleMappings
        {
            get => _customerRoleMappings ?? LazyLoader.Load(this, ref _customerRoleMappings) ?? (_customerRoleMappings ??= new HashSet<CustomerRoleMapping>());
            protected set => _customerRoleMappings = value;
        }

        private ICollection<ShoppingCartItem> _shoppingCartItems;
        /// <summary>
        /// Gets or sets shopping cart items
        /// </summary>
        public ICollection<ShoppingCartItem> ShoppingCartItems
        {
            get => _shoppingCartItems ?? LazyLoader.Load(this, ref _shoppingCartItems) ?? (_shoppingCartItems ??= new HashSet<ShoppingCartItem>());
            set => _shoppingCartItems = value;
        }

        private ICollection<Order> _orders;
        /// <summary>
        /// Gets or sets orders
        /// </summary>        
        public ICollection<Order> Orders
        {
            get => _orders ?? LazyLoader.Load(this, ref _orders) ?? (_orders ??= new HashSet<Order>());
            protected internal set => _orders = value;
        }

        private ICollection<RewardPointsHistory> _rewardPointsHistory;
        /// <summary>
        /// Gets or sets the reward points history.
        /// </summary>
        public ICollection<RewardPointsHistory> RewardPointsHistory
        {
            get => _rewardPointsHistory ?? LazyLoader.Load(this, ref _rewardPointsHistory) ?? (_rewardPointsHistory ??= new HashSet<RewardPointsHistory>());
            protected set => _rewardPointsHistory = value;
        }

        private ICollection<WalletHistory> _walletHistory;
        /// <summary>
        /// Gets or sets the wallet history.
        /// </summary>
        public ICollection<WalletHistory> WalletHistory
        {
            get => _walletHistory ?? LazyLoader.Load(this, ref _walletHistory) ?? (_walletHistory ??= new HashSet<WalletHistory>());
            protected set => _walletHistory = value;
        }

        private ICollection<ReturnRequest> _returnRequests;
        /// <summary>
        /// Gets or sets the return requests.
        /// </summary>
        public ICollection<ReturnRequest> ReturnRequests
        {
            get => _returnRequests ?? LazyLoader.Load(this, ref _returnRequests) ?? (_returnRequests ??= new HashSet<ReturnRequest>());
            protected set => _returnRequests = value;
        }

        #endregion

        #region Utils

        /// <summary>
        /// Gets a string identifier for the customer's roles by joining all role ids
        /// </summary>
        /// <param name="onlyActiveCustomerRoles"><c>true</c> ignores all inactive roles</param>
        /// <returns>The identifier</returns>
        public string GetRolesIdent(bool onlyActiveCustomerRoles = true)
        {
            return string.Join(',', GetRoleIds(onlyActiveCustomerRoles));
        }

        /// <summary>
        /// Get identifiers of assigned customer roles.
        /// </summary>
        /// <param name="onlyActiveCustomerRoles"><c>true</c> ignores all inactive roles</param>
        /// <returns>Customer role identifiers.</returns>
        public int[] GetRoleIds(bool onlyActiveCustomerRoles = true)
        {
            return CustomerRoleMappings
                .Select(x => x.CustomerRole)
                .Where(x => !onlyActiveCustomerRoles || x.Active)
                .Select(x => x.Id)
                .ToArray();
        }

        /// <summary>
        /// Removes an address from the addresses assigned to this customer.
        /// </summary>
        /// <param name="address">Address to remove.</param>
        public void RemoveAddress(Address address)
        {
            if (Addresses.Contains(address))
            {
                if (BillingAddress == address)
                {
                    BillingAddress = null;
                }
                if (ShippingAddress == address)
                {
                    ShippingAddress = null;
                }

                Addresses.Remove(address);
            }
        }

        /// <summary>
        /// Adds a reward points history entry.
        /// </summary>
        /// <param name="points">Points to add.</param>
        /// <param name="message">Optional message.</param>
        /// <param name="usedWithOrder">Order for which the points were used.</param>
        /// <param name="usedAmount">Used amount.</param>
        public void AddRewardPointsHistoryEntry(
            int points,
            string message = "",
            Order usedWithOrder = null,
            decimal usedAmount = 0M)
        {
            var newPointsBalance = GetRewardPointsBalance() + points;

            RewardPointsHistory.Add(new RewardPointsHistory
            {
                Customer = this,
                UsedWithOrder = usedWithOrder,
                Points = points,
                PointsBalance = newPointsBalance,
                UsedAmount = usedAmount,
                Message = message,
                CreatedOnUtc = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Gets the reward points balance.
        /// </summary>
        public int GetRewardPointsBalance()
        {
            if (RewardPointsHistory.Any())
            {
                return RewardPointsHistory
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .ThenByDescending(x => x.Id)
                    .First()
                    .PointsBalance;
            }

            return 0;
        }

        #endregion
    }
}