using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Smartstore.Core.Platform.DataExchange.Export
{
    /// <summary>
    /// Allows to exclude XML nodes from export.
    /// </summary>
    [Flags]
    public enum ExportXmlExclude
    {
        None = 0,
        Category = 1
    }


    // TODO: (mg) (core) make all methods in ExportXmlHelper async.
    public class ExportXmlHelper : Disposable
    {
        protected XmlWriter _writer;
        protected CultureInfo _culture;
        protected bool _doNotDispose;

        public ExportXmlHelper(XmlWriter writer, bool doNotDispose = false, CultureInfo culture = null)
        {
            _writer = writer;
            _doNotDispose = doNotDispose;
            _culture = culture == null ? CultureInfo.InvariantCulture : culture;
        }

        public ExportXmlHelper(Stream stream, XmlWriterSettings settings = null, CultureInfo culture = null)
        {
            if (settings == null)
            {
                settings = DefaultSettings;
            }

            _writer = XmlWriter.Create(stream, settings);
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public static XmlWriterSettings DefaultSettings => new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CheckCharacters = false,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        };

        public ExportXmlExclude Exclude { get; set; }

        public XmlWriter Writer => _writer;


        protected override void OnDispose(bool disposing)
        {
            if (_writer != null && !_doNotDispose)
            {
                _writer.Dispose();
            }
        }
    }
}
