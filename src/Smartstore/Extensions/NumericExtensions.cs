using System.Globalization;
using System.Runtime.CompilerServices;

namespace Smartstore;

public static class NumericExtensions
{
    extension(int? value)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? ZeroToNull()
        {
            return value <= 0 ? null : value;
        }
    }


    extension(int value)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ToHex()
        {
            if (value <= 9)
            {
                return (char)(value + 48);
            }

            return (char)((value - 10) + 97);
        }

        public (int lower, int upper) GetRange(int size = 500)
        {
            // Max [size] values per cache item
            var lower = (int)Math.Floor((decimal)value / size) * size;
            var upper = lower + (size - 1);

            return (lower, upper);
        }

        /// <summary>
        /// The number of digits in a non-negative number. Returns 1 for all
        /// negative numbers. That is ok because we are using it to calculate
        /// string length for a <see cref="StringBuilder"/> for numbers that
        /// aren't supposed to be negative, but when they are it is just a little
        /// slower.
        /// </summary>
        /// <remarks>
        /// This approach is based on https://stackoverflow.com/a/51099524/268898
        /// where the poster offers performance benchmarks showing this is the
        /// fastest way to get a number of digits.
        /// </remarks>
        public int NumDigits()
        {
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1_000) return 3;
            if (value < 10_000) return 4;
            if (value < 100_000) return 5;
            if (value < 1_000_000) return 6;
            if (value < 10_000_000) return 7;
            if (value < 100_000_000) return 8;
            if (value < 1_000_000_000) return 9;
            return 10;
        }
    }

    #region decimal

    /// <summary>
    /// Calculates the tax (percentage) from a gross and a net currency amount.
    /// </summary>
    /// <param name="amountInclTax">Gross amount.</param>
    /// <param name="amountExclTax">Net amount.</param>
    /// <returns>Tax percentage (unrounded).</returns>
    public static decimal ToTaxPercentage(this decimal amountInclTax, decimal amountExclTax)
    {
        if (amountExclTax == decimal.Zero)
        {
            return decimal.Zero;
        }

        return ((amountInclTax / amountExclTax) - 1.0m) * 100.0m;
    }

    #endregion

    #region IComparable

    /// <summary>
    /// Checks whether given <paramref name="value"/> is between a minimum and a maximum value (inclusively).
    /// </summary>
    /// <param name="value">The value to be checked</param>
    /// <param name="min">Minimum (inclusive) value</param>
    /// <param name="max">Maximum (inclusive) value</param>
    public static bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    #endregion

    #region IFormattable

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStringInvariant(this IFormattable source)
        => source.ToString(null, CultureInfo.InvariantCulture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStringInvariant(this IFormattable source, string format)
        => source.ToString(format, CultureInfo.InvariantCulture);

    #endregion

    #region Clamp

    /// <summary>
    /// Restricts a <see cref="byte"/> to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>
    /// The <see cref="byte"/> representing the clamped value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clamp(this byte value, byte min, byte max)
    {
        // Order is important here as someone might set min to higher than max.
        if (value >= max)
        {
            return max;
        }

        if (value <= min)
        {
            return min;
        }

        return value;
    }

    /// <summary>
    /// Restricts a <see cref="uint"/> to be within a specified range.
    /// </summary>
    /// <param name="value">The The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>
    /// The <see cref="int"/> representing the clamped value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Clamp(this uint value, uint min, uint max)
    {
        if (value >= max)
        {
            return max;
        }

        if (value <= min)
        {
            return min;
        }

        return value;
    }

    /// <summary>
    /// Restricts a <see cref="int"/> to be within a specified range.
    /// </summary>
    /// <param name="value">The The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>
    /// The <see cref="int"/> representing the clamped value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int value, int min, int max)
    {
        if (value >= max)
        {
            return max;
        }

        if (value <= min)
        {
            return min;
        }

        return value;
    }

    /// <summary>
    /// Restricts a <see cref="float"/> to be within a specified range.
    /// </summary>
    /// <param name="value">The The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>
    /// The <see cref="float"/> representing the clamped value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(this float value, float min, float max)
    {
        if (value >= max)
        {
            return max;
        }

        if (value <= min)
        {
            return min;
        }

        return value;
    }

    /// <summary>
    /// Restricts a <see cref="double"/> to be within a specified range.
    /// </summary>
    /// <param name="value">The The value to clamp.</param>
    /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
    /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
    /// <returns>
    /// The <see cref="double"/> representing the clamped value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp(this double value, double min, double max)
    {
        if (value >= max)
        {
            return max;
        }

        if (value <= min)
        {
            return min;
        }

        return value;
    }

    #endregion
}
