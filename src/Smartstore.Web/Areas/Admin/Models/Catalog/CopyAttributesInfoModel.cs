namespace Smartstore.Admin.Models.Catalog
{
    public class CopyAttributesInfoModel : EntityModelBase
    {
        public List<AttributeInfo> Attributes { get; init; }

        public class AttributeInfo : EntityModelBase
        {
            public string Name { get; init; }
            public string AttributeControlType { get; init; }
            public bool IsRequired { get; init; }

            public int NumberOfRules { get; init; }
            public List<string> Values { get; init; }
        }
    }
}
