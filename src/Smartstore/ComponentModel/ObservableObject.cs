using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// A base object that implements <see cref="INotifyPropertyChanged"/>.
    /// Changes to any property value will raise the <see cref="PropertyChanged"/>
    /// event handler automatically. Property usage:
    /// <code>
    ///     public string MyProperty
    ///     {
    ///         get => GetProperty<string>(() => "MyDefaultValue");
    ///         set => SetProperty(value);
    ///     }
    /// </code>
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _properties = new();

        public event PropertyChangedEventHandler PropertyChanged;

        [IgnoreDataMember]
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
            _properties.TryGetValue(name, out var oldValue);

            _properties[name] = value;

            if (value != oldValue)
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
