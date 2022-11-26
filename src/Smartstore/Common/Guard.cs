#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Smartstore
{
    public class Guard
    {
        const string AgainstMessage = "Assertion evaluation failed with 'false'.";
        const string ImplementsMessage = "Type '{0}' must implement type '{1}'.";
        const string InheritsFromMessage = "Type '{0}' must inherit from type '{1}'.";
        const string IsTypeOfMessage = "Type '{0}' must be of type '{1}'.";
        const string IsEqualMessage = "Compared objects must be equal.";
        const string IsPositiveMessage = "Argument '{0}' must be a positive value. Value: '{1}'.";
        const string IsTrueMessage = "True expected for '{0}' but the condition was False.";
        const string NotNegativeMessage = "Argument '{0}' cannot be a negative value. Value: '{1}'.";
        const string NotEmptyStringMessage = "String parameter '{0}' cannot be null or all whitespace.";
        const string NotEmptyColMessage = "Collection cannot be null and must contain at least one item.";
        const string NotEmptyGuidMessage = "Argument '{0}' cannot be an empty guid.";
        const string InRangeMessage = "The argument '{0}' must be between '{1}' and '{2}'.";
        const string NotOutOfLengthMessage = "Argument '{0}' cannot be more than {1} characters long.";
        const string NotZeroMessage = "Argument '{0}' must be greater or less than zero. Value: '{1}'.";
        const string IsEnumTypeMessage = "Type '{0}' must be a valid Enum type.";
        const string IsEnumTypeMessage2 = "The value of the argument '{0}' provided for the enumeration '{1}' is invalid.";
        const string IsClosedTypeOfMessage = "Type '{0}' must be a closed type of '{1}'.";
        const string HasDefaultConstructorMessage = "The type '{0}' must have a default parameterless constructor.";

        private Guard()
        {
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotNull<T>(T? arg, [CallerArgumentExpression("arg")] string? argName = null)
        {
            return arg ?? throw new ArgumentNullException(argName);
        }

        [DebuggerStepThrough]
        public static void NotEmpty(string? arg, [CallerArgumentExpression("arg")] string? argName = null)
        {
            if (arg is null)
            {
                throw new ArgumentNullException(argName);
            }
            else if (arg.Trim().Length == 0)
            {
                throw new ArgumentException(string.Format(NotEmptyStringMessage, argName), argName);
            }
        }

        [DebuggerStepThrough]
        public static void NotOutOfLength(string? arg, int maxLength, [CallerArgumentExpression("arg")] string? argName = null)
        {
            if (arg is null)
            {
                throw new ArgumentNullException(argName);
            }
            else if (arg.Trim().Length > maxLength)
            {
                throw new ArgumentException(string.Format(NotOutOfLengthMessage, argName, maxLength), argName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ICollection<T> NotEmpty<T>(ICollection<T>? arg, [CallerArgumentExpression("arg")] string? argName = null)
        {
            if (arg == null || arg.Count == 0)
            {
                throw new ArgumentException(NotEmptyColMessage, argName);
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotEmpty<T>(T arg, [CallerArgumentExpression("arg")] string? argName = null) where T : struct
        {
            if (Equals(arg, default(T)))
            {
                throw new ArgumentException(string.Format(NotEmptyGuidMessage, argName), argName);
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T InRange<T>(T arg, T min, T max, [CallerArgumentExpression("arg")] string? argName = null) where T : struct, IComparable<T>
        {
            if (arg.CompareTo(min) < 0 || arg.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(argName, string.Format(InRangeMessage, argName, min, max));
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotNegative<T>(T arg, [CallerArgumentExpression("arg")] string? argName = null, string message = NotNegativeMessage) where T : struct, IComparable<T>
        {
            if (arg.CompareTo(default) < 0)
            {
                throw new ArgumentOutOfRangeException(argName, string.Format(message, argName, arg));
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IsPositive<T>(T arg, [CallerArgumentExpression("arg")] string? argName = null, string message = IsPositiveMessage) where T : struct, IComparable<T>
        {
            if (arg.CompareTo(default) < 1)
            {
                throw new ArgumentOutOfRangeException(argName, string.Format(message, argName, arg));
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotZero<T>(T arg, [CallerArgumentExpression("arg")] string? argName = null) where T : struct, IComparable<T>
        {
            if (arg.CompareTo(default) == 0)
            {
                throw new ArgumentOutOfRangeException(argName, string.Format(NotZeroMessage, argName, arg));
            }

            return arg;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Against<TException>(bool assertion, string message = AgainstMessage) where TException : Exception
        {
            if (assertion)
                throw (TException)Activator.CreateInstance(typeof(TException), message)!;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Against<TException>(Func<bool> assertion, string message = AgainstMessage) where TException : Exception
        {
            //Execute the lambda and if it evaluates to true then throw the exception.
            if (assertion())
                throw (TException)Activator.CreateInstance(typeof(TException), message)!;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool arg, [CallerArgumentExpression("arg")] string? argName = null, string message = IsTrueMessage)
        {
            if (!arg)
                throw new ArgumentException(string.Format(message, argName), argName);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEnumType(Type arg, [CallerArgumentExpression("arg")] string? argName = null)
        {
            if (!arg.IsEnum)
            {
                throw new ArgumentException(string.Format(IsEnumTypeMessage, arg.FullName), argName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEnumType(Type enumType, object arg, [CallerArgumentExpression("arg")] string? argName = null)
        {
            if (!Enum.IsDefined(enumType, arg))
            {
                throw new ArgumentException(string.Format(IsEnumTypeMessage2, arg, enumType.FullName), argName);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NotDisposed<T>(T arg, [CallerArgumentExpression("arg")] string? argName = null) where T : Disposable
        {
            if (arg.IsDisposed)
            {
                throw new ObjectDisposedException(argName);
            }

            return arg;
        }

        [DebuggerStepThrough]
        public static void PagingArgsValid(int indexArg, int sizeArg, [CallerArgumentExpression("indexArg")] string? indexArgName = null, [CallerArgumentExpression("sizeArg")] string? sizeArgName = null)
        {
            NotNegative(indexArg, indexArgName, "PageIndex cannot be below 0");

            if (indexArg > 0)
            {
                // if pageIndex is specified (> 0), PageSize CANNOT be 0 
                IsPositive(sizeArg, sizeArgName, "PageSize cannot be below 1 if a PageIndex greater 0 was provided.");
            }
            else
            {
                // pageIndex 0 actually means: take all!
                NotNegative(sizeArg, sizeArgName);
            }
        }

        [DebuggerStepThrough]
        public static void InheritsFrom<TBase>(Type type)
        {
            InheritsFrom<TBase>(type, InheritsFromMessage.FormatInvariant(type.FullName, typeof(TBase).FullName));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InheritsFrom<TBase>(Type type, string message)
        {
            if (type.BaseType != typeof(TBase))
            {
                throw new InvalidOperationException(message);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsAssignableFrom<TBase>(Type type, string message = ImplementsMessage)
        {
            if (!typeof(TBase).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(message.FormatInvariant(type.FullName, typeof(TBase).FullName));
            }
        }

        [DebuggerStepThrough]
        public static void IsClosedTypeOf<TBase>(Type type)
        {
            var baseType = typeof(TBase);
            if (!baseType.IsClosedGenericTypeOf(type))
            {
                throw new InvalidOperationException(IsClosedTypeOfMessage.FormatInvariant(type.FullName, baseType.FullName));
            }
        }

        [DebuggerStepThrough]
        public static TType IsTypeOf<TType>(object instance)
        {
            return IsTypeOf<TType>(instance, IsTypeOfMessage.FormatInvariant(instance.GetType().Name, typeof(TType).FullName));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TType IsTypeOf<TType>(object? instance, string message)
        {
            if (instance is not TType casted)
            {
                throw new InvalidOperationException(message);
            }

            return casted;
        }

        [DebuggerStepThrough]
        public static void IsEqual<TException>(object compare, object? instance, string message = IsEqualMessage) where TException : Exception
        {
            if (!compare.Equals(instance))
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message)!;
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HasDefaultConstructor<T>()
        {
            HasDefaultConstructor(typeof(T));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HasDefaultConstructor(Type t)
        {
            if (!t.HasDefaultConstructor())
            {
                throw new InvalidOperationException(string.Format(HasDefaultConstructorMessage, t.FullName));
            }
        }
    }

}
