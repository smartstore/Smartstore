namespace Smartstore.Core.Content.Menus
{
    public interface IBreadcrumb
    {
        void Track(MenuItem item, bool prepend = false);
        IEnumerable<MenuItem> Trail { get; }
    }

    internal class DefaultBreadcrumb : IBreadcrumb
    {
        private List<MenuItem> _trail;

        public void Track(MenuItem item, bool prepend = false)
        {
            Guard.NotNull(item, nameof(item));

            if (_trail == null)
            {
                _trail = new List<MenuItem>();
            }

            if (prepend)
            {
                _trail.Insert(0, item);
            }
            else
            {
                _trail.Add(item);
            }
        }

        public IEnumerable<MenuItem> Trail => _trail;
    }
}
