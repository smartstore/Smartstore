using Smartstore.Diagnostics;
using StackExchange.Profiling;

namespace Smartstore.DevTools.Services
{
    public class MiniProfilerChronometer : IChronometer
    {
        private readonly Dictionary<string, Stack<IDisposable>> _steps = new();

        protected MiniProfiler Profiler => MiniProfiler.Current;

        public void StepStart(string key, string message)
        {
            if (Profiler == null)
            {
                return;
            }

            var stack = _steps.Get(key);
            if (stack == null)
            {
                _steps[key] = stack = new Stack<IDisposable>();
            }

            var step = Profiler.Step(message);
            stack.Push(step);
        }

        public void StepStop(string key)
        {
            if (_steps.ContainsKey(key) && _steps[key].Count > 0)
            {
                var step = _steps[key].Pop();
                step.Dispose();
                if (_steps[key].Count == 0)
                {
                    _steps.Remove(key);
                }
            }
        }

        private void StopAll()
        {
            // Dispose any orphaned steps
            foreach (var stack in _steps.Values)
            {
                stack.Each(x => x.Dispose());
            }

            _steps.Clear();
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}
