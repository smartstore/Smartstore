using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;
using Smartstore.Threading;

namespace Smartstore.Web.Modelling
{
    public abstract partial class ModelBase
    {
        private readonly static ContextState<Dictionary<ModelBase, IDictionary<string, object>>> _contextState = new("ModelBase.CustomContextProperties");

        public virtual void BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
        }

        /// <summary>
        /// Gets a custom property value either from the thread local or the static storage (in this particular order)
        /// </summary>
        /// <typeparam name="TProperty">Type of property</typeparam>
        /// <param name="key">Custom property key</param>
        /// <returns>The property value or null</returns>
        public TProperty Get<TProperty>(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            if (TryGetCustomContextProperties(false, out var dict) && dict.TryGetValue(key, out var value))
            {
                return (TProperty)value;
            }

            if (CustomProperties.TryGetValue(key, out value))
            {
                return (TProperty)value;
            }

            return default;
        }

        /// <summary>
        /// Use this property to store any custom value for your models. 
        /// </summary>
        [ValidateNever]
        public CustomPropertiesDictionary CustomProperties { get; set; } = new();

        /// <summary>
        /// A data bag for custom model properties which only
        /// lives during a thread/request lifecycle
        /// </summary>
        /// <remarks>
        /// Use thread properties whenever you need to persist request-scoped data,
        /// but the model is potentially cached statically.
        /// </remarks>
        [IgnoreDataMember]
        public IDictionary<string, object> CustomContextProperties
        {
            get
            {
                TryGetCustomContextProperties(true, out var dict);
                return dict;
            }
        }

        private bool TryGetCustomContextProperties(bool create, out IDictionary<string, object> dict)
        {
            dict = null;
            var state = _contextState.Get();

            if (state == null && create)
            {
                state = new Dictionary<ModelBase, IDictionary<string, object>>();
                _contextState.Push(state);
            }

            if (state != null)
            {
                if (!state.TryGetValue(this, out dict))
                {
                    if (create)
                    {
                        dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        state[this] = dict;
                    }
                }

                return dict != null;
            }

            return false;
        }
    }

    public abstract partial class EntityModelBase : ModelBase
    {
        [LocalizedDisplay("Admin.Common.Entity.Fields.Id")]
        public virtual int Id { get; set; }

        /// <remarks>
        /// This property is required for serialization JSON data in grid controls.
        /// Without a lower case Id property in JSON results its AJAX operations do not work correctly.
        /// Occurs since RouteCollection.LowercaseUrls was set to true in Global.asax.
        /// </remarks>
        [JsonProperty("id")]
        internal int EntityId => Id;
    }

    public abstract partial class TabbableModel : EntityModelBase
    {
        public virtual string[] LoadedTabs { get; set; }

        public bool IsTabLoaded(string tabName)
        {
            if (LoadedTabs != null && tabName.HasValue())
            {
                return LoadedTabs.Contains(tabName, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CustomModelPartAttribute : Attribute
    {
    }
}
