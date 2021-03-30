using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Smartstore.Web.Modelling.Validation
{
    public abstract class SmartValidator<TModel> : AbstractValidator<TModel> where TModel : class
    {
        protected SmartValidator()
        {
            // TODO: (core) Implement a method to sync entity type validators with model type:
            // determine all (data annotations) validators in entity and try to apply to model type.
        }
    }
}
