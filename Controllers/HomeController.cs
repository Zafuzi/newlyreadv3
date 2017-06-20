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

        [Route("About")]
        [Route("Home/About")]
        public IActionResult About(){
            return View();
        }

        public IActionResult Category(string category)
        {
            ViewBag.Articles = NewlyReadv3.Controllers.v1.getArticles(category);
            ViewBag.Category = category.ToUpperInvariant();
            return View();
        }

        public IActionResult ViewArticle(string url, string title){
            dynamic article = NewlyReadv3.Controllers.v1.Extract(url, title);
            try{
                ViewBag.Article = article;
                Console.WriteLine("\n\n Article Content: {0} \n\n", ViewBag.Article.content);
            } catch(Exception e){
                Console.WriteLine("Exception caught while trying to deserialize article: " + e);
            }
            ViewBag.Original = url;
            return View();
        }
    }
}