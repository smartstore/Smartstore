using Dasync.Collections;
using Smartstore.Core.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeParser : ICheckoutAttributeParser
    {
        private readonly SmartDbContext _db;

        public CheckoutAttributeParser(SmartDbContext db)
        {
            _db = db;
        }

        public IEnumerable<int> ParseCheckoutAttributeIds(string attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var ids = new List<int>();
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(attributes);
                var nodeList = xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute");
                foreach (XmlNode node in nodeList)
                {
                    if (node.Attributes is null || node.Attributes["ID"] is null)
                        continue;

                    var str = node.Attributes["ID"].InnerText.Trim();
                    if (int.TryParse(str, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }

            return ids;
        }

        public Task<List<CheckoutAttribute>> ParseCheckoutAttributesAsync(string attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var ids = ParseCheckoutAttributeIds(attributes);
            return _db.CheckoutAttributes.GetManyAsync(ids);
        }

        public async Task<List<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(string attributes)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var valuesList = new List<CheckoutAttributeValue>();
            var attributesList = await ParseCheckoutAttributesAsync(attributes);

            foreach (var attribute in attributesList)
            {
                if (!attribute.ShouldHaveValues())
                    continue;

                var values = await ParseValuesAsync(attributes, attribute.Id);
                var ids = values
                    .Select(x => int.TryParse(x, out var id) ? id : -1)
                    .Where(x => x is not -1);

                // TODO: (core) (ms) Fetch this in ONE rountrip!
                var attributeValues = await _db.CheckoutAttributeValues.GetManyAsync(ids);
                valuesList.AddRange(attributeValues);
            }

            return valuesList;
        }

        public Task<IEnumerable<string>> ParseValuesAsync(string attributes, int attributeId)
        {
            Guard.NotNull(attributes, nameof(attributes));

            var attributesList = new List<string>();
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(attributes);

                var nodeList = xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute");
                foreach (XmlNode node in nodeList)
                {
                    if (node.Attributes is null || node.Attributes["ID"] is null)
                        continue;

                    var str = node.Attributes["ID"].InnerText.Trim();
                    if (int.TryParse(str, out var id))
                    {
                        if (id != attributeId)
                            continue;

                        var innerNodeList = node.SelectNodes(@"CheckoutAttributeValue/Value");
                        foreach (XmlNode innerNode in innerNodeList)
                        {
                            attributesList.Add(innerNode.InnerText.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString()); // ? logger?
            }

            return Task.FromResult<IEnumerable<string>>(attributesList);
        }

        // TODO: (core) (ms) needs OrganizedShoppingCartItem here
        //public async Task<string> EnsureOnlyActiveAttributesAsync(string attributes, IList<OrganizedShoppingCartItem> cart)
        //{
        //    Guard.NotNull(attributes, nameof(attributes));

        //    Remove "shippable" checkout attributes, if there are not any shippable products in the cart
        //    var result = attributes;
        //    if (cart.IsNullOrEmpty() || cart.RequiresShipping())
        //        return result;

        //    Find attribute Ids to remove
        //   var attributeIdsToRemove = new List<int>();
        //    var attributesList = (await ParseCheckoutAttributesAsync(attributes)).ToListAsync();
        //    for (var i = 0; i < attributesList.Count; i++)
        //    {
        //        var attribute = attributesList[i];
        //        if (attribute.ShippableProductRequired)
        //        {
        //            attributeIdsToRemove.Add(attribute.Id);
        //        }
        //    }

        //    Remove from XML
        //    try
        //    {
        //        var xmlDoc = new XmlDocument();
        //        xmlDoc.LoadXml(attributes);
        //        var nodesToRemove = new List<XmlNode>();
        //        foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute"))
        //        {
        //            if (node.Attributes is null || node.Attributes["ID"] is null)
        //                continue;

        //            var str = node.Attributes["ID"].InnerText.Trim();
        //            if (int.TryParse(str, out var id) && attributeIdsToRemove.Contains(id))
        //            {
        //                nodesToRemove.Add(node);
        //            }
        //        }

        //        foreach (var node in nodesToRemove)
        //        {
        //            node.ParentNode.RemoveChild(node);
        //        }

        //        result = xmlDoc.OuterXml;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Write(ex.ToString()); // ? logger instead
        //    }

        //    return result;
        //}
    }
}
