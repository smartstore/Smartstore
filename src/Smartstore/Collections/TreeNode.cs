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
            Guard.NotNull(value, nameof(value));

            Value = value;
        }

        public TreeNode(TValue value, IEnumerable<TValue> children)
            : this(value)
        {
            if (children != null && children.Any())
            {
                AppendRange(children);
            }
        }

        public TreeNode(TValue value, IEnumerable<TreeNode<TValue>> children)
            : this(value)
        {
            // for serialization
            if (children != null && children.Any())
            {
                AppendRange(children);
            }
        }

        public TValue Value
        {
            get;
            private set;
        }

        protected override TreeNode<TValue> CreateInstance()
        {
            TValue value = this.Value;

            if (value is ICloneable<TValue>)
            {
                value = ((ICloneable<TValue>)value).Clone();
            }

            var clonedNode = new TreeNode<TValue>(value);

            // Assign or clone Metadata
            if (_metadata != null && _metadata.Count > 0)
            {
                foreach (var kvp in _metadata)
                {
                    var metadataValue = kvp.Value is ICloneable
                        ? ((ICloneable)kvp.Value).Clone()
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

        public TreeNode<TValue> Append(TValue value, object id = null)
        {
            var node = new TreeNode<TValue>(value);
            node._id = id;
            this.Append(node);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendRange(IEnumerable<TValue> values)
        {
            values.Each(x => Append(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendRange(IEnumerable<TValue> values, Func<TValue, object> idSelector)
        {
            Guard.NotNull(idSelector, nameof(idSelector));

            values.Each(x => Append(x, idSelector(x)));
        }

        public TreeNode<TValue> Prepend(TValue value, object id = null)
        {
            var node = new TreeNode<TValue>(value);
            node._id = id;
            this.Prepend(node);
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TValue> Flatten(bool includeSelf = true)
        {
            return this.Flatten(null, includeSelf);
        }

        public IEnumerable<TValue> Flatten(Func<TValue, bool> predicate, bool includeSelf = true)
        {
            IEnumerable<TValue> list;
            if (includeSelf)
            {
                list = new[] { this.Value };
            }
            else
            {
                list = Enumerable.Empty<TValue>();
            }

            if (!HasChildren)
                return list;

            var result = list.Union(Children.SelectMany(x => x.Flatten()));
            if (predicate != null)
            {
                result = result.Where(predicate);
            }

            return result;
        }
    }
}
