﻿using Smartstore.Collections;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering.Menus
{
    public class MenuModel
    {
        private TreeNode<MenuItem> _selectedNode;
        private bool _seekedSelectedNode;

        /// <summary>
        /// System name used to get the <see cref="IMenu"/>.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Template view name.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Used to be displayed in frontend if needed.
        /// </summary>
        public string PublicName { get; set; }

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
                    : [];

                _seekedSelectedNode = true;
            }
        }
    }
}
