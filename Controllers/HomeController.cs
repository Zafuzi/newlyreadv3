using System;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using ServiceStack.Redis;

namespace NewlyReadv3.Controllers
{
    public class HomeController : Controller
    {

        [Route("")]
        [Route("Home")]
        [Route("Home/Index")]
        public IActionResult Index()
        {
            ViewBag.Articles = NewlyReadv3.Controllers.v1.getExtracted();
            return View();
        }

        public IActionResult Category(string category)
        {
            ViewBag.Articles = NewlyReadv3.Controllers.v1.getArticles(category);
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
                                ViewBag.Article = extract;
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