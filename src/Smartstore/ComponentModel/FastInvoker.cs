using System.Collections.Concurrent;
using System.Reflection;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel
{
    public class FastInvoker
    {
        private static readonly ConcurrentDictionary<MethodKey, FastInvoker> _invokersCache = new();

        private MethodInvoker _invoker;

        public FastInvoker(MethodInfo methodInfo)
        {
            Guard.NotNull(methodInfo);

            Method = methodInfo;
            ParameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        }

        /// <summary>
        /// Gets the backing <see cref="MethodInfo"/>.
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Gets the parameter types from the backing <see cref="MethodInfo"/>
        /// </summary>
        public Type[] ParameterTypes { get; private set; }

        /// <summary>
        /// Gets the method invoker.
        /// </summary>
        public MethodInvoker Invoker
        {
            get
            {
                if (_invoker == null)
                {
                    Interlocked.Exchange(ref _invoker, MethodInvoker.Create(Method));
                }

                return _invoker;
            }
        }

        /// <summary>
        /// Invokes the method using the specified parameters.
        /// </summary>
        /// <returns>The method invocation result.</returns>
        public object Invoke(object obj, params object[] parameters)
        {
            return Invoker.Invoke(obj, parameters.AsSpan());
        }

        #region Static

        /// <summary>
        /// Invokes a method using the specified object and parameter instances.
        /// </summary>
        /// <param name="obj">The objectinstance</param>
        /// <param name="methodName">Method name</param>
        /// <param name="parameterTypes">Argument types of the matching method overload (in exact order)</param>
        /// <param name="parameters">Parameter instances to pass to invocation</param>
        /// <returns>The method invocation result.</returns>
        public static object Invoke(object obj, string methodName, Type[] parameterTypes, object[] parameters)
        {
            Guard.NotNull(obj);

            FastInvoker invoker;

            if (parameterTypes == null || parameterTypes.Length == 0)
            {
                invoker = GetInvoker(obj.GetType(), methodName);
            }
            else
            {
                invoker = GetInvoker(obj.GetType(), methodName, parameterTypes);
            }

            return invoker.Invoke(obj, parameters ?? []);
        }

        /// <summary>
        /// Creates and caches a fast method invoker.
        /// </summary>
        /// <param name="methodName">Name of method to create an invoker for.</param>
        /// <param name="argTypes">Argument types of method to create an invoker for.</param>
        /// <returns>The fast method invoker.</returns>
        public static FastInvoker GetInvoker<T>(string methodName, params Type[] argTypes)
        {
            return GetInvoker(typeof(T), methodName, argTypes);
        }

        /// <summary>
        /// Creates and caches a fast method invoker.
        /// </summary>
        /// <param name="type">The type to extract fast method invoker for.</param>
        /// <param name="methodName">Name of method to create an invoker for.</param>
        /// <param name="argTypes">Argument types of method to create an invoker for.</param>
        /// <returns>The fast method invoker.</returns>
        public static FastInvoker GetInvoker(Type type, string methodName, params Type[] argTypes)
        {
            Guard.NotNull(type);
            Guard.NotEmpty(methodName);

            var cacheKey = MethodKey.Create(type, methodName, argTypes);

            var invoker = _invokersCache.GetOrAdd(cacheKey, key => 
            {
                var method = FindMatchingMethod(type, methodName, argTypes);
                if (method == null)
                {
                    throw new MethodAccessException("Could not find a matching method '{0}' in type {1}.".FormatInvariant(methodName, type));
                }

                return new FastInvoker(method);
            });

            return invoker;
        }

        /// <summary>
        /// Creates and caches a fast method invoker.
        /// </summary>
        /// <param name="method">Method info instance to create an invoker for.</param>
        /// <returns>The fast method invoker.</returns>
        public static FastInvoker GetInvoker(MethodInfo method)
        {
            Guard.NotNull(method);

            return _invokersCache.GetOrAdd(MethodKey.Create(method), key =>
            {
                return new FastInvoker(method);
            });
        }

        private static MethodInfo FindMatchingMethod(Type type, string methodName, Type[] argTypes)
        {
            var method = argTypes == null || argTypes.Length == 0
                ? type.GetMethod(methodName)
                : type.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null,
                    argTypes ?? [],
                    null);

            return method;
        }

        #endregion

        abstract class MethodKey
        {
            public override bool Equals(object obj) =>
                throw new NotImplementedException();

            public override int GetHashCode() =>
                throw new NotImplementedException();

            public static bool operator ==(MethodKey left, MethodKey right) =>
                object.Equals(left, right);

            public static bool operator !=(MethodKey left, MethodKey right) =>
                !(left == right);

            internal static MethodKey Create(Type type, string methodName, IEnumerable<Type> parameterTypes)
            {
                return new HashMethodKey(type, methodName, parameterTypes);
            }

            internal static MethodKey Create(MethodInfo method)
            {
                return new MethodInfoKey(method);
            }
        }

        class HashMethodKey : MethodKey, IEquatable<HashMethodKey>
        {
            private readonly int _hash;

            public HashMethodKey(Type type, string methodName, IEnumerable<Type> parameterTypes)
            {
                _hash = HashCodeCombiner.Start().Add(type).Add(methodName).Add(parameterTypes).CombinedHash;
            }

            public override bool Equals(object obj) =>
                this.Equals(obj as HashMethodKey);

            public bool Equals(HashMethodKey other)
            {
                if (other == null)
                    return false;

                return this._hash == other._hash;
            }

            public override int GetHashCode()
            {
                return _hash;
            }
        }

        class MethodInfoKey(MethodInfo method) : MethodKey, IEquatable<MethodInfoKey>
        {
            private readonly MethodInfo _method = method;

            public override bool Equals(object obj) =>
                this.Equals(obj as MethodInfoKey);

            public bool Equals(MethodInfoKey other)
            {
                if (other == null)
                    return false;

                return this._method == other._method;
            }

            public override int GetHashCode()
            {
                return _method.GetHashCode();
            }
        }
    }
}