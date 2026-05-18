#nullable enable

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;
using Smartstore.Collections;
using Smartstore.Utilities;

namespace Smartstore.Json;

/// <summary>
/// Builds structured data (JSON-LD) for the current page by collecting <see cref="JsonLdFragment"/> fragments
/// from views, partials and components, then rendering them as consolidated script blocks.
/// </summary>
public class JsonLdBuilder
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Key: schema.org @type (e.g., "Product", "BreadcrumbList"). Value: One or more top-level fragments of that type.
    private readonly Multimap<string, JsonLdFragment> _fragments = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc cref="Fragment(string)" />
    public JsonLdFragment this[string type]
        => Fragment(type);

    /// <summary>
    /// Gets or creates the <see cref="JsonLdFragment"/> fragment for the given schema.org @type.
    /// Multiple callers accessing the same type receive the same instance and contribute via deep merge.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
    /// <param name="create">A value indicating whether to create a new fragment even if one of the same type already exists.</param>
    public JsonLdFragment Fragment(string type, bool create = false)
    {
        Guard.NotEmpty(type);

        if (_fragments.TryGetValues(type, out var fragments))
        {
            if (create || fragments.IsNullOrEmpty())
            {
                return Add();
            }
            else
            {
                return fragments.First();
            }
        }
        else
        {
            return Add();
        }

        JsonLdFragment Add()
        {
            var fragment = JsonLdFragment.CreateTopLevel(type);
            _fragments.Add(type, fragment);
            return fragment;
        }
    }

    /// <summary>Gets or creates the <c>Product</c> fragment.</summary>
    public JsonLdFragment Product
        => this["Product"];

    /// <summary>Gets or creates the <c>CollectionPage</c> fragment.</summary>
    public JsonLdFragment CollectionPage
        => this["CollectionPage"];

    /// <summary>Gets or creates the <c>ItemList</c> fragment.</summary>
    public JsonLdFragment ItemList
        => this["ItemList"];

    /// <summary>Gets or creates the <c>BreadcrumbList</c> fragment.</summary>
    public JsonLdFragment BreadcrumbList
        => this["BreadcrumbList"];

    /// <summary>Gets or creates the <c>BlogPosting</c> fragment.</summary>
    public JsonLdFragment BlogPosting
        => this["BlogPosting"];

    /// <summary>Gets or creates the <c>Organization</c> fragment.</summary>
    public JsonLdFragment Organization
        => this["Organization"];

    /// <summary>Gets or creates the <c>WebSite</c> fragment.</summary>
    public JsonLdFragment WebSite
        => this["WebSite"];

    /// <summary>
    /// Gets a value indicating whether any JSON-LD fragments have been registered.
    /// </summary>
    public bool HasFragments
        => _fragments.Count > 0;

    /// <summary>
    /// Gets all currently registered fragments as a read-only sequence.
    /// Does not create new fragments. Useful for inspection, testing, or conditional logic.
    /// </summary>
    public IReadOnlyCollection<JsonLdFragment> Fragments
        => _fragments.SelectMany(x => x.Value).AsReadOnly();

    /// <summary>
    /// Returns the fragments for the given schema.org @type if they have already been registered,
    /// without creating a new one.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
    /// <param name="fragments">The fragments if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the fragment exists; otherwise <see langword="false"/>.</returns>
    public bool TryGetFragments(string type, out ICollection<JsonLdFragment>? fragments)
        => _fragments.TryGetValues(type, out fragments);

    /// <summary>
    /// Renders all collected JSON-LD fragments as &lt;script type="application/ld+json"&gt; blocks.
    /// Each distinct @type produces a separate script block with <c>"@context": "https://schema.org/"</c>.
    /// </summary>
    public IHtmlContent RenderScripts()
    {
        if (_fragments.Count == 0)
        {
            return HtmlString.Empty;
        }

        using var psb = StringBuilderPool.Instance.Get(out var sb);

        foreach (var fragment in _fragments.SelectMany(x => x.Value))
        {
            var data = fragment.AsJsonObject();

            sb.Append($"<script type=\"application/ld+json\" fragment-type=\"{fragment.Type}\">");
            if (CommonHelper.IsDevEnvironment)
            {
                sb.Append(_jsonOptions.SerializeIndented(data));
            }
            else
            {
                sb.Append(JsonSerializer.Serialize(data, _jsonOptions));
            }
            sb.Append("</script>");
        }

        return new HtmlString(sb.ToString());
    }
}

