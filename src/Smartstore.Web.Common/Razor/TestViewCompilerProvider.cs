//using System.Diagnostics;
//using Microsoft.AspNetCore.Mvc.Razor.Compilation;

//namespace Smartstore.Web.Razor
//{
//    internal class TestViewCompilerProvider : IViewCompilerProvider
//    {
//        class TestViewCompiler : IViewCompiler
//        {
//            private readonly IViewCompiler _inner;

//            public TestViewCompiler(IViewCompiler inner) => _inner = inner;

//            public async Task<CompiledViewDescriptor> CompileAsync(string relativePath)
//            {
//                var watch = new Stopwatch();

//                try
//                {
//                    watch.Start();
//                    return await _inner.CompileAsync(relativePath);
//                }
//                finally
//                {
//                    watch.Stop();
//                    Debug.WriteLine($"COMPILE view {relativePath}: {watch.ElapsedMilliseconds}ms");
//                }
//            }
//        }

//        private readonly IViewCompilerProvider _inner;

//        public TestViewCompilerProvider(IViewCompilerProvider inner) =>_inner = inner;
//        public IViewCompiler GetCompiler() => new TestViewCompiler(_inner.GetCompiler());
//    }
//}
