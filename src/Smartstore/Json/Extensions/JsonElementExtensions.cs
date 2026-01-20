#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Smartstore.Json;

public static class JsonElementExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="JsonElement"/> represents a JSON null or undefined value.
    /// </summary>
    public static bool IsNullOrUndefined(this JsonElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    /// <summary>
    /// Attempts to extract a scalar value from the specified JSON element.
    /// </summary>
    /// <remarks>This method returns true for JSON null and undefined values, setting the output value to
    /// null. For JSON strings, the method attempts to parse the value as a Guid or DateTime before returning the
    /// string. For JSON numbers, the method attempts to parse the value as Int64, then Double, and finally Decimal. If
    /// the element is not a scalar value (such as an object or array), the method returns false.</remarks>
    /// <param name="element">The JSON element from which to extract the scalar value.</param>
    /// <param name="value">When this method returns, contains the extracted scalar value if the operation succeeds; otherwise, contains
    /// null. The value may be a Boolean, Guid, DateTime, string, Int64, Double, or Decimal, depending on the JSON
    /// element's value kind.</param>
    /// <returns>true if a scalar value was successfully extracted or the element is null or undefined; otherwise, false.</returns>
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
                return true;

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
