using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Events;

namespace Smartstore.Web
{
    internal partial class WebCacheInvalidator : AsyncDbSaveHook<BaseEntity>, IConsumer
    {
        // TODO: (core) Implement WebCacheInvalidator (formerly FrameworkCacheConsumer)

        #region Consts

        /// <summary>
        /// Key for ThemeVariables caching
        /// </summary>
        /// <remarks>
        /// {0} : theme name
        /// {1} : store identifier
        /// </remarks>
        public const string THEMEVARS_KEY = "web:themevars-{0}-{1}";
        public const string THEMEVARS_THEME_KEY = "web:themevars-{0}";

        /// <summary>
        /// Key for tax display type caching
        /// </summary>
        /// <remarks>
        /// {0} : customer role ids
        /// {1} : store identifier
        /// </remarks>
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY = "web:customerroles:taxdisplaytypes-{0}-{1}";
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY = "web:customerroles:taxdisplaytypes*";

        #endregion
    }
}
