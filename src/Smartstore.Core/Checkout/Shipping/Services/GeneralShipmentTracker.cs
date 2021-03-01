using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Shipping.Events;

namespace Smartstore.Core.Checkout.Shipping.Services
{
    /// <summary>
    /// General shipment tracker (finds an appropriate tracker by tracking number).
    /// </summary>
    public partial class GeneralShipmentTracker : IShipmentTracker
    {
        // TODO: (ms) (core) ItypeFinder & ContainerManager are missing
        //private readonly ITypeFinder _typeFinder;

        public GeneralShipmentTracker(/*ITypeFinder typeFinder*/)
        {
            //_typeFinder = typeFinder;
        }

        /// <summary>
        /// Gets all trackers.
        /// </summary>
        /// <returns>All available shipment trackers.</returns>
        protected virtual IList<IShipmentTracker> GetAllTrackers()
        {
            return new List<IShipmentTracker>();
                //_typeFinder.FindClassesOfType<IShipmentTracker>(ignoreInactivePlugins: true)
                //.Where(x => x != typeof(GeneralShipmentTracker))
                //.Select(x => EngineContext.Current.ContainerManager.ResolveUnregistered(x) as IShipmentTracker)
                //.ToList();
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