using MangaManager.Models.ExternalModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace MangaManager.Tasks.HttpClient
{
    public partial class MangaCollecHttpClient
    {
        private static string s_clientId = "";
        private static string s_clientSecret = "";
        private static string s_userLogin = "";
        private static string s_userPassword = "";


        private readonly System.Net.Http.HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(1) };
        private HttpToken _token;
        private readonly Dictionary<string, object> _dataStoreCache = new();

        public void Login()
        {
            _token = null;
            GetOrRefeshToken(s_userLogin, s_userPassword);
        }
        private void GetOrRefeshToken(string username = null, string password = null)
        {
            if (_token != null && DateTimeOffset.FromUnixTimeSeconds(_token.CreatedAt + _token.ExpriresIn).LocalDateTime > DateTime.Now.AddMinutes(-5))
            {
                //Keep token expiring in more than 5 minutes
                return;
            }

            var postValues = new Dictionary<string, string>()
            {
                { "client_id", s_clientId  },
                { "client_secret", s_clientSecret },
            };
            if (_token != null && !string.IsNullOrEmpty(_token.RefreshToken))
            {
                //Refresh an existing token
                postValues["grant_type"] = "refresh_token";
                postValues["refresh_token"] = _token.RefreshToken;
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
            var response = _httpClient.PostAsync("https://api.mangacollec.com/oauth/token", content).GetAwaiter().GetResult();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            _token = JsonConvert.DeserializeObject<HttpToken>(responseString);
        }

        public T GetDataStore<T>(string virtualPath)
        {
            if (_dataStoreCache.ContainsKey(virtualPath))
            {
                return (T)_dataStoreCache[virtualPath];
            }

            GetOrRefeshToken();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"{_token.TokenType} {_token.AccessToken}");
            var response = _httpClient.GetAsync($"https://api.mangacollec.com{virtualPath}").GetAwaiter().GetResult();
            var responseString = WebUtility.HtmlDecode(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var result = JsonConvert.DeserializeObject<T>(responseString);
            _dataStoreCache[virtualPath] = result;
            return result;
        }

        public TResult Post<TParam, TResult>(string virtualPath, TParam postParams)
        {
            GetOrRefeshToken();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"{_token.TokenType} {_token.AccessToken}");
            var content = new StringContent(JsonConvert.SerializeObject(postParams), Encoding.UTF8, "application/json");
            var response = _httpClient.PostAsync($"https://api.mangacollec.com{virtualPath}", content).GetAwaiter().GetResult();
            var responseString = WebUtility.HtmlDecode(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var result = JsonConvert.DeserializeObject<TResult>(responseString);
            return result;
        }
    }
}