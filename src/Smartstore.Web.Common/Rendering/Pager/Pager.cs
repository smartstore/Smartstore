using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore.Web.Rendering.Pager
{
    public class Pager : NavigationItem
    {
        private ILocalizationService _localizationService;

        private string _firstButtonText;
        private string _lastButtonText;
        private string _nextButtonText;
        private string _previousButtonText;
        private string _currentPageText;

        public Pager()
            : this(EngineContext.Current.Scope.Resolve<ILocalizationService>())
        {
        }

        public Pager(ILocalizationService localizationService)
            : base()
        {
            _localizationService = localizationService;
        }

        /// <summary>
        /// Gets or sets the first button text
        /// </summary>
        public string FirstButtonText
        {
            get => (!string.IsNullOrEmpty(_firstButtonText)) ?
                    _firstButtonText :
                    _localizationService.GetResource("Pager.First");
            set => _firstButtonText = value;
        }

        /// <summary>
        /// Gets or sets the last button text
        /// </summary>
        public string LastButtonText
        {
            get => (!string.IsNullOrEmpty(_lastButtonText)) ?
                    _lastButtonText :
                    _localizationService.GetResource("Pager.Last");
            set => _lastButtonText = value;
        }

        /// <summary>
        /// Gets or sets the next button text
        /// </summary>
        public string NextButtonText
        {
            get => (!string.IsNullOrEmpty(_nextButtonText)) ?
                    _nextButtonText :
                    _localizationService.GetResource("Pager.Next");
            set => _nextButtonText = value;
        }

        /// <summary>
        /// Gets or sets the previous button text
        /// </summary>
        public string PreviousButtonText
        {
            get => (!string.IsNullOrEmpty(_previousButtonText)) ?
                    _previousButtonText :
                    _localizationService.GetResource("Pager.Previous");
            set => _previousButtonText = value;
        }

        /// <summary>
        /// Gets or sets the current page text
        /// </summary>
        public string CurrentPageText
        {
            get => (!string.IsNullOrEmpty(_currentPageText)) ?
                    _currentPageText :
                    _localizationService.GetResource("Pager.CurrentPage");
            set => _currentPageText = value;
        }
    }
}
