namespace Smartstore.Web.Models;

/// <summary>
/// An amount editor's stable unit identifier and its visible label.
/// </summary>
/// <param name="Value">The identifier posted in Amount.Unit.</param>
/// <param name="Text">The encoded text displayed in the postfix and dropdown.</param>
public sealed record AmountUnit(string Value, string Text = null)
{
    private string _text = Text;

    /// <summary>Gets or sets the display text, falling back to Value when no text is supplied.</summary>
    public string Text
    {
        get => _text ?? Value;
        init => _text = value;
    }
}
