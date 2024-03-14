#nullable enable

using System.Text;

namespace Smartstore.Core.Checkout.Orders
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CheckoutStepAttribute(int order, params string[] actions) : Attribute
    {
        /// <inheritdoc cref="CheckoutHandlerMetadata.Actions" />
        public string[] Actions { get; } = actions;

        /// <inheritdoc cref="CheckoutHandlerMetadata.Controller" />
        public string? Controller { get; init; }

        /// <inheritdoc cref="CheckoutHandlerMetadata.Area" />
        public string? Area { get; init; }

        /// <inheritdoc cref="CheckoutHandlerMetadata.Order" />
        public int Order { get; } = order;

        /// <inheritdoc cref="CheckoutHandlerMetadata.ProgressLabelKey" />
        public string? ProgressLabelKey { get; init; }
    }

    public sealed class CheckoutHandlerMetadata
    {
        private readonly static CompositeFormat _formatRouteIdent = CompositeFormat.Parse("{0}{1}.{2}");

        public required Type HandlerType { get; set; }

        /// <summary>
        /// Gets or sets the name of the action methods associated with a checkout handler.
        /// The first action must be the one through which the associated checkout page can be accessed (convention).
        /// </summary>
        public required string[] Actions { get; set; }

        /// <summary>
        /// Gets the default action (the first element of <see cref="Actions"/>).
        /// </summary>
        public string DefaultAction => Actions[0];

        /// <summary>
        /// Gets or sets the name of the controller associated with a checkout handler.
        /// </summary>
        public required string Controller { get; set; }

        /// <summary>
        /// Gets or sets the area name of the controller associated with a checkout handler.
        /// </summary>
        public string? Area { get; set; }

        /// <summary>
        /// Gets or sets a value that corresponds to the order in which checkout handlers are processed,
        /// thus in which the associated checkout steps are completed.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the name of the string resource for the checkout progress navigation.
        /// <c>null</c> to not display the step in checkout progress at all.
        /// </summary>
        public string? ProgressLabelKey { get; set; }

        public override string ToString()
            => _formatRouteIdent.FormatInvariant(Area.HasValue() ? Area + '.' : string.Empty, Controller, DefaultAction);
    }
}
