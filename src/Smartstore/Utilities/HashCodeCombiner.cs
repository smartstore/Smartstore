#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

namespace Smartstore.Utilities;

/// <summary>
/// Deterministic hash code combiner.
/// </summary>
[DebuggerDisplay("{CombinedHashString}")]
public struct HashCodeCombiner
{
    private const long _globalSeed = 0x1505L;

    private long _combinedHash64;

    public HashCodeCombiner()
    {
    }

    public HashCodeCombiner(long seed)
    {
        _combinedHash64 = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashCodeCombiner Start()
        => new(_globalSeed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashCodeCombiner StartNonDeterministic()
        => new(CurrentSeed);

    public int CombinedHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _combinedHash64.GetHashCode();
    }

    public string CombinedHashString
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _combinedHash64.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(HashCodeCombiner self)
        => self.CombinedHash;

    internal static long GlobalSeed { get; } = _globalSeed;
    internal static long CurrentSeed { get; } = CommonHelper.GenerateRandomInteger(min: int.MinValue);

    public HashCodeCombiner AddSequence<T>(IEnumerable<T> sequence, IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        if (sequence is null)
        {
            return this;
        }

        var count = 0;
        foreach (var o in sequence)
        {
            Add(o, comparer);
            count++;
        }

        Append(count);
        return this;
    }

    public HashCodeCombiner AddDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        if (dictionary is null)
        {
            return this;
        }

        if (dictionary is ICollection<KeyValuePair<TKey, TValue>> col && col.Count == 0)
        {
            return this;
        }

        var list = new List<KeyValuePair<TKey, TValue>>();
        foreach (var kvp in dictionary)
        {
            list.Add(kvp);
        }

        if (list.Count == 0)
        {
            return this;
        }

        list.Sort(static (a, b) => Comparer<TKey>.Default.Compare(a.Key, b.Key));

        for (var i = 0; i < list.Count; i++)
        {
            var kvp = list[i];
            Add(kvp.Key);
            Add(kvp.Value);
        }

        return this;
    }

    public HashCodeCombiner Add(IFileEntry entry, bool deep = true)
    {
        Guard.NotNull(entry);

        if (!entry.Exists)
        {
            return this;
        }

        Append(HashPathIgnoreCase(entry.PhysicalPath));
        Append(entry.LastModified.GetHashCode());

        if (entry is IFile file)
        {
            Append(file.Length.GetHashCode());
        }

        if (entry is IDirectory dir)
        {
            foreach (IFileEntry e in dir.EnumerateFiles(deep: deep))
            {
                Add(e, false);
            }
        }

        return this;
    }

    public HashCodeCombiner Add(IFileInfo entry)
    {
        Guard.NotNull(entry);

        if (!entry.Exists)
        {
            return this;
        }

        Append(HashPathIgnoreCase(entry.PhysicalPath));
        Append(entry.LastModified.GetHashCode());

        if (!entry.IsDirectory)
        {
            Append(entry.Length.GetHashCode());
        }

        return this;
    }

    public HashCodeCombiner Add(FileSystemInfo fi, bool deep = true)
    {
        Guard.NotNull(fi);

        if (!fi.Exists)
        {
            return this;
        }

        Append(HashPathIgnoreCase(fi.FullName));
        Append(fi.CreationTimeUtc.GetHashCode());
        Append(fi.LastWriteTimeUtc.GetHashCode());

        if (fi is FileInfo file)
        {
            Append(file.Length.GetHashCode());
        }

        if (fi is DirectoryInfo dir)
        {
            var options = deep ? LocalDirectory.DeepEnumerationOptions : LocalDirectory.FlatEnumerationOptions;

            foreach (FileSystemInfo f in dir.GetFiles("*", options))
            {
                Add(f, false);
            }
        }

        return this;
    }

    public HashCodeCombiner Add<TStruct>(TStruct? value)
        where TStruct : struct
    {
        if (value.HasValue)
        {
            Append(value.GetHashCode());
        }

        return this;
    }

    public HashCodeCombiner Add<TStruct>(TStruct value)
        where TStruct : struct
    {
        Append(value.GetHashCode());
        return this;
    }

    public HashCodeCombiner Add<T>(T value, IEqualityComparer<T>? comparer = null)
    {
        if (value is null)
        {
            return this;
        }

        // Strings are case-sensitive here (do NOT change semantics).
        if (value is string s)
        {
            Append(HashString(s));
            return this;
        }

        Append(comparer?.GetHashCode(value) ?? value.GetHashCode());
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Append(long hash)
    {
        if (hash != 0)
        {
            _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ hash;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long HashString(string value)
        => HashUtf8(value, ignoreCase: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long HashPathIgnoreCase(string? path)
        => string.IsNullOrEmpty(path) ? 0 : HashUtf8(path, ignoreCase: true);

    /// <summary>
    /// Hashes a string using XxHash3 without allocating for UTF-8 bytes in the common case.
    /// For paths, optional ASCII-only case folding can be applied (A-Z -> a-z) before hashing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long HashUtf8(string value, bool ignoreCase)
    {
        const int StackLimit = 256; // bytes
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);

        if (maxByteCount <= StackLimit)
        {
            Span<byte> buffer = stackalloc byte[StackLimit];
            var len = Encoding.UTF8.GetBytes(value, buffer);
            var slice = buffer.Slice(0, len);

            if (ignoreCase)
            {
                AsciiToLowerInPlace(slice);
            }

            return (long)XxHash3.HashToUInt64(slice);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
            try
            {
                var len = Encoding.UTF8.GetBytes(value, 0, value.Length, rented, 0);
                var span = new Span<byte>(rented, 0, len);

                if (ignoreCase)
                {
                    AsciiToLowerInPlace(span);
                }

                return (long)XxHash3.HashToUInt64(span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AsciiToLowerInPlace(Span<byte> utf8Bytes)
    {
        for (var i = 0; i < utf8Bytes.Length; i++)
        {
            var b = utf8Bytes[i];
            if ((uint)(b - (byte)'A') <= ('Z' - 'A'))
            {
                utf8Bytes[i] = (byte)(b | 0x20);
            }
        }
    }
}