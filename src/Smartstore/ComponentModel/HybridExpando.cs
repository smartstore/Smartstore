#nullable enable

using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel
{
    public enum MemberOptMethod
    {
        Allow,
        Disallow
    }

    /// <summary>
    /// Class that provides extensible properties and methods to an
    /// existing object when cast to dynamic. This
    /// dynamic object stores 'extra' properties in a dictionary or
    /// checks the actual properties of the instance passed via 
    /// constructor.
    /// 
    /// This class can be subclassed to extend an existing type or 
    /// you can pass in an instance to extend. Properties (both
    /// dynamic and strongly typed) can be accessed through an 
    /// indexer.
    /// 
    /// This type allows you three ways to access its properties:
    /// 
    /// Directly: any explicitly declared properties are accessible.
    /// Dynamic: dynamic cast allows access to dictionary and native properties/methods.
    /// Dictionary: Any of the extended properties are accessible via dictionary interface
    /// </summary>
    [Serializable]
    public class HybridExpando : DynamicObject, IDictionary<string, object?>, INotifyPropertyChanged
    {
        /// <summary>
        /// Instance of object passed in
        /// </summary>
        private object? _instance;

        /// <summary>
        /// Type of the instance
        /// </summary>
        private Type? _instanceType;

        /// <summary>
        /// Adjusted property list for the wrapped instance type after white/black-list members has been applied.
        /// </summary>
        private IDictionary<string, FastProperty>? _instanceProps;
        
        /// <summary>
        /// String Dictionary that contains the extra dynamic values
        /// stored on this object/instance
        /// </summary>        
        public Dictionary<string, object?> Properties = new();

        private readonly ISet<string>? _optMembers;
        private readonly MemberOptMethod _optMethod;

        private readonly bool _returnNullWhenFalsy;

        private PropertyChangedEventHandler? _propertyChanged;

        /// <summary>
        /// This constructor just works off the internal dictionary and any 
        /// public properties of this object.
        /// 
        /// Note you can subclass HybridExpando.
        /// </summary>
        public HybridExpando(bool returnNullWhenFalsy = false)
        {
            _returnNullWhenFalsy = returnNullWhenFalsy;
        }

        /// <summary>
        /// Allows passing in an existing instance variable to 'extend'.        
        /// </summary>
        /// <param name="instance"></param>
        public HybridExpando(object instance, bool returnNullWhenFalsy = false)
        {
            Guard.NotNull(instance);

            _returnNullWhenFalsy = returnNullWhenFalsy;
            Initialize(instance);
        }

        /// <summary>
        /// Allows passing in an existing instance variable to 'extend'
        /// along with a list of member names to allow or disallow.
        /// </summary>
        /// <param name="instance"></param>
        public HybridExpando(object instance, IEnumerable<string> optMembers, MemberOptMethod optMethod, bool returnNullWhenFalsy = false)
        {
            Guard.NotNull(instance);
            
            _returnNullWhenFalsy = returnNullWhenFalsy;
            Initialize(instance);

            _optMethod = optMethod;

            if (optMembers is ISet<string> h)
            {
                _optMembers = h;
            }
            else
            {
                _optMembers = new HashSet<string>(optMembers);
            }
        }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        protected void Initialize(object? instance)
        {
            _instance = instance;
            _instanceType = instance?.GetType();
        }

        protected object? WrappedObject
        {
            get { return _instance; }
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Properties.Keys
                .Union(InstanceProperties.Keys)
                .ToArray();
        }

        /// <summary>
        /// Try to retrieve a member by name first from instance properties
        /// followed by the collection entries.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
            => TryGetMemberCore(binder.Name, out result);

        protected virtual bool TryGetMemberCore(string name, out object? result)
        {
            // First check the Properties collection for member
            var exists = Properties.TryGetValue(name, out result);

            // Next check for public properties via Reflection
            if (!exists && _instance != null)
            {
                try
                {
                    exists = GetProperty(_instance, name, out result);
                }
                catch
                {
                }
            }

            // Falsy check
            if (_returnNullWhenFalsy && result != null && !CommonHelper.IsTruthy(result))
            {
                result = null;
            }

            // Failed to retrieve a property
            return exists;
        }


        /// <summary>
        /// Property setter implementation tries to retrieve value from instance 
        /// first, then into this object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool TrySetMember(SetMemberBinder binder, object? value)
            => TrySetMemberCore(binder.Name, value);

        protected virtual bool TrySetMemberCore(string name, object? value)
        {
            var result = false;

            // First check to see if there's a dictionary entry to set
            if (Properties.TryGetValue(name, out var oldValue))
            {
                Properties[name] = value;
                result = true;
            }

            // Check to see if there's a native property to set
            if (!result && _instance != null)
            {
                try
                {
                    result = SetProperty(_instance, name, value, out oldValue);
                }
                catch
                {
                }
            }

            // No match - set or add to dictionary
            if (!result)
            {
                Properties[name] = value;
            }

            // Notify property changed
            if (_propertyChanged != null && value != oldValue)
            {
                _propertyChanged(this, new PropertyChangedEventArgs(name));
            }

            return true;
        }

        /// <summary>
        /// Dynamic invocation method. Currently allows only for Reflection based
        /// operation (no ability to add members dynamically).
        /// </summary>
        public void Override(string name, object? value = null)
        {
            Guard.NotEmpty(name);

            Properties.TryGetValue(name, out var oldValue);

            Properties[name] = value;

            // Notify property changed
            if (_propertyChanged != null && value != oldValue)
            {
                _propertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <returns></returns>
        /// Dynamic invocation method. Currently allows only for Reflection based
        /// operation (no ability to add methods dynamically).
        /// </summary>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (_instance != null)
            {
                try
                {
                    // Check instance passed in for methods to invoke
                    if (InvokeMethod(_instance, binder.Name, args, out result))
                    {
                        return true;
                    }   
                }
                catch
                {
                }
            }

            result = null;
            return false;
        }


        /// <summary>
        /// Reflection Helper method to retrieve a property
        /// </summary>
        protected bool GetProperty(object instance, string name, out object? result)
        {
            if (InstanceProperties.TryGetValue(name, out var fastProp))
            {
                result = fastProp.GetValue(instance);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Reflection helper method to set a property value
        /// </summary>
        protected bool SetProperty(object instance, string name, object? value, out object? oldValue)
        {
            oldValue = null;

            if (InstanceProperties.TryGetValue(name, out var fastProp))
            {
                oldValue = fastProp.GetValue(instance);
                fastProp.SetValue(instance, value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Reflection helper method to invoke a method
        /// </summary>
        protected bool InvokeMethod(object instance, string name, object?[]? args, out object? result)
        {
            // Look at the instanceType
            var mi = _instanceType?.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                result = mi.Invoke(instance, args);
                return true;
            }

            result = null;
            return false;
        }


        /// <summary>
        /// Convenience method that provides a string Indexer 
        /// to the Properties collection AND the strongly typed
        /// properties of the object by name.
        /// 
        /// // dynamic
        /// exp["Address"] = "112 nowhere lane"; 
        /// // strong
        /// var name = exp["StronglyTypedProperty"] as string; 
        /// </summary>
        /// <remarks>
        /// The getter checks the Properties dictionary first
        /// then looks in PropertyInfo for properties.
        /// The setter checks the instance properties before
        /// checking the Properties dictionary.
        /// </remarks>
        public object? this[string key]
        {
            get
            {
                if (!TryGetMemberCore(key, out var result))
                {
                    throw new KeyNotFoundException();
                }

                return result;
            }
            set
            {
                TrySetMemberCore(key, value);
            }
        }


        /// <summary>
        /// Enumerates all properties in both dictionary and instance.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>> GetProperties(bool includeInstanceProperties = false)
        {
            foreach (var kvp in Properties)
            {
                yield return kvp;
            }

            if (includeInstanceProperties)
            {
                foreach (var kvp2 in InstanceProperties)
                {
                    var prop = kvp2.Value;
                    if (!Properties.ContainsKey(prop.Name))
                    {
                        yield return new KeyValuePair<string, object?>(prop.Name, prop.GetValue(_instance));
                    }
                }
            }
        }

        private IDictionary<string, FastProperty> InstanceProperties
        {
            get
            {
                if (_instance == null)
                {
                    return FrozenDictionary<string, FastProperty>.Empty;
                }

                if (_instanceProps == null)
                {
                    var props = FastProperty.GetProperties(_instance.GetType()) as IDictionary<string, FastProperty>;

                    if (_optMembers != null)
                    {
                        props = props!
                            .Where(x => _optMethod == MemberOptMethod.Allow ? _optMembers.Contains(x.Key) : !_optMembers.Contains(x.Key))
                            .ToDictionary(x => x.Key, x => x.Value);
                    }

                    _instanceProps = props;
                }

                return _instanceProps!;
            }
        }

        /// <summary>
        /// Checks whether a property exists in the Property collection
        /// or as a property on the instance
        /// </summary>
        public bool Contains(KeyValuePair<string, object?> item, bool includeInstanceProperties = false)
            => Contains(item.Key, includeInstanceProperties);

        /// <summary>
        /// Checks whether a property exists in the Property collection
        /// or as a property on the instance
        /// </summary>
        public bool Contains(string propertyName, bool includeInstanceProperties = false)
        {
            return 
                Properties.ContainsKey(propertyName) || 
                (includeInstanceProperties && InstanceProperties.ContainsKey(propertyName));
        }

        #region IDictionary<string, object?>

        ICollection<string> IDictionary<string, object?>.Keys
        {
            get => GetProperties(true).Select(x => x.Key).AsReadOnly();
        }

        ICollection<object?> IDictionary<string, object?>.Values
        {
            get => GetProperties(true).Select(x => x.Value).AsReadOnly();
        }

        int ICollection<KeyValuePair<string, object?>>.Count
        {
            get => GetDynamicMemberNames().Count();
        }

        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly
        {
            get => false;
        }

        object? IDictionary<string, object?>.this[string key]
        {
            get => this[key];
            set => this[key] = value;
        }

        bool IDictionary<string, object?>.ContainsKey(string key)
            => Contains(key, true);

        void IDictionary<string, object?>.Add(string key, object? value)
            => throw new NotImplementedException();

        bool IDictionary<string, object?>.Remove(string key)
            => throw new NotImplementedException();

        public bool TryGetValue(string key, out object? value)
        {
            value = null;

            if (Contains(key, true))
            {
                value = this[key];
                return true;
            }

            return false;
        }

        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
            => throw new NotImplementedException();

        void ICollection<KeyValuePair<string, object?>>.Clear()
            => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
            => TryGetValue(item.Key, out var value) && Equals(value, item.Value);

        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
            => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
            => throw new NotImplementedException();

        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
            => GetProperties(true).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetProperties(true).GetEnumerator();

        #endregion
    }
}