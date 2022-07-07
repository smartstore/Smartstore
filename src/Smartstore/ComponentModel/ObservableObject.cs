using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Smartstore.ComponentModel
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _properties = new();

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        protected IReadOnlyDictionary<string, object> Properties 
        { 
            get => _properties;
        } 

        protected virtual T GetProperty<T>(Func<T> defaultValue = null, [CallerMemberName] string name = null)
        {
            if (_properties.TryGetValue(name, out var value))
            {
                return (T)value;
            }
            else if (defaultValue != null)
            {
                return defaultValue();
            }

            return default;
        }

        protected virtual void SetProperty(object value, [CallerMemberName] string name = null)
        {
            var hasProperty = _properties.TryGetValue(name, out var oldValue);

            _properties[name] = value;

            if (!hasProperty || value != oldValue)
            {
                OnPropertyChanged(name);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
