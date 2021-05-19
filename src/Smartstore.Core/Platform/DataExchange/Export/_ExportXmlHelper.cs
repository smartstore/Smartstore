using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Smartstore.Core.Common;

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

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Uses synchronous XmlWriter methods to avoid many atomic asynchronous write statements for each small text portion.
    /// https://stackoverflow.com/questions/16641074/xmlwriter-async-methods/37391267
    /// </remarks>
    public class ExportXmlHelper : Disposable
    {
        protected XmlWriter _writer;
        protected CultureInfo _culture;
        protected bool _doNotDispose;

        public ExportXmlHelper(XmlWriter writer, bool doNotDispose = false, CultureInfo culture = null)
        {
            _writer = writer;
            _doNotDispose = doNotDispose;
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public ExportXmlHelper(Stream stream, XmlWriterSettings settings = null, CultureInfo culture = null)
        {
            _writer = XmlWriter.Create(stream, settings ?? DefaultSettings);
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public static XmlWriterSettings DefaultSettings => new()
        {
            Encoding = Encoding.UTF8,
            CheckCharacters = false,
            Indent = false,
            NewLineHandling = NewLineHandling.None
        };

        public ExportXmlExclude Exclude { get; set; }

        public XmlWriter Writer => _writer;

        public void WriteLocalized(dynamic parentNode)
        {
            if (parentNode == null || parentNode._Localized == null)
                return;

            _writer.WriteStartElement("Localized");
            foreach (dynamic item in parentNode._Localized)
            {
                _writer.WriteStartElement((string)item.LocaleKey);
                _writer.WriteAttributeString("culture", (string)item.Culture);
                _writer.WriteString(((string)item.LocaleValue).RemoveInvalidXmlChars());
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }

        public void WriteGenericAttributes(dynamic parentNode)
        {
            if (parentNode == null || parentNode._GenericAttributes == null)
                return;

            _writer.WriteStartElement("GenericAttributes");
            foreach (dynamic genericAttribute in parentNode._GenericAttributes)
            {
                GenericAttribute entity = genericAttribute.Entity;

                _writer.WriteStartElement("GenericAttribute");
                _writer.WriteElementString("Id", entity.Id.ToString());
                _writer.WriteElementString("EntityId", entity.EntityId.ToString());
                _writer.WriteElementString("KeyGroup", entity.KeyGroup);
                _writer.WriteElementString("Key", entity.Key);
                _writer.WriteElementString("Value", (string)genericAttribute.Value);
                _writer.WriteElementString("StoreId", entity.StoreId.ToString());
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }



        protected override void OnDispose(bool disposing)
        {
            if (_writer != null && !_doNotDispose)
            {
                _writer.Dispose();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (_writer != null && !_doNotDispose)
            {
                await _writer.DisposeAsync();
            }
        }
    }
}
