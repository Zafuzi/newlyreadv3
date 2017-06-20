using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Newtonsoft.Json;
using System.Threading;
using NewlyReadv3.Tools;
using StackExchange.Redis;

namespace NewlyReadv3
{
    public class Program
    {
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1");
        private static IDatabase db = redis.GetDatabase();
        public static void Main(string[] args)
        {
            var rclient = new RestClient("http://newsapi.org/v1/");
            var request = new RestRequest();
            if (db.StringGet("sources").IsNullOrEmpty)
            {
                request = new RestRequest("sources?language=en", Method.GET);
                rclient.ExecuteAsync(request, response =>
                {
                    RedisValue sources = response.Content;
                    db.StringSet("sources", sources);
                });
            }
            var sourcesTimer = new System.Threading.Timer((e) =>
            {
                if (db.StringGet("sources").IsNullOrEmpty)
                {
                    return;
                }
                request = new RestRequest();
                dynamic sources = JsonConvert.DeserializeObject(db.StringGet("sources"));
                var response = new RestResponse();
                foreach(dynamic source in sources.sources){
                    try{
                        request = new RestRequest("articles?apiKey=ccfdc66609fc4b7b87258020b85d4380&source=" + source.id);
                        Task.Run(async () =>
                        {
                            response = await GetResponseContentAsync(db, source, rclient, request) as RestResponse;
                        }).Wait();
                    }catch(Exception b){
                        Console.WriteLine("\n Error converting the source: {0} \n", b);
                    }
                }
                
             }, null, TimeSpan.Zero, TimeSpan.FromMinutes(20));
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }

        public static Task<IRestResponse> GetResponseContentAsync(IDatabase db, dynamic source, RestClient rclient, RestRequest request)
        {
            var tcs = new TaskCompletionSource<IRestResponse>();
            var sw = new Stopwatch();
            var t = rclient.ExecuteAsync(request, response =>
            {
                var sourceKey = string.Format("articles:{0}:{1}", source.category, source.id);
                var article = response.Content;
                db.StringSet(sourceKey, article);
                Console.WriteLine("finished getting source: {0}", StringTools.Concat(response.Content, 100));
                tcs.SetResult(response);
            });
            return tcs.Task;
        }
    }
}
