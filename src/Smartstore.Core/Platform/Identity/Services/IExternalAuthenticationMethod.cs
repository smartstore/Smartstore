using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Identity
{
    public partial interface IExternalAuthenticationMethod : IProvider, IUserEditable
    {
        /// <summary>
        /// Gets an invoker for displaying a widget.
        /// </summary>
        /// <param name="storeId">The id of the current store.</param>
        Widget GetDisplayWidget(int storeId);
    }
}
