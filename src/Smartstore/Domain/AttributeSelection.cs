using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.Utilities;

namespace Smartstore.Domain
{
    /// <summary>
    /// Represents an attribute selection.
    /// </summary>
    /// <remarks>
    /// This class can parse strings with XML or JSON format to <see cref="Multimap{int, object}"/> and vice versa.
    /// </remarks>
    public abstract class AttributeSelection : IEquatable<AttributeSelection>
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

            _rawAttributes = rawAttributes.TrimSafe();
            _xmlAttributeName = xmlAttributeName;
            _xmlAttributeValueName = xmlAttributeValueName.HasValue() ? xmlAttributeValueName : xmlAttributeName + "Value";

            _map = _rawAttributes.HasValue()
                ? FromXmlOrJson(_rawAttributes, _xmlAttributeName, _xmlAttributeValueName)
                : new Multimap<int, object>();
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
        private Multimap<int, object> FromXmlOrJson(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName)
        {
            var firstChar = rawAttributes[0];
            if (firstChar == '<')
            {
                return FromXml(rawAttributes, xmlAttributeName, xmlAttributeValueName);
            }
            else if (firstChar is '{' or '[')
            {
                return FromJson(rawAttributes);
            }

            throw new ArgumentException("Raw attributes must either be in XML or JSON format.", nameof(rawAttributes));
        }

        private Multimap<int, object> FromXml(string xmlAttributes, string attributeName, string attributeValueName)
        {
            try
            {
                var map = new Multimap<int, object>();
                var xElement = XElement.Parse(xmlAttributes);
                foreach (var element in xElement.Elements())
                {
                    if (element.Name.LocalName == attributeName)
                    {
                        var id = element.Attribute("ID")?.Value?.Convert<int>() ?? 0;

                        var values = element
                            .Descendants(attributeValueName)
                            .Select(x => x.Value);

                        map.AddRange(id, values);
                    }
                    else
                    {
                        MapUnknownElement(element, map);
                    }
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
        /// Tries to parse and map custom/unknown XML element.
        /// Gets called if XML element name is unknown.
        /// </summary>
        /// <param name="element">Current element to parse</param>
        /// <param name="map">The traget attributes<see cref="Multimap{int, object}"/>.</param>
        protected virtual void MapUnknownElement(XElement element, Multimap<int, object> map) { }

        /// <summary>
        /// Tries to parse additional XML.
        /// </summary>
        /// <param name="root">Root element</param>
        /// <param name="pair">Attribute <see cref="KeyValuePair{int, object}"/></param>
        /// <returns><c>True</c> if additional XML was found and parsed; <c>False</c> otherwise</returns>
        protected virtual void OnSerialize(XElement root) { }

        /// <summary>
        /// Creates and returns the raw attributes string in JSON format.
        /// </summary>
        /// <remarks>
        /// Tries to serialize <see cref="Multimap{int, object}"/> and throws a <see cref="JsonSerializationException"/> if not possible.
        /// </remarks>
        // TODO: (ms) (core) Have MapUnkownElement applied with AsJson(), too
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
        /// Creates and returns the raw attributes string in XML format.
        /// </summary>
        /// <remarks>
        /// Tries to serialize <see cref="Multimap{int, object}"/> and throws a <see cref="JsonSerializationException"/> if not possible.
        /// </remarks>
        public string AsXml()
        {
            if (_rawAttributes.HasValue() && !_isJson && !_dirty)
                return _rawAttributes;

            var root = new XElement("Attributes");
            foreach (var attribute in _map)
            {
                if (attribute.Key <= 0) 
                    continue;

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

            OnSerialize(root);

            _isJson = false;
            _dirty = false;
            _rawAttributes = root.ToString(SaveOptions.DisableFormatting);
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
        /// Gets attribute value identifiers.
        /// </summary>
        /// <param name="attributeId">Attribute identifier. <c>null</c> to get all value identifiers.</param>
        /// <returns>Attribute value identifiers.</returns>
        public int[] GetAttributeValueIds(int? attributeId = null)
        {
            var values = attributeId.HasValue
                ? (_map.ContainsKey(attributeId.Value) ? _map[attributeId.Value] : Enumerable.Empty<object>())
                : _map.SelectMany(x => x.Value);

            var valueIds = values
                .Select(x => x.ToString())
                .Where(x => x.HasValue())   // Important, avoid exception when string is empty.
                .Select(x => x.ToInt())
                .Where(x => x != 0)
                .Distinct()
                .ToArray();

            return valueIds;
        }

        /// <summary>
        /// Adds an attribute with possible multiple values to <see cref="AttributesMap"/>.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        /// <param name="attributeId">Attribute identifier</param>
        /// <param name="value">Attribute value</param>
        public void AddAttribute(int attributeId, IEnumerable<object> values)
        {
            _map.AddRange(attributeId, values);
            _dirty = true;
        }

        /// <summary>
        /// Adds an attribute value to <see cref="AttributesMap"/> by attribute id.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void AddAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Add(attributeId, value);
            _dirty = true;
        }

        /// <summary>
        /// Removes an attribute set from <see cref="AttributesMap"/> by attribute id.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        /// <param name="attributeId">Identifier of attribute</param>
        public void RemoveAttribute(int attributeId)
        {
            _map.RemoveAll(attributeId);
            _dirty = true;
        }

        /// <summary>
        /// Removes attribute sets from <see cref="AttributesMap"/> by attribute ids.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        /// <param name="attributeIds">List of attribute identifiers</param>
        public void RemoveAttributes(IEnumerable<int> attributeIds)
        {
            foreach (var attributeId in attributeIds)
            {
                _map.RemoveAll(attributeId);
            }

            _dirty = true;
        }

        /// <summary>
        /// Removes an attribute value from <see cref="AttributesMap"/> by attribute id.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        /// <param name="attributeId">Identifier of attribute</param>
        /// <param name="value">Attribute value</param>
        public void RemoveAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value, nameof(value));

            _map.Remove(attributeId, value);
            _dirty = true;
        }

        /// <summary>
        /// Removes all attribute sets from <see cref="AttributesMap"/>.
        /// </summary>
        /// <remarks>
        /// Changes to <see cref="AttributesMap"/> causes selection to reparse attributes string.
        /// </remarks>
        public void ClearAttributes()
        {
            _map.Clear();
            _dirty = true;
        }

        #region Compare

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();

            foreach (var kvp in _map)
            {
                combiner.Add(kvp.GetHashCode());
                kvp.Value.Each(x => combiner.Add(x.GetHashCode()));
            }

            return combiner.CombinedHash;
        }

        public override bool Equals(object obj) =>
            ((IEquatable<AttributeSelection>)this).Equals(obj as AttributeSelection);

        bool IEquatable<AttributeSelection>.Equals(AttributeSelection other)
        {
            if (other?._map == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var map1 = _map;
            var map2 = other._map;

            if (map1.Count != map2.Count)
            {
                return false;
            }

            foreach (var kvp in map1)
            {
                if (!map2.ContainsKey(kvp.Key))
                {
                    // The second list does not contain this key > not equal.
                    return false;
                }

                // Compare the values.
                var values1 = kvp.Value;
                var values2 = map2[kvp.Key];

                if (values1.Count != values2.Count)
                {
                    // Number of values differ > not equal.
                    return false;
                }

                foreach (var value1 in values1)
                {
                    var str1 = value1.ToString().TrimSafe();

                    if (!values2.Any(x => x.ToString().TrimSafe().EqualsNoCase(str1)))
                    {
                        // The second values list for this attribute does not contain this value > not equal.
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}