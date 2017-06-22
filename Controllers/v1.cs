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
        private static IDatabase db = redis.GetDatabase();
        private static IServer server = redis.GetServer("127.0.0.1:6379");
        [HttpGet]
        public dynamic Get(){
            return new string[] {
                "Please specify an endpoint."
            };
        }

        [HttpGet("views/{view}/{data?}")]
        public IActionResult GetView(String view, String data){
            switch(view){
                case "Category":
                    ViewBag.Articles = GetArticles(data);
                    break;
                case "ViewArticle":
                    String keys = "";
                    var keysToScan = string.Format("html:*:" + data);
                    foreach (var key in server.Keys(pattern: keysToScan))
                    {
                        keys = key;
                    }
                    dynamic article = db.StringGet(keys);
                    Console.WriteLine("ARTICLE: " + article);

                    try{
                        ViewBag.Article = article;
                        Console.WriteLine("\n\n Article Content: {0} \n\n", ViewBag.Article.content);
                    } catch(Exception e){
                        Console.WriteLine("Exception caught while trying to deserialize article: " + e);
                    }
                    ViewBag.Article = JsonConvert.DeserializeObject(article);
                    break;
            }
            return View("~/Views/Home/" + view + ".cshtml");
        }

        [HttpGet("sources")]
        public IActionResult GetSources()
        {
            dynamic data = "";
            if (db.StringGet("sources").IsNullOrEmpty)
            {
                return new ObjectResult("Error: Database returned nothing.");
            }
            data = JsonConvert.DeserializeObject(db.StringGet("sources"));
            return new ObjectResult(data);
        }

        public dynamic GetArticles(string category)
        {
            List<String> articlesFromSources = new List<String>();
            var keysToScan = string.Format("articles:{0}:*", category);
            // show all keys in database 0 that include "foo" in their name
            foreach (var key in server.Keys(pattern: keysToScan))
            {
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
        [HttpGet("extracted")]
        public IActionResult GetExtracted()
        {
            dynamic data = "";
            List<dynamic> articles = new List<dynamic>();
            List<String> keys = new List<String>();
            var keysToScan = string.Format("html:*");
            foreach (var key in server.Keys(pattern: keysToScan))
            {
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

            return new ObjectResult(data);
        }
        [HttpGet("extract/{url}/{title}")]
        public IActionResult Extract(string url, string title)
        {
            Console.WriteLine("\n\nURL: {0},  \nTITLE: {1} \n\n", url, title);

            DateTime now = DateTime.UtcNow;
            dynamic article = "";

            var TOKEN = "4305b7c99372aca246ab9a79fb8658fe";
            var client = new RestClient("https://api.diffbot.com/v3/article");
            var request = new RestRequest(Method.GET);
            request.AddParameter("token", TOKEN);
            request.AddParameter("url", Uri.UnescapeDataString(url));

            EventWaitHandle Wait = new AutoResetEvent(false);

            var asyncHandle = client.ExecuteAsync(request, response =>
            {
                string content = response.Content;
                dynamic extract, objects;
                try
                {
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
                }
                catch (Exception e)
                {
                    Console.WriteLine("General Exception caught: " + e);
                }
                Wait.Set();
            });
            Wait.WaitOne();
            ViewBag.Article = article;
            return View("~/Views/Home/ViewArticle.cshtml");
        }

        class ExtractedArticle
        {
            public string date { get; set; }
            public string content { get; set; }
        }
    }
}