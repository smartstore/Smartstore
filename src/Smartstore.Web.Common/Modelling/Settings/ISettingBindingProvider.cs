#nullable enable

namespace Smartstore.Web.Modelling.Settings;

/// <summary>
/// Supplies the persisted settings represented by one composite model property.
/// All returned bindings share the outer model property's store override.
/// </summary>
public interface ISettingBindingProvider
{
    /// <summary>
    /// Gets bindings for this model instance using the name of its outer model property.
    /// </summary>
    /// <remarks>Bindings may capture the current instance and must not be cached across models.</remarks>
    IEnumerable<SettingBinding> GetBindings(string modelPropertyName);
}

/// <summary>
/// Connects a composite model value to one persisted settings property.
/// </summary>
/// <param name="TargetPropertyName">The property name on the settings object.</param>
/// <param name="GetValue">Reads the value to write to the settings object.</param>
/// <param name="SetValue">Applies a loaded settings value to the model instance.</param>
public sealed record SettingBinding(
    string TargetPropertyName,
    Func<object?> GetValue,
    Action<object?> SetValue);
