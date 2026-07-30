using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Core.AI.Metadata;

namespace Smartstore.Core.AI.JsonConverters;

public sealed class AIOutputTypeConverter : JsonStringEnumConverter<AIOutputType>
{
    public AIOutputTypeConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
