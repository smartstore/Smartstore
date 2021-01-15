using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Data;

namespace Smartstore.Core.Content.Media
{
    public partial class SystemAlbumProvider
    {
        private readonly SmartDbContext _db;

        public SystemAlbumProvider(SmartDbContext db)
        {
            _db = db;
        }

        public const string Catalog = "catalog";
        public const string Content = "content";
        public const string Downloads = "download";
        public const string Messages = "message";
        public const string Customers = "customer";
        public const string Files = "file";
    }
}
