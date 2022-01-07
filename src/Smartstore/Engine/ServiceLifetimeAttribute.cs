namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Sepcifies the lifetime of a service instance that was registered 
    /// per auto-discovery by the dependency injection provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceLifetimeAttribute : Attribute
    {
        public ServiceLifetimeAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        public ServiceLifetime Lifetime { get; }
    }
}
