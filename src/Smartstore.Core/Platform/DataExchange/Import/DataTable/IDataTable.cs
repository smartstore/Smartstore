namespace Smartstore.Core.DataExchange.Import
{
    public interface IDataColumn
    {
        string Name { get; }
        Type Type { get; }
    }

    public interface IDataRow
    {
        object[] Values { get; }
        object this[int index] { get; set; }
        object this[string name] { get; set; }

        IDataTable Table { get; }
    }

    public interface IDataTable
    {
        bool HasColumn(string name);
        int GetColumnIndex(string name);
        IList<IDataColumn> Columns { get; }
        IList<IDataRow> Rows { get; }
    }
}
