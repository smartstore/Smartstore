using System;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Smartstore.Web.Razor.RuntimeCompilation
{
    internal sealed class RuntimeViewCompilerProvider : IViewCompilerProvider
    {
        private readonly IViewCompilerProvider _inner;
        private readonly Func<IViewCompiler> _createCompiler;

        private object _initializeLock = new object();
        private bool _initialized;
        private IViewCompiler _compiler;

        public RuntimeViewCompilerProvider(IViewCompilerProvider inner)
        {
            _inner = inner;
            _createCompiler = CreateCompiler;
        }

        public IViewCompiler GetCompiler()
        {
            return LazyInitializer.EnsureInitialized(
                ref _compiler,
                ref _initialized,
                ref _initializeLock,
                _createCompiler);
        }

        private IViewCompiler CreateCompiler()
        {
            var createCompilerMethod = _inner.GetType().GetMethod("CreateCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var innerCompiler = (IViewCompiler)createCompilerMethod.Invoke(_inner, Array.Empty<object>());

            return new RuntimeViewCompiler(innerCompiler);
        }
    }
}
