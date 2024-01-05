using System.Collections.Concurrent;
using System.Reflection;

namespace Smartstore.ComponentModel
{
    public class FastActivator
    {
        private static readonly ConcurrentDictionary<Type, FastActivator[]> _activatorsCache = new();

        private ConstructorInvoker _invoker;

        public FastActivator(ConstructorInfo constructorInfo)
        {
            Guard.NotNull(constructorInfo);
            
            Constructor = constructorInfo;
            ParameterTypes = constructorInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        }

        /// <summary>
        /// Gets the backing <see cref="ConstructorInfo"/>.
        /// </summary>
        public ConstructorInfo Constructor { get; private set; }

        /// <summary>
        /// Gets the parameter types from the backing <see cref="ConstructorInfo"/>
        /// </summary>
        public Type[] ParameterTypes { get; private set; }

        /// <summary>
        /// Gets the constructor invoker.
        /// </summary>
        public ConstructorInvoker Invoker
        {
            get
            {
                if (_invoker == null)
                {
                    Interlocked.Exchange(ref _invoker, ConstructorInvoker.Create(Constructor));
                }

                return _invoker;
            }
        }

        /// <summary>
        /// Creates an instance of the type using the specified parameters.
        /// </summary>
        /// <returns>A reference to the newly created object.</returns>
        public object Activate(params object[] parameters)
        {
            return Invoker.Invoke(parameters.AsSpan());
        }

        #region Static

        /// <summary>
        /// Creates and caches fast constructor invokers 
        /// </summary>
        /// <param name="type">The type to extract fast constructor invokers for</param>
        /// <returns>A cached array of all public instance constructors from the given type.</returns>
        /// <remarks>The parameterless default constructor is always excluded from the list of activators</remarks>
        public static FastActivator[] GetActivators(Type type)
        {
            return GetActivatorsCore(type);
        }

        private static FastActivator[] GetActivatorsCore(Type type)
        {
            var activators = _activatorsCache.GetOrAdd(type, key =>
            {
                var candidates = GetCandidateConstructors(type);
                return candidates.Select(c => new FastActivator(c)).ToArray();
            });

            return activators;
        }

        /// <summary>
        /// Creates an instance of the specified type using the constructor that best matches the specified parameters.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="args">
        /// An array of arguments that match in number, order, and type the parameters of the constructor to invoke. 
        /// If args is an empty array or null, the constructor that takes no parameters (the default constructor) is invoked. 
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        public static T CreateInstance<T>(params object[] args)
        {
            return (T)CreateInstance(typeof(T), args);
        }

        /// <summary>
        /// Creates an instance of the specified type using the constructor that best matches the specified parameters.
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <param name="args">
        /// An array of arguments that match in number, order, and type the parameters of the constructor to invoke. 
        /// If args is an empty array or null, the constructor that takes no parameters (the default constructor) is invoked. 
        /// </param>
        /// <returns>A reference to the newly created object.</returns>
        public static object CreateInstance(Type type, params object[] args)
        {
            Guard.NotNull(type);

            if (args == null || args.Length == 0)
            {
                // don't struggle with FastActivator: native reflection is really fast with default constructor!
                return Activator.CreateInstance(type);
            }

            var activators = GetActivatorsCore(type);
            var matchingActivator = FindMatchingActivatorCore(activators, type, args);

            if (matchingActivator == null)
            {
                throw new ArgumentException("No matching contructor was found for the given arguments.", "args");
            }

            return matchingActivator.Activate(args);
        }

        public static FastActivator FindMatchingActivator(Type type, params object[] args)
        {
            var activators = GetActivatorsCore(type);
            var matchingActivator = FindMatchingActivatorCore(activators, type, args);

            return matchingActivator;
        }

        private static FastActivator FindMatchingActivatorCore(FastActivator[] activators, Type type, object[] args)
        {
            if (activators.Length == 0)
            {
                return null;
            }

            if (activators.Length == 1)
            {
                // this seems to be bad design, but it's on purpose for performance reasons.
                // In nearly ALL cases there is only one constructor.
                return activators[0];
            }

            var argTypes = args.Select(x => x.GetType()).ToArray();
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null,
                argTypes,
                null);

            if (constructor != null)
            {
                var matchingActivator = activators.FirstOrDefault(a => a.Constructor == constructor);
                return matchingActivator;
            }

            return null;
        }

        private static IEnumerable<ConstructorInfo> GetCandidateConstructors(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            return constructors.Where(c => c.GetParameters().Length > 0);

        }

        #endregion
    }
}