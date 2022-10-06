using System.Runtime.Serialization;

namespace Smartstore.Core.Content.Blocks
{
    /// <summary>
    /// When implemented on <see cref="IBlock"/> types, makes the block bindable to
    /// product, category and manufacturer entities. The UI will display a 'Data binding'
    /// section which allows selection of an entity.
    /// </summary>
    /// <remarks>
    /// The handler for a bindable block type MUST implement <see cref="IBindableBlockHandler"/>.
    /// </remarks>
    public interface IBindableBlock : IBlock
    {
        /// <summary>
        /// The name of the bound entity, e.g. 'product'.
        /// </summary>
        [IgnoreDataMember]
        string BindEntityName { get; set; }

        /// <summary>
        /// The id of the bound entity.
        /// </summary>
        [IgnoreDataMember]
        int? BindEntityId { get; set; }

        /// <summary>
        /// Returns a value to indicate whether the block can be bound. A block can be bound
		/// if both <see cref="BindEntityName"/> and <see cref="BindEntityId"/> have values.
		/// However, this property does not indicate whether the bound entity actually exists.
        /// </summary>
        bool CanBind { get; }

        /// <summary>
        /// Returns a value to indicate whether the binding source (<see cref="DataItem"/>) has been loaded from the store already.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// The data item of the bound entity.
        /// </summary>
        IDictionary<string, object> DataItem { get; set; }

        /// <summary>
        /// Returns a value to indicate whether the block has been bound already.
        /// A block is considered bound if the binding source dictionary has been
        /// applied to the block instance's bindable properties.
        /// </summary>
        bool IsBound { get; set; }

        /// <summary>
        /// Resets the data item of the bound entity. After calling this method,
		/// <see cref="DataItem"/> is <c>null</c>, <see cref="IsLoaded"/> and <see cref="IsBound"/> are <c>false</c>.
        /// </summary>
        void Reset();
    }

    public abstract class BindableBlockBase : IBindableBlock
    {
        public virtual string BindEntityName { get; set; }
        public virtual int? BindEntityId { get; set; }

        [IgnoreDataMember]
        public bool CanBind => BindEntityName.HasValue() && BindEntityId.HasValue;

        [IgnoreDataMember]
        public bool IsLoaded => DataItem != null;

        [IgnoreDataMember]
        public IDictionary<string, object> DataItem { get; set; }

        [IgnoreDataMember]
        public bool IsBound { get; set; }

        public void Reset()
        {
            DataItem = null;
            IsBound = false;
        }
    }
}
