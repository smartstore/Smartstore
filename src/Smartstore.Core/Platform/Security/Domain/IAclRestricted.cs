namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents an entity with restricted access rights.
    /// </summary>
    public partial interface IAclRestricted
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity has restricted access rights.
        /// </summary>
        bool SubjectToAcl { get; set; }
    }
}
