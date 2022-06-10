using System.Diagnostics;
using Newtonsoft.Json;

namespace Smartstore.Core.Utilities
{
    public static class ObjectDumper
    {
        public static void Dump(object value, TextWriter writer)
        {
            writer.WriteLine(Dump(value));
        }

        public static string Dump(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public static void ToConsole(object value)
        {
            Console.WriteLine(Dump(value));
        }

        public static void ToDebug(object value)
        {
            Debug.WriteLine(Dump(value));
        }
    }
}
