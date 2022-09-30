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
}