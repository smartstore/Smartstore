using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Indexes conceptual memberships for one library/variant and defers source revisions per kit.
/// Owned by the catalog generation; contains no live files or SVG bodies.
/// </summary>
internal sealed class IconKitIndex
{
    /// <summary>
    /// Gets the preferred kit and symbol for each actual icon and mapping transformation.
    /// </summary>
    internal Dictionary<(string Name, IconTransform Transform), (string Kit, string Symbol)> Memberships { get; } = new();

    /// <summary>
    /// Gets deferred source plans keyed by configured kit name.
    /// </summary>
    internal Dictionary<string, Lazy<Plan>> Plans { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Maps concepts without parsing SVG bodies. Shared wins over ordinal kit-name order.
    /// </summary>
    /// <param name="catalog">The owning source generation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    internal IconKitIndex(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
    {
        foreach (var kit in catalog.Kits.Values.OrderBy(x => x.Name == "shared" ? 0 : 1).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            var entries = kit.Icons.OrderBy(x => x, StringComparer.Ordinal)
                .Select(concept => (Concept: concept, Mapping: library.Mapping.GetValueOrDefault(concept) ?? new IconMapping(concept, default))).ToArray();
            foreach (var entry in entries)
            {
                Memberships.TryAdd((entry.Mapping.Name, entry.Mapping.Transform), (kit.Name, entry.Concept));
            }

            Plans.Add(kit.Name, new Lazy<Plan>(() => new Plan(catalog, library, variant, kit.Name, entries)));
        }
    }

    /// <summary>
    /// Captures only the source descriptors required by one kit and fingerprints the final output inputs.
    /// </summary>
    internal sealed class Plan
    {
        private readonly IconCatalog _catalog;
        private readonly IconCatalog.Variant _variant;
        private readonly List<(string Concept, IconInfo Info, IconCatalog.Source Source)> _entries = [];

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
        internal Plan(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant,
            string kit, (string Concept, IconMapping Mapping)[] entries)
        {
            _catalog = catalog;
            _variant = variant;
            // Bump the sprite version whenever symbol preparation changes.
            var fingerprint = new StringBuilder("sprite-3:").Append(IconSvgParser.Revision);
            // System identities prevent ambiguous flat filenames and distinguish empty kits.
            foreach (var identity in new[] { library.Manifest.SystemName, variant.Manifest.Name, kit })
            {
                fingerprint.Append('|').Append(identity.Length).Append(':').Append(identity);
            }
            foreach (var entry in entries)
            {
                var source = variant.GetSource(entry.Mapping.Name)
                    ?? throw new InvalidDataException($"Icon kit '{kit}': concept '{entry.Concept}' targets missing icon '{entry.Mapping.Name}' in {library.Manifest.SystemName}/{variant.Manifest.Name}.");
                var info = IconService.CreateInfo(library, variant, entry.Mapping.Name, false);
                info.Transform = entry.Mapping.Transform;
                _entries.Add((entry.Concept, info, source));
                fingerprint.Append('|').Append(entry.Concept.Length).Append(':').Append(entry.Concept)
                    .Append('|').Append(info.Address.Length).Append(':').Append(info.Address)
                    .Append('|').Append(source.Revision)
                    .Append('|').Append(info.Transform.RevisionKey);
            }

            // A 96-bit content fingerprint keeps URLs and filenames compact.
            // This is a cache identity, not a security token.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString()));
            Revision = Convert.ToHexStringLower(hash.AsSpan(0, 12));
            catalog.KitRevisions.TryAdd(kit + ":" + Revision, this);
        }

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
            using var sources = _variant.OpenReader();
            writer.WriteStartElement("svg", ns.NamespaceName);
            var rendered = new Dictionary<string, (string Symbol, string ViewBox, string AspectRatio)>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                cancelToken.ThrowIfCancellationRequested();
                if (!rendered.TryGetValue(entry.Info.Name, out var original))
                {
                    // Store original artwork once, independent of concept transformations.
                    var sourceId = "source:" + rendered.Count.ToString(CultureInfo.InvariantCulture);
                    var symbol = new XElement(ns + "g", new XAttribute("id", sourceId));
                    string aspectRatio = null;
                    var svg = sources.Prepare(entry.Info, entry.Source, IconSvgParser.Revision + entry.Source.Revision);
                    // This XML tree exists only on a sprite cache miss. Prefix all original IDs
                    // and references before combining unrelated SVG documents into one sprite.
                    var drawing = XElement.Parse("<g>" + svg.Content + "</g>", LoadOptions.PreserveWhitespace);
                    foreach (var attribute in svg.RootAttributes)
                    {
                        if (attribute.Key == "preserveAspectRatio")
                        {
                            aspectRatio = attribute.Value;
                        }
                        else
                        {
                            drawing.SetAttributeValue(attribute.Key, attribute.Value);
                        }
                    }

                    // Prefix with a character forbidden in conceptual addresses, avoiding collisions
                    // between source IDs and the public concept symbol IDs.
                    var prefix = "source:" + rendered.Count.ToString(CultureInfo.InvariantCulture) + ":";
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

                    // The wrapper preserves root transforms, presentation and referenced root IDs.
                    symbol.Add(drawing);
                    // Reuse a group, not a nested symbol viewport: original viewBox offsets
                    // must not translate or scale the artwork a second time through <use>.
                    new XElement(ns + "defs", symbol).WriteTo(writer);
                    original = (sourceId, svg.ViewBox, aspectRatio);
                    rendered.Add(entry.Info.Name, original);
                }

                var conceptSymbol = new XElement(ns + "symbol", new XAttribute("id", entry.Concept),
                    new XAttribute("viewBox", original.ViewBox));
                if (original.AspectRatio != null)
                {
                    conceptSymbol.SetAttributeValue("preserveAspectRatio", original.AspectRatio);
                }

                var use = new XElement(ns + "use", new XAttribute("href", "#" + original.Symbol));
                conceptSymbol.Add(entry.Info.Transform.IsIdentity
                    ? use
                    : new XElement(ns + "g", new XAttribute("transform", entry.Info.Transform.ToSvg(original.ViewBox)), use));
                conceptSymbol.WriteTo(writer);
            }

            if (_catalog.ChangeToken.HasChanged)
            {
                throw new IOException("Icon sources changed during sprite preparation. Retry after catalog reload.");
            }

            writer.WriteEndElement();
            writer.Flush();
        }
    }
}
