using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Web.Modelling
{
    public static class HtmlHelperModelExtensions
    {
        public static ModelExpression ModelExpressionFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpressionProvider = helper.ViewContext.HttpContext.GetServiceScope().Resolve<IModelExpressionProvider>();
            var modelExpression = modelExpressionProvider.CreateModelExpression(helper.ViewData, expression);

            return modelExpression;
        }
    }
}
