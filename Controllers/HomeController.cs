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
            return View();
        }

        public IActionResult ViewArticle(string url, string title){
            ViewBag.Article = NewlyReadv3.Controllers.v1.Extract(url, title);
            ViewBag.Article = "";
            ViewBag.Original = url;
            return View();
        }
    }
}