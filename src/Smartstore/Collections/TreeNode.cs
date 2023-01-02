#nullable enable

using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Smartstore.Collections.JsonConverters;

namespace Smartstore.Collections
{
    [JsonConverter(typeof(TreeNodeJsonConverter))]
    public class TreeNode<TValue> : TreeNodeBase<TreeNode<TValue>>
    {
        public TreeNode(TValue value)
        {
            Value = Guard.NotNull(value, nameof(value));
        }

        public TreeNode(TValue value, IEnumerable<TValue>? children)
            : this(value)
        {
            if (children != null && children.Any())
            {
                AppendRange(children);
            }
        }

        public TreeNode(TValue value, IEnumerable<TreeNode<TValue>>? children)
            : this(value)
        {
            // for serialization
            if (children != null && children.Any())
            {
                AppendRange(children);
            }
        }

        public TValue Value { get; }

        protected override TreeNode<TValue> CreateInstance()
        {
            TValue value = this.Value;

            if (value is ICloneable<TValue> clone)
            {
                value = clone.Clone();
            }

            var clonedNode = new TreeNode<TValue>(value);

            // Assign or clone Metadata
            if (_metadata != null && _metadata.Count > 0)
            {
                foreach (var kvp in _metadata)
                {
                    var metadataValue = kvp.Value is ICloneable cloneable
                        ? cloneable.Clone()
                        : kvp.Value;
                    clonedNode.SetMetadata(kvp.Key, metadataValue);
                }
            }

            if (_id != null)
            {
                clonedNode._id = _id;
            }

            return clonedNode;
        }

        public TreeNode<TValue> Append(TValue value, object? id = null)
        {
            Guard.NotNull(value, nameof(value));

            var node = new TreeNode<TValue>(value)
            {
                _id = id
            };

            Append(node);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendRange(IEnumerable<TValue> values)
        {
            foreach (var value in values)
            {
                Append(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendRange(IEnumerable<TValue> values, Func<TValue, object> idSelector)
        {
            Guard.NotNull(values, nameof(values));
            Guard.NotNull(idSelector, nameof(idSelector));

            foreach (var value in values)
            {
                Append(value, idSelector(value));
            }
        }

        public TreeNode<TValue> Prepend(TValue value, object? id = null)
        {
            Guard.NotNull(value, nameof(value));

            var node = new TreeNode<TValue>(value)
            {
                _id = id
            };

            Prepend(node);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TValue> Flatten(bool includeSelf = true)
        {
            return Flatten(null, includeSelf);
        }

        public IEnumerable<TValue> Flatten(Func<TValue, bool>? predicate, bool includeSelf = true)
        {
            var list = includeSelf 
                ? new[] { Value } 
                : Enumerable.Empty<TValue>();

            if (!HasChildren)
            {
                return list;
            }  

            var result = list.Union(Children.SelectMany(x => x.Flatten()));
            if (predicate != null)
            {
                result = result.Where(predicate);
            }

            return result;
        }
    }
}
