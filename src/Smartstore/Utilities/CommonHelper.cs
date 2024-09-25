using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Utilities
{
    public static partial class CommonHelper
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

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
            return EngineContext.Current?.Application?.ContentRoot ?? new LocalFileSystem(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
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
        /// <summary>
        /// Generates a random digit code
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Result string</returns>
        public static string GenerateRandomDigitCode(int length)
        {
            var buffer = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                buffer[i] = (byte)Random.Shared.Next(10);
            }

            return string.Join(string.Empty, buffer);
        }

        /// <summary>
        /// Generates a cryptographically secure unique random string with a specified length.
        /// The string is composed of alphanumeric characters (A-Z, a-z, 0-9).
        /// The default length of 16 characters provides approximately 96 bits of entropy, 
        /// which makes collisions highly unlikely.
        /// </summary>
        /// <param name="length">The length of the random string to generate. Default is 16.</param>
        /// <returns>A cryptographically secure random string of the specified length.</returns>
        public static string GenerateRandomString(int length = 16)
        {
            var result = new char[length];
            var data = new byte[length];

            // Fill the byte array with cryptographically secure random data
            RandomNumberGenerator.Fill(data);

            // Convert each byte into a character from the alphanumeric set
            for (int i = 0; i < length; i++)
            {
                result[i] = Chars[data[i] % Chars.Length];
            }

            // Return the generated random string
            return new string(result);
        }

        /// <summary>
        /// Returns a random number within the range <paramref name="min"/> to <paramref name="max"/> - 1.
        /// </summary>
        /// <param name="min">Minimum number</param>
        /// <param name="max">Maximum number (exclusive!).</param>
        /// <returns>Random integer number.</returns>
        public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
        {
            return Random.Shared.Next(min, max);
        }

        #endregion

        #region TryAction

        /// <summary>
        /// A simple action executor that tries to execute the given <paramref name="action"/>.
        /// This method does not throw any exception.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="onException">Optional exception handler.</param>
        public static void TryAction(
            Action action, 
            Action<Exception> onException = null)
        {
            Guard.NotNull(action);

            try
            {
                action();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }

        /// <summary>
        /// A simple function executor that tries to execute the given <paramref name="action"/>.
        /// This method does not throw any exception.
        /// </summary>
        /// <param name="action">Function to execute.</param>
        /// <param name="defaultValue">The default value to return when an exception occurs.</param>
        /// <param name="onException">Optional exception handler.</param>
        public static TResult TryAction<TResult>(
            Func<TResult> action, 
            TResult defaultValue = default, 
            Action<Exception> onException = null)
        {
            Guard.NotNull(action);

            try
            {
                return action();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }

            return defaultValue;
        }

        /// <summary>
        /// A simple function executor that tries to execute the given <paramref name="action"/> asynchronously.
        /// This method does not throw any exception.
        /// </summary>
        /// <param name="action">Function to execute.</param>
        /// <param name="defaultValue">The default value to return when an exception occurs.</param>
        /// <param name="onException">Optional exception handler.</param>
        public static async Task<TResult> TryAction<TResult>(
            Func<Task<TResult>> action,
            TResult defaultValue = default,
            Action<Exception> onException = null)
        {
            Guard.NotNull(action);

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }

            return defaultValue;
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
        /// Calculate the optimistic size af any managed object in bytes.
        /// Get the minimal memory footprint of given <paramref name="obj" />.
        /// Counted are all fields, including auto-generated, private and protected.
        /// Not counted: any static fields, any properties, functions, member methods.
        /// </summary>
        [Obsolete("Don't use, too unstable.")]
        public static long CalculateObjectSizeInBytes(object obj, ISet<object> visitedObjects = null)
        {
            if (obj == null)
            {
                return sizeof(int);
            }

            var size = 0L;
            var type = obj.GetType();

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
                    case TypeCode.Decimal:
                        return sizeof(decimal);
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
            else if (type.IsEnum || obj is DateTime || obj is DateTimeOffset || obj is DateOnly || obj is TimeOnly)
            {
                return sizeof(int);
            }
            else if (obj is string str)
            {
                return sizeof(char) * (str.Length + 1);
            }
            else if (obj is StringBuilder sb)
            {
                return _pointerSize + (sizeof(char) * (sb.Length + 1));
            }
            else if (obj is Stream stream)
            {
                return stream.CanSeek ? _pointerSize + stream.Length : _pointerSize;
            }
            else if (obj is IDictionary dic)
            {
                foreach (var key in dic.Keys)
                {
                    size += CalculateObjectSizeInBytes(key, visitedObjects);
                }

                foreach (var value in dic.Values)
                {
                    size += CalculateObjectSizeInBytes(value, visitedObjects);
                }

                return _pointerSize + size;
            }
            else if (obj is IEnumerable e)
            {
                foreach (var item in e)
                {
                    size += CalculateObjectSizeInBytes(item, visitedObjects);
                }

                return _pointerSize + size;
            }
            else if (obj is Pointer)
            {
                return _pointerSize;
            }
            else if (obj is IMemoryCache memCache)
            {
                foreach (var key in memCache.EnumerateKeys())
                {
                    if (memCache.TryGetValue(key, out var value))
                    {
                        size += CalculateObjectSizeInBytes(value, visitedObjects);
                    }
                }
                return _pointerSize;
            }
            else
            {
                size = _pointerSize;
                
                if (ObjectVisited(obj))
                {
                    return size;
                }

                if (IsToxicType(type))
                {
                    return size;
                }

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (IsToxicType(type))
                    {
                        size = _pointerSize;
                        continue;
                    }

                    size += CalculateObjectSizeInBytes(field.GetValue(obj), visitedObjects);
                }

                return size;
            }

            bool ObjectVisited(object o)
            {
                visitedObjects ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

                if (visitedObjects.Contains(o))
                {
                    return true;
                }

                visitedObjects.Add(o);
                return false;
            }

            bool IsToxicType(Type t)
            {
                if (typeof(ILazyLoader).IsAssignableFrom(t))
                {
                    return true;
                }

                if (type.IsDelegate())
                {
                    // Don't visit delegates (Action, Func<> etc.)
                    return true;
                }

                if (type.IsGenericType)
                {
                    var gtdef = type.GetGenericTypeDefinition();
                    if (gtdef == typeof(Lazy<>) || gtdef == typeof(Work<>))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion
    }
}