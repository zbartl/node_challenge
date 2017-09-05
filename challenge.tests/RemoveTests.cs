using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Neo4j.Driver.V1;
using System.Linq;
using Shouldly;

namespace challenge.tests
{
    [TestClass]
    public class RemoveTests
    {
        [TestMethod]
        public async Task Should_only_remove_one_node()
        {
            var builder = new WebHostBuilder().UseStartup(typeof(Startup));

            var server = new TestServer(builder);

            var client = server.CreateClient();

            using (var driver = GraphDatabase.Driver("bolt://hobby-ilkndjgcjildgbkeejcngapl.dbs.graphenedb.com:24786", AuthTokens.Basic("neo4j", "b.jSzmmKQQak2w.adk8es1bWYoBSXUt")))
            using (var session = driver.Session())
            {
                var beforeCountResult = session.Run(
                    @"
                        MATCH (n)
                        RETURN count(n) as count
                    ");
                var beforeCount = beforeCountResult.First()["count"].As<int>();

                var result = await client.PostAsync("/api/remove", new StringContent(""));

                var afterCountResult = session.Run(
                    @"
                        MATCH (n)
                        RETURN count(n) as count
                    ");
                var afterCount = afterCountResult.First()["count"].As<int>();

                afterCount.ShouldBe(beforeCount - 1);
            }
        }
    }
}
