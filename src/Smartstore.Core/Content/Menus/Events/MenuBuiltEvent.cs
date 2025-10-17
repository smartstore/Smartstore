using Smartstore.Collections;
using Smartstore.Events;

namespace Smartstore.Core.Content.Menus
{
    public class MenuBuiltEvent : IEventMessage
    {
        public MenuBuiltEvent(string name, TreeNode<MenuItem> root)
        {
            Name = name;
            Root = root;
        }

        public string Name { get; }
        public TreeNode<MenuItem> Root { get; }
    }
}
