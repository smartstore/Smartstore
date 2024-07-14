﻿namespace Smartstore.Core.Content.Media
{
    public class TrackedMediaPropertyTable
    {
        private readonly IList<TrackedMediaProperty> _propertyList = new List<TrackedMediaProperty>();

        protected internal TrackedMediaPropertyTable(string album) => Album = album;

        public void Register<T>(Expression<Func<T, int>> foreignKeyProperty) where T : BaseEntity => 
            RegisterInternal(typeof(T), foreignKeyProperty.ExtractPropertyInfo().Name);

        public void Register<T>(Expression<Func<T, int?>> foreignKeyProperty) where T : BaseEntity => 
            RegisterInternal(typeof(T), foreignKeyProperty.ExtractPropertyInfo().Name);

        internal string Album { get; set; }

        private void RegisterInternal(Type entityType, string propertyName) => 
        _propertyList.Add(new TrackedMediaProperty
        {
            Name = propertyName,
            EntityType = entityType,
            Album = Album,
        });

        internal TrackedMediaProperty[] GetProperties() => _propertyList.ToArray();
    }

    public class TrackedMediaProperty
    {
        public string Name { get; set; }
        public Type EntityType { get; set; }
        public string Album { get; set; }
    }
}
