using System.Text;

namespace Smartstore.Web.Razor
{
    public class ViewNotFoundException : Exception
    {
        public ViewNotFoundException(string viewName, IEnumerable<string> searchedLocations)
            : base(GenerateMessage(viewName, searchedLocations))
        {
            Guard.NotEmpty(viewName, nameof(viewName));
            Guard.NotNull(searchedLocations, nameof(searchedLocations));

            ViewName = viewName;
            SearchedLocations = searchedLocations;
        }

        private static string GenerateMessage(string viewName, IEnumerable<string> searchedLocations)
        {
            var locations = new StringBuilder(400);
            locations.AppendLine();

            foreach (string location in searchedLocations)
            {
                locations.AppendLine(location);
            }

            return string.Format("The view '{0}' or its master was not found, searched locations: {1}", viewName, locations.ToString());
        }

        public string ViewName { get; private set; }
        public IEnumerable<string> SearchedLocations { get; private set; }
    }
}
