using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;

namespace Smartstore.WebApi.Services
{
    public class CustomRoutingConvention : IODataControllerActionConvention
    {
        public int Order => 401;

        public bool AppliesToController(ODataControllerActionContext context)
        {
            if (context.Controller.ControllerName == "Categories")
            {
                return true;
            }

            return false;
        }

        public bool AppliesToAction(ODataControllerActionContext context)
        {
            var navigationSource = context.NavigationSource;
            if (navigationSource == null)
            {
                return false;
            }

            if (context.Action.ActionName == "GetProperty")
            {
                var entitySet = navigationSource as IEdmEntitySet;
                var entityType = navigationSource.EntityType();

                var segments = new List<ODataSegmentTemplate>();
                if (entitySet != null)
                {
                    segments.Add(new EntitySetSegmentTemplate(entitySet));
                    segments.Add(CreateKeySegment(entityType, entitySet));

                    //var ps = new PropertySegmentTemplate(
                }

                var path = new ODataPathTemplate(segments);

                context.Action.AddSelector("Get", context.Prefix, context.Model, path);
                return true;
            }

            return false;
        }

        private static KeySegmentTemplate CreateKeySegment(IEdmEntityType entityType, IEdmNavigationSource navigationSource, string keyPrefix = "key")
        {
            Guard.NotNull(entityType, nameof(entityType));

            var keyTemplates = new Dictionary<string, string>();
            var keys = entityType.Key().ToArray();
            if (keys.Length == 1)
            {
                // Id={key}
                keyTemplates[keys[0].Name] = $"{{{keyPrefix}}}";
            }
            else
            {
                // Id1={keyId1},Id2={keyId2}
                foreach (var key in keys)
                {
                    keyTemplates[key.Name] = $"{{{keyPrefix}{key.Name}}}";
                }
            }

            return new KeySegmentTemplate(keyTemplates, entityType, navigationSource);
        }
    }


    //public class CustomRoutingConvention : EntitySetRoutingConvention
    //{
    //    /// <inheritdoc />
    //    public override bool AppliesToController(ODataControllerActionContext context)
    //    {
    //        var result = base.AppliesToController(context);

    //        var action = context.Action;
    //        var entitySet = context.EntitySet;
    //        var entityType = entitySet.EntityType();

    //        return result;
    //    }

    //    /// <inheritdoc />
    //    public override bool AppliesToAction(ODataControllerActionContext context)
    //    {
    //        var result = base.AppliesToAction(context);

    //        var action = context.Action;
    //        var entitySet = context.EntitySet;
    //        var entityType = entitySet.EntityType();

    //        if (!result
    //            && context.EntitySet.IncludeInServiceDocument
    //            && action.ActionMethod.Name.EqualsNoCase("GetProperty"))
    //        {
    //            var assemblyName = context?.Controller?.ControllerType?.Assembly?.GetName()?.Name;

    //            $"{result} {context.Controller.ControllerName} {assemblyName} ... {action.DisplayName}".Dump();
    //            return true;

    //        }

    //        return result;
    //    }
    //}



    // Holy shit, just stay away from this IODataControllerActionConvention awkward crap!
    // https://github.com/OData/AspNetCoreOData/issues/75
    // Offer built-in conventional routings and new OData features must be enough, or use attribute routing.
    // https://devblogs.microsoft.com/odata/routing-in-asp-net-core-8-0-preview/#built-in-conventional-routings
    //public class CustomRoutingConvention : IODataControllerActionConvention
    //{
    //    // One behind PropertyRoutingConvention.
    //    public int Order => 401;

    //    public bool AppliesToController(ODataControllerActionContext context)
    //    {
    //        return context?.NavigationSource != null;
    //    }

    //    public bool AppliesToAction(ODataControllerActionContext context)
    //    {
    //        var navigationSource = context.NavigationSource;
    //        if (navigationSource == null)
    //        {
    //            return false;
    //        }

    //        var action = context.Action;
    //        var actionName = action.ActionName;
    //        var entityType = navigationSource.EntityType();
    //        //string declared = null;
    //        string cast = null;
    //        var property = "Name";
    //        var method = "GetProperty";

    //        //var method = SplitActionName(actionName, out string property, out string cast, out string declared);
    //        //if (method == null || string.IsNullOrEmpty(property))
    //        //{
    //        //    return false;
    //        //}

    //        var hasKeyParameter = action.HasODataKeyParameter(entityType, context.Options?.RouteOptions?.EnablePropertyNameCaseInsensitive ?? false);
    //        if (!(context.Singleton != null ^ hasKeyParameter))
    //        {
    //            return false;
    //        }

    //        var declaringEntityType = entityType;
    //        //if (declared != null)
    //        //{
    //        //    if (declared.Length == 0)
    //        //    {
    //        //        return false;
    //        //    }

