#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

namespace Smartstore.Utilities
{
    /// <summary>
    /// Deterministic hash code combiner.
    /// </summary>
    [DebuggerDisplay("{CombinedHashString}")]
    public struct HashCodeCombiner
    {
        const long _globalSeed = 0x1505L;

        private long _combinedHash64;

        /// <summary>
        /// Initializes the <see cref="HashCodeCombiner"/> with zero seed.
        /// </summary>
        public HashCodeCombiner()
        {
        }

        /// <summary>
        /// Initializes the <see cref="HashCodeCombiner"/> with the given <paramref name="seed"/>.
        /// </summary>
        public HashCodeCombiner(long seed)
        {
            _combinedHash64 = seed;
        }

        /// <summary>
        /// Initializes a deterministic <see cref="HashCodeCombiner"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashCodeCombiner Start()
        {
            return new HashCodeCombiner(_globalSeed);
        }

        /// <summary>
        /// Initializes a non-deterministic <see cref="HashCodeCombiner"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashCodeCombiner StartNonDeterministic()
        {
            return new HashCodeCombiner(CurrentSeed);
        }

        public int CombinedHash
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _combinedHash64.GetHashCode(); }
        }

        public string CombinedHashString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _combinedHash64.GetHashCode().ToString("x", CultureInfo.InvariantCulture); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(HashCodeCombiner self)
        {
            return self.CombinedHash;
        }

        internal static long GlobalSeed { get; } = _globalSeed;
        internal static long CurrentSeed { get; } = CommonHelper.GenerateRandomInteger(min: int.MinValue);

        public HashCodeCombiner AddSequence<T>(IEnumerable<T> sequence, IEqualityComparer<T>? comparer = null) 
            where T : notnull
        {
            if (sequence is not null)
            {
                var count = 0;
                foreach (var o in sequence)
                {
                    Add(o, comparer);
                    count++;
                }

                Append(count);
            }

            return this;
        }

        public HashCodeCombiner AddDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
            where TKey : notnull
            where TValue : notnull
        {
            if (dictionary is not null)
            {
                foreach (var kvp in dictionary.OrderBy(x => x.Key))
                {
                    Add(kvp.Key);
                    Add(kvp.Value);
                }
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

            Add(entry.PhysicalPath?.ToLower());
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

            Add(entry.PhysicalPath?.ToLower());
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

            Add(fi.FullName.ToLower());
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
            // Optimization: for value types, we can avoid boxing "value" by skipping the null check
            if (value.HasValue)
            {
                Append(value.GetHashCode());
            }

            return this;
        }

        public HashCodeCombiner Add<TStruct>(TStruct value)
            where TStruct : struct
        {
            // Optimization: for value types, we can avoid boxing "value" by skipping the null check
            Append(value.GetHashCode());

            return this;
        }

        public HashCodeCombiner Add<T>(T value, IEqualityComparer<T>? comparer = null)
        {
            if (value is string str)
            {
                // XxHash3 is faster than Marvin
                Append((long)XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(str)));
            }
            else if (value is not null)
            {
                Append(comparer?.GetHashCode(value) ?? value.GetHashCode());
            }
            
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
    }
}
