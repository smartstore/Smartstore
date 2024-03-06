using System.Linq.Expressions;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers
{
    public class CustomerController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly INewsletterSubscriptionService _newsletterSubscriptionService;
        private readonly ITaxService _taxService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IMediaService _mediaService;
        private readonly IDownloadService _downloadService;
        private readonly ICurrencyService _currencyService;
        private readonly IShippingService _shippingService;
        private readonly IMessageFactory _messageFactory;
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CustomerController(
            SmartDbContext db,
            INewsletterSubscriptionService newsletterSubscriptionService,
            ITaxService taxService,
            ILocalizationService localizationService,
            IOrderProcessingService orderProcessingService,
            IOrderCalculationService orderCalculationService,
            IOrderService orderService,
            IPaymentService paymentService,
            IMediaService mediaService,
            IDownloadService downloadService,
            ICurrencyService currencyService,
            IShippingService shippingService,
            IMessageFactory messageFactory,
            UserManager<Customer> userManager,
            SignInManager<Customer> signInManager,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            IDateTimeHelper dateTimeHelper,
            ProductUrlHelper productUrlHelper,
            DateTimeSettings dateTimeSettings,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings,
            MediaSettings mediaSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _newsletterSubscriptionService = newsletterSubscriptionService;
            _taxService = taxService;
            _localizationService = localizationService;
            _orderProcessingService = orderProcessingService;
            _orderCalculationService = orderCalculationService;
            _orderService = orderService;
            _paymentService = paymentService;
            _mediaService = mediaService;
            _downloadService = downloadService;
            _currencyService = currencyService;
            _shippingService = shippingService;
            _messageFactory = messageFactory;
            _userManager = userManager;
            _signInManager = signInManager;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _dateTimeHelper = dateTimeHelper;
            _productUrlHelper = productUrlHelper;
            _dateTimeSettings = dateTimeSettings;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
            _localizationSettings = localizationSettings;
            _orderSettings = orderSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _mediaSettings = mediaSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<IActionResult> Info()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var model = new CustomerInfoModel();
            await PrepareCustomerInfoModelAsync(model, customer, false);

            return View(model);
        }

        [HttpPost]
        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> Info(CustomerInfoModel model)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            if (model.Email.IsEmpty())
            {
                ModelState.AddModelError(string.Empty, T("Account.Register.Errors.EmailIsNotProvided"));
            }
            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && _customerSettings.AllowUsersToChangeUsernames && model.Username.IsEmpty())
            {
                ModelState.AddModelError(string.Empty, T("Account.Register.Errors.UsernameIsNotProvided"));
            }

            // INFO: update email and username requires SaveChangesAttribute to be set to 'false'.
            var newEmail = model.Email.TrimSafe();
            var newUsername = model.Username.TrimSafe();

            // Email.
            if (ModelState.IsValid && !newEmail.Equals(customer.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(customer, newEmail);
                var result = await _userManager.ChangeEmailAsync(customer, newEmail, token);
                if (result.Succeeded)
                {
                    // Re-authenticate (if usernames are disabled).
                    if (_customerSettings.CustomerLoginType == CustomerLoginType.Email)
                    {
                        await _signInManager.SignInAsync(customer, true);
                    }
                }
                else
                {
                    result.Errors.Select(x => x.Description).Distinct()
                        .Each(x => ModelState.AddModelError(nameof(model.Email), x));
                }
            }

            // Username.
            if (ModelState.IsValid
                && _customerSettings.CustomerLoginType != CustomerLoginType.Email
                && _customerSettings.AllowUsersToChangeUsernames
                && !newUsername.EqualsNoCase(customer.Username))
            {
                var result = await _userManager.SetUserNameAsync(customer, newUsername);
                if (result.Succeeded)
                {
                    // Re-authenticate.
                    await _signInManager.SignInAsync(customer, true);
                }
                else
                {
                    result.Errors.Select(x => x.Description).Distinct()
                        .Each(x => ModelState.AddModelError(nameof(model.Username), x));
                }
            }

            try
            {
                if (ModelState.IsValid)
                {
                    customer.FirstName = model.FirstName;
                    customer.LastName = model.LastName;

                    // VAT number.
                    if (_taxSettings.EuVatEnabled)
                    {
                        var prevVatNumber = customer.GenericAttributes.VatNumber;
                        customer.GenericAttributes.VatNumber = model.VatNumber;

                        if (prevVatNumber != model.VatNumber)
                        {
                            var vatCheckResult = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                            customer.VatNumberStatusId = (int)vatCheckResult.Status;

                            // Send VAT number admin notification.
                            if (model.VatNumber.HasValue() && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                            {
                                await _messageFactory.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatCheckResult.Address, _localizationSettings.DefaultAdminLanguageId);
                            }
                        }
                    }

                    // Customer number.
                    if (_customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled)
                    {
                        customer.CustomerNumber = model.CustomerNumber;
                    }

                    if (_customerSettings.DateOfBirthEnabled)
                    {
                        customer.BirthDate = model.DateOfBirth;
                    }

                    if (_customerSettings.CompanyEnabled)
                    {
                        customer.Company = model.Company;
                    }
                    if (_customerSettings.TitleEnabled)
                    {
                        customer.Title = model.Title;
                    }
                    if (_customerSettings.GenderEnabled)
                    {
                        customer.Gender = model.Gender;
                    }
                    if (_customerSettings.StreetAddressEnabled)
                    {
                        customer.GenericAttributes.StreetAddress = model.StreetAddress;
                    }
                    if (_customerSettings.StreetAddress2Enabled)
                    {
                        customer.GenericAttributes.StreetAddress2 = model.StreetAddress2;
                    }
                    if (_customerSettings.ZipPostalCodeEnabled)
                    {
                        customer.GenericAttributes.ZipPostalCode = model.ZipPostalCode;
                    }
                    if (_customerSettings.CityEnabled)
                    {
                        customer.GenericAttributes.City = model.City;
                    }
                    if (_customerSettings.CountryEnabled)
                    {
                        customer.GenericAttributes.CountryId = model.CountryId;
                    }
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                    {
                        customer.GenericAttributes.StateProvinceId = model.StateProvinceId;
                    }
                    if (_customerSettings.PhoneEnabled)
                    {
                        customer.GenericAttributes.Phone = model.Phone;
                    }
                    if (_customerSettings.FaxEnabled)
                    {
                        customer.GenericAttributes.Fax = model.Fax;
                    }
                    if (_customerSettings.NewsletterEnabled)
                    {
                        await _newsletterSubscriptionService.ApplySubscriptionAsync(model.Newsletter, customer.Email, Services.StoreContext.CurrentStore.Id);
                    }
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    {
                        customer.TimeZoneId = model.TimeZoneId;
                    }

                    if (_shoppingCartSettings.QuickCheckoutEnabled)
                    {
                        customer.GenericAttributes.PreferredShippingOption = new() { ShippingMethodId = model.PreferredShippingMethodId ?? 0 };
                        customer.GenericAttributes.PreferredPaymentMethod = model.PreferredPaymentMethod.NullEmpty();
                    }

                    var updateResult = await _userManager.UpdateAsync(customer);
                    if (updateResult.Succeeded)
                    {
                        await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, customer, Request.Form));

                        return RedirectToAction(nameof(Info));
                    }
                    else
                    {
                        updateResult.Errors.Select(x => x.Description).Distinct()
                            .Each(x => ModelState.AddModelError(string.Empty, x));
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            await PrepareCustomerInfoModelAsync(model, customer, false);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            username = username.TrimSafe();

            var usernameAvailable = false;
            string statusText = null;

            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && username.HasValue())
            {
                var customer = Services.WorkContext.CurrentCustomer;
                if (customer != null && customer.Username != null && customer.Username.EqualsNoCase(username))
                {
                    statusText = T("Account.CheckUsernameAvailability.CurrentUsername");
                }
                else
                {
                    var userExists = await _db.Customers
                        .IgnoreQueryFilters()
                        .ApplyIdentFilter(null, username, null, true)
                        .AnyAsync();

                    if (!userExists)
                    {
                        statusText = T("Account.CheckUsernameAvailability.Available");
                        usernameAvailable = true;
                    }
                }
            }

            return Json(new
            {
                Available = usernameAvailable,
                Text = statusText.NullEmpty() ?? T("Account.CheckUsernameAvailability.NotAvailable")
            });
        }

        #region Addresses

        public async Task<IActionResult> Addresses()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var models = await customer.Addresses.MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled);

            return View(models);
        }

        public async Task<IActionResult> AddressDelete(int id)
        {
            if (id < 1)
            {
                return NotFound();
            }

            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
            if (address != null)
            {
                customer.RemoveAddress(address);
                // Now delete the address record.
                _db.Addresses.Remove(address);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Addresses));
        }

        public async Task<IActionResult> AddressAdd()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var model = await new Address().MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddressAdd(AddressModel model)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var address = new Address();

            if (ModelState.IsValid)
            {
                await model.MapAsync(address);
                customer.Addresses.Add(address);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Addresses));
            }

            await address.MapAsync(model, customer, _shoppingCartSettings.QuickCheckoutEnabled);

            return View(model);
        }

        public async Task<IActionResult> AddressEdit(int id)
        {
            if (id < 1)
            {
                return NotFound();
            }

            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.Where(a => a.Id == id).FirstOrDefault();
            if (address == null)
            {
                return RedirectToAction(nameof(Addresses));
            }

            var model = await address.MapAsync(customer, _shoppingCartSettings.QuickCheckoutEnabled);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddressEdit(AddressModel model, int id)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            // Find address and ensure that it belongs to the current customer.
            var address = customer.Addresses.FirstOrDefault(x => x.Id == id);
            if (address == null)
            {
                return RedirectToAction(nameof(Addresses));
            }

            if (ModelState.IsValid)
            {
                await model.MapAsync(address, customer, _shoppingCartSettings.QuickCheckoutEnabled);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Addresses));
            }

            await address.MapAsync(model, customer, _shoppingCartSettings.QuickCheckoutEnabled);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var address = customer.Addresses.FirstOrDefault(x => x.Id == id);
            if (address != null)
            {
                var billingAllowed = address.Country?.AllowsBilling ?? true;
                var shippingAllowed = address.Country?.AllowsShipping ?? true;

                if (billingAllowed && shippingAllowed)
                {
                    customer.GenericAttributes.DefaultBillingAddressId = address.Id;
                    customer.GenericAttributes.DefaultShippingAddressId = address.Id;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (!billingAllowed)
                    {
                        NotifyError(T("Order.CountryNotAllowedForBilling", address.Country?.GetLocalized(x => x.Name)));
                    }
                    if (!shippingAllowed)
                    {
                        NotifyError(T("Order.CountryNotAllowedForShipping", address.Country?.GetLocalized(x => x.Name)));
                    }
                }
            }

            return RedirectToAction(nameof(Addresses));
        }

        #endregion

        #region Orders

        public async Task<IActionResult> Orders(int? page, int? recurringPaymentsPage)
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var ordersPageIndex = Math.Max((page ?? 0) - 1, 0);
            var rpPageIndex = Math.Max((recurringPaymentsPage ?? 0) - 1, 0);

            var model = await PrepareCustomerOrderListModelAsync(customer, ordersPageIndex, rpPageIndex);
            model.OrdersPage = page;
            model.RecurringPaymentsPage = recurringPaymentsPage;

            return View(model);
        }

        [HttpPost, ActionName("Orders")]
        [FormValueRequired(FormValueRequirementOperator.StartsWith, "cancelRecurringPayment")]
        public async Task<IActionResult> CancelRecurringPayment()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var form = HttpContext.Request.Form;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            // Get recurring payment identifier.
            var recurringPaymentId = 0;
            foreach (var formValue in form.Keys)
            {
                if (formValue.StartsWith("cancelRecurringPayment", StringComparison.InvariantCultureIgnoreCase))
                {
                    recurringPaymentId = Convert.ToInt32(formValue["cancelRecurringPayment".Length..]);
                }
            }

            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.InitialOrder)
                .ThenInclude(x => x.Customer)
                .FindByIdAsync(recurringPaymentId, false);

            if (recurringPayment == null)
            {
                return RedirectToAction("Orders");
            }

            if (await _orderProcessingService.CanCancelRecurringPaymentAsync(recurringPayment, customer))
            {
                var model = await PrepareCustomerOrderListModelAsync(customer, 0, 0);

                try
                {
                    await _orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    model.CancelRecurringPaymentErrors.Add(ex.Message);
                }

                return View(model);
            }

            return RedirectToAction("Orders");
        }

        #endregion

        #region Return request

        public async Task<IActionResult> ReturnRequests()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var model = new CustomerReturnRequestsModel();
            var returnRequests = await _db.ReturnRequests
                .AsNoTracking()
                .ApplyStandardFilter(storeId: Services.StoreContext.CurrentStore.Id, customerId: customer.Id)
                .ToListAsync();

            foreach (var returnRequest in returnRequests)
            {
                var orderItem = await _db.OrderItems
                    .Include(x => x.Product)
                    .FindByIdAsync(returnRequest.OrderItemId, false);

                if (orderItem != null)
                {
                    var itemModel = new CustomerReturnRequestsModel.ReturnRequestModel
                    {
                        Id = returnRequest.Id,
                        ReturnRequestStatus = returnRequest.ReturnRequestStatus.GetLocalizedEnum(Services.WorkContext.WorkingLanguage.Id),
                        ProductId = orderItem.Product.Id,
                        ProductName = orderItem.Product.GetLocalized(x => x.Name),
                        ProductSeName = await orderItem.Product.GetActiveSlugAsync(),
                        Quantity = returnRequest.Quantity,
                        ReturnAction = returnRequest.RequestedAction,
                        ReturnReason = returnRequest.ReasonForReturn,
                        Comments = returnRequest.CustomerComments,
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(returnRequest.CreatedOnUtc, DateTimeKind.Utc)
                    };

                    itemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(itemModel.ProductSeName, orderItem);

                    model.Items.Add(itemModel);
                }
            }

            return View(model);
        }

        #endregion

        #region Downloadable products

        public async Task<IActionResult> DownloadableProducts()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            var model = new CustomerDownloadableProductsModel();

            var items = await _db.OrderItems
                .AsNoTracking()
                .ApplyStandardFilter(customerId: customer.Id)
                .Include(x => x.Product)
                .Include(x => x.Order)
                .Where(x => x.Product.IsDownload)
                .ToListAsync();

            foreach (var item in items)
            {
                var itemModel = new CustomerDownloadableProductsModel.DownloadableProductsModel
                {
                    OrderItemGuid = item.OrderItemGuid,
                    OrderId = item.OrderId,
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(item.Order.CreatedOnUtc, DateTimeKind.Utc),
                    ProductName = item.Product.GetLocalized(x => x.Name),
                    ProductSeName = await item.Product.GetActiveSlugAsync(),
                    ProductAttributes = item.AttributeDescription,
                    ProductId = item.ProductId
                };

                itemModel.ProductUrl = await _productUrlHelper.GetProductPathAsync(item.ProductId, itemModel.ProductSeName, item.AttributeSelection);

                model.Items.Add(itemModel);

                itemModel.IsDownloadAllowed = _downloadService.IsDownloadAllowed(item);

                if (itemModel.IsDownloadAllowed)
                {
                    var downloads = (await _db.Downloads
                        .AsNoTracking()
                        .ApplyEntityFilter(item.Product)
                        .ApplyVersionFilter()
                        .Include(x => x.MediaFile)
                        .ToListAsync())
                        .OrderByVersion();

                    itemModel.DownloadVersions = downloads
                        .Select(x => new DownloadVersion
                        {
                            DownloadId = x.Id,
                            FileVersion = x.FileVersion,
                            FileName = x.UseDownloadUrl ? x.DownloadUrl : x.MediaFile.Name,
                            DownloadGuid = x.DownloadGuid,
                            Changelog = x.Changelog
                        })
                        .ToList();
                }

                if (_downloadService.IsLicenseDownloadAllowed(item))
                {
                    itemModel.LicenseId = item.LicenseDownloadId ?? 0;
                }
            }

            return View(model);
        }

        /// <summary>
        /// Gets the user agreement for purchased download.
        /// </summary>
        /// <param name="id">OrderItemId <see cref="OrderItem.OrderItemGuid"/></param>
        /// <param name="fileVersion">Requested version of purchased download.</param>
        public async Task<IActionResult> UserAgreement(Guid id, string fileVersion = "")
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var orderItem = await _db.OrderItems
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.OrderItemGuid == id)
                .FirstOrDefaultAsync();

            if (orderItem == null)
            {
                NotifyError(T("Customer.UserAgreement.OrderItemNotFound"));
                return RedirectToRoute("Homepage");
            }

            var product = orderItem.Product;
            if (product == null || !product.HasUserAgreement)
            {
                NotifyError(T("Customer.UserAgreement.ProductNotFound"));
                return RedirectToRoute("Homepage");
            }

            var model = new UserAgreementModel
            {
                UserAgreementText = product.UserAgreementText,
                OrderItemGuid = id,
                FileVersion = fileVersion
            };

            return View(model);
        }

        #endregion

        #region Avatar

        public async Task<IActionResult> Avatar()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            if (!_customerSettings.AllowCustomersToUploadAvatars)
            {
                return RedirectToAction(nameof(Info));
            }

            var model = new CustomerAvatarEditModel
            {
                Avatar = await customer.MapAsync(null, true, true),
                MaxFileSizeValue = _customerSettings.MaxAvatarFileSize,
                MaxFileSize = _customerSettings.MaxAvatarFileSize.Kilobytes().Humanize()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar()
        {
            long maxFileSize = _customerSettings.MaxAvatarFileSize * 1024;
            var customer = Services.WorkContext.CurrentCustomer;
            var success = false;
            string avatarUrl = null;

            if (customer.IsRegistered() && _customerSettings.AllowCustomersToUploadAvatars)
            {
                var uploadedFile = Request.Form.Files[0];
                if (uploadedFile != null && uploadedFile.FileName.HasValue())
                {
                    if (uploadedFile.Length <= maxFileSize)
                    {
                        var oldAvatar = await _db.MediaFiles.FindByIdAsync(customer.GenericAttributes.AvatarPictureId ?? 0);
                        if (oldAvatar != null)
                        {
                            await _mediaService.DeleteFileAsync(oldAvatar, true);
                        }

                        var path = _mediaService.CombinePaths(SystemAlbumProvider.Customers, PathUtility.SanitizeFileName(uploadedFile.FileName));

                        using var stream = uploadedFile.OpenReadStream();
                        var newAvatar = await _mediaService.SaveFileAsync(path, stream, false, DuplicateFileHandling.Rename);

                        if (newAvatar != null)
                        {
                            customer.GenericAttributes.AvatarPictureId = newAvatar.Id;
                            await _db.SaveChangesAsync();

                            avatarUrl = _mediaService.GetUrl(newAvatar, _mediaSettings.AvatarPictureSize, null, false);
                            success = avatarUrl.HasValue();
                        }
                    }
                    else
                    {
                        NotifyError(T("Account.Avatar.MaximumUploadedFileSize", _customerSettings.MaxAvatarFileSize.Kilobytes().Humanize()));
                    }
                }
            }

            return Json(new { success, avatarUrl });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAvatar()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (customer.IsRegistered() && _customerSettings.AllowCustomersToUploadAvatars)
            {
                var avatar = await _db.MediaFiles.FindByIdAsync((int)customer.GenericAttributes.AvatarPictureId);
                if (avatar != null)
                {
                    await _mediaService.DeleteFileAsync(avatar, true);
                }

                customer.GenericAttributes.AvatarPictureId = 0;
                customer.GenericAttributes.AvatarColor = null;

                await _db.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        #endregion

        #region Reward points

        public IActionResult RewardPoints()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (!customer.IsRegistered())
            {
                return ChallengeOrForbid();
            }

            if (!_rewardPointsSettings.Enabled)
            {
                return RedirectToAction("Info");
            }

            var model = new CustomerRewardPointsModel();
            foreach (var rph in customer.RewardPointsHistory.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id))
            {
                model.RewardPoints.Add(new CustomerRewardPointsModel.RewardPointsHistoryModel()
                {
                    Points = rph.Points,
                    PointsBalance = rph.PointsBalance,
                    Message = rph.Message,
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
                });
            }

            int rewardPointsBalance = customer.GetRewardPointsBalance();
            var rewardPointsAmountBase = _orderCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
            var rewardPointsAmount = _currencyService.ConvertFromPrimaryCurrency(rewardPointsAmountBase.Amount, Services.WorkContext.WorkingCurrency);
            model.RewardPointsBalanceFormatted = T("RewardPoints.CurrentBalance", rewardPointsBalance, rewardPointsAmount.ToString());

            return View(model);
        }

        #endregion

        #region Stock subscriptions

        public async Task<IActionResult> StockSubscriptions(int? page)
        {
            if (_customerSettings.HideBackInStockSubscriptionsTab)
            {
                return RedirectToAction("Info");
            }

            int pageIndex = 0;
            if (page > 0)
            {
                pageIndex = page.Value - 1;
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var list = await _db.BackInStockSubscriptions
                .AsNoTracking()
                .Include(x => x.Product)
                .ApplyStandardFilter(customerId: customer.Id, storeId: Services.StoreContext.CurrentStore.Id)
                .ToPagedList(pageIndex, 10)
                .LoadAsync();

            var model = new CustomerStockSubscriptionsModel(list);

            foreach (var subscription in list)
            {
                var product = subscription.Product;

                if (product != null)
                {
                    var subscriptionModel = new StockSubscriptionModel
                    {
                        Id = subscription.Id,
                        ProductId = product.Id,
                        ProductName = product.GetLocalized(x => x.Name),
                        SeName = await product.GetActiveSlugAsync()
                    };
                    model.Subscriptions.Add(subscriptionModel);
                }
            }

            return View(model);
        }

        [HttpPost, ActionName("StockSubscriptions")]
        public async Task<IActionResult> StockSubscriptionsPOST()
        {
            var form = HttpContext.Request.Form;
            var customerId = Services.WorkContext.CurrentCustomer.Id;

            foreach (var key in form.Keys)
            {
                var value = form[key];

                if (value.Equals("on") && key.StartsWith("biss", StringComparison.InvariantCultureIgnoreCase))
                {
                    var id = key.Replace("biss", string.Empty).Trim();

                    if (int.TryParse(id, out var subscriptionId))
                    {
                        var subscription = await _db.BackInStockSubscriptions.FindByIdAsync(subscriptionId);
                        if (subscription != null && subscription.CustomerId == customerId)
                        {
                            _db.BackInStockSubscriptions.Remove(subscription);
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("StockSubscriptions");
        }

        #endregion

        #region Utilities

        private async Task PrepareCustomerInfoModelAsync(CustomerInfoModel model, Customer customer, bool excludeProperties)
        {
            Guard.NotNull(model);
            Guard.NotNull(customer);

            var store = Services.StoreContext.CurrentStore;
            var language = Services.WorkContext.WorkingLanguage;

            model.Id = customer.Id;
            model.AllowCustomersToSetTimeZone = _dateTimeSettings.AllowCustomersToSetTimeZone;

            ViewBag.AvailableTimeZones = _dateTimeHelper.GetSystemTimeZones()
                .ToSelectListItems(excludeProperties ? model.TimeZoneId : _dateTimeHelper.CurrentTimeZone.Id);

            if (!excludeProperties)
            {
                var newsletterSubscription = await _db.NewsletterSubscriptions
                    .AsNoTracking()
                    .ApplyMailAddressFilter(customer.Email, store.Id)
                    .FirstOrDefaultAsync();

                model.Company = customer.Company;
                model.Title = customer.Title;
                model.FirstName = customer.FirstName;
                model.LastName = customer.LastName;
                model.Gender = customer.Gender;
                model.CustomerNumber = customer.CustomerNumber;
                model.Email = customer.Email;
                model.Username = customer.Username;
                model.DateOfBirth = customer.BirthDate;
                model.VatNumber = customer.GenericAttributes.VatNumber;
                model.StreetAddress = customer.GenericAttributes.StreetAddress;
                model.StreetAddress2 = customer.GenericAttributes.StreetAddress2;
                model.City = customer.GenericAttributes.City;
                model.ZipPostalCode = customer.GenericAttributes.ZipPostalCode;
                model.CountryId = Convert.ToInt32(customer.GenericAttributes.CountryId);
                model.StateProvinceId = Convert.ToInt32(customer.GenericAttributes.StateProvinceId);
                model.Phone = customer.GenericAttributes.Phone;
                model.Fax = customer.GenericAttributes.Fax;
                model.Newsletter = newsletterSubscription != null && newsletterSubscription.Active;
            }
            else
            {
                if (_customerSettings.CustomerLoginType != CustomerLoginType.Email && !_customerSettings.AllowUsersToChangeUsernames)
                {
                    model.Username = customer.Username;
                }
            }

            // Countries and state provinces.
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            {
                var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(model.CountryId);
                ViewBag.AvailableStates = stateProvinces.ToSelectListItems(model.StateProvinceId) ?? new List<SelectListItem>
                {
                    new() { Text = T("Address.OtherNonUS"), Value = "0" }
                };
            }

            model.FirstNameRequired = _customerSettings.FirstNameRequired;
            model.LastNameRequired = _customerSettings.LastNameRequired;
            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            model.VatNumberStatusNote = ((VatNumberStatus)customer.VatNumberStatusId).GetLocalizedEnum(language.Id);
            model.GenderEnabled = _customerSettings.GenderEnabled;
            model.TitleEnabled = _customerSettings.TitleEnabled;
            model.DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled;
            model.CompanyEnabled = _customerSettings.CompanyEnabled;
            model.CompanyRequired = _customerSettings.CompanyRequired;
            model.StreetAddressEnabled = _customerSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _customerSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _customerSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _customerSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _customerSettings.ZipPostalCodeRequired;
            model.CityEnabled = _customerSettings.CityEnabled;
            model.CityRequired = _customerSettings.CityRequired;
            model.CountryEnabled = _customerSettings.CountryEnabled;
            model.StateProvinceEnabled = _customerSettings.StateProvinceEnabled;
            model.PhoneEnabled = _customerSettings.PhoneEnabled;
            model.PhoneRequired = _customerSettings.PhoneRequired;
            model.FaxEnabled = _customerSettings.FaxEnabled;
            model.FaxRequired = _customerSettings.FaxRequired;
            model.NewsletterEnabled = _customerSettings.NewsletterEnabled;
            model.UsernamesEnabled = _customerSettings.CustomerLoginType != CustomerLoginType.Email;
            model.AllowUsersToChangeUsernames = _customerSettings.AllowUsersToChangeUsernames;
            model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
            model.DisplayCustomerNumber = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
                && _customerSettings.CustomerNumberVisibility != CustomerNumberVisibility.None;

            model.CustomerNumberEnabled = _customerSettings.CustomerNumberMethod != CustomerNumberMethod.Disabled
                && (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.Editable
                || (_customerSettings.CustomerNumberVisibility == CustomerNumberVisibility.EditableIfEmpty && model.CustomerNumber.IsEmpty()));

            // External authentication.
            await _db.LoadCollectionAsync(customer, x => x.ExternalAuthenticationRecords);

            var authSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var authProviders = _providerManager.GetAllProviders<IExternalAuthenticationMethod>()
                .ToDictionarySafe(x => x.Metadata.SystemName, x => x, StringComparer.OrdinalIgnoreCase);

            model.AssociatedExternalAuthRecords = customer.ExternalAuthenticationRecords
                .Select(x =>
                {
                    var methodName = authProviders.TryGetValue(x.ProviderSystemName, out var provider)
                        ? _moduleManager.GetLocalizedFriendlyName(provider.Metadata).NullEmpty() ?? provider.Metadata.FriendlyName.NullEmpty()
                        : null;

                    if (methodName == null)
                    {
                        // Method has a system name mapping.
                        var authScheme = authSchemes.FirstOrDefault(scheme => x.ProviderSystemName.Contains(scheme.Name, StringComparison.OrdinalIgnoreCase));
                        methodName = authScheme?.Name;
                    }

                    return new CustomerInfoModel.AssociatedExternalAuthModel
                    {
                        Id = x.Id,
                        Email = x.Email,
                        ExternalIdentifier = x.ExternalIdentifier,
                        AuthMethodName = methodName ?? x.ProviderSystemName
                    };
                })
                .ToList();

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(store.Id);
                var paymentProviders = await _paymentService.LoadActivePaymentProvidersAsync(null, store.Id);
                var preferredShippingMethodId = customer.GenericAttributes.PreferredShippingOption?.ShippingMethodId ?? 0;
                var preferredPaymentMethod = customer.GenericAttributes.PreferredPaymentMethod;

                ViewBag.ShippingMethods = shippingMethods
                    .Select(x => new SelectListItem
                    {
                        Text = x.GetLocalized(y => y.Name),
                        Value = x.Id.ToString(),
                        Selected = x.Id == preferredShippingMethodId
                    })
                    .ToList();

                ViewBag.PaymentMethods = paymentProviders
                    .Where(x => !x.Value.RequiresPaymentSelection)
                    .Select(x => x.Metadata)
                    .Select(x => new SelectListItem
                    {
                        Text = _moduleManager.GetLocalizedFriendlyName(x) ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                        Value = x.SystemName,
                        Selected = x.SystemName.EqualsNoCase(preferredPaymentMethod)
                    })
                    .ToList();
            }
        }

        private async Task<CustomerOrderListModel> PrepareCustomerOrderListModelAsync(Customer customer, int orderPageIndex, int recurringPaymentPageIndex)
        {
            Guard.NotNull(customer, nameof(customer));

            var store = Services.StoreContext.CurrentStore;
            var model = new CustomerOrderListModel();

            var orders = await _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(customer.Id, _orderSettings.DisplayOrdersOfAllStores ? null : store.Id)
                .ToPagedList(orderPageIndex, _orderSettings.OrderListPageSize)
                .LoadAsync();

            var customerCurrencyCodes = orders.ToDistinctArray(x => x.CustomerCurrencyCode);
            var customerCurrencies = (await _db.Currencies
                .AsNoTracking()
                .Where(x => customerCurrencyCodes.Contains(x.CurrencyCode))
                .ToListAsync())
                .ToDictionarySafe(x => x.CurrencyCode, x => x, StringComparer.OrdinalIgnoreCase);

            var orderModels = await orders
                .SelectAwait(async x =>
                {
                    customerCurrencies.TryGetValue(x.CustomerCurrencyCode, out var customerCurrency);

                    (var orderTotal, _) = await _orderService.GetOrderTotalInCustomerCurrencyAsync(x, customerCurrency);

                    var orderModel = new CustomerOrderListModel.OrderDetailsModel
                    {
                        Id = x.Id,
                        OrderNumber = x.GetOrderNumber(),
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                        OrderStatus = _localizationService.GetLocalizedEnum(x.OrderStatus),
                        IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(x),
                        OrderTotal = orderTotal
                    };

                    return orderModel;
                })
                .AsyncToList();

            model.Orders = orderModels.ToPagedList(orders.PageIndex, orders.PageSize, orders.TotalCount);

            // Recurring payments.
            var recurringPayments = await _db.RecurringPayments
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.InitialOrder)
                .ThenInclude(x => x.Customer)
                .Include(x => x.RecurringPaymentHistory)
                .ApplyStandardFilter(customerId: customer.Id, storeId: store.Id)
                .ToPagedList(recurringPaymentPageIndex, _orderSettings.RecurringPaymentListPageSize)
                .LoadAsync();

            var rpModels = await recurringPayments
                .SelectAwait(async x =>
                {
                    var nextPaymentDate = await _paymentService.GetNextRecurringPaymentDateAsync(x);

                    return new CustomerOrderListModel.RecurringPaymentModel
                    {
                        Id = x.Id,
                        StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc, DateTimeKind.Utc),
                        CycleInfo = $"{x.CycleLength} {_localizationService.GetLocalizedEnum(x.CyclePeriod)}",
                        NextPayment = nextPaymentDate.HasValue ? _dateTimeHelper.ConvertToUserTime(nextPaymentDate.Value, DateTimeKind.Utc) : null,
                        TotalCycles = x.TotalCycles,
                        CyclesRemaining = await _paymentService.GetRecurringPaymentRemainingCyclesAsync(x),
                        InitialOrderId = x.InitialOrder.Id,
                        CanCancel = await _orderProcessingService.CanCancelRecurringPaymentAsync(x, customer)
                    };
                })
                .AsyncToList();

            model.RecurringPayments = rpModels.ToPagedList(recurringPayments.PageIndex, recurringPayments.PageSize, recurringPayments.TotalCount);

            return model;
        }

        private async Task<Product> GetShippableProduct(Customer customer)
        {
            Expression<Func<Product, bool>> predicate = x => x.Published
                && x.Visibility < ProductVisibility.ProductPage
                && !x.IsDownload
                && !x.IsRecurring
                && x.IsShippingEnabled
                && !x.IsFreeShipping
                && !x.IsSystemProduct
                && x.ProductType == ProductType.SimpleProduct;

            var product = await _db.Orders
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OrderByDescending(x => x.CreatedOnUtc)
                .SelectMany(x => x.OrderItems.Select(y => y.Product))
                .Where(predicate)
                .FirstOrDefaultAsync() 
                ?? await _db.Products.AsNoTracking().Where(predicate).FirstOrDefaultAsync() 
                ?? await _db.Products.FirstOrDefaultAsync();

            return product;
        }

        #endregion
    }
}