    //        //    declaringEntityType = FindTypeInInheritance(entityType, context.Model, declared) as IEdmEntityType;
    //        //    if (declaringEntityType == null)
    //        //    {
    //        //        return false;
    //        //    }
    //        //}

    //        var edmProperty = declaringEntityType.FindProperty(property, context?.Options?.RouteOptions.EnablePropertyNameCaseInsensitive ?? false);
    //        if (edmProperty == null || edmProperty.PropertyKind != EdmPropertyKind.Structural)
    //        {
    //            return false;
    //        }

    //        IEdmComplexType castType;
    //        // Only process structural property
    //        IEdmStructuredType castComplexType = null;
    //        if (cast != null)
    //        {
    //            if (cast.Length == 0)
    //            {
    //                return false;
    //            }

    //            IEdmType propertyElementType = edmProperty.Type.Definition.AsElementType();
    //            if (propertyElementType.TypeKind == EdmTypeKind.Complex)
    //            {
    //                IEdmComplexType complexType = (IEdmComplexType)propertyElementType;
    //                castType = FindTypeInInheritance(complexType, context.Model, cast) as IEdmComplexType;
    //                if (castType == null)
    //                {
    //                    return false;
    //                }
    //            }
    //            else
    //            {
    //                // only support complex type cast, (TODO: maybe consider to support Edm.PrimitiveType cast)
    //                return false;
    //            }

    //            IEdmTypeReference propertyType = edmProperty.Type;
    //            if (propertyType.IsCollection())
    //            {
    //                propertyType = propertyType.AsCollection().ElementType();
    //            }

    //            if (!propertyType.IsComplex())
    //            {
    //                return false;
    //            }

    //            castComplexType = FindTypeInInheritance(propertyType.ToStructuredType(), context.Model, cast);
    //            if (castComplexType == null)
    //            {
    //                return false;
    //            }
    //        }

    //        AddSelector(method, context, action, navigationSource, (IEdmStructuralProperty)edmProperty, castComplexType, declaringEntityType, false, false);

    //        return true;
    //    }

    //    private static void AddSelector(string httpMethod, ODataControllerActionContext context, ActionModel action,
    //        IEdmNavigationSource navigationSource,
    //        IEdmStructuralProperty edmProperty,
    //        IEdmType cast, IEdmEntityType declaringType, bool dollarValue, bool dollarCount)
    //    {
    //        IEdmEntitySet entitySet = navigationSource as IEdmEntitySet;
    //        IEdmEntityType entityType = navigationSource.EntityType();

    //        IList<ODataSegmentTemplate> segments = new List<ODataSegmentTemplate>();
    //        if (entitySet != null)
    //        {
    //            segments.Add(new EntitySetSegmentTemplate(entitySet));
    //            segments.Add(CreateKeySegment(entityType, navigationSource));
    //        }
    //        else
    //        {
    //            segments.Add(new SingletonSegmentTemplate(navigationSource as IEdmSingleton));
    //        }

    //        if (declaringType != null && declaringType != entityType)
    //        {
    //            segments.Add(new CastSegmentTemplate(declaringType, entityType, navigationSource));
    //        }

    //        segments.Add(new PropertySegmentTemplate(edmProperty));

    //        if (cast != null)
    //        {
    //            if (edmProperty.Type.IsCollection())
    //            {
    //                cast = new EdmCollectionType(ToEdmTypeReference(cast, edmProperty.Type.IsNullable));
    //            }

    //            // TODO: maybe create the collection type for the collection????
    //            segments.Add(new CastSegmentTemplate(cast, edmProperty.Type.Definition, navigationSource));
    //        }

    //        if (dollarValue)
    //        {
    //            segments.Add(new ValueSegmentTemplate(edmProperty.Type.Definition));
    //        }

    //        if (dollarCount)
    //        {
    //            segments.Add(CountSegmentTemplate.Instance);
    //        }

    //        ODataPathTemplate template = new ODataPathTemplate(segments);
    //        action.AddSelector(NormalizeHttpMethod(httpMethod), context.Prefix, context.Model, template, context.Options?.RouteOptions);
    //    }

    //    private static string SplitActionName(string actionName, out string property, out string cast, out string declared)
    //    {
    //        string method = null;
    //        string text = "";
    //        // Get{PropertyName}Of<cast>From<declaring>: GetCityOfSubAddressFromVipCustomer
    //        foreach (var prefix in new[] { "Get", "PostTo", "PutTo", "PatchTo", "DeleteTo" })
    //        {
    //            if (actionName.StartsWith(prefix, StringComparison.Ordinal))
    //            {
    //                method = prefix;
    //                text = actionName.Substring(prefix.Length);
    //                break;
    //            }
    //        }

