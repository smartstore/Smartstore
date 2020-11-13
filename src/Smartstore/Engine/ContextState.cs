using System;
using System.Collections.Generic;
using System.Threading;

namespace Smartstore.Engine
{
    /// <summary>
    /// Holds some state for the current HttpContext or thread
    /// </summary>
    /// <typeparam name="T">The type of data to store</typeparam>
    public class ContextState<T> where T : class
    {
        private readonly string _name;
        private readonly Func<T> _defaultValue;
        private readonly AsyncLocal<IDictionary<string, T>> _asyncLocal = new();

        public ContextState(string name)
        {
            _name = name;
        }

        public ContextState(string name, Func<T> defaultValue)
        {
            _name = name;
            _defaultValue = defaultValue;
        }

        public T GetState()
        {
            var key = BuildKey();

            if (_asyncLocal.Value != null)
            {
                if (_asyncLocal.Value.TryGetValue(key, out var state))
                {
                    return state;
                }
            }

            if (_defaultValue != null)
            {
                SetState(_defaultValue());
                return _asyncLocal.Value.Get(key);
            }

            return default;
        }

        public T SetState(T state)
        {
            var dict = _asyncLocal.Value;

            if (state == null && dict == null)
            {
                return default;
            }

            if (dict == null)
            {
                _asyncLocal.Value = dict = new Dictionary<string, T>();
            }

            var key = BuildKey();

            if (state == null && dict.ContainsKey(key))
            {
                dict.Remove(key);
            }

            if (state != null)
            {
                dict[key] = state;
            }

            return state;
        }

        public void RemoveState()
        {
            SetState(null);
        }

        private string BuildKey()
        {
            return "__ContextState." + _name;
        }
    }
}
