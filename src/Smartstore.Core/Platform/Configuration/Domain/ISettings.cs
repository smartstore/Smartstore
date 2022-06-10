using Smartstore.ComponentModel;

namespace Smartstore.Core.Configuration
{
    /// <summary>
    /// Marker interface for setting classes.
    /// </summary>
    public interface ISettings : ICloneable<ISettings>
    {
        /// <inheritdoc/>
        ISettings ICloneable<ISettings>.Clone()
            => ((ICloneable)this).Clone() as ISettings;

        /// <inheritdoc/>
        object ICloneable.Clone()
        {
            var clone = Activator.CreateInstance(this.GetType());
            MiniMapper.Map(this, clone);
            return clone;
        }
    }
}
