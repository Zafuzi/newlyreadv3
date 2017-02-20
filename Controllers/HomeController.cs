using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

namespace NewlyReadv3.Controllers
{
    public class HomeController : Controller
    {

        [Route("")]
        [Route("Home")]
        [Route("Home/Index")]
        public IActionResult Index()
        {
            using (var redisClient = new RedisClient())
            {
                var articlesFromSources = redisClient.ScanAllKeys("articles:*").ToList();
                ViewBag.Articles = redisClient.GetValues<dynamic>(articlesFromSources);
            }
            return View();
        }

        public IActionResult Category(string category)
        {
            using (var redisClient = new RedisClient())
            {
                var keysToScan = string.Format("articles:{0}:*", category);
                var articlesFromSources = redisClient.ScanAllKeys(keysToScan);
                List<dynamic> articles = new List<dynamic>();
                foreach (dynamic source in articlesFromSources)
                {
                    if(source != null && source.Length > 0){
                        try{
                            dynamic data = JsonConvert.DeserializeObject(redisClient.GetValue(source));
                            if(data != null){
                                foreach (dynamic item in data.articles)
                                {
                                    articles.Add(item);
                                }
                            }
                        }catch(Exception e){
                            Console.WriteLine("\n Error reading articles from DB: {0} \n {1}", source, e);
                        }
                    }
                }
                articles = articles.OrderBy(item => item.title).ToList();
                ViewBag.Articles = articles;
                Console.WriteLine(articlesFromSources);
            }
            return View();
        }

        public IActionResult ViewArticle(string url, string title){

                using (var redisClient = new RedisClient())
                {
                    string x = string.Format("html:{0}", title);
                    Console.WriteLine(x);
                    if(redisClient.ContainsKey(x)){
                        ViewBag.Article = JsonConvert.DeserializeObject(redisClient.GetValue(x));
                        Console.WriteLine("\n\n FOUND IN DB \n\n");
                    }else{
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
                                if(title == null) title="unknown";
                                var sourceKey = string.Format("html:{0}", title);
                                var article = response.Content;
                                redisClient.SetValue(sourceKey, article);

                                ViewBag.Article = JsonConvert.DeserializeObject(content);
                                Wait.Set();
                            }
                        });
                        Wait.WaitOne();
                    }
                }
                ViewBag.Original = url;
            return View();
        }
    }
}