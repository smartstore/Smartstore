using System.Reflection;
using Autofac;
using Autofac.Core.Resolving.Pipeline;

namespace Smartstore.Core.Bootstrapping
{
    public class AutofacSerilogMiddleware : IResolveMiddleware
    {
        private readonly Type _limitType;
        private readonly bool _changeParameters;
        private readonly bool _autowireProperties;

        public AutofacSerilogMiddleware(Type limitType, bool changeParameters, bool autowireProperties)
        {
            _limitType = limitType;
            _changeParameters = changeParameters;
            _autowireProperties = autowireProperties;
        }

        public PipelinePhase Phase => PipelinePhase.ParameterSelection;

        public void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
        {
            if (_changeParameters)
            {
                // Add our parameters.
                var logger = GetLoggerFor(_limitType, context);
                context.ChangeParameters(new[] { TypedParameter.From(logger) }.Concat(context.Parameters));
            }

            // Continue the resolve.
            next(context);

            if (_autowireProperties && context.NewInstanceActivated)
            {
                var logger = GetLoggerFor(context.Instance.GetType(), context);
                if (context.Registration.Metadata.Get("LoggerProperties") is PropertyInfo[] loggerProps)
                {
                    foreach (var prop in loggerProps)
                    {
                        prop.SetValue(context.Instance, logger);
                    }
                }
            }
        }

        private static ILogger GetLoggerFor(Type componentType, IComponentContext ctx)
        {
            return ctx.Resolve<ILogger>(new TypedParameter(typeof(Type), componentType));
        }
    }
}