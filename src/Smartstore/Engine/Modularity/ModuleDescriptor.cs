using System;
using System.Collections.Generic;
using System.Text;
using Smartstore.IO;

namespace Smartstore.Engine
{
    public class ModuleDescriptor
    {
        private string _resourceRootKey;

        public string SystemName { get; set; }

        /// <summary>
        /// Module installer runtime type.
        /// </summary>
        public Type ModuleClrType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is installed
        /// </summary>
        public bool Installed { get; set; }

        /// <summary>
        /// Gets a value indicating whether the module is incompatible with the current application version
        /// </summary>
        public bool Incompatible { get; set; }

        /// <summary>
        /// Gets the file provider that references the module's root directory.
        /// </summary>
        public IFileSystem FileProvider
        {
            get;
            protected internal set;
        }

        /// <summary>
        /// Gets or sets the root key of string resources.
        /// </summary>
        /// <remarks>
        /// Tries to get it from first entry of resource XML file if not specified.
        /// In that case the first resource name should not contain a dot if it's not part of the root key.
        /// Otherwise you get the wrong root key.
        /// </remarks>
        public string ResourceRootKey
        {
            // TODO: (core) Impl ModuleDescriptor.ResourceRootKey getter
            get => "Smartstore.PseudoModule";
            set => _resourceRootKey = value;
        }
    }
}