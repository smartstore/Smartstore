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
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{TKey, TValue}"/> and vice versa.
    /// </remarks>
    public abstract class AttributeSelection
    {
        private readonly Multimap<int, object> _map;
        private readonly string _xmlAttributeName;
        private readonly string _xmlAttributeValueName;
        private string _rawAttributes;
        private bool _dirty = true;
        private bool _isJson;

        /// <summary>
        /// Creates a new attribute selection from string as <see cref="Multimap{int, object}"/>. 
        /// Use <see cref="AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>        
        /// <param name="rawAttributes">XML or JSON attributes string.</param>
        /// <param name="xmlAttributeName">Attribute name for XML format.</param>
        /// <param name="xmlAttributeValueName">Optional attribute value name for XML format. If it is <c>null</c>, it becomes XmlAttributeName + "Value".</param>
        protected AttributeSelection(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName = null)
        {
            Guard.NotEmpty(xmlAttributeName, nameof(xmlAttributeName));
            Guard.NotEmpty(rawAttributes, nameof(rawAttributes));

            _rawAttributes = rawAttributes.Trim();
            _xmlAttributeName = xmlAttributeName;
            _xmlAttributeValueName = xmlAttributeValueName.HasValue() ? xmlAttributeValueName : xmlAttributeName + "Value";

            _map = FromXmlOrJson(_rawAttributes, _xmlAttributeName, _xmlAttributeValueName);
        }

        /// <summary>
        /// Creates and returns an instance of <see cref="Multimap{int, object}"/> from attribute string that is either in XML or JSON format.
        /// </summary>
        /// <remarks>
        /// Calls either <see cref="FromXml(string, string, string)"/> or <see cref="FromJson(string)"/> according to attributes string format.
        /// Throws an <see cref="ArgumentException"/> if no valid format was found.
        /// </remarks>
        /// <param name="rawAttributes">XML or JSON attributes string.</param>
        /// <param name="xmlAttributeName">Attribute name for XML format.</param>
        /// <param name="xmlAttributeValueName">Attribute value name for XML format.</param>
        private static Multimap<int, object> FromXmlOrJson(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName)
        {
            var firstChar = rawAttributes[0];
            if (firstChar == '<')
            {
                return FromXml(rawAttributes, xmlAttributeName, xmlAttributeValueName);
            }
            else if (firstChar == '{')
            {
                return FromJson(rawAttributes);
            }

            throw new ArgumentException("Raw attributes must either be in XML or JSON format.", nameof(rawAttributes));
        }

        private static Multimap<int, object> FromXml(string xmlAttributes, string attributeName, string attributeValueName)
        {
            try
            {
                var map = new Multimap<int, object>();
                var xElement = XElement.Parse(xmlAttributes);
                var attributeElements = xElement.Descendants(attributeName).ToList();

                foreach (var element in attributeElements)
                {
                    var values = element.Descendants(attributeValueName).Select(x => x.Value).ToList();
                    map.Add(element.Value.Convert<int>(), values);
                }

                return map;
            }
            catch (Exception ex)
            {
                throw new XmlException("Error while trying to parse from XML: " + xmlAttributes, ex);
            }
        }

        private static Multimap<int, object> FromJson(string jsonAttributes)
        {            
            try
            {
                return JsonConvert.DeserializeObject<Multimap<int, object>>(jsonAttributes);                
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("Error while trying to deserialize object from Json: " + jsonAttributes, ex);
            }
        }

        /// <summary>
        /// Creates and returns a string in JSON format.
        /// </summary>
        /// <remarks>
        /// Tries to serialize <see cref="Multimap{TKey, TValue}"/> and throws a <see cref="JsonSerializationException"/> if not possible.
        /// </remarks>
        public string AsJson()
        {
            if (_rawAttributes.HasValue() && _isJson && !_dirty)
                return _rawAttributes;

            try
            {
                var json = JsonConvert.SerializeObject(_map);
                _isJson = true;
                _dirty = false;
                _rawAttributes = json;

                return json;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("Error while trying to serialize Json string from: " + nameof(_map), ex);
            }
        }

        /// <summary>
        /// Creates and returns a string in XML format.
        /// </summary>
        /// <remarks>
        /// Tries to serialize <see cref="Multimap{TKey, TValue}"/> and throws a <see cref="JsonSerializationException"/> if not possible.
        /// </remarks>
        public string AsXml()
        {
            if (_rawAttributes.HasValue() && !_isJson && !_dirty)
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
            _dirty = false;
            _rawAttributes = root.ToString();
            return _rawAttributes;
        }

        /// <summary>
        /// Gets deserialized attributes.
        /// </summary>
        public IEnumerable<KeyValuePair<int, ICollection<object>>> AttributesMap 
            => _map;

        /// <summary>
        /// Gets deserialized attribute values by attribute id.
        /// </summary>
        /// <param name="attributeId">Attribute identifier</param>
        public IEnumerable<object> GetAttributeValues(int attributeId)
            => _map.ContainsKey(attributeId) ? _map[attributeId] : null;

        /// <summary>
        /// Adds an attribute with possible multiple values to <see cref="Multimap{TKey, TValue}"/>.
        /// </summary>
        /// <param name="attributeId">Attribute identifier</param>
        /// <param name="value">Attribute value</param>
        public void AddAttribute(int attributeId, params object[] values)
        {
            Guard.NotEmpty(values, nameof(values));

            _map.AddRange(attributeId, values);
            _dirty = true;
        }

        /// <summary>
        /// Adds an attribute value to <see cref="Multimap{TKey, TValue}"/> by attribute id.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void AddAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Add(attributeId, value);
            _dirty = true;
        }

        /// <summary>
        /// Removes an attribute set from <see cref="Multimap{TKey, TValue}"/> by attribute id.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        public void RemoveAttribute(int attributeId)
        {
            _map.RemoveAll(attributeId);
            _dirty = true;
        }

        /// <summary>
        /// Removes an attribute value from <see cref="Multimap{TKey, TValue}"/> by attribute id.
        /// </summary>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void RemoveAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Remove(attributeId, value);
            _dirty = true;
        }

        /// <summary>
        /// Removes all attribute sets from <see cref="Multimap{TKey, TValue}"/>.
        /// </summary>
        public void ClearAttributes()
        {
            _map.Clear();
            _dirty = true;
        }
    }
}