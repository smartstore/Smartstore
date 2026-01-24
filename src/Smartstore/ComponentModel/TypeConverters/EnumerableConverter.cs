#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using Smartstore.Collections;

namespace Smartstore.ComponentModel.TypeConverters;

internal class EnumerableConverter<T> : DefaultTypeConverter
{
    private readonly Func<IEnumerable<T>, object> _activator;
    private readonly ITypeConverter _elementTypeConverter;

    public EnumerableConverter(Type sequenceType)
        : base(typeof(object))
    {
        _elementTypeConverter = TypeConverterFactory.GetConverter<T>()
            ?? throw new InvalidOperationException("No type converter exists for type " + typeof(T).FullName);

        _activator = CreateSequenceActivator(sequenceType);
    }

    internal static Func<IEnumerable<T>, object> CreateSequenceActivator(Type sequenceType)
    {
        // Default is IEnumerable<T>
        Func<IEnumerable<T>, object>? activator = null;

        var t = sequenceType;

        if (t == typeof(IEnumerable<T>))
        {
            activator = static x => x;
        }
        else if (t == typeof(T[]))
        {
            activator = static x => x.ToArray();
        }
        else if (t == typeof(IReadOnlyCollection<T>) || t == typeof(IReadOnlyList<T>))
        {
            activator = static x => x.AsReadOnly();
        }
        else if (t.IsAssignableFrom(typeof(List<T>)))
        {
            activator = static x => x.ToList();
        }
        else if (t.IsAssignableFrom(typeof(HashSet<T>)))
        {
            if (typeof(T) == typeof(string))
                activator = static x => (object)new HashSet<T>(x, (IEqualityComparer<T>)StringComparer.OrdinalIgnoreCase);
            else
                activator = static x => new HashSet<T>(x);
        }
        else if (t.IsAssignableFrom(typeof(FrozenSet<T>)))
        {
            if (typeof(T) == typeof(string))
                activator = static x => x.ToFrozenSet((IEqualityComparer<T>)StringComparer.OrdinalIgnoreCase);
            else
                activator = static x => x.ToFrozenSet();
        }
        else if (t.IsAssignableFrom(typeof(Queue<T>)))
        {
            activator = static x => new Queue<T>(x);
        }
        else if (t.IsAssignableFrom(typeof(Stack<T>)))
        {
            activator = static x => new Stack<T>(x);
        }
        else if (t.IsAssignableFrom(typeof(LinkedList<T>)))
        {
            activator = static x => new LinkedList<T>(x);
        }
        else if (t.IsAssignableFrom(typeof(ConcurrentBag<T>)))
        {
            activator = static x => new ConcurrentBag<T>(x);
        }
        else if (t.IsAssignableFrom(typeof(SyncedCollection<T>)))
        {
            // Materialize once; avoids iterator replays and avoids the spread over an IEnumerable
            activator = static x => new SyncedCollection<T>(x is IList<T> list ? list : x.ToList());
        }
        else if (t.IsAssignableFrom(typeof(ArraySegment<T>)))
        {
            activator = static x => new ArraySegment<T>(x.ToArray());
        }

        if (activator == null)
        {
            throw new InvalidOperationException(
                "'{0}' is not a valid type for enumerable conversion.".FormatInvariant(sequenceType.FullName));
        }

        return activator;
    }

    public override bool CanConvertFrom(Type type)
    {
        if (type.IsEnumerableType(out var elementType))
        {
            return elementType.IsAssignableFrom(typeof(T))
                || _elementTypeConverter.CanConvertFrom(elementType)
                || TypeConverterFactory.GetConverter(elementType).CanConvertTo(typeof(T));
        }

        return type == typeof(string) || typeof(IConvertible).IsAssignableFrom(type);
    }

    public override bool CanConvertTo(Type type)
        => type == typeof(string) && _elementTypeConverter.CanConvertTo(type);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value is null)
        {
            return _activator([]);
        }

        // Fast path: already of the right type.
        if (value is IEnumerable<T> typed)
        {
            return _activator(typed);
        }

        if (value is string str)
        {
            return _activator(ConvertFromStrings(culture, GetStringArray(str)));
        }

        if (value is IConvertible)
        {
            // Original implementation used LINQ + Convert.ChangeType redundantly. Keep behavior (Convert.ChangeType),
            // but remove allocations/enumerables.
            var converted = (T)Convert.ChangeType(value, typeof(T), culture);
            return _activator([converted]);
        }

        if (value is IEnumerable items)
        {
            items.GetType().IsEnumerableType(out var elementType);

            var elementConverter = _elementTypeConverter;
            var isOtherConverter = false;

            // NOTE: Preserve original semantics (even though elementType.IsAssignableFrom(typeof(T)) is odd).
            var isAssignable = typeof(T).IsAssignableFrom(elementType);
            if (!isAssignable && !elementConverter.CanConvertFrom(elementType))
            {
                elementConverter = TypeConverterFactory.GetConverter(elementType);
                isOtherConverter = true;
            }

            // Avoid LINQ (allocations + iterator overhead). Materialize once.
            var list = new List<T>();

            foreach (var raw in items)
            {
                if (raw is null)
                    continue;

                object? convertedObj;

                if (isAssignable)
                {
                    convertedObj = raw;
                }
                else
                {
                    convertedObj = !isOtherConverter
                        ? elementConverter.ConvertFrom(culture, raw)
                        : elementConverter.ConvertTo(culture, null, raw, elementType);
                }

                if (convertedObj is null)
                    continue;

                list.Add((T)convertedObj);
            }

            return _activator(list);
        }

        return base.ConvertFrom(culture, value);
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        if (to == typeof(string))
        {
            if (value is IEnumerable<T> enumerable)
            {
                // Replace O(n^2) string concatenation with StringBuilder.
                var sb = new StringBuilder();

                foreach (var token in enumerable)
                {
                    var str = _elementTypeConverter.ConvertTo(culture, format, token, typeof(string));
                    if (sb.Length > 0)
                        sb.Append(',');

                    sb.Append(str);
                }

                return sb.ToString();
            }

            return string.Empty;
        }

        return base.ConvertTo(culture, format, value, to);
    }

    protected virtual string[] GetStringArray(string input)
    {
        // SplitSafe likely already returns an array; avoid ToArray() if possible.
        var result = input.SplitSafe(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return result as string[] ?? result.ToArray();
    }

    private static IEnumerable<T> ConvertFromStrings(CultureInfo culture, string[] items)
    {
        // Local iterator to keep ConvertFrom() allocation-free unless enumerated;
        // activators that need materialization (e.g., array/list) will do so.
        foreach (var s in items)
        {
            // ConvertFrom may return null; preserve earlier behavior of filtering nulls by skipping.
            var obj = TypeConverterFactory.GetConverter<T>()?.ConvertFrom(culture, s);
            if (obj is T t)
                yield return t;
        }
    }
}
