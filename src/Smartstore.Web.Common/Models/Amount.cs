#nullable enable

using System.Globalization;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Web.Models;

/// <summary>
/// A numeric input value and its unit identifier, bound by MVC as two nested form fields.
/// </summary>
public class Amount : ISettingBindingProvider
{
    private readonly string? _unitField;

    /// <summary>Initializes an amount without a separate percentage setting.</summary>
    public Amount()
    {
    }

    /// <summary>Initializes an amount mapped to a value setting and a Boolean percentage setting.</summary>
    /// <param name="unitField">The name of the Boolean settings property indicating percentage mode.</param>
    public Amount(string unitField)
    {
        Guard.NotEmpty(unitField);
        _unitField = unitField;
    }

    /// <summary>Gets or sets the numeric value without converting it when the unit changes.</summary>
    public decimal Value { get; set; }

    /// <summary>Gets or sets the unit identifier understood by the consuming view model.</summary>
    public string? Unit { get; set; }

    /// <inheritdoc />
    public IEnumerable<SettingBinding> GetBindings(string modelPropertyName)
    {
        Guard.NotEmpty(modelPropertyName);

        yield return new SettingBinding(
            modelPropertyName,
            () => Value,
            value => Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture));

        if (_unitField != null)
        {
            yield return new SettingBinding(
                _unitField,
                () => Unit == "%",
                value => Unit = Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? "%" : null);
        }
    }
}
