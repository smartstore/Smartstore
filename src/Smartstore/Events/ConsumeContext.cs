#nullable enable

using Microsoft.AspNetCore.Http;

namespace Smartstore.Events
{
    /// <summary>
    /// Wrapper/Envelope for event message objects. For now, only wraps
    /// the message. There may be other context data in future.
    /// </summary>
    /// <typeparam name="TMessage">Type of message.</typeparam>
    public class ConsumeContext<TMessage>
    {
        public ConsumeContext(TMessage message)
        {
            Message = Guard.NotNull(message);
        }

        public TMessage Message { get; }

        public Endpoint? Endpoint { get; internal set; }
        public string? RawUrl { get; internal set; }

        public string? Scheme { get; internal set; }
        public HostString Host { get; internal set; }
        public PathString PathBase { get; internal set; }
        public PathString Path { get; internal set; }
        public QueryString QueryString { get; internal set; }
    }
}
