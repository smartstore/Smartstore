using System;
using System.IO;
using System.Text;
using System.Xml;
using Humanizer;

namespace Smartstore.Utilities
{
    public static class Prettifier
    {
	    public static string BytesToString(long bytes)
        {
            double dsize = bytes;

            if (bytes < 1024)
            {
                return bytes.Bytes().ToString();

            }
            else if (bytes < Math.Pow(1024, 2))
            {
                return (dsize / 1024).Kilobytes().ToString();
            }
            else if (bytes < Math.Pow(1024, 3))
            {
                return (dsize / Math.Pow(1024, 2)).Megabytes().ToString();
            }
            else
            {
                return (dsize / Math.Pow(1024, 3)).Gigabytes().ToString();
            }
        }

        public static string PrettifyXML(string xml)
        {
            if (xml.IsEmpty() || xml.IsWhiteSpace())
                return xml;
            
            // first read the xml ignoring whitespace
            using (var xmlReader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true, CheckCharacters = false }))
            {
                // then write it out with indentation
                var sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "\t", CheckCharacters = false }))
                {
                    writer.WriteNode(xmlReader, true);
                }

                var result = sb.ToString();
                return result;
            }
        }
    }
}