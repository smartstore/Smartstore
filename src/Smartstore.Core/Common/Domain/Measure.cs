using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Common;

/// <summary>
/// Represents a physical measurement, e.g. 2 kg, 5 cm, 3.75 m.
/// </summary>
[JsonConverter(typeof(MeasureJsonConverter))]
public readonly struct Measure : IHtmlContent, IComparable<Measure>, IEquatable<Measure>
{
    public static readonly Measure Zero = default;

    /// <summary>
    /// Initializes a new <see cref="Measure"/> with flexible formatting (min 0, max 2 decimal places).
    /// </summary>
    public Measure(decimal value, string unit)
        : this(value, unit, null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="Measure"/> with a fixed number of decimal places for display.
    /// </summary>
    /// <param name="value">The numeric measurement value.</param>
    /// <param name="unit">The unit symbol, e.g. "kg", "cm", "m". Required.</param>
    /// <param name="decimals">
    /// Fixed number of decimal digits for display. Any non-negative value is accepted.
    /// <c>null</c> = flexible: no trailing zeros.
    /// </param>
    public Measure(decimal value, string unit, int? decimals)
    {
        Guard.NotEmpty(unit);

        Value = value;
        Unit = unit.Trim();
        Decimals = decimals.HasValue ? Math.Max(0, decimals.Value) : null;
    }

    /// <summary>
    /// Gets the numeric measurement value.
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Gets the unit symbol, e.g. "kg", "cm", "m".
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets the fixed number of decimal digits used for display,
    /// or <c>null</c> for flexible formatting (no trailing zeros).
    /// </summary>
    public int? Decimals { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is empty.
    /// </summary>
    public bool IsEmpty => Unit.IsEmpty() || Value == 0;

    #region With*

    /// <summary>
    /// Returns a new <see cref="Measure"/> with a changed value.
    /// </summary>
    public Measure WithValue(decimal value) 
        => new(value, Unit, Decimals);

    /// <summary>
    /// Returns a new <see cref="Measure"/> with a changed unit.
    /// </summary>
    public Measure WithUnit(string unit) 
        => new(Value, unit, Decimals);

    /// <summary>
    /// Returns a new <see cref="Measure"/> with a changed decimal display setting.
    /// </summary>
    public Measure WithDecimals(int? decimals) 
        => new(Value, Unit, decimals);

    #endregion

    #region Operators

    public static Measure operator +(Measure a, Measure b) { GuardUnit(a, b); return new(a.Value + b.Value, a.Unit, a.Decimals); }
    public static Measure operator -(Measure a, Measure b) { GuardUnit(a, b); return new(a.Value - b.Value, a.Unit, a.Decimals); }
    public static Measure operator *(Measure a, decimal factor) => new(a.Value * factor, a.Unit, a.Decimals);
    public static Measure operator /(Measure a, decimal divisor) => new(a.Value / divisor, a.Unit, a.Decimals);
    public static Measure operator -(Measure a) => new(-a.Value, a.Unit, a.Decimals);

    public static bool operator ==(Measure a, Measure b) => a.Equals(b);
    public static bool operator !=(Measure a, Measure b) => !a.Equals(b);
    public static bool operator >(Measure a, Measure b) => a.CompareTo(b) > 0;
    public static bool operator <(Measure a, Measure b) => a.CompareTo(b) < 0;
    public static bool operator >=(Measure a, Measure b) => a.CompareTo(b) >= 0;
    public static bool operator <=(Measure a, Measure b) => a.CompareTo(b) <= 0;

    #endregion

    #region Equality & Comparison

    public int CompareTo(Measure other)
    {
        GuardUnit(this, other);
        return Value.CompareTo(other.Value);
    }

    public bool Equals(Measure other)
        => Value == other.Value && string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object obj)
        => obj is Measure other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Value, Unit?.ToLowerInvariant());

    #endregion

    #region Formatting

    /// <summary>
    /// Returns the formatted measure, e.g. <c>3.75 kg</c> or <c>3.50 kg</c> (if <see cref="Decimals"/> = 2).
    /// </summary>
    public override string ToString()
    {
        if (Unit.IsEmpty())
            return string.Empty;

        var fmt = Decimals.HasValue ? "0." + new string('0', Decimals.Value) : "0.##########";
        var rounded = decimal.Round(Value, Decimals ?? 10, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString(fmt)}\u00a0{Unit}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IHtmlContent.WriteTo(TextWriter writer, HtmlEncoder encoder)
        => writer.Write(ToString());

    #endregion

    private static void GuardUnit(Measure a, Measure b)
    {
        if (!string.Equals(a.Unit, b.Unit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot operate on measurements with different units ('{a.Unit}' vs '{b.Unit}').");
    }
}

internal sealed class MeasureJsonConverter : JsonConverter<Measure>
{
    public override Measure Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Measure measure, JsonSerializerOptions options)
        => writer.WriteStringValue(measure.ToString());
}