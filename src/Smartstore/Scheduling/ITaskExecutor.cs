using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Scheduling
{
    public interface ITaskExecutor
    {
        Task ExecuteAsync(
            ITaskDescriptor task,
            IDictionary<string, string> taskParameters = null,
            bool throwOnError = false);
    }
}
