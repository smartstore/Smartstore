using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Core.Configuration
{
    /// <summary>
    /// Represents a setting entry
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    [Index(nameof(Name))]
    [Index(nameof(StoreId))]
    public partial class Setting : BaseEntity
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [MaxLength]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the store for which this setting is valid. 0 is set when the setting is for all stores
        /// </summary>
        public int StoreId { get; set; }
    }
}
