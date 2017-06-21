using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using StackExchange.Redis;

namespace NewlyReadv3.Controllers
{
    
    [Route("api/[controller]")]
    public class v1 : Controller
    {
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1");
        connection.PreserveAsyncOrder = false;
        private static IDatabase db = redis.GetDatabase();
        private static IServer server = redis.GetServer("127.0.0.1:6379");
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
            if (db.StringGet("sources").IsNullOrEmpty)
            {
                return "Error: Database returned nothing.";
            }
            data = JsonConvert.DeserializeObject(db.StringGet("sources"));
            return data;
        }

        public static dynamic getArticles(string category)
        {
            List<String> articlesFromSources = new List<String>();
            var keysToScan = string.Format("articles:{0}:*", category);
            // show all keys in database 0 that include "foo" in their name
            foreach(var key in server.Keys(pattern: keysToScan)) {
                articlesFromSources.Add(key);
            }

            dynamic data = "";
            
            List<dynamic> articles = new List<dynamic>();
            
            foreach (var key in articlesFromSources)
            {
                try
                {
                    dynamic key_data = JsonConvert.DeserializeObject(db.StringGet(key));
                    if (data != null)
                    {
                        foreach (dynamic item in key_data.articles)
                        {
                            string date = item.publishedAt;
                            item.publishedAt = date;
                            articles.Add(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n Error reading articles from DB: {0} \n", e);
                }
            }
            data = articles.OrderByDescending(item => item.publishedAt).ToList();
            return data;
        }

        public static dynamic getExtracted()
        {
            dynamic data = "";
            List<dynamic> articles = new List<dynamic>();
            List<String> keys = new List<String>();
            var keysToScan = string.Format("html:*");
            // show all keys in database 0 that include "foo" in their name
            foreach(var key in server.Keys(pattern: keysToScan)) {
                keys.Add(key);
            }

            foreach (var key in keys)
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
                        Console.WriteLine("\n Error reading articles from DB: {0} \n", e);
                    }
                }
            }
            data = articles.OrderByDescending(item => item.date);

            return data;
        }
        [HttpGet("extract/{url}")]
        public static dynamic Extract(string url, string title)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1");
            IDatabase db = redis.GetDatabase();
            IServer server = redis.GetServer("127.0.0.1:6379");
            DateTime now = DateTime.UtcNow;
            dynamic article = "";
                string article_key = string.Format("html:{0}", title);
                if (db.KeyExists(article_key))
                {
                    article = JsonConvert.DeserializeObject(db.StringGet(article_key));
                    Console.WriteLine("\n\n FOUND IN DB \n\n");
                }
                else
                {
                    Console.WriteLine("\n\n NOT IN DB \n\n");
                    // Contact
                    var TOKEN = "4305b7c99372aca246ab9a79fb8658fe";
                    var client = new RestClient("https://api.diffbot.com/v3/article");
                    var request = new RestRequest(Method.GET);
                    request.AddParameter("token", TOKEN);
                    request.AddParameter("url", url);

                    EventWaitHandle Wait = new AutoResetEvent(false);

                    var asyncHandle = client.ExecuteAsync(request, response =>{
                            string content = response.Content;
                            dynamic extract, objects;
                            try{
                                extract = JsonConvert.DeserializeObject(content);
                                objects = extract.objects;
                                // Console.WriteLine(objects[0]);

                                title = title.Replace(":", "");

                                var site_name = objects[0].siteName;
                                if (site_name == null) site_name = "GENERAL";
                                var sourceKey = string.Format("html:{0}:{1}", site_name, title);
                                var html = JsonConvert.SerializeObject(objects[0]);
                                var obj = new ExtractedArticle
                                {
                                    date = now.ToString("u"),
                                    content = html
                                };

                                string s = JsonConvert.SerializeObject(obj);
                                db.StringSet(sourceKey, s);
                                article = extract;
                            }catch(Exception e){
                                Console.WriteLine("General Exception caught: " + e);
                            }
                            Wait.Set();
                    });
                    Wait.WaitOne();
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