using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Smartstore.Domain
{
    public interface ISoftDeletable
    {
        bool Deleted { get; set; }

        /// <summary>
        /// An unmapped property indicating whether to delete a soft-deletable entity physically anyway.
        /// If <c>true</c> the entity will be deleted physically when it is removed from the context.
        /// If <c>false</c> (default) the deletion is suppressed and the <see cref="Deleted"/> property is set to <c>true</c> instead.
        /// </summary>
        [NotMapped, IgnoreDataMember]
        bool ForceDeletion
        {
            get { return false; }
        }
    }
}