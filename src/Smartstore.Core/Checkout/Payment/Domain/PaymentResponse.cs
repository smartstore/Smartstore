#nullable enable

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Smartstore.Core.Checkout.Payment;

public class PaymentResponse
{
    private readonly object? _body;

    public PaymentResponse(HttpStatusCode status)
        : this(status, null, null)
    {
    }

    public PaymentResponse(HttpStatusCode status, IDictionary<string, string>? headers)
        : this(status, headers, null)
    {
    }

    public PaymentResponse(HttpStatusCode status, IDictionary<string, string>? headers, object? body)
    {
        Headers = headers;
        Status = status;
        _body = body;
    }

    public HttpStatusCode Status { get; }

    public IDictionary<string, string>? Headers { get; }

    public bool HasBody
        => _body != null;

    public T Body<T>()
        => (_body is null) ? default! : (T)_body;

    public JsonNode? BodyAsJsonNode()
    {
        return _body switch
        {
            JsonNode n => n,

            JsonElement el => el.ValueKind switch
            {
                // fast path
                JsonValueKind.Object => JsonObject.Create(el),
                // fast path
                JsonValueKind.Array => JsonArray.Create(el),
                // primitives + null
                JsonValueKind.Undefined => null,
                _ => JsonValue.Create((JsonElement?)el)          
            },

            string s => ParseJsonStringOrThrow(s),

            _ => null
        };
        
        static JsonNode? ParseJsonStringOrThrow(string s)
        {
            s = s.Trim();
            return s.Length == 0 ? null : JsonNode.Parse(s);
        }
    }
}
