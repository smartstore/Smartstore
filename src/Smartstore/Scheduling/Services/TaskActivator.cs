using System.Collections.Concurrent;
using Autofac;

namespace Smartstore.Scheduling
{
    public class TaskActivator : ITaskActivator
    {
        private readonly static Dictionary<string, string> _legacyTypeNamesMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "FileImportTask", "BMEcatImportTask" }
        };

        private readonly static ConcurrentDictionary<string, string> _normalizedTypeNames = new(StringComparer.OrdinalIgnoreCase);

        private readonly IComponentContext _componentContext;

        public TaskActivator(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }

        public virtual string GetNormalizedTypeName(TaskDescriptor task)
        {
            Guard.NotNull(task, nameof(task));

            if (task.Type.IsEmpty())
            {
                return null;
            }

            return _normalizedTypeNames.GetOrAdd(task.Type, name =>
            {
                if (name.IndexOf(',') > -1)
                {
                    // Type name is legacy and fully qualified, e.g.: "SmartStore.Services.Customers.DeleteGuestsTask, SmartStore.Services".
                    // We need to extract "DeleteGuestsTask".
                    name = name
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim()
                        .Split('.')
                        .Last();

                    if (_legacyTypeNamesMap.TryGetValue(name, out var mappedName))
                    {
                        // E.g.: map FileImportTask --> BMEcatImportTask
                        name = mappedName;
                    }
                }

                return name;
            });
        }

        public virtual Type GetTaskClrType(string normalizedTypeName, bool throwOnError = false)
        {
            Guard.NotEmpty(normalizedTypeName, nameof(normalizedTypeName));

            var lazyTask = _componentContext.ResolveOptionalNamed<Lazy<ITask, TaskMetadata>>(normalizedTypeName);
            if (throwOnError && lazyTask == null)
            {
                throw new TaskActivationException($"No task registered for '{normalizedTypeName}'.");
            }

            return lazyTask?.Metadata?.Type;
        }

        public virtual ITask Activate(string normalizedTypeName)
        {
            Guard.NotEmpty(normalizedTypeName, nameof(normalizedTypeName));

            try
            {
                return _componentContext.ResolveNamed<Lazy<ITask, TaskMetadata>>(normalizedTypeName).Value;
            }
            catch (Exception ex)
            {
                throw new TaskActivationException($"Error while activating task '{normalizedTypeName}'.", ex);
            }
        }
    }
}