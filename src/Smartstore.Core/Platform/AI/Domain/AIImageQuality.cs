#nullable enable

using System.ComponentModel;
using Smartstore.ComponentModel;

namespace Smartstore.Core.AI;

/// <summary>
/// Represents a quality level for AI-generated images.
/// </summary>
/// <remarks>This is a string-backed value type with predefined quality levels including Auto, Low, Medium, and
/// High. Supports implicit conversions to and from string values.</remarks>
[TypeConverter(typeof(StringBackedTypeConverter<AIImageQuality>))]
public readonly partial struct AIImageQuality : IStringBacked<AIImageQuality>, IEquatable<AIImageQuality>
{
    private readonly string _value;

    internal AIImageQuality(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the string value represented by this instance.
    /// </summary>
    public string Value => _value;

    public static readonly AIImageQuality Auto = new("auto");

    /// <summary>
    /// Represents low quality for AI image generation.
    /// </summary>
    public static readonly AIImageQuality Low = new("low");

    /// <summary>
    /// Represents medium quality for AI image generation.
    /// </summary>
    public static readonly AIImageQuality Medium = new("medium");

    /// <summary>
    /// Represents high quality for AI image generation.
    /// </summary>
    public static readonly AIImageQuality High = new("high");

    /// <summary>
    /// Represents a collection of all supported image qualities.
    /// </summary>
    public static readonly AIImageQuality[] All = [Auto, Low, Medium, High];

    public static implicit operator string?(AIImageQuality obj)
        => obj._value;

    public static implicit operator AIImageQuality?(string? value)
        => FromString(value);

    public static AIImageQuality? FromString(string? value)
    {
        if (value == null) return null;
        return value switch
        {
            "auto" => Auto,
            "low" => Low,
            "medium" => Medium,
            "high" => High,
            _ => throw new InvalidCastException($"Unknown image quality '{value}'."),
        };
    }

    public static bool operator ==(AIImageQuality left, AIImageQuality right)
        => left.Equals(right);

    public static bool operator !=(AIImageQuality left, AIImageQuality right)
        => !left.Equals(right);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj)
        => obj is AIImageQuality other && Equals(other);

    public bool Equals(AIImageQuality other)
        => _value?.EqualsNoCase(other._value) ?? false;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
        => _value?.GetHashCode() ?? 0;

    public override string? ToString()
        => _value;
}