#nullable enable

using System.ComponentModel;
using System.Text.Json.Serialization;
using Smartstore.ComponentModel;
using Smartstore.Json.Converters;

namespace Smartstore.Core.AI;

/// <summary>
/// Represents the reasoning effort used for AI response generation.
/// </summary>
[TypeConverter(typeof(StringBackedTypeConverter<AIReasoningEffort>))]
public readonly partial struct AIReasoningEffort : IStringBacked<AIReasoningEffort>, IEquatable<AIReasoningEffort>
{
    private readonly string _value;

    internal AIReasoningEffort(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the string value represented by this instance.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Lets the provider or model determine the reasoning effort.
    /// </summary>
    public static readonly AIReasoningEffort Auto = new("auto");

    public static readonly AIReasoningEffort None = new("none");
    public static readonly AIReasoningEffort Minimal = new("minimal");
    public static readonly AIReasoningEffort Low = new("low");
    public static readonly AIReasoningEffort Medium = new("medium");
    public static readonly AIReasoningEffort High = new("high");
    public static readonly AIReasoningEffort XHigh = new("xhigh");
    public static readonly AIReasoningEffort Max = new("max");

    /// <summary>
    /// Represents all known reasoning effort levels.
    /// </summary>
    public static readonly AIReasoningEffort[] All = [Auto, None, Minimal, Low, Medium, High, XHigh, Max];

    public static implicit operator string?(AIReasoningEffort obj)
        => obj._value;

    public static implicit operator AIReasoningEffort?(string? value)
        => FromString(value);

    public static AIReasoningEffort? FromString(string? value)
    {
        if (value == null) return null;
        return value switch
        {
            "auto" => Auto,
            "none" => None,
            "minimal" => Minimal,
            "low" => Low,
            "medium" => Medium,
            "high" => High,
            "xhigh" => XHigh,
            "max" => Max,
            _ => throw new InvalidCastException($"Unknown reasoning effort '{value}'."),
        };
    }

    public static bool operator ==(AIReasoningEffort left, AIReasoningEffort right)
        => left.Equals(right);

    public static bool operator !=(AIReasoningEffort left, AIReasoningEffort right)
        => !left.Equals(right);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
        => obj is AIReasoningEffort other && Equals(other);

    public bool Equals(AIReasoningEffort other)
        => _value?.EqualsNoCase(other._value) ?? false;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
        => _value?.GetHashCode() ?? 0;

    public override string? ToString()
        => _value;
}