    //        property = null;
    //        cast = null;
    //        declared = null;
    //        if (method == null)
    //        {
    //            return null;
    //        }

    //        int index = text.IndexOf("Of", StringComparison.Ordinal);
    //        if (index > 0)
    //        {
    //            property = text.Substring(0, index);
    //            text = text.Substring(index + 2);
    //            cast = Match(text, out declared);
    //        }
    //        else
    //        {
    //            property = Match(text, out declared);
    //        }

    //        return method;
    //    }

    //    private static string Match(string text, out string declared)
    //    {
    //        declared = null;
    //        int index = text.IndexOf("From", StringComparison.Ordinal);
    //        if (index > 0)
    //        {
    //            declared = text.Substring(index + 4);
    //            return text.Substring(0, index);
    //        }

    //        return text;
    //    }

    //    private static IEdmStructuredType FindTypeInInheritance(IEdmStructuredType structuralType, IEdmModel model, string typeName, bool caseInsensitive = false)
    //    {
    //        StringComparison typeStringComparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    //        IEdmStructuredType baseType = structuralType;
    //        while (baseType != null)
    //        {
    //            if (GetName(baseType).Equals(typeName, typeStringComparison))
    //            {
    //                return baseType;
    //            }

    //            baseType = baseType.BaseType;
    //        }

    //        return model.FindAllDerivedTypes(structuralType).FirstOrDefault(c => GetName(c).Equals(typeName, typeStringComparison));
    //    }

    //    private static IEdmTypeReference ToEdmTypeReference(IEdmType edmType, bool isNullable)
    //    {
    //        if (edmType == null)
    //        {
    //            throw new ArgumentNullException(nameof(edmType));
    //        }

    //        switch (edmType.TypeKind)
    //        {
    //            case EdmTypeKind.Collection:
    //                return new EdmCollectionTypeReference((IEdmCollectionType)edmType);

    //            case EdmTypeKind.Complex:
    //                return new EdmComplexTypeReference((IEdmComplexType)edmType, isNullable);

    //            case EdmTypeKind.Entity:
    //                return new EdmEntityTypeReference((IEdmEntityType)edmType, isNullable);

    //            case EdmTypeKind.EntityReference:
    //                return new EdmEntityReferenceTypeReference((IEdmEntityReferenceType)edmType, isNullable);

    //            case EdmTypeKind.Enum:
    //                return new EdmEnumTypeReference((IEdmEnumType)edmType, isNullable);

    //            case EdmTypeKind.Primitive:
    //                return EdmCoreModel.Instance.GetPrimitive(((IEdmPrimitiveType)edmType).PrimitiveKind, isNullable);

    //            case EdmTypeKind.Path:
    //                return new EdmPathTypeReference((IEdmPathType)edmType, isNullable);

    //            case EdmTypeKind.TypeDefinition:
    //                return new EdmTypeDefinitionReference((IEdmTypeDefinition)edmType, isNullable);

    //            default:
    //                throw new NotSupportedException($"Not supported EDM type {edmType.ToString()}");
    //        }
    //    }

    //    private static KeySegmentTemplate CreateKeySegment(IEdmEntityType entityType, IEdmNavigationSource navigationSource, string keyPrefix = "key")
    //    {
    //        if (entityType == null)
    //        {
    //            throw new ArgumentNullException(nameof(entityType));
    //        }

    //        IDictionary<string, string> keyTemplates = new Dictionary<string, string>();
    //        var keys = entityType.Key().ToArray();
    //        if (keys.Length == 1)
    //        {
    //            // Id={key}
    //            keyTemplates[keys[0].Name] = $"{{{keyPrefix}}}";
    //        }
    //        else
    //        {
    //            // Id1={keyId1},Id2={keyId2}
    //            foreach (var key in keys)
    //            {
    //                keyTemplates[key.Name] = $"{{{keyPrefix}{key.Name}}}";
    //            }
    //        }

    //        return new KeySegmentTemplate(keyTemplates, entityType, navigationSource);
    //    }

    //    private static string GetName(IEdmStructuredType type)
    //    {
    //        var entityType = type as IEdmEntityType;
    //        if (entityType != null)
    //        {
    //            return entityType.Name;
    //        }

    //        return ((IEdmComplexType)type).Name;
    //    }

    //    private static string NormalizeHttpMethod(string method)
    //    {
    //        switch (method.ToUpperInvariant())
    //        {
    //            case "POSTTO":
    //                return "Post";

    //            case "PUTTO":
    //                return "Put";

    //            case "PATCHTO":
    //                return "Patch";

    //            case "DELETETO":
    //                return "Delete";

    //            default:
    //                return method;
    //        }
    //    }
    //}
}
