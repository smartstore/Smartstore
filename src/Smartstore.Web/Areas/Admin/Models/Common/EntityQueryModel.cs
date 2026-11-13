#nullable enable

using Smartstore.Web.Models;

namespace Smartstore.Admin.Models.Common;

/// <summary>
/// Represents a model to query entities.
/// </summary>
public class EntityQueryModel
{
    /// <summary>
    /// Gets or sets a value to filter entities by term.
    /// </summary>
    public string? Term { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the selected item.
    /// </summary>
    public string? SelectedValues { get; set; }
    
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    /// <remarks>Page numbers are zero-based.</remarks>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity field used as the source for <see cref="ChoiceListItem.Id"/>.
    /// </summary>
    public string? SourceFieldName { get; set; }
}
