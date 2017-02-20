using System;
using System.Linq;

namespace NewlyReadv3.Tools{
    public static class StringTools{
        public static string Concat(string s, int length){
            string n = "";
            if(s.Length <= length){
                return s;
            }
            for(int i = 0; i < length; i ++){
                n += s[i];
            }
            return n;
        }
        public static string GetDomainName(string url)
        {
            string domain = new Uri(url).DnsSafeHost.ToLower();
            var tokens = domain.Split('.');
            if (tokens.Length > 2)
            {
                //Add only second level exceptions to the < 3 rule here
                string[] exceptions = { "info", "firm", "name", "com", "biz", "gen", "ltd", "web", "net", "pro", "org" }; 
                var validTokens = 2 + ((tokens[tokens.Length - 2].Length < 3 || exceptions.Contains(tokens[tokens.Length - 2])) ? 1 : 0);
                domain = string.Join(".", tokens, tokens.Length - validTokens, validTokens);
            }
            return domain;
        }
    }
}