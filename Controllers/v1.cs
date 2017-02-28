using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack.Redis;
using StackExchange.Redis;

namespace NewlyReadv3.Controllers
{
    [Route("api/[controller]")]
    public class v1 : Controller
    {
        [HttpGet]
        public dynamic Get()
        {
            return new string[] {
                "Please specify and endpoint."
           };
        }

        [HttpGet("{endpoint}/{category?}")]
        public dynamic Get(string endpoint, string category)
        {
            dynamic data = "";
            switch (endpoint)
            {
                case "sources":
                    return getSources();
                case "articles":
                    return getArticles(category);
                case "extracted":
                    return getExtracted();
                default:
                    return "Invalid Request.";
            }
        }

        public static dynamic getSources()
        {
            dynamic data = "";
            using (var db = new RedisClient())
            {
                if (!db.ContainsKey("sources"))
                {
                    return "Error: Database returned nothing.";
                }
                data = JsonConvert.DeserializeObject(db.Get<dynamic>("sources"));
            }
            return data;
        }

        public static dynamic getArticles(string category)
        {
            dynamic data = "";
            using (var redisClient = new RedisClient())
            {
                var keysToScan = string.Format("articles:{0}:*", category);
                var articlesFromSources = redisClient.ScanAllKeys(keysToScan);
                List<dynamic> articles = new List<dynamic>();
                foreach (dynamic source in articlesFromSources)
                {
                    if (source != null && source.Length > 0)
                    {
                        try
                        {
                            dynamic temp = JsonConvert.DeserializeObject(redisClient.GetValue(source));
                            if (data != null)
                            {
                                foreach (dynamic item in temp.articles)
                                {
                                    string date = item.publishedAt;
                                    item.publishedAt = date;
                                    articles.Add(item);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\n Error reading articles from DB: {0} \n {1}", source, e);
                        }
                    }
                }
                data = articles.OrderByDescending(item => item.publishedAt).ToList();
            }
            return data;
        }

        public static dynamic getExtracted()
        {
            dynamic data = "";
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1");
            var server = redis.GetServer("127.0.0.1:6379");
            var db = redis.GetDatabase();

            List<dynamic> articles = new List<dynamic>();
            foreach (var key in server.Keys(pattern: "html:*"))
            {
                string source = db.StringGet(key);
                if (source != null && source.Length > 0)
                {
                    try
                    {
                        dynamic temp = JsonConvert.DeserializeObject(source);
                        articles.Add(temp);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\n Error reading articles from DB: {0} \n {1} \n", source, e);
                    }
                }
            }
            data = articles.OrderByDescending(item => item.date);
            foreach (dynamic item in data)
            {
                try
                {
                    item.content = JsonConvert.DeserializeObject(item);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error converting content for article: {0} \n {1} \n", item, e);
                }

            }
            return data;
        }
        [HttpGet("extract/{url}")]
        public static dynamic Extract(string url, string title)
        {
            DateTime now = DateTime.UtcNow;
            dynamic article = "";

            using (var redisClient = new RedisClient())
            {
                string x = string.Format("html:{0}", title);
                Console.WriteLine(x);
                if (redisClient.ContainsKey(x))
                {
                    article = JsonConvert.DeserializeObject(redisClient.GetValue(x));
                    Console.WriteLine("\n\n FOUND IN DB \n\n");
                }
                else
                {
                    Console.WriteLine("\n\n NOT IN DB \n\n");
                    var client = new RestClient("https://api.embed.ly/1/extract");
                    var request = new RestRequest(Method.GET);
                    request.AddParameter("key", "08ad220089e14298a88f0810a73ce70a");
                    request.AddParameter("url", url);
                    EventWaitHandle Wait = new AutoResetEvent(false);
                    var asyncHandle = client.ExecuteAsync(request, response =>
                    {
                        if (response.ResponseStatus == ResponseStatus.Completed)
                        {
                            string content = response.Content;
                            dynamic extract = JsonConvert.DeserializeObject(content);
                            var pdisplay = extract.provider_display;
                            if (pdisplay == null) pdisplay = "GENERAL";
                            var sourceKey = string.Format("html:{0}:{1}", pdisplay, title);
                            var html = response.Content;
                            var obj = new ExtractedArticle
                            {
                                date = now.ToString("u"),
                                content = html
                            };
                            string s = JsonConvert.SerializeObject(obj);
                            redisClient.SetValue(sourceKey, s);
                            article = extract;
                            Wait.Set();
                        }
                    });
                    Wait.WaitOne();
                }
            }
            return article;
        }

        class ExtractedArticle
        {
            public string date { get; set; }
            public string content { get; set; }
        }
    }
}