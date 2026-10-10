using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Captures source descriptors and streams sprites for both conceptual kits and complete variants.
/// </summary>
internal sealed class IconSprite
{
    private readonly IconCatalog _catalog;
    private readonly List<(string Concept, IconInfo Info, IconCatalog.Variant Variant, IconCatalog.Source Source)> _entries = [];

    /// <summary>
    /// Gets the output revision, including mapping, membership, source and preparation changes.
    /// </summary>
    internal string Revision { get; }

    /// <summary>
    /// Resolves sources and computes a stable revision without parsing their drawing.
    /// </summary>
    /// <param name="catalog">The owning source generation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    /// <param name="kit">The configured kit name.</param>
    /// <param name="entries">Concepts and their actual mapped names.</param>
    internal IconSprite(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant,
        string kit, IEnumerable<(string Concept, IconInfo Info, IconCatalog.Variant Variant)> entries)
    {
        _catalog = catalog;
        // Bump the sprite version whenever symbol preparation changes.
        var fingerprint = new StringBuilder("sprite-7:").Append(IconSvgParser.Revision);
        // System identities prevent ambiguous flat filenames and distinguish empty kits.
        foreach (var identity in new[] { library.Manifest.SystemName, variant.Manifest.Name, kit })
        {
            fingerprint.Append('|').Append(identity.Length).Append(':').Append(identity);
        }
        foreach (var entry in entries)
        {
            var source = entry.Variant.GetSource(entry.Info.Name)
                ?? throw new InvalidDataException($"Icon kit '{kit}': concept '{entry.Concept}' targets missing icon '{entry.Info.Name}' in {entry.Info.LibraryName}/{entry.Variant.Manifest.Name}.");
            var info = entry.Info;
            _entries.Add((entry.Concept, info, entry.Variant, source));
            fingerprint.Append('|').Append(entry.Concept.Length).Append(':').Append(entry.Concept)
                .Append('|').Append(info.Address.Length).Append(':').Append(info.Address)
                .Append('|').Append(source.Revision)
                .Append('|').Append(info.Transform.RevisionKey)
                .Append('|').Append(info.StrokeScale.ToString("R", CultureInfo.InvariantCulture));
        }

        // A 96-bit content fingerprint keeps URLs and filenames compact.
        // This is a cache identity, not a security token.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString()));
        Revision = Convert.ToHexStringLower(hash.AsSpan(0, 12));
    }

    /// <summary>
    /// Publishes this sprite once without retaining artwork or filling the individual icon cache.
    /// </summary>
    /// <param name="path">The immutable destination file.</param>
    internal Task<string> PublishAsync(string path, CancellationToken cancelToken)
        => IconFileCache.PublishAsync(path, (stream, token) =>
        {
            Write(stream, token);
            return ValueTask.CompletedTask;
        }, cancelToken);

