namespace Smartstore.Data.Migrations
{
    /// <summary>
    /// A list of table names to check during database initialization. If all given table names
    /// exist in the database, the initial migration will be applied to the database,
    /// but no longer executed, because the existence of these tables indicate that a database generation
    /// script was executed somehow in the past.
    /// Use this attribute to annotate concrete <see cref="DbContext"/> types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CheckTablesAttribute : Attribute
    {
        public CheckTablesAttribute(params string[] tableNames)
        {
            TableNames = tableNames;
        }

        public string[] TableNames { get; }
    }
}
