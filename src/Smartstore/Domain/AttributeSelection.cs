using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    /// This class can parse strings with XML or JSON format.
    /// </remarks>
    public abstract class AttributeSelection : IEquatable<AttributeSelection>
    {
        private string _rawAttributes;
        protected readonly AllAttributes _attributes;

        private readonly string _xmlAttributeName;
        private readonly string _xmlAttributeValueName;
        private bool _dirty = true;
        private bool _isJson;

        /// <summary>
        /// Creates a new attribute selection from string. 
        /// Use <see cref="AttributesMap"/> to access parsed attributes afterwards.
        /// </summary>
        /// <remarks>
        /// Automatically differentiates between XML and JSON.
        /// </remarks>        
        /// <param name="rawAttributes">XML or JSON attributes string.</param>
        /// <param name="xmlAttributeName">Attribute name for XML format.</param>
        /// <param name="xmlAttributeValueName">Optional attribute value name for XML format. If it is <c>null</c>, XmlAttributeName + "Value" is used.</param>
        protected AttributeSelection(string rawAttributes, string xmlAttributeName, string xmlAttributeValueName = null)
        {
            Guard.NotEmpty(xmlAttributeName);

            _rawAttributes = rawAttributes.TrimSafe();
            _xmlAttributeName = xmlAttributeName;

            _xmlAttributeValueName = xmlAttributeValueName.HasValue()
                ? xmlAttributeValueName
                : xmlAttributeName + "Value";

            _attributes = GetFromXmlOrJson();
        }

        /// <summary>
        /// Gets deserialized attributes.
        /// </summary>
        public IEnumerable<KeyValuePair<int, ICollection<object>>> AttributesMap
            => _attributes.Attributes;
        
        /// <summary>
        /// Gets a value indicating whether the selection contains any attributes.
        /// </summary>
        public bool HasAttributes
            => _attributes.Attributes.Count > 0;

        /// <summary>
        /// Gets deserialized attribute values by attribute id.
        /// </summary>
        /// <param name="attributeId">Attribute identifier</param>
        public IEnumerable<object> GetAttributeValues(int attributeId)
        {
            if (_attributes.Attributes.TryGetValues(attributeId, out var values))
            {
                return values;
            }

            return null;
        }

        /// <summary>
        /// Adds an attribute with values.
        /// </summary>
        /// <param name="attributeId">Attribute identifier.</param>
        /// <param name="value">Attribute values.</param>
        public void AddAttribute(int attributeId, IEnumerable<object> values)
        {
            _attributes.Attributes.AddRange(attributeId, values);
            _dirty = true;
        }

        /// <summary>
        /// Adds an attribute value.
        /// </summary>
        /// <param name="attributeId">Attribute identifier.</param>
        /// <param name="value">Attribute value.</param>
        public void AddAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value);

            _attributes.Attributes.Add(attributeId, value);
            _dirty = true;
        }

        /// <summary>
        /// Removes an attribute.
        /// </summary>
        /// <param name="attributeId">Attribute identifier.</param>
        public void RemoveAttribute(int attributeId)
        {
            if (_attributes.Attributes.RemoveAll(attributeId))
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// Removes attributes from.
        /// </summary>
        /// <param name="attributeIds">List of attribute identifiers.</param>
        public void RemoveAttributes(IEnumerable<int> attributeIds)
        {
            foreach (var attributeId in attributeIds)
            {
                if (_attributes.Attributes.RemoveAll(attributeId))
                {
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Removes an attribute value.
        /// </summary>
        /// <param name="attributeId">Attribute identifier.</param>
        /// <param name="value">Attribute value</param>
        public void RemoveAttributeValue(int attributeId, object value)
        {
            Guard.NotNull(value);

            if (_attributes.Attributes.Remove(attributeId, value))
            {
                _dirty = true;
            }
        }

        /// <summary>
        /// Removes all attributes.
        /// </summary>
        public void ClearAttributes()
        {
            _attributes.Attributes.Clear();
            _attributes.CustomAttributes.Clear();
            _dirty = true;
        }

        #region Serialization

        /// <summary>
        /// Serializes attributes in JSON format.
        /// </summary>
        public virtual string AsJson()
        {
            if (_rawAttributes.HasValue() && _isJson && !_dirty)
            {
                return _rawAttributes;
            }

            if (_attributes.Attributes.Count == 0 && _attributes.CustomAttributes.Count == 0)
            {
                return null;
            }

            try
            {
                var json = JsonConvert.SerializeObject(_attributes);

                _isJson = true;
                _dirty = false;
                _rawAttributes = json;

                return json;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("Failed to serialize JSON string from: " + nameof(_attributes), ex);
            }
        }

        /// <summary>
        /// Serializes attributes in XML format.
        /// </summary>
        public virtual string AsXml()
        {
            if (_rawAttributes.HasValue() && !_isJson && !_dirty)
            {
                return _rawAttributes;
            }

            var root = new XElement("Attributes");

            foreach (var attribute in _attributes.Attributes)
            {
                if (attribute.Key > 0)
                {
                    var attributeElement = new XElement(_xmlAttributeName, new XAttribute("ID", attribute.Key));

                    foreach (var attributeValue in attribute.Value.Distinct())
                    {
                        attributeElement.Add(new XElement(_xmlAttributeValueName, new XElement("Value", attributeValue)));
                    }

                    root.Add(attributeElement);
                }
            }

            foreach (var customAttribute in _attributes.CustomAttributes)
            {
                foreach (var customValue in customAttribute.Value)
                {
                    var customElement = ToCustomAttributeElement(customValue);
                    if (customElement != null)
                    {
                        root.Add(customElement);
                    }
                }
            }

            _isJson = false;
            _dirty = false;
            _rawAttributes = root.ToString(SaveOptions.DisableFormatting);

            return _rawAttributes;
        }

        /// <summary>
        /// Gets attributes from attribute string that is either in XML or JSON format.
        /// </summary>
        protected virtual AllAttributes GetFromXmlOrJson()
        {
            if (_rawAttributes.IsEmpty() || _rawAttributes.Length <= 2)
            {
                return new AllAttributes();
            }

            var isXml = _rawAttributes[0] == '<';
            var isJson = _rawAttributes[0] == '{';

            if (!isXml && !isJson)
            {
                throw new ArgumentException("Raw attributes must either be in XML or JSON format: " + _rawAttributes, nameof(_rawAttributes));
            }

            try
            {
                if (isXml)
                {
                    var attributes = new AllAttributes();
                    var xElement = XElement.Parse(_rawAttributes);

                    foreach (var el in xElement.Elements())
                    {
                        var attributeName = el.Name.LocalName;

                        if (attributeName == _xmlAttributeName)
                        {
                            var id = el.Attribute("ID")?.Value?.Convert<int>() ?? 0;

                            var values = el
                                .Descendants(_xmlAttributeValueName)
                                .Select(x => x.Value);

                            attributes.Attributes.AddRange(id, values);
                        }
                        else
                        {
                            var value = ToCustomAttributeValue(attributeName, el);
                            if (value != null)
                            {
                                attributes.CustomAttributes.Add(attributeName, value);
                            }
                        }
                    }

                    return attributes;
                }
                else
                {
                    var attributes = JsonConvert.DeserializeObject<AllAttributes>(_rawAttributes);

                    if (attributes.CustomAttributes.Count > 0)
                    {
                        // Convert custom attributes from JObject to specific type.
                        var newAttributes = new Multimap<string, object>();

                        foreach (var pair in attributes.CustomAttributes)
                        {
                            foreach (var rawValue in pair.Value)
                            {
                                var value = ToCustomAttributeValue(pair.Key, rawValue);
                                if (value != null)
                                {
                                    newAttributes.Add(pair.Key, value);
                                }
                            }
                        }

                        attributes.CustomAttributes.Clear();
                        newAttributes.Each(pair => attributes.CustomAttributes.AddRange(pair.Key, pair.Value));
                    }

                    return attributes;
                }
            }
            catch (Exception ex)
            {
                Exception exception = isXml
                    ? new XmlException("Failed to deserialize attributes from XML: " + _rawAttributes, ex)
                    : new JsonSerializationException("Failed to deserialize attributes from JSON: " + _rawAttributes, ex);

                throw exception;
            }
        }

        #endregion

        #region Custom attributes

        /// <summary>
        /// Converts an XElement (XML) or JObject (JSON) object into a desired custom attribute value instance (e.g. GiftCardInfo).
        /// This method is called when deserializing the raw attributes string.
        /// </summary>
        /// <param name="attributeName">The name of the custom attribute, e.g. GiftCardInfo.</param>
        /// <param name="value">An XElement object if the raw data is XML serialized or an JObject object if it is JSON serialized.</param>
        /// <returns>The converted custom attribute value, e.g. a GiftCardInfo instance.</returns>
        protected virtual object ToCustomAttributeValue(string attributeName, object value)
            => null;

        /// <summary>
        /// Converts a custom attribute to an XElement node for XML serialization.
        /// </summary>
        /// <param name="value">A custom attribute value, e.g. a GiftCardInfo instance.</param>
        protected virtual XElement ToCustomAttributeElement(object value)
            => null;

        /// <summary>
        /// Gets custom attribute values for a given custom attribute name.
        /// </summary>
        /// <param name="attributeName">Custom attribute name (e.g. GiftCardInfo).</param>
        /// <returns>List of of custom attribute values.</returns>
        /// <remarks>Custom attributes are included in the serialization\deserialization
        /// but ignored when <see cref="AttributeSelection2"/> is checked for equality.</remarks>
        protected IEnumerable<object> GetCustomAttributeValues(string attributeName)
        {
            if (_attributes.CustomAttributes.TryGetValues(attributeName, out var values))
            {
                return values;
            }

            return null;
        }

        /// <summary>
        /// Adds a custom attribute value.
        /// </summary>
        /// <param name="attributeName">Custom attribute name (e.g. GiftCardInfo).</param>
        /// <param name="value">Custom attribute value.</param>
        /// <remarks>Custom attributes are included in the serialization\deserialization
        /// but ignored when <see cref="AttributeSelection2"/> is checked for equality.</remarks>
        protected void AddCustomAttributeValue(string attributeName, object value)
        {
            Guard.NotEmpty(attributeName);

            if (value != null)
            {
                _attributes.CustomAttributes.Add(attributeName, value);
                _dirty = true;
            }
        }

        /// <summary>
        /// Removes a custom attribute and all its values.
        /// </summary>
        /// <param name="attributeName">Custom attribute name (e.g. GiftCardInfo).</param>
        protected void RemoveCustomAttribute(string attributeName)
        {
            Guard.NotEmpty(attributeName);

            _attributes.CustomAttributes.RemoveAll(attributeName);
            _dirty = true;
        }

        #endregion

        #region Equality

        public static bool operator ==(AttributeSelection left, AttributeSelection right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AttributeSelection left, AttributeSelection right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Creates a unqiue hash code for attributes contained in this selection.
        /// </summary>
        /// <remarks>
        /// For the hash code to work when stored in the database, the attribute selection must contain list types only!
        /// That means no plain text or date values.
        /// <see cref="AllAttributes.CustomAttributes"/> (like gift card data) are always ignored when creating the hash code.
        /// See also IProductAttributeMaterializer.FindAttributeCombinationAsync (it only includes list type attributes).
        /// </remarks>
        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            var attributes = _attributes.Attributes.OrderBy(x => x.Key).ToArray();

            for (var i = 0; i < attributes.Length; ++i)
            {
                var attribute = attributes[i];
                
                combiner.Add(attribute.Key);

                var values = attribute.Value
                    .Select(x => x.ToString())
                    .OrderBy(x => x)
                    .ToArray();

                for (var j = 0; j < values.Length; ++j)
                {
                    combiner.Add(values[j]);
                }
            }

            return combiner.CombinedHash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AttributeSelection);
        }

        bool IEquatable<AttributeSelection>.Equals(AttributeSelection other)
        {
            return Equals(other);
        }

        protected virtual bool Equals(AttributeSelection other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Check attributes.
            var map1 = _attributes.Attributes;
            var map2 = other._attributes.Attributes;

            if (map1.Count != map2.Count)
            {
                return false;
            }

            foreach (var pair1 in map1)
            {
                if (!map2.ContainsKey(pair1.Key))
                {
                    // The second list does not contain this key > not equal.
                    return false;
                }

                // Compare the values.
                var values1 = pair1.Value;
                var values2 = map2[pair1.Key];

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

            // INFO: custom attributes never included in equality check by default.
            // We never had such a use case in practice.

            return true;
        }

        #endregion

        protected class AllAttributes
        {
            public Multimap<int, object> Attributes { get; set; } = new();
            public Multimap<string, object> CustomAttributes { get; set; } = new();

            // INFO: Json.NET conditional property serialization convention:
            // prevents CustomAttributes from being serialized if empty.
            // Deserialized to an empty map if missing in raw JSON string.
            public bool ShouldSerializeCustomAttributes()
                => CustomAttributes?.Count > 0;
        }
    }

    public static class AttributeSelectionExtensions
    {
        /// <summary>
        /// Checks whether given <paramref name="selection"/> collection is either <c>null</c> or empty.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this AttributeSelection selection)
        {
            return selection == null || !selection.HasAttributes;
        }
    }
}