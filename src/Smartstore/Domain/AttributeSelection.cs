using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Smartstore.Collections;

namespace Smartstore.Domain
{
    // TODO: (ms) (core) Make this work => abstract class with static methods has conflicts creating new instances => have static overrides on children
    public abstract class AttributeSelection
    {
        private readonly string _xmlAttributeName;
        private readonly Multimap<int, object> _map = new();
        private string _rawAttributes;
        private bool _isJson;

        /// <summary>
        /// TODO!!!!!!!!!!
        /// </summary>
        /// <param name="xmlAttributeName"></param>
        /// <param name="rawAttributes"></param>
        protected AttributeSelection(string xmlAttributeName, string rawAttributes)
        {
            Guard.NotEmpty(xmlAttributeName, nameof(xmlAttributeName));
            Guard.NotEmpty(rawAttributes, nameof(rawAttributes));

            _xmlAttributeName = xmlAttributeName;
            _rawAttributes = rawAttributes;

            //_map = Parse(rawAttributes);
        }

        /// <summary>
        /// Creates an instance of <see cref="AttributeSelection"/> from attribute string that is either in XML or Json format
        /// Calls either <see cref="FromXml(string)"/> or <see cref="FromJson(string)"/> according to attributes string format
        /// Populates <see cref="_map"/> with ids and value objects
        /// </summary>
        //public static AttributeSelection FromXmlOrJson(string attributesXmlOrJson)
        //{
        //    if (!attributesXmlOrJson.HasValue())
        //        return new(_attributeName, _valueName);

        //    var firstChar = attributesXmlOrJson.TrimSafe()[0];
        //    if (firstChar == '<')
        //    {
        //        return FromXml(attributesXmlOrJson);
        //    }
        //    else if (firstChar == '{')
        //    {
        //        return FromJson(attributesXmlOrJson);
        //    }

        //    Debug.Write("An error occured while trying to convert attributes string: " + attributesXmlOrJson);
        //    return new(_attributeName, _valueName);
        //}

        /// <summary>
        /// Creates an instance of <see cref="AttributeSelection"/> from XML attributes string
        /// Populates <see cref="_map"/> with ids and value objects from XML formatted attributes string
        /// </summary>
        //public static AttributeSelection FromXml(string attributesXml)
        //{
        //    if (!attributesXml.HasValue())
        //        return new();

        //    var selection = new AttributeSelection();
        //    try
        //    {
        //        var xElement = XElement.Parse(attributesXml);
        //        var attributeElements = xElement.Descendants(_attributeName).ToList();
        //        foreach (var element in attributeElements)
        //        {
        //            var values = element.Descendants(_valueName).Select(x => x.Value).ToList();
        //            selection._attributesMap.Add(element.Value.Convert<int>(), values);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.Message);
        //    }

        //    return selection;
        //}

        /// <summary>
        /// Creates an instance of <see cref="AttributeSelection"/> from Json attributes string
        /// Populates <see cref="_attributesMap"/> with ids and value objects from Json formatted attributes string
        /// </summary>
        //public static AttributeSelection FromJson(string attributesJson)
        //{
        //    // TODO: (ms) (core) Finish json implementation
        //    return new();
        //}

        /// <summary>
        /// Gets values of <see cref="_attributesMap"/>
        /// </summary>
        public IEnumerable<KeyValuePair<int, ICollection<object>>> AttributesMap 
            => _map;

        /// <summary>
        /// Gets <see cref="AttributeSelection"/> as Json string
        /// </summary>
        public string AsJson()
        {
            // TODO: (ms) (core) Finish json implementation
            return string.Empty;
        }

        /// <summary>
        /// Gets <see cref="AttributeSelection"/> as XML string
        /// </summary>
        public string AsXml()
        {
            if (_rawAttributes.HasValue() && !_isJson)
                return _rawAttributes;

            var root = new XElement("Attributes");
            foreach (var attribute in _map)
            {
                var attributeElement = new XElement(_xmlAttributeName, new XAttribute("ID", attribute.Key));

                foreach (var attributeValue in attribute.Value.Distinct())
                {
                    attributeElement.Add(
                        new XElement(
                            _xmlAttributeName + "Value",
                            new XElement("Value", attributeValue))
                        );
                }

                root.Add(attributeElement);
            }

            _isJson = false;
            _rawAttributes = root.ToString();
            return _rawAttributes;
        }

        public IEnumerable<object> GetAttributeValues(int attributeId)
        {
            if (_map.ContainsKey(attributeId))
            {
                return _map[attributeId];
            }

            return null;
        }

        public void AddAttribute(int attributeId, params object[] values) 
        { 
        }

        public void AddAttributeValue(int attributeId, object value)
        {
            _map.Add(attributeId, value);
        }

        /// <summary>
        /// Adds new attribute set to <see cref="_map"/> or updates an existing one
        /// </summary>
        public void AddOrUpdateAttribute(int attributeId, params object[] values)
        {
            if (_map.Keys.Contains(attributeId))
                _map.Keys.Remove(attributeId);
            // TODO: (ms) (core) Set dirty flag!
            _map.Add(attributeId, values);
        }

        /// <summary>
        /// Removes attribute set from <see cref="_map"/>
        /// </summary>
        public void RemoveAttribute(int attributeId)
        {
            _map.RemoveAll(attributeId);
        }

        public void RemoveAttributeValue(int attributeId, object value)
        {
        }

        public void ClearAttributes()
        {
            _map.Clear();
        }
    }
}