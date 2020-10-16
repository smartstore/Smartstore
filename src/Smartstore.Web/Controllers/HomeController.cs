using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Data;
using Smartstore.Core.Events;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using Smartstore.Engine;
using Smartstore.Web.Models;
using Z.EntityFramework.Plus;
using Smartstore.Core.Configuration;
using Smartstore.Core.Common;
using Smartstore.Core.Stores;
using Microsoft.EntityFrameworkCore.Internal;
using Smartstore.Caching;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Core.Tax.Settings;
using Smartstore.Threading;
using System.Threading;

namespace Smartstore.Web.Controllers
{
    public class MyProgress
    {
        public int Percent { get; set; }
        public string Message { get; set; }
    }
    
    public class HomeController : Controller
    {
        private static CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        
        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger<HomeController> _logger;
        private readonly ICacheManager _cache;
        private readonly IAsyncState _asyncState;

        public HomeController(
            SmartDbContext db, 
            ILogger<HomeController> logger,
            ISettingService settingService,
            IEventPublisher eventPublisher,
            IDbContextFactory<SmartDbContext> dbContextFactory,
            IStoreContext storeContext,
            IEnumerable<IDbSaveHook> hooks,
            ICacheManager cache,
            IAsyncState asyncState,
            TaxSettings taxSettings)
        {
            _db = db;
            _eventPublisher = eventPublisher;
            _settingService = settingService;
            _storeContext = storeContext;
            _logger = logger;
            _cache = cache;
            _asyncState = asyncState;

            var currentStore = storeContext.CurrentStore;
        }


        public async Task<IActionResult> Countries()
        {
            #region Test

            var taxSettings = await _settingService.LoadSettingsAsync<TaxSettings>(_storeContext.CurrentStore.Id);

            //_cache.Put("a", new CacheEntry { Key = "a", Value = "a" });
            //_cache.Put("b", new CacheEntry { Key = "b", Value = "b", Dependencies = new[] { "a" } });
            //_cache.Put("c", new CacheEntry { Key = "c", Value = "c", Dependencies = new[] { "a", "b" } });
            //_cache.Put("d", new CacheEntry { Key = "d", Value = "d", Dependencies = new[] { "a", "b", "c" } });

            ////_cache.Remove("d");
            ////_cache.Remove("c");
            //_cache.Remove("b");
            ////_cache.Remove("a");

            #endregion

            _asyncState.Cancel<MyProgress>();
            //_cancelTokenSource.Cancel();
            _cancelTokenSource = new CancellationTokenSource();

            var query = _db.Countries
                .AsNoTracking()
                .ApplyLimitToStore(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var countries = await query.ToListAsync();

            _db.SaveChanges();

            return View(countries);
        }

        public async Task<IActionResult> Settings()
        {
            _asyncState.Remove<MyProgress>();
            
            var settings = await _db.Settings
                .AsNoTracking()
                .ApplySorting()
                .Take(500)
                .ToListAsync();

            #region Test

            var p = _db.DataProvider;

            //await p.BackupDatabaseAsync(@"D:\_Backup\db\yoman.bak");
            //await p.RestoreDatabaseAsync(@"D:\_Backup\db\yoman.bak");

            //var x = p.HasTable("Product");
            //var y = await p.HasTableAsync("xxxxxProduct");
            //var z = p.HasDatabase("yogisan-db");
            //z = await p.HasDatabaseAsync("FelgenOnline");
            //z = p.HasDatabase("yodeleeeeeee");
            //z = p.HasColumn("Discount", "Name");
            //z = await p.HasColumnAsync("Discount", "xxxxxName");

            //var ident = p.GetTableIdent<Store>();
            //ident = await p.GetTableIdentAsync<Country>();
            //ident = p.GetTableIdent<Setting>();

            //var size = p.GetDatabaseSize();
            //////await p.ShrinkDatabaseAsync();
            ////p.ReIndexTables();
            ////p.ShrinkDatabase();
            ////size = p.GetDatabaseSize();

            return View(settings);

            //var attrs = new GenericAttribute[] 
            //{
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" }
            //};
            //var maps = new StoreMapping[]
            //{
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 }
            //};

            //_db.GenericAttributes.AddRange(attrs);
            //_db.StoreMappings.AddRange(maps);

            //await _db.SaveChangesAsync();

            //_db.GenericAttributes.RemoveRange(attrs);
            //_db.StoreMappings.RemoveRange(maps);

            //_db.SaveChanges();

            #endregion
        }




        public IActionResult Index()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _asyncState.Create(new MyProgress(), cancelTokenSource: _cancelTokenSource);
            
            return View();
        }

        public IActionResult Privacy()
        {
            _asyncState.Update<MyProgress>(x => 
            {
                x.Percent++;
                x.Message = $"Fortschritt {x.Percent}";
            });
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
