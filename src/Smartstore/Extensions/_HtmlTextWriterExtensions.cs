//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web.UI;

//namespace Smartstore
//{ 
//    public static class HtmlTextWriterExtensions
//    {
//        public static void AddAttributes(this HtmlTextWriter writer, IDictionary<string, object> attributes)
//        {
//            if (attributes.Any())
//            {
//                foreach (var pair in attributes)
//                {
//                    if (pair.Value != null)
//                        writer.AddAttribute(pair.Key, pair.Value.ToString(), true);
//                }
//            }
//        }
//    }
//}
