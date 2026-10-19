namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Thrown when seed data files are malformed or inconsistent, e.g. unknown overlay keys,
/// duplicate keys, unresolvable references or unknown fields.
/// </summary>
public class SeedDataFormatException : Exception
{
    public SeedDataFormatException(string message)
        : base(message)
    {
    }

    public SeedDataFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
