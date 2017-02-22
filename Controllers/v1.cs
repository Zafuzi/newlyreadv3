using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiceStack.Redis;

namespace NewlyReadv3.Controllers{
    [Route("api/[controller]")]
    public class v1 : Controller{
        [HttpGet]
        public dynamic Get(){
           return new string[] {
                "Please specify and endpoint."
           };
        }

        [HttpGet("{endpoint}/{category?}")]
        public dynamic Get(string endpoint, string category)
        {
            dynamic data = "";
            switch(endpoint){
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

        public static dynamic getSources(){
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

        public static dynamic getArticles(string category){
            dynamic data = "";
            using (var redisClient = new RedisClient())
            {
                var keysToScan = string.Format("articles:{0}:*", category);
                var articlesFromSources = redisClient.ScanAllKeys(keysToScan);
                List<dynamic> articles = new List<dynamic>();
                foreach (dynamic source in articlesFromSources)
                {
                    if(source != null && source.Length > 0){
                        try{
                            dynamic temp = JsonConvert.DeserializeObject(redisClient.GetValue(source));
                            if(data != null){
                                foreach (dynamic item in temp.articles)
                                {
                                    string date = item.publishedAt;
                                    item.publishedAt = date;
                                    articles.Add(item);
                                }
                            }
                        }catch(Exception e){
                            Console.WriteLine("\n Error reading articles from DB: {0} \n {1}", source, e);
                        }
                    }
                }
                data = articles.OrderByDescending(item => item.publishedAt).ToList();
            }
            return data;
        }

        public static dynamic getExtracted(){
            dynamic data = "";
            using (var redisClient = new RedisClient())
            {
                var keysToScan = string.Format("html:*");
                var articlesFromSources = redisClient.ScanAllKeys(keysToScan);
                List<dynamic> articles = new List<dynamic>();
                foreach (dynamic source in articlesFromSources)
                {
                    if(source != null && source.Length > 0){
                        try{
                            articles.Add(JsonConvert.DeserializeObject(redisClient.GetValue(source)));
                        }catch(Exception e){
                            Console.WriteLine("\n Error reading articles from DB: {0} \n {1}", source, e);
                        }
                    }
                }
                articles.Reverse();
                data = articles;
            }
            return data;
        }
        [HttpGet("extract/{url}")]
        public static dynamic Extract(string url){
            return url;
        }
    }
}