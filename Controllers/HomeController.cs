using System;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace NewlyReadv3.Controllers
{
    public class HomeController : Controller
    {

        [Route("")]
        [Route("Home")]
        
        public IActionResult Home()
        {
            return View("~/Views/Home/Layout.cshtml");
        }
        [Route("Index")]
        [Route("Home/Index")]
        public IActionResult Index(){  
            return View();
        }

        [Route("About")]
        [Route("Home/About")]
        public IActionResult About(){
            return View();
        }

        public IActionResult Category(string category)
        {
            return View();
        }

        public IActionResult ViewArticle(string url, string title){
            return View();
        }
    }
}