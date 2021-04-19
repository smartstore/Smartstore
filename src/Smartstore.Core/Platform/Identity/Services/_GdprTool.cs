using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Data;
using Smartstore.Data.Batching;
using Smartstore.Domain;
using Smartstore.Events;
using Smartstore.Utilities;

namespace Smartstore.Core.Identity
{
    public partial class GdprTool : IGdprTool
    {
		private readonly SmartDbContext _db;
		private readonly ICustomerService _customerService;
		private readonly IMessageModelProvider _messageModelProvider;
		private readonly IGenericAttributeService _genericAttributeService;
		private readonly IShoppingCartService _shoppingCartService;
		//// TODO: (core) Apply IBackInStockSubscriptionService to GdprTool when ported.
		//private readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
		private readonly ILanguageService _languageService;
		private readonly IWorkContext _workContext;
		private readonly IEventPublisher _eventPublisher;

		private static readonly DateTime MinDate = new(1900, 1, 1);

		public GdprTool(
			SmartDbContext db,
			ICustomerService customerService,
			IMessageModelProvider messageModelProvider,
			IGenericAttributeService genericAttributeService,
			IShoppingCartService shoppingCartService,
			//IBackInStockSubscriptionService backInStockSubscriptionService,
			ILanguageService languageService,
			IWorkContext workContext,
			IEventPublisher eventPublisher)
		{
			_db = db;
			_customerService = customerService;
			_messageModelProvider = messageModelProvider;
			_genericAttributeService = genericAttributeService;
			_shoppingCartService = shoppingCartService;
			//_backInStockSubscriptionService = backInStockSubscriptionService;
			_languageService = languageService;
			_workContext = workContext;
			_eventPublisher = eventPublisher;
		}

		public LocalizerEx T { get; set; } = NullLocalizer.InstanceEx;
		public ILogger Logger { get; set; } = NullLogger.Instance;

		public Task<Dictionary<string, object>> ExportCustomerAsync(Customer customer)
        {
			// TODO: (core) Port GdprTool.ExportCustomer (too many missing parts at this moment)
			throw new NotImplementedException();
        }

        public async Task AnonymizeCustomerAsync(Customer customer, bool pseudomyzeContent)
        {
			Guard.NotNull(customer, nameof(customer));

			var language = GetLanguage(customer);
			var customerName = customer.GetFullName() ?? customer.Username ?? customer.FindEmail();

			// Set to deleted
			customer.Deleted = true;

			// Unassign roles
			await _db.LoadCollectionAsync(customer, x => x.CustomerRoleMappings);
			var roleMappings = customer.CustomerRoleMappings.ToList();
			var guestRole = await _db.CustomerRoles.FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Guests);
			var insertGuestMapping = !roleMappings.Any(x => x.CustomerRoleId == guestRole.Id);

			roleMappings
				.Where(x => x.CustomerRoleId != guestRole.Id)
				.Each(x => _db.CustomerRoleMappings.Remove(x));

			if (insertGuestMapping)
			{
				_db.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
			}

			//// TODO: (core) Delete forum subscriptions for GDPR anonymization per external module (publish event)
			//// Delete forum subscriptions
			//var forumSubscriptions = _forumService.GetAllSubscriptions(customer.Id, 0, 0, 0, int.MaxValue);
			//foreach (var forumSub in forumSubscriptions)
			//{
			//	_forumService.DeleteSubscription(forumSub);
			//}

			//// TODO: (ms) (core) Delete BackInStock subscriptions for GDPR anonymization once service is ported.
			//// Delete BackInStock subscriptions
			//var backInStockSubscriptions = _backInStockSubscriptionService.GetAllSubscriptionsByCustomerId(customer.Id, 0, 0, int.MaxValue);
			//foreach (var stockSub in backInStockSubscriptions)
			//{
			//	_backInStockSubscriptionService.DeleteSubscription(stockSub);
			//}

			// We don't need to mask generic attrs, we just delete them.
			customer.GenericAttributes.DeleteAll();

