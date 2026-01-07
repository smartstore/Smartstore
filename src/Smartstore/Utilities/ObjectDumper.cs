using System.Diagnostics;
using System.Text;
using Smartstore.Json;

namespace Smartstore.Core.Utilities;

public static class ObjectDumper
{
    public static void Dump(object value, TextWriter writer)
    {
        WriteIndented(value, writer);
        writer.WriteLine();
    }

    public static string Dump(object value)
        => SmartJsonOptions.Default.SerializeIndented(value);

    public static void ToConsole(object value)
        => Dump(value, Console.Out);

    public static void ToDebug(object value)
        => Debug.WriteLine(Dump(value));

    private static void WriteIndented(object value, TextWriter writer)
    {
        // Best case: we can write UTF-8 JSON directly to the underlying stream (no intermediate string).
        if (writer is StreamWriter sw)
        {
            // We only take the fast path when the encoding is UTF-8 to avoid mojibake.
            // (Many StreamWriters are UTF-8 by default nowadays, but not guaranteed.)
            var encoding = sw.Encoding;
            if (encoding.CodePage == Encoding.UTF8.CodePage)
            {
                SmartJsonOptions.Default.SerializeIndented(sw.BaseStream, value);
                sw.Flush();
                return;
            }
        }

        // Fallback: TextWriter without stream or non-UTF8 encoding -> allocate string.
        writer.Write(SmartJsonOptions.Default.SerializeIndented(value));
    }
}
