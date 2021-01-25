//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Smartstore.Core.Customers;

//namespace Smartstore.Core.Search
//{
//    // TODO: (mg) (core) Put forum specific search stuff to external module Smartstore.Forums
//    public static partial class ForumPostQueryExtensions
//    {
//        public static IQueryable<Customer> ApplyCustomersByNumberOfPostsFilter(this IQueryable<ForumPost> query, int storeId, int minHitCount = 1)
//        {
//            var postQuery = forumPostRepository.TableUntracked
//                .Expand(x => x.Customer)
//                .Expand(x => x.Customer.BillingAddress)
//                .Expand(x => x.Customer.ShippingAddress)
//                .Expand(x => x.Customer.Addresses);

//            if (storeId > 0)
//            {
//                postQuery =
//                    from p in postQuery
//                    join sm in storeMappingRepository.TableUntracked on new { eid = p.ForumTopic.Forum.ForumGroupId, ename = "ForumGroup" } equals new { eid = sm.EntityId, ename = sm.EntityName } into gsm
//                    from sm in gsm.DefaultIfEmpty()
//                    where !p.ForumTopic.Forum.ForumGroup.LimitedToStores || sm.StoreId == storeId
//                    select p;
//            }

//            var groupQuery =
//                from p in postQuery
//                group p by p.CustomerId into grp
//                select new
//                {
//                    Count = grp.Count(),
//                    grp.FirstOrDefault().Customer   // Cannot be null.
//                };

//            groupQuery = minHitCount > 1
//                ? groupQuery.Where(x => x.Count >= minHitCount)
//                : groupQuery;

//            var query = groupQuery
//                .OrderByDescending(x => x.Count)
//                .Select(x => x.Customer)
//                .Where(x => x.CustomerRoleMappings.FirstOrDefault(y => y.CustomerRole.SystemName == SystemCustomerRoleNames.Guests) == null && !x.Deleted && x.Active && !x.IsSystemAccount);

//            return query;
//        }
//    }
//}
