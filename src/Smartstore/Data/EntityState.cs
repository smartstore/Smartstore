using System;

namespace Smartstore.Data
{
    public enum EntityState
    {
        Detached,
        Unchanged,
        Deleted,
        Modified,
        Added
    }
}