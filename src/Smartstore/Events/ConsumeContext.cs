#nullable enable

using Microsoft.AspNetCore.Http;

namespace Smartstore.Events;

/// <summary>
/// Read-only covariant view of a <see cref="ConsumeContext{TMessage}"/>.
/// Declare consumer parameters as <c>IConsumeContext&lt;TBase&gt;</c> instead of
/// <c>ConsumeContext&lt;TBase&gt;</c> to receive events of any derived message type.
/// </summary>
/// <typeparam name="TMessage">Type of message (covariant).</typeparam>
public interface IConsumeContext<out TMessage> where TMessage : IEventMessage
{
    /// <summary>
    /// Gets the message associated with this instance.
    /// </summary>
    TMessage Message { get; }

    /// <summary>
    /// Gets the type of the message represented by this instance, usually the name of the message class.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the endpoint associated with the current instance.
    /// </summary>
    Endpoint? Endpoint { get; }

    /// <summary>
    /// Gets the raw, unprocessed URL as received by the server.
    /// </summary>
    string? RawUrl { get; }

    string? Scheme { get; }
    HostString Host { get; }
    PathString PathBase { get; }
    PathString Path { get; }
    QueryString QueryString { get; }
}

/// <summary>
/// Wrapper/Envelope for event message objects.
/// </summary>
/// <typeparam name="TMessage">Type of message.</typeparam>
public sealed class ConsumeContext<TMessage> : IConsumeContext<TMessage>, IEventMessage
    where TMessage : IEventMessage
{
    private bool _initialized;

    public ConsumeContext(TMessage message)
    {
        Message = Guard.NotNull(message);
    }

    public TMessage Message { get; }
    public string MessageType { get; internal set; } = default!;

    public Endpoint? Endpoint { get; internal set; }
    public string? RawUrl { get; internal set; }
    public string? Scheme { get; internal set; }
    public HostString Host { get; internal set; }
    public PathString PathBase { get; internal set; }
    public PathString Path { get; internal set; }
    public QueryString QueryString { get; internal set; }

    internal void Initialize(HttpContext? httpContext)
    {
        if (!_initialized)
        {
            MessageType = Message.GetType().Name;

            if (httpContext != null)
            {
                var req = httpContext.Request;

                Endpoint = httpContext.GetEndpoint();
                Scheme = req.Scheme;
                Host = req.Host;
                PathBase = req.PathBase;
                Path = req.Path;
                QueryString = req.QueryString;
                RawUrl = req.RawUrl();
            }

            _initialized = true;
        }
    }
}