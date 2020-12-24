using Dasync.Collections;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
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

            var attributeIds = new List<int>();
            var valuesList = new List<CheckoutAttributeValue>();
            var attributesList = await ParseCheckoutAttributesAsync(attributes);

            foreach (var attribute in attributesList)
            {
                if (!attribute.ShouldHaveValues())
                    continue;

                var values = ParseValues(attributes, attribute.Id);
                var ids = values
                    .Select(x => int.TryParse(x, out var id) ? id : -1)
                    .Where(x => x != -1)
                    .Distinct();

                attributeIds.AddRange(ids);
            }

            if (!attributeIds.IsNullOrEmpty())
            {
                var attributeValues = await _db.CheckoutAttributeValues.GetManyAsync(attributeIds);
                valuesList.AddRange(attributeValues);
            }

            return valuesList;
        }

        public IList<string> ParseValues(string attributes, int attributeId)
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
                    if (int.TryParse(str, out var id) && id == attributeId)
                    {
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
                Debug.Write(ex.ToString());
            }

            return attributesList;
        }
                
        public async Task<string> RemoveNotApplicableAttributesAsync(string attributes, IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(attributes, nameof(attributes));

            // Remove "shippable" checkout attributes, if there are not any shippable products in the cart
            var result = attributes;
            if (cart.IsNullOrEmpty() || cart.IsShippingRequired())
                return result;

            // Find attribute Ids to remove
            var idsToRemove = new List<int>();
            var attributesList = await ParseCheckoutAttributesAsync(attributes);
            foreach (var attribute in attributesList)
            {
                if (attribute.ShippableProductRequired)
                {
                    idsToRemove.Add(attribute.Id);
                }
            }

            try
            {
                // Get nodes by ids to remove
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(attributes);
                var nodesToRemove = new List<XmlNode>();
                var nodeList = xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute");

                foreach (XmlNode node in nodeList)
                {
                    if (node.Attributes is null || node.Attributes["ID"] is null)
                        continue;

                    var str = node.Attributes["ID"].InnerText.Trim();
                    if (int.TryParse(str, out var id) && idsToRemove.Contains(id))
                    {
                        nodesToRemove.Add(node);
                    }
                }

                // Remove from XML
                foreach (var node in nodesToRemove)
                {
                    node.ParentNode.RemoveChild(node);
                }

                result = xmlDoc.OuterXml;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }

            return result;
        }
    }
}