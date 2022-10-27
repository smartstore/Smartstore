namespace Smartstore.Core.Seo
{
    public class NamedEntity : BaseEntity, ISlugSupported
    {
        public string EntityName { get; set; }
        public string DisplayName { get; set; }
        public string Slug { get; set; }
        public DateTime LastMod { get; set; }
        public int? LanguageId { get; set; }

        public string GetDisplayName()
            => DisplayName;

        public string[] GetDisplayNameMemberNames()
            => new[] { nameof(DisplayName) };

        public override string GetEntityName()
            => EntityName;

        public static string GetEntityName<T>() 
            where T : INamedEntity, new()
        {
            return new T().GetEntityName();
        }

        public static string GetEntityName(Type entityType)
        {
            Guard.NotNull(entityType, nameof(entityType));

            if (entityType.HasDefaultConstructor() && typeof(INamedEntity).IsAssignableFrom(entityType))
            {
                return (Activator.CreateInstance(entityType) as INamedEntity).GetEntityName();
            }

            return entityType.Name;
        }
    }
}
