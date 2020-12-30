using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Smartstore.Collections;

namespace Smartstore.Domain
{
    // TODO: (ms) (core) Make this work => abstract class with static methods has conflicts creating new instances => have static overrides on children
    public class AttributeSelection
    {
        protected AttributeSelection(string attributeName, string valueName)
        {
            _attributeName = attributeName;
            _valueName = valueName;
        }

        /// <summary>
        /// Creates an instance of <see cref="AttributeSelection"/> from attribute string that is either in XML or Json format
        /// Calls either <see cref="FromXml(string)"/> or <see cref="FromJson(string)"/> according to attributes string format
        /// Populates <see cref="_attributesMap"/> with ids and value objects
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
        /// Populates <see cref="_attributesMap"/> with ids and value objects from XML formatted attributes string
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
        public List<KeyValuePair<int, ICollection<object>>> AttributesMap => _attributesMap.ToList();

        protected bool _isJson;

        protected string _attributesXmlOrJson;

        protected readonly Multimap<int, object> _attributesMap = new();

        protected string _attributeName;
        protected string _valueName;

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
            if (_attributesXmlOrJson.HasValue() && !_isJson)
                return _attributesXmlOrJson;

            var root = new XElement("Attributes");
            foreach (var attribute in _attributesMap)
            {
                var attributeElement = new XElement("CheckoutAttribute", new XAttribute("ID", attribute.Key));

                foreach (var attributeValue in attribute.Value)
                {
                    attributeElement.Add(
                        new XElement(
                            "CheckoutAttributeValue",
                            new XElement("Value", attributeValue))
                        );
                }

                root.Add(attributeElement);
            }

            _isJson = false;
            _attributesXmlOrJson = root.ToString();
            return _attributesXmlOrJson;
        }

        /// <summary>
        /// Adds new attribute set to <see cref="_attributesMap"/> or updates an existing one
        /// </summary>
        public void AddOrUpdateAttribute(int attributeId, params object[] values)
        {
            if (_attributesMap.Keys.Contains(attributeId))
                _attributesMap.Keys.Remove(attributeId);

            _attributesMap.Add(attributeId, values);
        }

        /// <summary>
        /// Removes attribute set from <see cref="_attributesMap"/>
        /// </summary>
        public void RemoveAttribute(int attributeId)
        {
            _attributesMap.Keys.Remove(attributeId);
        }
    }
}