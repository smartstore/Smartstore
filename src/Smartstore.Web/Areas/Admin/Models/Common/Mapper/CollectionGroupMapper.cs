using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Common
{
    public static partial class CollectionGroupMappingExtensions
    {
        public static async Task<IList<CollectionGroupModel>> MapAsync(this IEnumerable<CollectionGroup> entities, SmartDbContext db)
        {
            Guard.NotNull(entities);

            var mapper = MapperFactory.GetMapper<CollectionGroup, CollectionGroupModel>();
            var ids = entities.Select(x => x.Id).ToArray();

            dynamic parameters = new ExpandoObject();
            parameters.NumberOfAssignments = await db.CollectionGroups
                .Where(x => ids.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    NumberOfAssignments = x.CollectionGroupMappings.Count
                })
                .ToDictionaryAsync(x => x.Id, x => x.NumberOfAssignments);

            var models = await entities
                .SelectAwait(async x =>
                {
                    var model = new CollectionGroupModel();
                    await mapper.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return models;
        }
    }

    public class CollectionGroupMapper(ILocalizationService localizationService) :
        IMapper<CollectionGroupModel, CollectionGroup>,
        IMapper<CollectionGroup, CollectionGroupModel>
    {
        private readonly ILocalizationService _localizationService = localizationService;

        public Task MapAsync(CollectionGroup from, CollectionGroupModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            to.LocalizedEntityName = _localizationService.GetResource("Common.Entity." + from.EntityName, 0, false, string.Empty, true);

            if (parameters?.NumberOfAssignments is Dictionary<int, int> numberOfAssignments)
            {
                to.NumberOfAssignments = numberOfAssignments?.Get(from.Id) ?? 0;
            }
            else
            {
                to.NumberOfAssignments = from.CollectionGroupMappings.Count;
            }

            return Task.CompletedTask;
        }

        public Task MapAsync(CollectionGroupModel from, CollectionGroup to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            return Task.CompletedTask;
        }
    }
}
