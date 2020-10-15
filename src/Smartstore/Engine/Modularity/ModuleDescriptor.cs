using System;
using System.Collections.Generic;
using System.Text;

namespace Smartstore.Engine
{
    public class ModuleDescriptor
    {
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
    }
}