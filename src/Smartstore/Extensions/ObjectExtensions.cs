#nullable enable

using System.Globalization;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore;

public static class ObjectExtensions
{
    extension(object? value)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Convert<T>()
        {
            if (ConvertUtility.TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object? result))
            {
                return (T)result!;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Convert<T>(T? defaultValue)
        {
            if (ConvertUtility.TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object? result))
            {
                return (T)result!;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Convert<T>(CultureInfo? culture)
        {
            if (ConvertUtility.TryConvert(value, typeof(T), culture, out object? result))
            {
                return (T)result!;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Convert<T>(T? defaultValue, CultureInfo? culture)
        {
            if (ConvertUtility.TryConvert(value, typeof(T), culture, out object? result))
            {
                return (T)result!;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? Convert(Type to)
        {
            if (ConvertUtility.TryConvert(value, to, CultureInfo.InvariantCulture, out object? result))
            {
                return result!;
            }

            // TODO: (core) we SHOULD really throw here, shouldn't we?
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? Convert(Type to, CultureInfo? culture)
        {
            if (ConvertUtility.TryConvert(value, to, culture, out object? result))
            {
                return result;
            }

            // TODO: (core) we SHOULD really throw here, shouldn't we?
            return null;
        }
    }
}
