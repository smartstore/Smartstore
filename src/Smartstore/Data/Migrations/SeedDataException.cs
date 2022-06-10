namespace Smartstore.Data.Migrations
{
    public class SeedDataException : Exception
    {
        public SeedDataException(string stage, Exception inner)
            : base("An error occurred during installation. Stage: " + stage, inner)
        {
            Stage = stage;
        }

        public string Stage { get; set; }
    }
}