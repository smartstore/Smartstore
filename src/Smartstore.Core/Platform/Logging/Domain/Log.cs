using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Logging
{
    /// <summary>
    /// Represents a log level
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 10,
        Information = 20,
        Warning = 30,
        Error = 40,
        Fatal = 50
    }

    /// <summary>
    /// Represents a log record
    /// </summary>
    [Index(nameof(Logger), Name = "IX_Log_Logger")]
    [Index(nameof(LogLevelId), Name = "IX_Log_Level")]
    [Hookable(false)]
    public partial class Log : BaseEntity
    {
        /// <summary>
        /// Gets or sets the log level identifier
        /// </summary>
        public int LogLevelId { get; set; }

        /// <summary>
        /// Gets or sets the short message
        /// </summary>
        [Required, StringLength(4000)]
        public string ShortMessage { get; set; }

        /// <summary>
        /// Gets or sets the full exception
        /// </summary>
        [MaxLength]
        public string FullMessage { get; set; }

        /// <summary>
        /// Gets or sets the IP address
        /// </summary>
        [StringLength(200)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the page URL
        /// </summary>
        [StringLength(1500)]
        public string PageUrl { get; set; }

        /// <summary>
        /// Gets or sets the referrer URL
        /// </summary>
        [StringLength(1500)]
        public string ReferrerUrl { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the logger name
        /// </summary>
        [Required, StringLength(400)]
        public string Logger { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method
        /// </summary>
        [StringLength(10)]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the user name
        /// </summary>
        [StringLength(100)]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the log level
        /// </summary>
        [NotMapped]
        public LogLevel LogLevel
        {
            get => (LogLevel)this.LogLevelId;
            set => this.LogLevelId = (int)value;
        }

        //// TODO: (core) Add "Customer" nav property to "Log" entity.
        ///// <summary>
        ///// Gets or sets the customer
        ///// </summary>
        //public virtual Customer Customer { get; set; }
    }
}
