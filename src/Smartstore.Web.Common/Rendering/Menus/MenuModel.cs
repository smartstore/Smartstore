using Smartstore.Collections;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering.Menus
{
    public class MenuModel
    {
        private TreeNode<MenuItem> _selectedNode;
        private bool _seekedSelectedNode;

        public string Name { get; set; }
        public string Template { get; set; }

        public TreeNode<MenuItem> Root { get; set; }
        public IList<TreeNode<MenuItem>> Path { get; set; }

        public TreeNode<MenuItem> SelectedNode
        {
            get
            {
                if (!_seekedSelectedNode)
                {
                    _selectedNode = Path?.LastOrDefault() ?? Root;
                    _seekedSelectedNode = true;
                }

                return _selectedNode ?? Root;
            }
            set
            {
                _selectedNode = value;
                Path = _selectedNode != null
                    ? _selectedNode.Trail.Where(x => !x.IsRoot).ToList()
                    : new List<TreeNode<MenuItem>>();

                _seekedSelectedNode = true;
            }
        }
    }
}
