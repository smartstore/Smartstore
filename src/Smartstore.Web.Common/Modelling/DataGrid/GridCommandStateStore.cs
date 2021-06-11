using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Modelling.DataGrid
{
    public interface IGridCommandStateStore
    {
        Task<GridCommand> LoadStateAsync(string gridId);
        Task SaveStateAsync(GridCommand command);
    }

    internal class GridCommandStateStore : IGridCommandStateStore
    {
        const string KeyPattern = "GridState.{0}__{1}";
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISession _session;

        public GridCommandStateStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public Task<GridCommand> LoadStateAsync(string gridId)
        {
            if (_session != null && gridId.HasValue())
            {
                var state = _session.GetObject<GridCommand>(BuildKey(gridId));
                return Task.FromResult(state);
            }
            
            return Task.FromResult<GridCommand>(null);
        }

        public Task SaveStateAsync(GridCommand command)
        {
            Guard.NotNull(command, nameof(command));
            
            if (_session != null && command.GridId.HasValue())
            {
                _session.TrySetObject(BuildKey(command.GridId), command);
            }

            return Task.CompletedTask;
        }

        private string BuildKey(string gridId)
        {
            var routeIdent = _httpContextAccessor.HttpContext.GetRouteData()?.Values?.GenerateRouteIdentifier();
            return KeyPattern.FormatInvariant(gridId, routeIdent.EmptyNull());
        }
    }
}
