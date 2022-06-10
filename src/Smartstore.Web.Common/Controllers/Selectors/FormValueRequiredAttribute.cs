using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Controllers
{
    public class FormValueRequiredAttribute : ActionMethodSelectorAttribute
    {
        private readonly string[] _submitButtonNames;
        private readonly FormValueRequirementOperator _operator;
        private readonly FormValueRequirementMatch _match;
        private readonly bool _inverse;

        public FormValueRequiredAttribute(params string[] submitButtonNames) :
            this(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAny, false, submitButtonNames)
        {
        }

        public FormValueRequiredAttribute(FormValueRequirementOperator requirement, params string[] submitButtonNames)
            : this(requirement, FormValueRequirementMatch.MatchAny, false, submitButtonNames)
        {
        }

        public FormValueRequiredAttribute(FormValueRequirementMatch rule, params string[] submitButtonNames)
            : this(FormValueRequirementOperator.Equal, rule, false, submitButtonNames)
        {
        }

        public FormValueRequiredAttribute(FormValueRequirementOperator requirement, FormValueRequirementMatch rule, params string[] submitButtonNames)
            : this(requirement, rule, false, submitButtonNames)
        {
        }

        protected internal FormValueRequiredAttribute(
            FormValueRequirementOperator requirement,
            FormValueRequirementMatch rule,
            bool inverse,
            params string[] submitButtonNames)
        {
            // At least one submit button should be found (or being absent if 'inverse')
            _submitButtonNames = submitButtonNames;
            _operator = requirement;
            _match = rule;
            _inverse = inverse;
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            if (!routeContext.HttpContext.Request.HasFormContentType)
            {
                return false;
            }

            return IsValidForRequest(routeContext.HttpContext.Request.Form);
        }

        protected internal virtual bool IsValidForRequest(IFormCollection form)
        {
            // For testing purposes
            try
            {
                var isMatch = _match == FormValueRequirementMatch.MatchAny
                    ? _submitButtonNames.Any(x => IsMatch(form, x))
                    : _submitButtonNames.All(x => IsMatch(form, x));

                return isMatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        private bool IsMatch(IFormCollection form, string key)
        {
            string value = string.Empty;

            if (_operator == FormValueRequirementOperator.Equal)
            {
                // Do not iterate because "Invalid request" exception can be thrown
                value = form[key];
            }
            else
            {
                var firstMatch = form.Keys.FirstOrDefault(x => x.StartsWith(key, StringComparison.InvariantCultureIgnoreCase));
                if (firstMatch != null)
                {
                    value = form[firstMatch];
                }
            }

            if (_inverse)
            {
                return value.IsEmpty();
            }

            return value.HasValue();
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FormValueAbsentAttribute : FormValueRequiredAttribute
    {
        public FormValueAbsentAttribute(params string[] submitButtonNames) :
            base(FormValueRequirementOperator.Equal, FormValueRequirementMatch.MatchAny, true, submitButtonNames)
        {
        }

        public FormValueAbsentAttribute(FormValueRequirementOperator requirement, params string[] submitButtonNames)
            : base(requirement, FormValueRequirementMatch.MatchAny, true, submitButtonNames)
        {
        }

        public FormValueAbsentAttribute(FormValueRequirementMatch rule, params string[] submitButtonNames)
            : base(FormValueRequirementOperator.Equal, rule, true, submitButtonNames)
        {
        }

        public FormValueAbsentAttribute(FormValueRequirementOperator requirement, FormValueRequirementMatch rule, params string[] submitButtonNames)
            : base(requirement, rule, true, submitButtonNames)
        {
        }
    }

    public enum FormValueRequirementOperator
    {
        Equal,
        StartsWith
    }

    public enum FormValueRequirementMatch
    {
        MatchAny,
        MatchAll
    }
}
