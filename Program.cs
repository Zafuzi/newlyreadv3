using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RestSharp;
using ServiceStack.Redis;
using Newtonsoft.Json;
using ServiceStack.Redis.Generic;

namespace NewlyReadv3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var db = new RedisClient())
            {
                var rclient = new RestClient("http://newsapi.org/v1/");
                var request = new RestRequest();
                if (!db.ContainsKey("sources"))
                {
                    request = new RestRequest("sources?language=en", Method.GET);
                    rclient.ExecuteAsync(request, response =>
                    {
                        db.Set("sources", response.Content);
                    });
                }
                var sourcesTimer = new System.Threading.Timer((e) =>
                {
                    Console.WriteLine("\n Updating Articles \n");
                    updateArticles();
                }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            }
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

        private static void updateArticles()
        {
            using (var db = new RedisClient())
            {
                if(!db.ContainsKey("sources")){
                    return;
                }
                var rclient = new RestClient("http://newsapi.org/v1/");
                var request = new RestRequest();
                dynamic sources = JsonConvert.DeserializeObject(db.Get<dynamic>("sources"));
                foreach (dynamic source in sources.sources)
                {
                    request = new RestRequest("articles?apiKey=ccfdc66609fc4b7b87258020b85d4380&source=" + source.id);
                    rclient.ExecuteAsync(request, response =>
                    {
                        var sourceKey = string.Format("articles:{0}:{1}", source.category, source.id);
                        var article = response.Content;
                        db.SetValue(sourceKey, article);
                    });
                }

            }
        }
    }
}
