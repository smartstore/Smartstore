#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Smartstore.Json;

public static class JsonElementExtensions
{
    public static bool IsNullOrUndefined(this JsonElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    public static bool TryGetScalarValue(this JsonElement element, [MaybeNullWhen(false)] out object? value)
    {
        value = null;

        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;

            case JsonValueKind.True:
                value = true;
                return true;

            case JsonValueKind.False:
                value = false;
                return false;

            case JsonValueKind.String:
                if (element.TryGetGuid(out var guid))
                    value = guid;
                else if (element.TryGetDateTime(out var dateTime))
                    value = dateTime;
                else
                    value = element.GetString();

                return true;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    value = l;
                else if (element.TryGetDouble(out var d))
                    value = d;
                else
                    value = element.GetDecimal();

                return true;

            default:
                return false;
        }
    }
}
