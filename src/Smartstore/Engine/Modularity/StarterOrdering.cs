using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Engine
{
    /// <summary>
    /// Some predefined defaults for the numerical ordering of starter implementations.
    /// </summary>
    public enum StarterOrdering
    {
        First = -1000,
        Early = -500,
        Default = 0,
        Late = 500,
        Last = 1000
    }
}
