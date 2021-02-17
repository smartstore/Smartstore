using Smartstore.Collections;

namespace Smartstore.Core.Content.Menus
{
    public class MenuBuiltEvent
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
