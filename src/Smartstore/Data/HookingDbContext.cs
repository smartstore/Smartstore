using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Data
{
    public abstract class HookingDbContext : DbContext
    {
        private DbSaveChangesOperation _currentSaveOperation;
        private DataProvider _dataProvider;

        public HookingDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DataProvider DataProvider
        {
            get
            {
                if (_dataProvider == null || _dataProvider.IsDisposed)
                {
                    var providerType = DataSettings.Instance.DataProviderClrType;
                    _dataProvider = (DataProvider)FastActivator.CreateInstance(providerType, new object[] { this.Database });
                }

                return _dataProvider;
            }
        }

        public override void Dispose()
        {
            ResetState();
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            ResetState();
            return base.DisposeAsync();
        }

        private void ResetState()
        {
            // Instance is returned to pool: reset state.
            HooksEnabled = true;
            _currentSaveOperation = null;

            if (_dataProvider != null)
            {
                _dataProvider.Dispose();
                _dataProvider = null;
            }
        }

        #region Save

        public bool HooksEnabled { get; set; } = true;

        [SuppressMessage("Performance", "CA1822:Member can be static", Justification = "Seriously?")]
        protected internal IDbHookHandler DbHookHandler
        {
            get => EngineContext.Current.Scope.ResolveOptional<IDbHookHandler>() ?? NullDbHookHandler.Instance;
        }

        protected internal bool IsInSaveOperation => _currentSaveOperation != null;

        /// <inheritdoc/>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var op = _currentSaveOperation;

            if (op != null)
            {
                if (op.Stage == DbSaveStage.PreSave)
                {
                    // This was called from within a PRE action hook. We must get out:... 
                    // 1.) to prevent cyclic calls
                    // 2.) we want new entities in the state tracker (added by pre hooks) to be committed atomically in the core SaveChanges() call later on.
                    return 0;
                }
                else if (op.Stage == DbSaveStage.PostSave)
                {
                    // This was called from within a POST action hook. Core SaveChanges() has already been called,
                    // but new entities could have been added to the state tracker by hooks.
                    // Therefore we need to commit them and get outta here, otherwise: cyclic nightmare!
                    // DetectChanges() here is important, 'cause we turned it off for the save process.
                    ChangeTracker.DetectChanges();
                    return SaveChangesCore(acceptAllChangesOnSuccess);
                }
            }

            _currentSaveOperation = new DbSaveChangesOperation(this, this.DbHookHandler);

            try
            {
                return _currentSaveOperation.Execute(acceptAllChangesOnSuccess);
            }
            finally
            {
                _currentSaveOperation?.Dispose();
                _currentSaveOperation = null;
            }
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var op = _currentSaveOperation;

            if (op != null)
            {
                if (op.Stage == DbSaveStage.PreSave)
                {
                    // This was called from within a PRE action hook. We must get out:... 
                    // 1.) to prevent cyclic calls
                    // 2.) we want new entities in the state tracker (added by pre hooks) to be committed atomically in the core SaveChanges() call later on.
                    return 0;
                }
                else if (op.Stage == DbSaveStage.PostSave)
                {
                    // This was called from within a POST action hook. Core SaveChanges() has already been called,
                    // but new entities could have been added to the state tracker by hooks.
                    // Therefore we need to commit them and get outta here, otherwise: cyclic nightmare!
                    // DetectChanges() here is important, 'cause we turned it off for the save process.
                    base.ChangeTracker.DetectChanges();
                    return await SaveChangesCoreAsync(acceptAllChangesOnSuccess, cancellationToken);
                }
            }

            _currentSaveOperation = new DbSaveChangesOperation(this, this.DbHookHandler);

            try
            {
                return await _currentSaveOperation.ExecuteAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            finally
            {
                _currentSaveOperation?.Dispose();
                _currentSaveOperation = null;
            }
        }

        /// <summary>
        /// Just calls <see cref="SaveChanges(bool)"/> without any sugar
        /// </summary>
        /// <returns>The number of affected records</returns>
        protected internal int SaveChangesCore(bool acceptAllChangesOnSuccess)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        /// <summary>
        /// Just calls <see cref="SaveChangesAsync(bool, CancellationToken)(bool)"/> without any sugar
        /// </summary>
        /// <returns>The number of affected records</returns>
        protected internal Task<int> SaveChangesCoreAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        #endregion

        #region Model bootstrapping

        protected void CreateModel(ModelBuilder modelBuilder, Assembly assembly)
        {
            Guard.NotNull(assembly, nameof(assembly));

            RegisterEntities(modelBuilder, assembly);
            RegisterEntityMappings(modelBuilder, assembly);
            RegisterConventions(modelBuilder);
        }

        private static void RegisterEntities(ModelBuilder modelBuilder, Assembly assembly)
        {
            var entityTypes = assembly.GetExportedTypes()
                .Where(x => typeof(BaseEntity).IsAssignableFrom(x) && !x.IsAbstract && x.HasDefaultConstructor())
                .ToList();

            foreach (var type in entityTypes)
            {
                // TODO: (core) Are we safe to add an entity model twice? ('cause EF did this already for publicly declared DbSet properties in SmartDbContext)
                modelBuilder.Entity(type);
            }
        }

        private static void RegisterEntityMappings(ModelBuilder modelBuilder, Assembly assembly)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        private static void RegisterConventions(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // TODO: (core) Add more proper conventions
                // TODO: (core) Make provider for conventions

                // SingularTableNameConvention
                entity.SetTableName(entity.DisplayName());

                // decimal HasPrecision(18, 4) convention
                var decimalProperties = entity.GetProperties().Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?));
                foreach (var property in decimalProperties)
                {
                    property.SetPrecision(18);
                    property.SetScale(4);
                }
            }
        }

        #endregion
    }
}
