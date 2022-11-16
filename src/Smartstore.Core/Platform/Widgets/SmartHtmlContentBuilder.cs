using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// A custom <see cref="HtmlContentBuilder"/> implementation that exposes
    /// the <c>Entries</c> property for faster emptyness checks.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public class SmartHtmlContentBuilder : IHtmlContentBuilder
    {
        private object _singleContent;
        private bool _isSingleContentSet;
        private bool _hasContent;
        private List<object> _entries;

        /// <summary>
        /// Gets the number of elements in the <see cref="SmartHtmlContentBuilder"/>.
        /// </summary>
        public int Count => _entries != null ? _entries.Count : 0;

        /// <summary>
        /// The list of content entries.
        /// </summary>
        public IList<object> Entries
        {
            get
            {
                if (_entries == null)
                {
                    _entries = new List<object>();
                }

                if (_isSingleContentSet)
                {
                    Debug.Assert(_entries.Count == 0);

                    _entries.Add(_singleContent);
                    _isSingleContentSet = false;
                }

                return _entries;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the content is empty or whitespace.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> for a cleared <see cref="SmartHtmlContentBuilder"/>.
        /// </remarks>
        public bool IsEmptyOrWhiteSpace
        {
            get
            {
                if (!_hasContent)
                {
                    return true;
                }

                if (_isSingleContentSet)
                {
                    return IsEmptyOrWhiteSpaceCore(_singleContent);
                }

                for (var i = 0; i < (_entries?.Count ?? 0); i++)
                {
                    if (!IsEmptyOrWhiteSpaceCore(Entries[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Append(string unencoded) 
            => AppendCore(unencoded);

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(IHtmlContent htmlContent)
            => AppendCore(htmlContent);

        /// <inheritdoc />
        public IHtmlContentBuilder AppendHtml(string encoded)
        {
            if (encoded == null)
            {
                return AppendCore(null);
            }

            return AppendCore(new HtmlString(encoded));
        }

        /// <inheritdoc />
        public IHtmlContentBuilder Clear()
        {
            _hasContent = false;
            _isSingleContentSet = false;
            _entries?.Clear();
            return this;
        }

        /// <inheritdoc />
        public void CopyTo(IHtmlContentBuilder destination)
        {
            Guard.NotNull(destination, nameof(destination));

            if (!_hasContent)
            {
                return;
            }

            if (_isSingleContentSet)
            {
                CopyToCore(_singleContent, destination);
            }
            else
            {
                for (var i = 0; i < (_entries?.Count ?? 0); i++)
                {
                    CopyToCore(Entries[i], destination);
                }
            }
        }

        /// <inheritdoc />
        public void MoveTo(IHtmlContentBuilder destination)
        {
            Guard.NotNull(destination, nameof(destination));

            if (!_hasContent)
            {
                return;
            }

            if (_isSingleContentSet)
            {
                MoveToCore(_singleContent, destination);
            }
            else
            {
                for (var i = 0; i < (_entries?.Count ?? 0); i++)
                {
                    MoveToCore(Entries[i], destination);
                }
            }

            Clear();
        }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            Guard.NotNull(writer, nameof(writer));
            Guard.NotNull(encoder, nameof(encoder));

            if (!_hasContent)
            {
                return;
            }

            if (_isSingleContentSet)
            {
                WriteToCore(_singleContent, writer, encoder);
                return;
            }

            for (var i = 0; i < (_entries?.Count ?? 0); i++)
            {
                WriteToCore(Entries[i], writer, encoder);
            }
        }

        private static bool IsEmptyOrWhiteSpaceCore(object entry)
        {
            if (entry == null)
            {
                return true;
            }

            if (entry is string stringValue)
            {
                // Do not encode the string because encoded value remains whitespace from user's POV.
                return string.IsNullOrWhiteSpace(stringValue);
            }
            else if (entry is IHtmlContent htmlContent)
            {
                return !htmlContent.HasContent();
            }

            return true;
        }

        private static void WriteToCore(object entry, TextWriter writer, HtmlEncoder encoder)
        {
            if (entry == null)
            {
                return;
            }

            if (entry is string stringValue)
            {
                encoder.Encode(writer, stringValue);
            }
            else
            {
                ((IHtmlContent)entry).WriteTo(writer, encoder);
            }
        }

        private static void CopyToCore(object entry, IHtmlContentBuilder destination)
        {
            if (entry == null)
            {
                return;
            }

            if (entry is string entryAsString)
            {
                destination.Append(entryAsString);
            }
            else if (entry is IHtmlContentContainer entryAsContainer)
            {
                entryAsContainer.CopyTo(destination);
            }
            else
            {
                destination.AppendHtml((IHtmlContent)entry);
            }
        }

        private static void MoveToCore(object entry, IHtmlContentBuilder destination)
        {
            if (entry == null)
            {
                return;
            }

            if (entry is string entryAsString)
            {
                destination.Append(entryAsString);
            }
            else if (entry is IHtmlContentContainer entryAsContainer)
            {
                entryAsContainer.MoveTo(destination);
            }
            else
            {
                destination.AppendHtml((IHtmlContent)entry);
            }
        }

        private SmartHtmlContentBuilder AppendCore(object entry)
        {
            if (entry == null)
            {
                return this;
            }
            
            if (!_hasContent)
            {
                _isSingleContentSet = true;
                _singleContent = entry;
            }
            else
            {
                Entries.Add(entry);
            }

            _hasContent = true;

            return this;
        }

        private string DebuggerToString()
        {
            using var writer = new StringWriter();
            WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }
    }
}
