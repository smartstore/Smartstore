using System.Runtime.CompilerServices;

namespace Smartstore.Engine
{
    public static class EngineFactory
    {
        /// <summary>
        /// Creates a static instance of the Smartstore engine.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static IEngine Create(SmartConfiguration configuration)
        {
            if (EngineContext.Current == null)
            {
                EngineContext.Replace(CreateEngineInstance(configuration));
            }

            return EngineContext.Current;
        }

        private static IEngine CreateEngineInstance(SmartConfiguration configuration)
        {
            var engineTypeSetting = configuration.EngineType;
            if (engineTypeSetting.HasValue())
            {
                var engineType = Type.GetType(engineTypeSetting);

                if (engineType == null)
                {
                    throw new ApplicationException($"The type '${engineType}' could not be found. Please check the configuration at 'appSettings.Smart.EngineType' or check for missing assemblies.");
                }

                if (!typeof(IEngine).IsAssignableFrom(engineType))
                {
                    throw new ApplicationException($"The type '${engineType}' does not implement '${typeof(IEngine).FullName}' and cannot be configured in 'appSettings.Smart.EngineType' for that purpose.");
                }

                return Activator.CreateInstance(engineType) as IEngine;
            }

            return new SmartEngine();
        }
    }
}