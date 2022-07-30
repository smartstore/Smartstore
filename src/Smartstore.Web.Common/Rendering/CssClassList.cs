using System.Collections;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.Rendering
{
    public sealed class CssClassList : Disposable, IEnumerable<string>
    {
        private readonly HashSet<string> _list;
        private readonly object _source;

        internal CssClassList(object source)
        {
            Guard.NotNull(source, nameof(source));

            string currentValue = null;

            if (source is TagHelperAttributeList list)
            {
                if (list.TryGetAttribute("class", out var attribute))
                {
                    currentValue = attribute.Value?.ToString();
                }
            }
            else if (source is IDictionary<string, string> dict)
            {
                dict.TryGetValue("class", out currentValue);
            }
            else
            {
                throw new ArgumentException($"source must be {nameof(TagHelperAttributeList)} or {nameof(IDictionary<string, string>)}", nameof(source));
            }

            _list = new HashSet<string>(currentValue.HasValue()
                ? currentValue.Trim().Tokenize(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) :
                Enumerable.Empty<string>());

            _source = source;
        }

        public int Count
            => _list.Count;

        public void Clear()
            => _list.Clear();

        public bool Contains(string classValue)
            => _list.Contains(classValue);

        public IEnumerator<string> GetEnumerator()
            => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _list.GetEnumerator();

        public bool Add(params string[] classValues)
        {
            int len = _list.Count;

            foreach (var classValue in classValues)
            {
                ValidateClass(classValue, nameof(classValues));

                if (classValue.HasValue())
                {
                    _list.Add(classValue);
                }
            }

            return _list.Count != len;
        }

        public bool Remove(params string[] classValues)
        {
            int len = _list.Count;

            foreach (var classValue in classValues)
            {
                ValidateClass(classValue, nameof(classValues));

                if (classValue.HasValue())
                {
                    _list.Remove(classValue);
                }
            }

            return _list.Count != len;
        }

        public bool Replace(string oldClass, string newClass)
        {
            Guard.NotEmpty(oldClass, nameof(oldClass));
            Guard.NotEmpty(newClass, nameof(newClass));

            ValidateClass(oldClass, nameof(oldClass));
            ValidateClass(newClass, nameof(newClass));

            if (_list.Remove(oldClass))
            {
                _list.Add(newClass);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds or removes a class, depending on either the class's presence or the value of the <paramref name="state"/> argument.
        /// </summary>
        /// <param name="classValue">Class to be toggled</param>
        /// <param name="state">A boolean value to determine whether the class should be added or removed.</param>
        public bool Toggle(string classValue, bool? state = null)
        {
            Guard.NotEmpty(classValue, nameof(classValue));
            ValidateClass(classValue, nameof(classValue));

            if (state.HasValue)
            {
                return state == true ? Add(classValue) : Remove(classValue);
            }

            if (_list.Remove(classValue))
            {
                return false;
            }
            else
            {
                _list.Add(classValue);
                return true;
            }
        }

        public void ApplyTo(TagHelperAttributeList target)
        {
            Guard.NotNull(target, nameof(target));

            if (_list.Count == 0)
            {
                target.RemoveAll("class");
            }
            else
            {
                target.SetAttribute("class", string.Join(' ', _list));
            }
        }

        public void ApplyTo(IDictionary<string, string> target)
        {
            Guard.NotNull(target, nameof(target));

            if (_list.Count == 0)
            {
                target.TryRemove("class", out _);
            }
            else
            {
                target["class"] = string.Join(' ', _list);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (_source is TagHelperAttributeList list)
                {
                    ApplyTo(list);
                }
                else if (_source is IDictionary<string, string> dict)
                {
                    ApplyTo(dict);
                }
            }
        }

        private static void ValidateClass(string value, string paramName)
        {
            if (value.Any(c => Char.IsWhiteSpace(c)))
            {
                throw new ArgumentException($"The class provided ('{value}') contains whitespace characters, which is not valid", paramName);
            }
        }
    }
}
