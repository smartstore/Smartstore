namespace Smartstore.Core.DataExchange.Import
{
    public static partial class IDataRowExtensions
    {
        public static bool TryGetValue(this IDataRow row, string name, out object value)
        {
            value = null;

            var index = row.Table.GetColumnIndex(name);
            if (index < 0)
                return false;

            value = row[index];
            return true;
        }

        public static bool TrySetValue(this IDataRow row, string name, object value)
        {
            var index = row.Table.GetColumnIndex(name);
            if (index < 0)
                return false;

            row[index] = value;
            return true;
        }
    }
}
