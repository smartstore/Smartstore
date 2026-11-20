#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Core.Tests.Platform.Widgets;

/// <summary>
/// Exposes protected dashboard serialization methods for service tests.
/// </summary>
internal sealed class TestDashboardService : DashboardService
{
    public TestDashboardService(
        IApplicationContext appContext,
        IMemoryCache memCache,
        IEventPublisher eventPublisher,
        IEnumerable<Lazy<IDashboardWidget, DashboardMetadata>> widgetRegistrations,
        IEnumerable<Lazy<IDashboardLayoutProvider, DashboardMetadata>> layoutProviderRegistrations)
        : base(appContext, memCache, eventPublisher, widgetRegistrations, layoutProviderRegistrations)
    {
    }

    /// <summary>
    /// Serializes a dashboard layout using the service contract.
    /// </summary>
    /// <param name="layout">The dashboard layout to serialize.</param>
    /// <returns>The serialized dashboard layout.</returns>
    public string Serialize(DashboardLayout layout)
        => SerializeLayout(layout);

    /// <summary>
    /// Deserializes a dashboard layout using the service contract.
    /// </summary>
    /// <param name="json">The serialized dashboard layout.</param>
    /// <returns>The deserialized dashboard layout.</returns>
    public DashboardLayout? Deserialize(string json)
        => DeserializeLayout(json);
}
