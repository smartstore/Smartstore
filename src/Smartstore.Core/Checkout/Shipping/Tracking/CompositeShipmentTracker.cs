using Smartstore.Core.Checkout.Shipping.Events;

namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// General shipment tracker (finds an appropriate tracker by tracking number).
    /// </summary>
    internal partial class CompositeShipmentTracker : IShipmentTracker
    {
        // TODO: (mg) (core) Implement a shipment tracker for DHL!
        private static Type[] _trackerTypes = null;
        private readonly static object _lock = new();

        private readonly ITypeScanner _typeScanner;

        public CompositeShipmentTracker(ITypeScanner typeScanner)
        {
            _typeScanner = typeScanner;
        }

        /// <summary>
        /// Gets all trackers. The result gets cached.
        /// </summary>
        /// <returns>All available shipment trackers.</returns>
        protected virtual IEnumerable<IShipmentTracker> GetAllTrackers()
        {
            if (_trackerTypes == null)
            {
                lock (_lock)
                {
                    if (_trackerTypes == null)
                    {
                        _trackerTypes = _typeScanner
                            .FindTypes<IShipmentTracker>()
                            .Where(x => x.IsPublic)
                            .ToArray();
                    }
                }
            }

            return _trackerTypes.Select(x => EngineContext.Current.Application.Services.ResolveUnregistered(x) as IShipmentTracker);
        }

        protected virtual IShipmentTracker GetTrackerByTrackingNumber(string trackingNumber)
            => GetAllTrackers().FirstOrDefault(c => c.IsMatch(trackingNumber));

        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns><c>True</c> if the tracker can track, otherwise false.</returns>
        public virtual bool IsMatch(string trackingNumber)
        {
            var tracker = GetTrackerByTrackingNumber(trackingNumber);
            return tracker != null && tracker.IsMatch(trackingNumber);
        }

        /// <summary>
        /// Gets an url for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number.</param>
        /// <returns>An url to the tracking page.</returns>
        public virtual string GetUrl(string trackingNumber)
        {
            var tracker = GetTrackerByTrackingNumber(trackingNumber);
            return tracker?.GetUrl(trackingNumber);
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number of events.</param>
        /// <returns>List of <see cref="ShipmentStatusEvent"/>.</returns>
        public virtual Task<List<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber)
        {
            var tracker = GetTrackerByTrackingNumber(trackingNumber);
            return tracker != null
                ? tracker.GetShipmentEventsAsync(trackingNumber)
                : Task.FromResult(new List<ShipmentStatusEvent>());
        }
    }
}