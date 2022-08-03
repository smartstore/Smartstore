using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using Smartstore.Domain;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Utilities
{
    public static partial class CommonHelper
    {
        private static readonly int _pointerSize = Environment.Is64BitOperatingSystem
            ? sizeof(long)
            : sizeof(int);

        #region Deployment

        private static bool? _isDevEnvironment;
        private static bool? _isHosted;
        private static IFileSystem _contentRoot;

        /// <summary>
        /// Gets or sets the file system provider pointing at the path that contains application content files.
        /// </summary>
        public static IFileSystem ContentRoot
        {
            get => LazyInitializer.EnsureInitialized(ref _contentRoot, GetContentRoot);
            set => _contentRoot = value;
        }

        private static IFileSystem GetContentRoot()
        {
            return EngineContext.Current?.Application?.ContentRoot ?? new LocalFileSystem(AppContext.BaseDirectory);
        }

        public static bool IsDevEnvironment
        {
            get => _isDevEnvironment ??= IsDevEnvironmentInternal();
            set => _isDevEnvironment = value;
        }

        public static bool IsHosted
        {
            get => _isHosted ??= EngineContext.Current?.Application?.IsWebHost == true;
            set => _isHosted = value;
        }

        /// <summary>
        /// Maps a path relative to application content root (<see cref="IApplicationContext.ContentRoot"/>) to the full physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <param name="findAppRoot">Specifies if the app root should be resolved when mapped directory does not exist</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        /// <remarks>
        /// This method is able to resolve the web application root
        /// even when it's called during design-time (e.g. from EF design-time tools).
        /// </remarks>
        public static string MapPath(string path, bool findAppRoot = true)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (IsHosted)
            {
                // hosted
                return ContentRoot.MapPath(path);
            }
            else
            {
                // Not hosted. For example, running in unit tests or EF tooling
                string baseDirectory = AppContext.BaseDirectory;
                path = PathUtility.NormalizeRelativePath(path);

                var testPath = Path.Combine(baseDirectory, path);

                if (findAppRoot /* && !Directory.Exists(testPath)*/)
                {
                    // Most likely we're in unit tests or design-mode (EF migration scaffolding)...
                    // find solution root directory first
                    var dir = FindSolutionRoot(baseDirectory);

                    // Concat the web root
                    if (dir != null)
                    {
                        baseDirectory = Path.Combine(dir.FullName, "src", "Smartstore.Web");
                        testPath = Path.Combine(baseDirectory, path);
                    }
                }

                return Path.GetFullPath(testPath);
            }
        }

        private static bool IsDevEnvironmentInternal()
        {
            if (!IsHosted)
                return true;

            if (EngineContext.Current?.Application == null)
                return true;

            if (EngineContext.Current.Application.HostEnvironment.IsDevelopment())
                return true;

            if (System.Diagnostics.Debugger.IsAttached)
                return true;

            // if there's a 'Smartstore.sln' in one of the parent folders,
            // then we're likely in a dev environment
            if (FindSolutionRoot(EngineContext.Current.Application.ContentRoot.Root) != null)
                return true;

            return false;
        }

        internal static DirectoryInfo FindSolutionRoot(string currentDir)
        {
            var dir = Directory.GetParent(currentDir);
            while (true)
            {
                if (dir == null || IsSolutionRoot(dir))
                    break;

                dir = dir.Parent;
            }

            return dir;
        }

        private static bool IsSolutionRoot(DirectoryInfo dir)
        {
            return File.Exists(Path.Combine(dir.FullName, "Smartstore.sln"));
        }

        #endregion

        #region Randomizer

        [ThreadStatic]
        private static Random _random;

        private static Random GetRandomizer()
        {
            return _random ??= new Random();
        }

        /// <summary>
        /// Generate random digit code
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Result string</returns>
        public static string GenerateRandomDigitCode(int length)
        {
            var buffer = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                buffer[i] = (byte)GetRandomizer().Next(10);
            }

            return string.Join(string.Empty, buffer);
        }

        /// <summary>
        /// Returns a random number within the range <paramref name="min"/> to <paramref name="max"/> - 1.
        /// </summary>
        /// <param name="min">Minimum number</param>
        /// <param name="max">Maximum number (exclusive!).</param>
        /// <returns>Random integer number.</returns>
        public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
        {
            using var random = new NumberRandomizer();
            return random.Next(min, max);
        }

        #endregion

        #region Misc

        public static bool IsTruthy(object value)
        {
            if (value == null)
            {
                return false;
            }

            switch (value)
            {
                case string x:
                    return x.HasValue();
                case bool x:
                    return x == true;
                case DateTime x:
                    return x > DateTime.MinValue;
                case TimeSpan x:
                    return x > TimeSpan.MinValue;
                case Guid x:
                    return x != Guid.Empty;
                case IComparable x:
                    return x.CompareTo(0) != 0;
                case IEnumerable<object> x:
                    return x.GetEnumerator().MoveNext();
                case IEnumerable x:
                    return x.GetEnumerator().MoveNext();
            }

            if (value.GetType().IsNullableType(out var underlyingType))
            {
                return IsTruthy(Convert.ChangeType(value, underlyingType));
            }

            return true;
        }

        /// <summary>
        /// Calculate the optimistic size af any managed object.
        /// Get the minimal memory footprint of given <paramref name="obj" />.
        /// Counted are all fields, including auto-generated, private and protected.
        /// Not counted: any static fields, any properties, functions, member methods.
        /// </summary>
        public static long GetObjectSizeInBytes(object obj, HashSet<object> instanceLookup = null)
        {
            if (obj == null)
            {
                return sizeof(int);
            }

            var type = obj.GetType();
            long size = 0;

            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        return sizeof(byte);
                    case TypeCode.Char:
                        return sizeof(char);
                    case TypeCode.Single:
                        return sizeof(float);
                    case TypeCode.Double:
                        return sizeof(double);
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        return sizeof(short);
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        return sizeof(int);
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    default:
                        return sizeof(long);
                }
            }
            else if (obj is decimal)
            {
                return sizeof(decimal);
            }
            else if (obj is string str)
            {
                return sizeof(char) * (str.Length + 1);
            }
            else if (obj is StringBuilder sb)
            {
                return _pointerSize + (sizeof(char) * (sb.Length + 1));
            }
            else if (type.IsEnum)
            {
                return sizeof(int);
            }
            else if (obj is Stream stream)
            {
                return _pointerSize + stream.Length;
            }
            else if (obj is IDictionary dic)
            {
                foreach (var key in dic.Keys)
                {
                    size += GetObjectSizeInBytes(key, instanceLookup);
                }

                foreach (var value in dic.Values)
                {
                    size += GetObjectSizeInBytes(value, instanceLookup);
                }

                return _pointerSize + size;
            }
            else if (obj is IEnumerable e)
            {
                foreach (var item in e)
                {
                    size += GetObjectSizeInBytes(item, instanceLookup);
                }

                return _pointerSize + size;
            }
            else if (type.IsValueType)
            {
                if (obj is DateTime || obj is DateTimeOffset)
                {
                    return 8;
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (var prop in properties)
                    {
                        size += GetObjectSizeInBytes(prop.GetValue(obj), instanceLookup);
                    }

                    return size;
                }
                else
                {
                    try
                    {
                        unsafe
                        {
                            return Marshal.SizeOf(obj);
                        }
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
            else if (obj is Pointer)
            {
                return _pointerSize;
            }
            else if (type.IsClass)
            {
                if (ObjectAnalyzed(obj))
                {
                    return _pointerSize;
                }

                if (typeof(BaseEntity).IsAssignableFrom(type) || type.IsNotPublic)
                {
                    return _pointerSize;
                }

                size += _pointerSize;
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    size += GetObjectSizeInBytes(field.GetValue(obj), instanceLookup);
                }

                return size;
            }

            return 0;

            bool ObjectAnalyzed(object o)
            {
                if (instanceLookup == null)
                {
                    instanceLookup = new HashSet<object>(ReferenceEqualityComparer.Instance);
                }

                if (instanceLookup.Contains(o))
                {
                    return true;
                }

                instanceLookup.Add(o);
                return false;
            }
        }

        #endregion
    }
}