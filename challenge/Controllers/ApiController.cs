using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver.V1;
using challenge.Models;

namespace challenge.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        [Route("seed")]
        public string Seed()
        {
            for(int i = 0; i < 10; i++)
            {
                AddRandomNode();
            }

            return "complete";
        }

        [Route("nodes")]
        public IActionResult Nodes()
        {
            using (var driver = GraphDatabase.Driver("bolt://hobby-ilkndjgcjildgbkeejcngapl.dbs.graphenedb.com:24786", AuthTokens.Basic("neo4j", "b.jSzmmKQQak2w.adk8es1bWYoBSXUt")))
            using (var session = driver.Session())
            {
                var result =
                    session.Run(
                        @"
                            MATCH (i:IP)-[:ASSIGNED]->(d:Domain)
                            RETURN d.domain as d, collect(i.ip) as ips
                        ");

                var nodes = new List<NodeResult>();
                var relationships = new List<object>();
                int i = 0;
                foreach (var record in result)
                {
                    var target = i;
                    nodes.Add(new NodeResult { title = record["d"].As<string>(), label = "domain" });
                    i += 1;

                    var ips = record["ips"].As<List<string>>();
                    foreach (var ip in ips)
                    {
                        var source = nodes.FindIndex(c => c.title == ip);
                        if (source == -1)
                        {
                            nodes.Add(new NodeResult { title = ip, label = "ip" });
                            source = i;
                            i += 1;
                        }
                        relationships.Add(new { source, target });
                    }
                }
                return Ok(new { nodes, links = relationships });
            }
        }

        [Route("add")]
        [HttpPost]
        public string Add()
        {
            AddRandomNode();
            return "added";
        }

        [Route("remove")]
        [HttpPost]
        public string Remove()
        {
            RemoveRandomNode();
            return "removed";
        }

        private void AddRandomNode()
        {
            using (var driver = GraphDatabase.Driver("bolt://hobby-ilkndjgcjildgbkeejcngapl.dbs.graphenedb.com:24786", AuthTokens.Basic("neo4j", "b.jSzmmKQQak2w.adk8es1bWYoBSXUt")))
            using (var session = driver.Session())
            {
                var random = new Random();

                //ip
                if ((random.Next(0, 100) > 50))
                {
                    var ip = $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}";
                    session.Run(
                        @"
                            CREATE (i:IP {ip: {ip}})
                        ",
                       new Dictionary<string, object> { { "ip", ip } });

                    var domains =
                        session.Run(
                        @"
                            MATCH (d:Domain)
                            RETURN d.domain as d
                        ");

                    foreach (var record in domains)
                    {
                        var existingDomain = record["d"].As<string>();
                        if ((random.Next(0, 100) > 80))
                        {
                            session.Run(
                                @"
                                    MATCH (i:IP { ip : {ip} })
                                    MATCH (d:Domain { domain : {domain} })
                                    CREATE (i)-[:ASSIGNED]->(d)
                                ",
                                new Dictionary<string, object> { { "ip", ip }, { "domain", existingDomain } });
                        }
                    }
                }
                //domain
                else
                {
                    var domain = "www.zachdbo_" + random.Next(1000, 999999) + ".com";
                    session.Run(
                        @"
                            CREATE (d:Domain {domain: {domain}})
                        ",
                       new Dictionary<string, object> { { "domain", domain } });

                    var ips =
                        session.Run(
                        @"
                            MATCH (i:IP)
                            RETURN i.ip as i
                        ");

                    foreach (var record in ips)
                    {
                        var existingIp = record["i"].As<string>();
                        if ((random.Next(0, 100) > 80))
                        {
                            session.Run(
                                @"
                                    MATCH (i:IP { ip : {ip} })
                                    MATCH (d:Domain { domain : {domain} })
                                    CREATE (i)-[:ASSIGNED]->(d)
                                ",
                                new Dictionary<string, object> { { "ip", existingIp }, { "domain", domain } });
                        }
                    }
                }
            }
        }

        private void RemoveRandomNode()
        {
            using (var driver = GraphDatabase.Driver("bolt://hobby-ilkndjgcjildgbkeejcngapl.dbs.graphenedb.com:24786", AuthTokens.Basic("neo4j", "b.jSzmmKQQak2w.adk8es1bWYoBSXUt")))
            using (var session = driver.Session())
            {
                var random = new Random();

                //ip
                if ((random.Next(0, 100) > 50))
                {
                    var countResult = session.Run(
                        @"
                            MATCH (i:IP)
                            RETURN count(i) as count
                        ");
                    var count = countResult.First()["count"].As<int>();

                    if (count > 0)
                    {
                        var removed = session.Run(
                            @"
                                MATCH (i:IP)
                                RETURN ID(i) as id
                                SKIP {offset} LIMIT 1
                            ", 
                            new Dictionary<string, object> { { "offset", new Random().Next(0, count - 1) } }
                            );

                        session.Run(
                            @"
                                MATCH (i:IP)
                                WHERE ID(i) = {id}
                                DETACH DELETE i
                            ", 
                            new Dictionary<string, object> { { "id", removed.FirstOrDefault()["id"] } }
                            );
                    }
                }
                //domain
                else
                {
                    var countResult = session.Run(
                        @"
                            MATCH (d:Domain)
                            RETURN count(d) as count
                        ");
                    var count = countResult.First()["count"].As<int>();

                    if (count > 0)
                    {

                        var removed = session.Run(
                            @"
                                MATCH (d:Domain)
                                RETURN ID(d) as id
                                SKIP {offset} LIMIT 1
                            ",
                            new Dictionary<string, object> { { "offset", new Random().Next(1, count) } }
                            );

                        session.Run(
                            @"
                                MATCH (d:Domain)
                                WHERE ID(d) = {id}
                                DETACH DELETE d
                            ",
                            new Dictionary<string, object> { { "id", removed.FirstOrDefault()["id"] } }
                            );
                    }
                }
            }
        }
    }
}
