namespace Smartstore.Threading
{
    public abstract class ContextState
    {
        private readonly static AsyncLocal<StateHolder> _asyncLocalState = new();

        public static IDictionary<string, object> StartAsyncFlow()
        {
            Items = new Dictionary<string, object>();
            return _asyncLocalState.Value?.Items;
        }

        public static IDictionary<string, object> Items
        {
            get
            {
                return _asyncLocalState.Value?.Items;
            }
            protected set
            {
                Guard.NotNull(value, nameof(value));

                var holder = _asyncLocalState.Value;
                if (holder != null)
                {
                    holder.Items = null;
                }

                if (value != null)
                {
                    // This one will set _threadState too
                    _asyncLocalState.Value = new StateHolder { Items = value };
                }
            }
        }

        class StateHolder
        {
            public IDictionary<string, object> Items;
        }
    }

    /// <summary>
    /// Holds some state for the current local call context
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class ContextState<T> : ContextState
    {
        private readonly string _name;
        private readonly Func<T> _defaultValue;

        public ContextState(string name)
            : this(name, null)
        {
        }

        public ContextState(string name, Func<T> defaultValue)
        {
            Guard.NotEmpty(name, nameof(name));

            _name = name;
            _defaultValue = defaultValue;
        }

        public T Get()
        {
            var items = Items;

            if (items != null && items.TryGetValue(_name, out var value))
            {
                return (T)value;
            }

            if (_defaultValue != null)
            {
                var state = _defaultValue();
                Push(state);
                return state;
            }

            return default;
        }

        public void Remove()
        {
            Push(default);
        }

        public T Push(T state)
        {
            var items = Items;

            if (state == null && items == null)
            {
                return default;
            }

            if (items == null)
            {
                items = StartAsyncFlow();
            }

            if (state == null && items.ContainsKey(_name))
            {
                items.Remove(_name);
            }

            if (state != null)
            {
                items[_name] = state;
            }

            return state;
        }
    }

    ///// <summary>
    ///// Holds some state for the current local call context
    ///// </summary>
    ///// <typeparam name="T">The type of data to store</typeparam>
    //public class ContextState<T> where T : class
    //{
    //    private readonly string _name;
    //    private readonly Func<T> _defaultValue;
    //    private readonly AsyncLocal<IDictionary<string, T>> _asyncLocal = new();

    //    public ContextState(string name)
    //        : this(name, null)
    //    {
    //    }

    //    public ContextState(string name, Func<T> defaultValue)
    //    {
    //        Guard.NotEmpty(name, nameof(name));

    //        _name = name;
    //        _defaultValue = defaultValue;
    //    }

    //    public T GetState()
    //    {
    //        var key = BuildKey();

    //        if (_asyncLocal.Value != null)
    //        {
    //            if (_asyncLocal.Value.TryGetValue(key, out var state))
    //            {
    //                return state;
    //            }
    //        }

    //        if (_defaultValue != null)
    //        {
    //            SetState(_defaultValue());
    //            return _asyncLocal.Value.Get(key);
    //        }

    //        return default;
    //    }

    //    public T SetState(T state)
    //    {
    //        var dict = _asyncLocal.Value;

    //        if (state == null && dict == null)
    //        {
    //            return default;
    //        }

    //        if (dict == null)
    //        {
    //            _asyncLocal.Value = dict = new Dictionary<string, T>();
    //        }

    //        var key = BuildKey();

    //        if (state == null && dict.ContainsKey(key))
    //        {
    //            dict.Remove(key);
    //        }

    //        if (state != null)
    //        {
    //            dict[key] = state;
    //        }

    //        return state;
    //    }

    //    public void RemoveState()
    //    {
    //        SetState(null);
    //    }

    //    private string BuildKey()
    //    {
    //        return "__ContextState." + _name;
    //    }
    //}
}