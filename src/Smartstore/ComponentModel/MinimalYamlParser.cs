
namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Just enough string parsing to recognize the useragent.yaml file format. Introduced to remove
    /// dependency on large Yaml parsing lib.
    /// </summary>
    /// <remarks>
    /// Forked from https://github.com/ua-parser/uap-csharp/
    /// </remarks>
    public class MinimalYamlParser
    {
        private readonly Dictionary<string, YamlMapping> _mappings = [];

        public MinimalYamlParser(string yamlString)
        {
            Guard.NotNull(yamlString);
            ReadIntoMappingModel(yamlString);
        }

        public IDictionary<string, YamlMapping> Mappings 
            => _mappings;

        public IEnumerable<Dictionary<string, string>> ReadMapping(string mappingName)
        {
            if (_mappings.TryGetValue(mappingName, out var mapping))
            {
                foreach (var s in mapping.Sequences)
                {
                    var temp = s;
                    yield return temp;
                }
            }
        }

        private void ReadIntoMappingModel(string yamlInputString)
        {
            // Line splitting using various splitting characters
            //string[] lines = yamlInputString.Split(new[] { Environment.NewLine, "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var lines = yamlInputString.ReadLines(removeEmptyLines: true);
            int lineCount = 0;
            YamlMapping activeMapping = null;

            foreach (var line in lines)
            {
                lineCount++;

                // Skipping comments
                if (line.Trim().StartsWith("#"))
                    continue;
                if (line.Trim().Length == 0)
                    continue;

                // Is this a new mapping entity
                if (line[0] != ' ')
                {
                    int indexOfMappingColon = line.IndexOf(':');
                    if (indexOfMappingColon == -1)
                        throw new ArgumentException("YamlParsing: Expecting mapping entry to contain a ':', at line " + lineCount);
                    var name = line[..indexOfMappingColon].Trim();
                    activeMapping = new YamlMapping();
                    _mappings.Add(name, activeMapping);
                    continue;
                }

                // Reading scalar entries into the active mapping
                if (activeMapping == null)
                    throw new ArgumentException("YamlParsing: Expecting mapping entry to contain a ':', at line " + lineCount);

                var seqLine = line.Trim();
                if (seqLine[0] == '-')
                {
                    activeMapping.BeginSequence();
                    seqLine = seqLine[1..];
                }

                int indexOfColon = seqLine.IndexOf(':');
                if (indexOfColon == -1)
                    throw new ArgumentException("YamlParsing: Expecting scalar mapping entry to contain a ':', at line " + lineCount);

                var key = seqLine[..indexOfColon].Trim();
                var value = ReadQuotedValue(seqLine[(indexOfColon + 1)..].Trim());
                activeMapping.AddToSequence(key, value);
            }
        }

        private static string ReadQuotedValue(string value)
        {
            if (value.StartsWith('\'') && value.EndsWith('\''))
                return value[1..^1];

            if (value.StartsWith('\"') && value.EndsWith('\"'))
                return value[1..^1];

            return value;
        }
    }

    public class YamlMapping
    {
        private Dictionary<string, string> _lastEntry;

        public List<Dictionary<string, string>> Sequences { get; } = new();

        internal void BeginSequence()
        {
            _lastEntry = new Dictionary<string, string>();
            Sequences.Add(_lastEntry);
        }

        internal void AddToSequence(string key, string value)
        {
            _lastEntry[key] = value;
        }
    }
}
