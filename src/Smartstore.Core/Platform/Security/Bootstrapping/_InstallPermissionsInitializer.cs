using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Engine;
using Smartstore.Engine.Initialization;

namespace Smartstore.Core.Bootstrapping
{
    public class InstallPermissionsInitializer : IApplicationInitializer
    {
        private readonly SmartDbContext _db;
        private readonly IPermissionService _permissionService;
        private readonly ITypeScanner _typeScanner;

        public InstallPermissionsInitializer(SmartDbContext db, IPermissionService permissionService, ITypeScanner typeScanner)
        {
            _db = db;
            _permissionService = permissionService;
            _typeScanner = typeScanner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int Order => 0;
        public bool ThrowOnError => true;

        public Task InitializeAsync()
        {
            // TODO: (mg) (core) Implement InstallPermissionsInitializer
            return Task.CompletedTask;
        }

        public void OnFail(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}
