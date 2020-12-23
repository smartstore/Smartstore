using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Web.UI
{
    public static class AttributeDictionaryExtensions
    {
        public static AttributeDictionary AppendCssClass(this AttributeDictionary attributes, Func<string> cssClass)
        {
            attributes.AppendInValue("class", ' ', cssClass());
            return attributes;
        }

        public static AttributeDictionary PrependCssClass(this AttributeDictionary attributes, Func<string> cssClass)
        {
            attributes.PrependInValue("class", ' ', cssClass());
            return attributes;
        }

        public static AttributeDictionary AppendCssClass(this AttributeDictionary attributes, string cssClass)
        {
            attributes.AppendInValue("class", ' ', cssClass);
            return attributes;
        }

        public static AttributeDictionary PrependCssClass(this AttributeDictionary attributes, string cssClass)
        {
            attributes.PrependInValue("class", ' ', cssClass);
            return attributes;
        }

        public static void AppendCssClass(this TagBuilder builder, Func<string> cssClass)
        {
            builder.Attributes.AppendInValue("class", ' ', cssClass());
        }

        public static void PrependCssClass(this TagBuilder builder, Func<string> cssClass)
        {
            builder.Attributes.PrependInValue("class", ' ', cssClass());
        }

        public static void AppendCssClass(this TagBuilder builder, string cssClass)
        {
            builder.Attributes.AppendInValue("class", ' ', cssClass);
        }

        public static void PrependCssClass(this TagBuilder builder, string cssClass)
        {
            builder.Attributes.PrependInValue("class", ' ', cssClass);
        }
    }
}
