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
        private readonly AsyncLocal<IDictionary<string, T>> _serviceProvider = new AsyncLocal<IDictionary<string, T>>();

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

            if (_serviceProvider.Value != null)
            {
                if (_serviceProvider.Value.ContainsKey(key))
                {
                    return _serviceProvider.Value[key];
                }
            }

            if (_defaultValue != null)
            {
                SetState(_defaultValue());
                return _serviceProvider.Value[key];
            }

            return default;
        }

        public T SetState(T state)
        {
            var dict = _serviceProvider.Value;

            if (state == null && dict == null)
            {
                return default;
            }
            
            if (dict == null)
            {
                _serviceProvider.Value = dict = new Dictionary<string, T>();
            }

            if (state == null && dict.ContainsKey(BuildKey()))
            {
                dict.Remove(BuildKey());
            }
            else
            {
                dict.Add(BuildKey(), state);
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