///// <summary>
///// Builds structured data (JSON-LD) for the current page by collecting <see cref="JsonLdFragment"/> fragments
///// from views, partials and components, then rendering them as consolidated script blocks.
///// </summary>
//public class JsonLdBuilder
//{
//    private static readonly JsonSerializerOptions _jsonOptions = new()
//    {
//        WriteIndented = false,
//        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
//        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//    };

//    private readonly Dictionary<string, JsonLdFragment> _fragments = new(StringComparer.OrdinalIgnoreCase);

//    /// <inheritdoc cref="Fragment(string)" />
//    public JsonLdFragment this[string type] 
//        => Fragment(type);

//    /// <summary>
//    /// Gets or creates the <see cref="JsonLdFragment"/> fragment for the given schema.org @type.
//    /// Multiple callers accessing the same type receive the same instance and contribute via deep merge.
//    /// </summary>
//    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
//    public JsonLdFragment Fragment(string type)
//    {
//        Guard.NotEmpty(type);

//        if (!_fragments.TryGetValue(type, out var fragment))
//        {
//            fragment = JsonLdFragment.CreateTopLevel(type);
//            _fragments[type] = fragment;
//        }

//        return fragment;
//    }

//    /// <summary>Gets or creates the <c>Product</c> fragment.</summary>
//    public JsonLdFragment Product 
//        => this["Product"];

//    /// <summary>Gets or creates the <c>CollectionPage</c> fragment.</summary>
//    public JsonLdFragment CollectionPage
//    => this["CollectionPage"];

//    /// <summary>Gets or creates the <c>ItemList</c> fragment.</summary>
//    public JsonLdFragment ItemList
//        => this["ItemList"];

//    /// <summary>Gets or creates the <c>BreadcrumbList</c> fragment.</summary>
//    public JsonLdFragment BreadcrumbList 
//        => this["BreadcrumbList"];

//    /// <summary>Gets or creates the <c>BlogPosting</c> fragment.</summary>
//    public JsonLdFragment BlogPosting 
//        => this["BlogPosting"];

//    /// <summary>Gets or creates the <c>Organization</c> fragment.</summary>
//    public JsonLdFragment Organization 
//        => this["Organization"];

//    /// <summary>Gets or creates the <c>WebSite</c> fragment.</summary>
//    public JsonLdFragment WebSite
//        => this["WebSite"];

//    /// <summary>
//    /// Gets a value indicating whether any JSON-LD fragments have been registered.
//    /// </summary>
//    public bool HasFragments 
//        => _fragments.Count > 0;

//    /// <summary>
//    /// Gets all currently registered fragments as a read-only sequence.
//    /// Does not create new fragments. Useful for inspection, testing, or conditional logic.
//    /// </summary>
//    public IReadOnlyCollection<JsonLdFragment> Fragments 
//        => _fragments.Values;

//    /// <summary>
//    /// Returns the fragment for the given schema.org @type if it has already been registered,
//    /// without creating a new one.
//    /// </summary>
//    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
//    /// <param name="fragment">The fragment if found; otherwise <see langword="null"/>.</param>
//    /// <returns><see langword="true"/> if the fragment exists; otherwise <see langword="false"/>.</returns>
//    public bool TryGetFragment(string type, out JsonLdFragment? fragment)
//        => _fragments.TryGetValue(type, out fragment);

//    /// <summary>
//    /// Renders all collected JSON-LD fragments as &lt;script type="application/ld+json"&gt; blocks.
//    /// Each distinct @type produces a separate script block with <c>"@context": "https://schema.org/"</c>.
//    /// </summary>
//    public IHtmlContent RenderScripts()
//    {
//        if (_fragments.Count == 0)
//        {
//            return HtmlString.Empty;
//        }

//        using var psb = StringBuilderPool.Instance.Get(out var sb);

//        foreach (var fragment in _fragments.Values)
//        {
//            var data = fragment.AsJsonObject();
            
//            sb.Append($"<script type=\"application/ld+json\" fragment-type=\"{fragment.Type}\">");
//            if (CommonHelper.IsDevEnvironment)
//            {
//                sb.Append(_jsonOptions.SerializeIndented(data));
//            }
//            else
//            {
//                sb.Append(JsonSerializer.Serialize(data, _jsonOptions));
//            }
//            sb.Append("</script>");
//        }

//        return new HtmlString(sb.ToString());
//    }
//}
