using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly IMessageFactory _messageFactory;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;

        public OrderProcessingService(
            SmartDbContext db,
            ILocalizationService localizationService,
            IMessageFactory messageFactory,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _localizationService = localizationService;
            _messageFactory = messageFactory;
            _rewardPointsSettings = rewardPointsSettings;
            _orderSettings = orderSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Utilities

        protected virtual string FormatTaxRates(SortedDictionary<decimal, decimal> taxRates)
        {
            return string.Join("   ", taxRates.Select(x => "{0}:{1};".FormatInvariant(x.Key.ToString(CultureInfo.InvariantCulture), x.Value.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>
        /// Logs errors and adds order notes. The caller is responsible for database commit.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="errors"></param>
        /// <param name="messageKey"></param>
        protected virtual void ProcessErrors(Order order, IList<string> errors, string messageKey)
        {
            if (errors.Any())
            {
                var msg = T(messageKey, order.GetOrderNumber()).ToString() + " " + string.Join(" ", errors);
                order.AddOrderNote(msg);

                Logger.Error(msg);
            }
        }

        /// <summary>
        /// Applies reward points. The caller is responsible for database commit.
        /// </summary>
        protected virtual void ApplyRewardPoints(Order order, bool reduce, decimal? amount = null)
        {
            if (!_rewardPointsSettings.Enabled ||
                _rewardPointsSettings.PointsForPurchases_Amount <= decimal.Zero ||
                // Ensure that reward points were not added before. We should not add reward points if they were already earned for this order.
                order.RewardPointsWereAdded ||
                order.Customer == null ||
                order.Customer.IsGuest())
            {
                return;
            }

            var rewardAmount = _rewardPointsSettings.PointsForPurchases_Amount * _rewardPointsSettings.PointsForPurchases_Points;

            if (reduce)
            {
                // We use Math.Round here because Truncate increases the risk of inaccuracy of rounding.
                var points = (int)Math.Round((amount ?? order.OrderTotal) / rewardAmount);

                if (order.RewardPointsRemaining.HasValue && order.RewardPointsRemaining.Value < points)
                {
                    points = order.RewardPointsRemaining.Value;
                }

                if (points != 0)
                {
                    order.Customer.AddRewardPointsHistoryEntry(-points, T("RewardPoints.Message.ReducedForOrder", order.GetOrderNumber()));

                    if (!order.RewardPointsRemaining.HasValue)
                    {
                        order.RewardPointsRemaining = (int)Math.Round(order.OrderTotal / rewardAmount);
                    }

                    order.RewardPointsRemaining = Math.Max(order.RewardPointsRemaining.Value - points, 0);
                }
            }
            else
            {
                // Truncate same as Floor for positive amounts.
                var points = (int)Math.Truncate((amount ?? order.OrderTotal) / rewardAmount);
                if (points != 0)
                {
                    order.Customer.AddRewardPointsHistoryEntry(points, T("RewardPoints.Message.EarnedForOrder", order.GetOrderNumber()));
                    order.RewardPointsWereAdded = true;
                }
            }
        }

        /// <summary>
        /// Activates gift cards. The caller is responsible for database commit.
        /// </summary>
        protected virtual async Task ActivateGiftCardsAsync(Order order)
        {
            var activateGiftCards = _orderSettings.GiftCards_Activated_OrderStatusId > 0 && _orderSettings.GiftCards_Activated_OrderStatusId == (int)order.OrderStatus;
            var deactivateGiftCards = _orderSettings.GiftCards_Deactivated_OrderStatusId > 0 && _orderSettings.GiftCards_Deactivated_OrderStatusId == (int)order.OrderStatus;

            if (activateGiftCards || deactivateGiftCards)
            {
                var giftCards = await _db.GiftCards
                    .ApplyStandardFilter(order.Id)
                    .ToListAsync();

                if (giftCards.Any())
                {
                    if (activateGiftCards)
                    {
                        var allLanguages = await _db.Languages.AsNoTracking().ToListAsync();
                        var allLanguagesDic = allLanguages.ToDictionary(x => x.Id);

                        foreach (var gc in giftCards.Where(x => !x.IsGiftCardActivated))
                        {
                            var isRecipientNotified = gc.IsRecipientNotified;

                            if (gc.GiftCardType == GiftCardType.Virtual)
                            {
                                // Send email for virtual gift card.
                                if (gc.RecipientEmail.HasValue() && gc.SenderEmail.HasValue())
                                {
                                    if (!allLanguagesDic.TryGetValue(order.CustomerLanguageId, out var customerLang))
                                    {
                                        customerLang = allLanguages.FirstOrDefault();
                                    }

                                    var msgResult = await _messageFactory.SendGiftCardNotificationAsync(gc, customerLang.Id);
                                    isRecipientNotified = msgResult?.Email.Id != null;
                                }
                            }

                            gc.IsGiftCardActivated = true;
                            gc.IsRecipientNotified = isRecipientNotified;
                        }
                    }

                    if (deactivateGiftCards)
                    {
                        giftCards.Where(x => x.IsGiftCardActivated).Each(x => x.IsGiftCardActivated = false);
                    }
                }
            }
        }

        protected virtual async Task SetOrderStatusAsync(Order order, OrderStatus status, bool notifyCustomer)
        {
            Guard.NotNull(order, nameof(order));

            var prevOrderStatus = order.OrderStatus;
            if (prevOrderStatus == status)
            {
                return;
            }

            order.OrderStatusId = (int)status;

            // Save new order status.
            await _db.SaveChangesAsync();

            order.AddOrderNote(T("Admin.OrderNotice.OrderStatusChanged", await _localizationService.GetLocalizedEnumAsync(status)));

            if (prevOrderStatus != OrderStatus.Complete && status == OrderStatus.Complete && notifyCustomer)
            {
                var msgResult = await _messageFactory.SendOrderCompletedCustomerNotificationAsync(order, order.CustomerLanguageId);
                if (msgResult?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerCompletedEmailQueued", msgResult.Email.Id));
                }
            }

            if (prevOrderStatus != OrderStatus.Cancelled && status == OrderStatus.Cancelled && notifyCustomer)
            {
                var msgResult = await _messageFactory.SendOrderCancelledCustomerNotificationAsync(order, order.CustomerLanguageId);
                if (msgResult?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerCancelledEmailQueued", msgResult.Email.Id));
                }
            }

            // Reward points.
            if (_rewardPointsSettings.PointsForPurchases_Awarded == order.OrderStatus)
            {
                ApplyRewardPoints(order, false);
            }
            if (_rewardPointsSettings.PointsForPurchases_Canceled == order.OrderStatus)
            {
                ApplyRewardPoints(order, true);
            }

            // Gift cards activation.
            await ActivateGiftCardsAsync(order);

            // Update order.
            await _db.SaveChangesAsync();
        }

        #endregion
    }
}