    /// <summary>
    /// Writes the sprite incrementally without retaining the full document or populating the individual icon cache.
    /// Each actual source is prepared once; additional concepts reference its symbol.
    /// </summary>
    /// <param name="stream">The destination stream, left open for the caller.</param>
    internal void Write(Stream stream, CancellationToken cancelToken)
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = true,
            CloseOutput = false
        });
        writer.WriteStartElement("svg", ns.NamespaceName);
        // Count metadata only; artwork is still read one source at a time.
        var sourceCounts = _entries.GroupBy(x => (x.Info.Address, x.Info.StrokeScale))
            .ToDictionary(x => x.Key, x => x.Count());
        int sourceIndex = 0;
        var rendered = new Dictionary<(string Address, double StrokeScale), (string Symbol, string ViewBox, string AspectRatio)>();
        // Process each variant as a group so only one archive is open at a time.
        // Readers never escape this write operation, including when preparation fails.
        foreach (var group in _entries.GroupBy(x => x.Variant))
        {
            using var sources = group.Key.OpenReader();
            foreach (var entry in group)
            {
                cancelToken.ThrowIfCancellationRequested();
                XElement artwork = null;
                var drawingKey = (entry.Info.Address, entry.Info.StrokeScale);
                bool shared = sourceCounts[drawingKey] > 1;
                if (!rendered.TryGetValue(drawingKey, out var original))
                {
                    var sourceId = "source:" + (sourceIndex++).ToString(CultureInfo.InvariantCulture);
                    var drawing = sources.Read(entry.Info, entry.Source);
                    if (drawing == null)
                    {
                        // Missing source and default viewBox: omit this symbol without failing the kit.
                        continue;
                    }

                    // Reuse the prepared tree directly. Only the public symbol keeps viewport
                    // attributes; the source root becomes a group preserving all other attributes.
                    var viewBox = (string)drawing.Attribute("viewBox");
                    var aspectRatio = (string)drawing.Attribute("preserveAspectRatio");
                    drawing.Attribute("viewBox").Remove();
                    drawing.Attribute("preserveAspectRatio")?.Remove();
                    drawing.Name = ns + "g";

                    // Prefix with a character forbidden in conceptual addresses, avoiding collisions
                    // between source IDs and the public concept symbol IDs.
                    var prefix = sourceId + ":";
                    var ids = drawing.DescendantsAndSelf().Attributes("id")
                        .ToDictionary(x => x.Value, x => prefix + x.Value, StringComparer.Ordinal);
                    foreach (var element in drawing.DescendantsAndSelf())
                    {
                        element.Name = ns + element.Name.LocalName;
                        foreach (var attribute in element.Attributes())
                        {
                            if (attribute.Name.LocalName == "id")
                            {
                                attribute.Value = ids[attribute.Value];
                            }
                            else if (attribute.Name.LocalName == "href" && attribute.Value.StartsWith('#'))
                            {
                                attribute.Value = "#" + ids.GetValueOrDefault(attribute.Value[1..], prefix + attribute.Value[1..]);
                            }
                            else if (attribute.Name.LocalName == "aria-labelledby")
                            {
                                attribute.Value = string.Join(" ", attribute.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => ids.GetValueOrDefault(x, prefix + x)));
                            }
                            else
                            {
                                attribute.Value = Regex.Replace(attribute.Value, @"url\(#([^)]+)\)",
                                    m => "url(#" + ids.GetValueOrDefault(m.Groups[1].Value, prefix + m.Groups[1].Value) + ")");
                            }
                        }
                    }

                    if (entry.Info.StrokeScale != 1)
                    {
                        var multiplier = entry.Info.StrokeScale.ToString("R", CultureInfo.InvariantCulture);
                        foreach (var style in drawing.DescendantsAndSelf().Attributes("style"))
                        {
                            style.Value = IconSvgParser.ScaleStrokeWidths(style.Value, multiplier);
                        }
                    }

                    original = (sourceId, viewBox, aspectRatio);
                    if (shared)
                    {
                        // Reuse a group, not a nested symbol viewport, to avoid applying
                        // viewBox offsets twice. The inner group preserves source root attributes.
                        var source = new XElement(ns + "g", new XAttribute("id", sourceId), drawing);
                        new XElement(ns + "defs", source).WriteTo(writer);
                        rendered.Add(drawingKey, original);
                    }
                    else
                    {
                        artwork = drawing;
                    }
                }

                var conceptSymbol = new XElement(ns + "symbol", new XAttribute("id", entry.Concept),
                    new XAttribute("viewBox", original.ViewBox));
                if (original.AspectRatio != null)
                {
                    conceptSymbol.SetAttributeValue("preserveAspectRatio", original.AspectRatio);
                }

                var content = artwork ?? new XElement(ns + "use", new XAttribute("href", "#" + original.Symbol));
                conceptSymbol.Add(entry.Info.Transform.IsIdentity
                    ? content
                    : new XElement(ns + "g", new XAttribute("transform", entry.Info.Transform.ToSvg(original.ViewBox)), content));
                conceptSymbol.WriteTo(writer);
            }
        }

        if (_catalog.ChangeToken.HasChanged)
        {
            throw new IOException("Icon sources changed during sprite preparation. Retry after catalog reload.");
        }

        writer.WriteEndElement();
        writer.Flush();
    }
}