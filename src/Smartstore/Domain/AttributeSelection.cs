using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Smartstore.Collections;

namespace Smartstore.Domain
{
    /// <summary>
    /// Represents an attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse from xml and json to <see cref="_map"/> and vice versa.
    /// </remarks>
    public abstract class AttributeSelection
    {
        private readonly Multimap<int, object> _map = new();
        private readonly string _xmlAttributeName;
        private readonly string _xmlAttributeValueName;
        private string _rawAttributes;
        private bool _hasChanged = true;
        private bool _isJson;

        /// <summary>
        /// Creates a new instance. Populates <see cref="_map"/> from Xml or Json string.        
        /// </summary>
        /// <remarks>
        /// Calls <see cref="FromXmlOrJson(string, string, string)"/>.
        /// Use <see cref="AttributesMap"/> to access parsed attributes afterwards.
        /// </remarks>        
        /// <param name="rawAttributes">Xml or Json attributes string.</param>
        /// <param name="xmlAttributeName">Attribute name for Xml format.</param>
        /// <param name="xmlAttributeValueName">Optional attribute value name for Xml format. If it is null, it becomes XmlAttributeName + "Value".</param>
        protected AttributeSelection(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName = null)
        {
            Guard.NotEmpty(xmlAttributeName, nameof(xmlAttributeName));
            Guard.NotEmpty(rawAttributes, nameof(rawAttributes));

            _rawAttributes = rawAttributes;
            _xmlAttributeName = xmlAttributeName;
            _xmlAttributeValueName = xmlAttributeValueName.HasValue() ? xmlAttributeValueName : xmlAttributeName + "Value";

            _map = FromXmlOrJson(_rawAttributes, _xmlAttributeName, _xmlAttributeValueName);
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="Multimap{int, object}"/> from attribute string that is either in XML or Json format.
        /// </summary>
        /// <remarks>
        /// Calls either <see cref="FromXml(string, string, string)"/> or <see cref="FromJson(string)"/> according to attributes string format.
        /// Throws a <see cref="FormatException"/> when no valid format was found.
        /// </remarks>
        /// <param name="rawAttributes">Xml or Json attributes string.</param>
        /// <param name="xmlAttributeName">Attribute name for Xml format.</param>
        /// <param name="xmlAttributeValueName">Optional attribute value name for Xml format. If this is null, it becomes XmlAttributeName + "Value"</param>        
        private static Multimap<int, object> FromXmlOrJson(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName)
        {
            var firstChar = rawAttributes.TrimSafe()[0];
            if (firstChar == '<')
            {
                return FromXml(rawAttributes, xmlAttributeName, xmlAttributeValueName);
            }
            else if (firstChar == '{')
            {
                return FromJson(rawAttributes);
            }

            throw new FormatException("No valid format found for 'rawAttributes': " + rawAttributes);
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="Multimap{int, object}"/> from Xml attribute string.
        /// </summary>
        /// <remarks>
        /// Tries to parse and throws an <see cref="XmlException"/> when not possible.
        /// </remarks>
        /// <param name="xmlAttributes">Xml attributes string</param>
        /// <param name="attributeName">Xml attribute name</param>
        /// <param name="attributeValueName">Xml attribute value name</param>
        public static Multimap<int, object> FromXml(string xmlAttributes, string attributeName, string attributeValueName)
        {
            Guard.NotEmpty(xmlAttributes, nameof(xmlAttributes));
            Guard.NotEmpty(attributeName, nameof(attributeName));
            Guard.NotEmpty(attributeValueName, nameof(attributeValueName));

            var map = new Multimap<int, object>();
            try
            {
                var xElement = XElement.Parse(xmlAttributes);
                var attributeElements = xElement.Descendants(attributeName).ToList();
                foreach (var element in attributeElements)
                {
                    var values = element.Descendants(attributeValueName).Select(x => x.Value).ToList();
                    map.Add(element.Value.Convert<int>(), values);
                }
            }
            catch (Exception ex)
            {
                throw new XmlException("Error while trying to parse from XML: " + xmlAttributes, ex);
            }

            return map;
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="Multimap{int, object}"/> populated with attribute ids and attribute value objects from Json attribute string.
        /// </summary>
        /// <remarks>
        /// Tries to deserialize object and throws a <see cref="JsonSerializationException"/> when not possible.
        /// </remarks>
        /// <param name="JsonAttributes">Json attributes string</param>
        public static Multimap<int, object> FromJson(string JsonAttributes)
        {
            Guard.NotEmpty(JsonAttributes, nameof(JsonAttributes));

            Multimap<int, object> map;
            try
            {
                map = JsonConvert.DeserializeObject<Multimap<int, object>>(JsonAttributes);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("Error while trying to deserialize object from Json: " + JsonAttributes, ex);
            }

            return map;
        }

        /// <summary>
        /// Creates and returns a Json string of <see cref="_map"/>.
        /// </summary>
        /// <remarks>
        /// Tries to serialize object and throws a <see cref="JsonSerializationException"/> when not possible.
        /// </remarks>
        public string AsJson()
        {
            if (_rawAttributes.HasValue() && _isJson && !_hasChanged)
                return _rawAttributes;

            string json;
            try
            {
                json = JsonConvert.SerializeObject(_map);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("Error while trying to serialize Json string from: " + nameof(_map), ex);
            }

            _isJson = true;
            _hasChanged = false;
            _rawAttributes = json;
            return _rawAttributes;
        }

        /// <summary>
        /// Creates and returns a Xml string of <see cref="_map"/>.
        /// </summary>
        /// <remarks>
        /// Tries to serialize and throws a <see cref="JsonSerializationException"/> when not possible.
        /// </remarks>
        public string AsXml()
        {
            if (_rawAttributes.HasValue() && !_isJson && !_hasChanged)
                return _rawAttributes;

            var root = new XElement("Attributes");
            foreach (var attribute in _map)
            {
                var attributeElement = new XElement(_xmlAttributeName, new XAttribute("ID", attribute.Key));

                foreach (var attributeValue in attribute.Value.Distinct())
                {
                    attributeElement.Add(
                        new XElement(
                            _xmlAttributeValueName,
                            new XElement("Value", attributeValue))
                        );
                }

                root.Add(attributeElement);
            }

            _isJson = false;
            _hasChanged = false;
            _rawAttributes = root.ToString();
            return _rawAttributes;
        }

        /// <summary>
        /// Gets <see cref="_attributesMap"/>
        /// </summary>
        public IEnumerable<KeyValuePair<int, ICollection<object>>> AttributesMap
            => _map;

        /// <summary>
        /// Gets values of attribute with id from <see cref="_attributesMap"/>
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        public IEnumerable<object> GetAttributeValues(int attributeId)
            => _map.ContainsKey(attributeId) ? _map[attributeId] : null;
        
        /// <summary>
        /// Adds an attribute with possible multiple values to <see cref="_map"/>
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void AddAttribute(int attributeId, params object[] values)
        {
            Guard.NotEmpty(values, nameof(values));

            _map.AddRange(attributeId, values);
            _hasChanged = true;
        }

        /// <summary>
        /// Adds an attribute value to attribute with id <see cref="_map"/>.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void AddAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Add(attributeId, value);
            _hasChanged = true;
        }

        /// <summary>
        /// Removes attribute set from <see cref="_map"/>.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        public void RemoveAttribute(int attributeId)
        {
            _map.RemoveAll(attributeId);
            _hasChanged = true;
        }

        /// <summary>
        /// Removes attribute value from <see cref="_map"/>.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void RemoveAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Remove(attributeId, value);
            _hasChanged = true;
        }

        /// <summary>
        /// Removes all attribute sets from <see cref="_map"/>.
        /// </summary>
        public void ClearAttributes()
        {
            _map.Clear();
            _hasChanged = true;
        }
    }
}