			// Customer Data
			AnonymizeData(customer, x => x.Username, IdentifierDataType.UserName, language);
			AnonymizeData(customer, x => x.Email, IdentifierDataType.EmailAddress, language);
			AnonymizeData(customer, x => x.LastIpAddress, IdentifierDataType.IpAddress, language);
			if (pseudomyzeContent)
			{
				AnonymizeData(customer, x => x.AdminComment, IdentifierDataType.LongText, language);
				AnonymizeData(customer, x => x.LastLoginDateUtc, IdentifierDataType.DateTime, language);
				AnonymizeData(customer, x => x.LastActivityDateUtc, IdentifierDataType.DateTime, language);
			}

			// Addresses
			foreach (var address in customer.Addresses)
			{
				AnonymizeAddress(address, language);
			}

			//// Private messages
			//if (pseudomyzeContent)
			//{
			//	//// TODO: (core) Anonymize private messages per external module (publish event)
			//	var privateMessages = _forumService.GetAllPrivateMessages(0, customer.Id, 0, null, null, null, 0, int.MaxValue);
			//	foreach (var msg in privateMessages)
			//	{
			//		AnonymizeData(msg, x => x.Subject, IdentifierDataType.Text, language);
			//		AnonymizeData(msg, x => x.Text, IdentifierDataType.LongText, language);
			//	}
			//}

			//// Forum topics
			//if (pseudomyzeContent)
			//{
			//	//// TODO: (core) Anonymize ForumTopics per external module (publish event)
			//	foreach (var topic in customer.ForumTopics)
			//	{
			//		AnonymizeData(topic, x => x.Subject, IdentifierDataType.Text, language);
			//	}
			//}

			//// Forum posts
			//// TODO: (core) Anonymize ForumPosts per external module (publish event)
			//foreach (var post in customer.ForumPosts)
			//{
			//	AnonymizeData(post, x => x.IPAddress, IdentifierDataType.IpAddress, language);
			//	if (pseudomyzeContent)
			//	{
			//		AnonymizeData(post, x => x.Text, IdentifierDataType.LongText, language);
			//	}
			//}

			// Customer Content
			foreach (var item in customer.CustomerContent)
			{
				AnonymizeData(item, x => x.IpAddress, IdentifierDataType.IpAddress, language);

				if (pseudomyzeContent)
				{
					switch (item)
					{
						case ProductReview c:
							AnonymizeData(c, x => x.ReviewText, IdentifierDataType.LongText, language);
							AnonymizeData(c, x => x.Title, IdentifierDataType.Text, language);
							break;
						////// TODO: (core) Anonymize NewsComment per external module (publish event)
						//case NewsComment c:
						//	AnonymizeData(c, x => x.CommentText, IdentifierDataType.LongText, language);
						//	AnonymizeData(c, x => x.CommentTitle, IdentifierDataType.Text, language);
						//	break;
						////// TODO: (core) Anonymize BlogComment per external module (publish event)
						//case BlogComment c:
						//	AnonymizeData(c, x => x.CommentText, IdentifierDataType.LongText, language);
						//	break;
					}
				}
			}

			//// Anonymize Order IPs
			//// TBD: Don't! Doesn't feel right because of fraud detection etc.
			//foreach (var order in customer.Orders)
			//{
			//	AnonymizeData(order, x => x.CustomerIp, IdentifierDataType.IpAddress, language);
			//}

			// SAVE!!!
			await _db.SaveChangesAsync();

			// Now it is safe to delete shopping cart & wishlist
			await _db.ShoppingCartItems
				.ApplyExpiredCartItemsFilter(DateTime.UtcNow, customer)
				.BatchDeleteAsync();

			// Log
			Logger.Info(T("Gdpr.Anonymize.Success", language.Id, customerName));
		}

