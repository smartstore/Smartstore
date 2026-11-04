using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Models.Dashboard;

/// <summary>
/// Contains the dashboard view, permissions and one unpersisted editor draft.
/// </summary>
public class DashboardEditorModel
{
    /// <summary>
    /// Gets or sets the selected layout state.
    /// </summary>
    public DashboardLayoutState State { get; set; }
    /// <summary>
    /// Gets or sets the prepared widget render model.
    /// </summary>
    public DashboardRenderModel Dashboard { get; set; }
    /// <summary>
    /// Gets or sets the widgets available to the current user.
    /// </summary>
    public IReadOnlyCollection<DashboardWidgetDescriptor> Catalog { get; set; } = [];
    /// <summary>
    /// Gets or sets whether a personal draft may be edited.
    /// </summary>
    public bool CanPersonalize { get; set; }
    /// <summary>
    /// Gets or sets whether the shared layout may be managed.
    /// </summary>
    public bool CanManageGlobal { get; set; }
    /// <summary>
    /// Gets or sets whether the editor is open.
    /// </summary>
    public bool IsEditing { get; set; }
    /// <summary>
    /// Gets or sets whether deletion of the selected customization is pending.
    /// </summary>
    public bool Reset { get; set; }
    /// <summary>
    /// Gets or sets whether the submitted draft contains unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Gets or sets the view whose placements were used to start this draft.
    /// </summary>
    public DashboardLayoutLayer? TemplateSource { get; set; }
}
