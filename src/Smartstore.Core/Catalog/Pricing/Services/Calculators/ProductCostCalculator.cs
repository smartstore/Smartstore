//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Smartstore.Core.Catalog.Attributes;
//using Smartstore.Core.Data;

//namespace Smartstore.Core.Catalog.Pricing.Calculators
//{
//    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Late)]
//    public class ProductCostCalculator : IPriceCalculator
//    {
//        private readonly SmartDbContext _db;

//        public ProductCostCalculator(SmartDbContext db)
//        {
//            _db = db;
//        }

//        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
//        {
//            if (!context.Options.DetermineProductCost)
//            {
//                // Proceed with pipeline and omit this calculator, it is made for product cost calculation only.
//                await next(context);
//                return;
//            }

//            var product = context.Product;
//            var productLinkageValues = new List<ProductVariantAttributeValue>();
//            var attributes = await context.Options.BatchContext.Attributes.GetOrLoadAsync(product.Id);

//            context.ProductCost = product.ProductCost;

//            // Apply cost of products linked through selected attribute values.
//            var selections = context.SelectedAttributes
//                .Where(x => x.ProductId == product.Id && x.BundleItemId == context.BundleItem?.Item?.Id)
//                .Select(x => x.Selection)
//                .ToList();

//            foreach (var selection in selections)
//            {
//                var selectedValues = selection.MaterializeProductVariantAttributeValues(attributes);

//                productLinkageValues.AddRange(
//                    selectedValues.Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0));
//            }

//            var linkedProductIds = productLinkageValues
//                .Select(x => x.LinkedProductId)
//                .Distinct()
//                .ToArray();

//            if (linkedProductIds.Any())
//            {
//                var linkedProducts = await _db.Products
//                    .AsNoTracking()
//                    .Where(x => linkedProductIds.Contains(x.Id))
//                    .Select(x => new { x.Id, x.ProductCost })
//                    .ToDictionaryAsync(x => x.Id, x => x.ProductCost);

//                foreach (var value in productLinkageValues)
//                {
//                    if (linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProductCost))
//                    {
//                        context.ProductCost += linkedProductCost * value.Quantity;
//                    }
//                }
//            }

//            await next(context);
//        }
//    }
//}