        public void AnonymizeData<TEntity>(TEntity entity, Expression<Func<TEntity, object>> expression, IdentifierDataType type, Language language = null) where TEntity : BaseEntity
        {
			Guard.NotNull(entity, nameof(entity));
			Guard.NotNull(expression, nameof(expression));

			var originalValue = expression.Compile().Invoke(entity);
			object maskedValue = null;

			if (originalValue is DateTime d)
			{
				maskedValue = MinDate;
			}
			else if (originalValue is string s)
			{
				if (s.IsEmpty())
				{
					return;
				}

				language ??= GetLanguage(entity as Customer);

				switch (type)
				{
					case IdentifierDataType.Address:
					case IdentifierDataType.Name:
					case IdentifierDataType.Text:
						maskedValue = T("Gdpr.DeletedText", language.Id).Value;
						break;
					case IdentifierDataType.LongText:
						maskedValue = T("Gdpr.DeletedLongText", language.Id).Value;
						break;
					case IdentifierDataType.EmailAddress:
						//maskedValue = s.Hash(Encoding.ASCII, true) + "@anony.mous";
						maskedValue = HashCodeCombiner.Start()
							.Add(entity.GetHashCode())
							.Add(s)
							.CombinedHashString + "@anony.mous";
						break;
					case IdentifierDataType.Url:
						maskedValue = "https://anony.mous";
						break;
					case IdentifierDataType.IpAddress:
						maskedValue = AnonymizeIpAddress(s);
						break;
					case IdentifierDataType.UserName:
						maskedValue = T("Gdpr.Anonymous", language.Id).Value.ToLower();
						break;
					case IdentifierDataType.PhoneNumber:
						maskedValue = "555-00000";
						break;
					case IdentifierDataType.PostalCode:
						maskedValue = "00000";
						break;
					case IdentifierDataType.DateTime:
						maskedValue = MinDate.ToString(CultureInfo.InvariantCulture);
						break;
				}
			}

			if (maskedValue != null)
			{
				var pi = expression.ExtractPropertyInfo();
				pi.SetValue(entity, maskedValue);
			}
		}

		private void AnonymizeAddress(Address address, Language language)
		{
			AnonymizeData(address, x => x.Address1, IdentifierDataType.Address, language);
			AnonymizeData(address, x => x.Address2, IdentifierDataType.Address, language);
			AnonymizeData(address, x => x.City, IdentifierDataType.Address, language);
			AnonymizeData(address, x => x.Company, IdentifierDataType.Address, language);
			AnonymizeData(address, x => x.Email, IdentifierDataType.EmailAddress, language);
			AnonymizeData(address, x => x.FaxNumber, IdentifierDataType.PhoneNumber, language);
			AnonymizeData(address, x => x.FirstName, IdentifierDataType.Name, language);
			AnonymizeData(address, x => x.LastName, IdentifierDataType.Name, language);
			AnonymizeData(address, x => x.PhoneNumber, IdentifierDataType.PhoneNumber, language);
			AnonymizeData(address, x => x.ZipPostalCode, IdentifierDataType.PostalCode, language);
		}

		/// <summary>
		/// Returns an anonymized IPv4 or IPv6 address.
		/// </summary>
		/// <param name="ipAddress">The IPv4 or IPv6 address to be anonymized.</param>
		/// <returns>The anonymized IP address.</returns>
		protected virtual string AnonymizeIpAddress(string ipAddress)
		{
			try
			{
				var ip = IPAddress.Parse(ipAddress);

				switch (ip.AddressFamily)
				{
					case AddressFamily.InterNetwork:
						break;
					case AddressFamily.InterNetworkV6:
						// Map to IPv4 first
						ip = ip.MapToIPv4();
						break;
					default:
						// we only support IPv4 and IPv6
						return "0.0.0.0";
				}

				// Keep the first 3 bytes and append ".0"
				return string.Join(".", ip.GetAddressBytes().Take(3)) + ".0";
			}
			catch
			{
				return null;
			}
		}

		private Language GetLanguage(Customer customer)
		{
			if (customer == null)
				return null;

			var language = _db.Languages.FindById(customer.GenericAttributes.LanguageId ?? 0, false);

			if (language == null || !language.Published)
			{
				language = _workContext.WorkingLanguage;
			}

			return language;
		}
	}
}
