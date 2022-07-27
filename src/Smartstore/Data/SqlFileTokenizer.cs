using System.Reflection;
using Smartstore.Utilities;

namespace Smartstore.Data
{
    /// <summary>
    /// Tokenizes an sql file. If <c>fileName</c> is an absolute physical path, then it will be read from disk.
    /// Otherwise the file will be obtained from the passed assembly or the current executing assembly.
    /// </summary>
    public class SqlFileTokenizer
    {
        public SqlFileTokenizer(string fileName, Assembly assembly = null, string location = null)
        {
            Guard.NotEmpty(fileName, nameof(fileName));

            FileName = fileName;
            Assembly = assembly;
            Location = location;
        }

        public string FileName { get; }
        public Assembly Assembly { get; private set; }
        public string Location { get; }

        public IEnumerable<string> Tokenize()
        {
            if (Assembly == null)
            {
                Assembly = Assembly.GetExecutingAssembly();
            }

            using (var reader = ReadSqlFile())
            {
                string statement;
                while ((statement = ReadNextSqlStatement(reader)) != null)
                {
                    yield return statement.EmptyNull();
                }
            }
        }

        protected virtual StreamReader ReadSqlFile()
        {
            var fileName = this.FileName;

            if (Path.IsPathFullyQualified(fileName))
            {
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException("Sql file '{0}' not found".FormatInvariant(this.FileName));
                }

                return new StreamReader(File.OpenRead(fileName));
            }

            // SQL file is obviously an embedded resource
            var assembly = this.Assembly;
            var asmName = assembly.FullName.Substring(0, assembly.FullName.IndexOf(','));
            var location = this.Location ?? asmName + ".Sql";
            var name = String.Format("{0}.{1}", location, fileName);

            try
            {
                var stream = assembly.GetManifestResourceStream(name);
                return new StreamReader(stream);
            }
            catch (Exception ex)
            {
                throw new FileLoadException("Error while loading embedded sql resource '{0}'".FormatInvariant(name), ex);
            }
        }

        private static string ReadNextSqlStatement(TextReader reader)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            string lineOfText;

            while (true)
            {
                lineOfText = reader.ReadLine();
                if (lineOfText == null)
                {
                    if (sb.Length > 0)
                        return sb.ToString();
                    else
                        return null;
                }

                if (lineOfText.TrimEnd().ToUpper() == "GO")
                    break;

                sb.Append(lineOfText + Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}