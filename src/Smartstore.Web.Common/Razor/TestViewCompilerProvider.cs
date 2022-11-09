using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Smartstore.Diagnostics;

namespace Smartstore.Web.Razor
{
    internal class TestViewCompilerProvider : IViewCompilerProvider
    {
        class TestViewCompiler : IViewCompiler
        {
            private readonly IViewCompiler _inner;

            public TestViewCompiler(IViewCompiler inner) => _inner = inner;

            public async Task<CompiledViewDescriptor> CompileAsync(string relativePath)
            {
                using (new AutoStopwatch($"COMPILE view {relativePath}"))
                {
                    return await _inner.CompileAsync(relativePath);
                }
            }
        }

        private readonly IViewCompilerProvider _inner;
        public TestViewCompilerProvider(IViewCompilerProvider inner) =>_inner = inner;
        public IViewCompiler GetCompiler() => new TestViewCompiler(_inner.GetCompiler());
    }
}
