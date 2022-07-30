using Microsoft.AspNetCore.Http;

namespace Smartstore.Web.Models.DataGrid
{
    public interface IGridCommandStateStore
    {
        Task<GridCommand> LoadStateAsync(string gridId);
        Task SaveStateAsync(GridCommand command);
    }

    internal class GridCommandStateStore : IGridCommandStateStore
    {
        const string KeyPattern = "GridState-{0}";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public GridCommandStateStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<GridCommand> LoadStateAsync(string gridId)
        {
            if (_httpContextAccessor.HttpContext?.Session != null && gridId.HasValue())
            {
                var state = _httpContextAccessor.HttpContext.Session.GetObject<GridCommand>(BuildKey(gridId));
                return Task.FromResult(state);
            }

            return Task.FromResult<GridCommand>(null);
        }

        public Task SaveStateAsync(GridCommand command)
        {
            Guard.NotNull(command, nameof(command));

            if (_httpContextAccessor.HttpContext?.Session != null && command.GridId.HasValue())
            {
                _httpContextAccessor.HttpContext.Session.TrySetObject(BuildKey(command.GridId), command);
            }

            return Task.CompletedTask;
        }

        private static string BuildKey(string gridId)
        {
            return KeyPattern.FormatCurrent(gridId).ToLower();
        }
    }
}
