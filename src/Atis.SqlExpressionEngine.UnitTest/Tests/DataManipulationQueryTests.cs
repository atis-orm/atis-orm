﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Atis.SqlExpressionEngine.UnitTest.Tests
{
    [TestClass]
    public class DataManipulationQueryTests : TestBase
    {
        [TestMethod]
        public void Update_query_single_table()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            Expression<Func<int>> expr = () => assets.Update(x => new Asset { SerialNumber = "ABC", Description = "Check" }, x => x.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
update a_1
	set SerialNumber = 'ABC',
		Description = 'Check'
from	Asset as a_1
where	(a_1.SerialNumber = '123')
";
            Test($"Update Query Single Table Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void Update_query_navigation_selected_to_update()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            Expression<Func<int>> expr = () => assets.Update(
                                                        asset => asset.NavItem(),
                                                        asset => new ItemBase { ItemDescription = asset.NavItem().ItemDescription + asset.SerialNumber },
                                                        asset => asset.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
update NavItem_1
	set ItemDescription = (NavItem_1.ItemDescription + a_2.SerialNumber)
from	Asset as a_2
	inner join ItemBase as NavItem_1 on (NavItem_1.ItemId = a_2.ItemId)
where	(a_2.SerialNumber = '123')
";
            Test($"Update Query Multiple Table Navigation Test", queryExpression, expectedResult);
        }


        [TestMethod]
        public void Update_query_with_query_syntax_multiple_tables_joined_with_one_table_selected_to_update()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var items = new Queryable<ItemBase>(this.queryProvider);

            Expression<Func<int>> expr = () => (
                                                from asset in assets
                                                join item in items on asset.ItemId equals item.ItemId
                                                select new { asset, item }                  // joined 2 tables in 1 query
                                               )
                                               .Update(                                     // <- Update query
                                                    ms => ms.item,                          // <- which table to update
                                                    ms => new ItemBase                      // <- which fields to update
                                                    {
                                                        ItemDescription = ms.item.ItemDescription + ms.asset.SerialNumber
                                                    },
                                                    ms => ms.asset.SerialNumber == "123"    // <- where condition
                                                );

            var queryExpression = expr.Body;
            string? expectedResult = @"
update a_1
	set ItemDescription = (a_1.ItemDescription + a_2.SerialNumber)
from	Asset as a_2
	inner join ItemBase as a_1 on (a_2.ItemId = a_1.ItemId)
where	(a_2.SerialNumber = '123')
";
            Test($"Update Query Multiple Table Join Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void Update_query_using_From_query_extension_method_then_finally_navigation_property_selected_to_update()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var items = new Queryable<ItemBase>(this.queryProvider);
            Expression<Func<int>> expr = () => queryProvider.From(() => new { asset = QueryExtensions.Table<Asset>(), item = QueryExtensions.Table<ItemBase>() })
                                                    .InnerJoin(x => x.item, x => x.asset.ItemId == x.item.ItemId)
                                                    .Select(x => x.item)
                                                    .Update(ms => ms.NavItemMoreInfo(), ms => new ItemMoreInfo { TrackingType = "TT" }, ms => ms.ItemDescription.Contains("123"));
            var queryExpression = expr.Body;
            string? expectedResult = @"
update NavItemMoreInfo_1
	set TrackingType = 'TT'
from	(
	select	a_3.ItemId as ItemId, a_3.ItemDescription as ItemDescription
	from	Asset as a_2
		inner join ItemBase as a_3 on (a_2.ItemId = a_3.ItemId)
) as a_4
	left join ItemMoreInfo as NavItemMoreInfo_1 on (a_4.ItemId = NavItemMoreInfo_1.ItemId)
where	(a_4.ItemDescription like ('%' + ('123' + '%')))
";
            Test($"Update Query Multiple Table From Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void Update_query_using_dynamic_joins_to_add_multiple_data_sources_with_multiple_data_source_selected_in_projection_then_updating_one_data_source_from_that_projection()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            var items = new Queryable<ItemBase>(this.queryProvider);
            var moreInfo = new Queryable<ItemMoreInfo>(this.queryProvider);
            Expression<Func<int>> expr = () => assets
                                                    .InnerJoin(items, (assets, joinedTable) => new { a = assets, j1 = joinedTable }, newShape => newShape.a.ItemId == newShape.j1.ItemId)
                                                    .InnerJoin(moreInfo, (oldShape, joinedTable) => new { os1 = oldShape, j2 = joinedTable }, newShape => newShape.j2.ItemId == newShape.os1.j1.ItemId)
                                                    .Select(x => x.os1)
                                                    .Update(ms => ms.j1, ms => new ItemBase { ItemDescription = ms.a.SerialNumber + ms.j1.ItemId }, ms => ms.j1.ItemDescription.Contains("123"));
            var queryExpression = expr.Body;
            string? expectedResult = @"
  update a_1
	set ItemDescription = (a_2.SerialNumber + a_1.ItemId)
from	Asset as a_2
	inner join ItemBase as a_1 on (a_2.ItemId = a_1.ItemId)
	inner join ItemMoreInfo as a_3 on (a_3.ItemId = a_1.ItemId)
where	(a_1.ItemDescription like ('%' + ('123' + '%')))
";
            Test($"Update Query Multiple Table Complex Object For ModelPath Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void Delete_query_single_table()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            Expression<Func<int>> expr = () => assets.Delete(x => x.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
delete a_1
from	Asset as a_1
where	(a_1.SerialNumber = '123')
";
            Test($"Delete Query Single Table Test", queryExpression, expectedResult);
        }

        [TestMethod]
        public void Delete_query_with_navigation_selected_to_delete()
        {
            var assets = new Queryable<Asset>(this.queryProvider);
            Expression<Func<int>> expr = () => assets.Delete(asset => asset.NavItem(),
                                                                asset => asset.SerialNumber == "123");
            var queryExpression = expr.Body;
            string? expectedResult = @"
delete NavItem_1
from	Asset as a_2
	inner join ItemBase as NavItem_1 on (NavItem_1.ItemId = a_2.ItemId)
where	(a_2.SerialNumber = '123')
";


            var employees = new Queryable<Employee>(this.queryProvider);

            var q = employees
                    .Select(x => new
                    {
                        C1 = x.EmployeeId,
                        SubordinateCount = x.NavSubOrdinates.Count()
                    });


            Test($"Delete Query Multiple Table Navigation Test", queryExpression, expectedResult);
        }

    }
}
