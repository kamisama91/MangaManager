using MangaManager.Models.ExternalModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace MangaManager.Tasks.Scrap
{
    public static class MangaCollecHttpHelper
    {
        private static string _clientId = "";
        private static string _clientSecret = "";
        private static string _userLogin = "";
        private static string _userPassword = "";

        private static readonly HttpClient s_client = new();
        private static HttpToken s_token;
        private static readonly Dictionary<string, object> s_dataStoreCache = new();

        public static void Login()
        {
            Logout();
            GetToken(_userLogin, _userPassword);
        }
        public static void Logout()
        {
            s_token = null;
        }
        
        private static void GetToken(string username, string password)
        {
            if (s_token != null && DateTimeOffset.FromUnixTimeSeconds(s_token.CreatedAt + s_token.ExpriresIn).LocalDateTime > DateTime.Now.AddMinutes(-5))
            {
                //Keep token expiring in more than 5 minutes
                return;
            }

            var postValues = new Dictionary<string, string>()
            {
                { "client_id", _clientId  },
                { "client_secret", _clientSecret },
            };
            if (s_token != null && !string.IsNullOrEmpty(s_token.RefreshToken))
            {
                //Refresh an existing token
                postValues["grant_type"] = "refresh_token";
                postValues["refresh_token"] = s_token.RefreshToken;
            }
            else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                //Get a website user token
                postValues["grant_type"] = "password";
                postValues["username"] = username;
                postValues["password"] = password;
            }
            else
            {
                //Get a website api token
                postValues["grant_type"] = "client_credentials";
            }

            var content = new FormUrlEncodedContent(postValues);
            var response = s_client.PostAsync("https://api.mangacollec.com/oauth/token", content).GetAwaiter().GetResult();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            s_token = JsonConvert.DeserializeObject<HttpToken>(responseString);
        }
        public static T GetDataStore<T>(string virtualPath)
        {
            if (s_dataStoreCache.ContainsKey(virtualPath))
            {
                return (T)s_dataStoreCache[virtualPath];
            }

            GetToken(string.Empty, string.Empty);

            s_client.DefaultRequestHeaders.Clear();
            s_client.DefaultRequestHeaders.Add("Authorization", $"{s_token.TokenType} {s_token.AccessToken}");
            var response = s_client.GetAsync($"https://api.mangacollec.com{virtualPath}").GetAwaiter().GetResult();
            var responseString = WebUtility.HtmlDecode(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var result = JsonConvert.DeserializeObject<T>(responseString);
            s_dataStoreCache[virtualPath] = result;
            return result;
        }
    }
}