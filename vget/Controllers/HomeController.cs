using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using HtmlAgilityPack;
using System;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using vget.Models;

namespace vget.Controllers
{
    public class HomeController : Controller
    {
        IConfiguration _configuration;

        static string fshareLogin = "https://www.fshare.vn/site/login";
        static string fshareGet = "https://www.fshare.vn/download/get";
        static string pwd = string.Empty;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;

            string encodedPwd = _configuration["Key"];
            pwd = decodeWithKey(encodedPwd, "xxx");
        }

        public IActionResult Index()
        {
            //string encodedPwd = encodeWithKey("xxx", "xxx");
            //string pwd = decodeWithKey(encodedPwd, "xxx");

            return View();
        }

        public async Task<ActionResult> GetLink(InputModel input)
        {
            HttpClient httpClient = new HttpClient();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "link isn't valid";

                return RedirectToAction("Index");
            }

            string crsfToken = await GetToken(httpClient, string.Empty);
            await Login(httpClient, crsfToken);

            string xcrsfToken = await GetToken(httpClient, input.URL);
            try
            {
                string result = await RetrieveLink(httpClient, xcrsfToken, input.URL);
                TempData["Result"] = result;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "unexpected error, please try again :)";
            }

            httpClient.Dispose();

            return RedirectToAction("Index");
        }

        public async Task Login(HttpClient httpClient, string crsfToken)
        {
            var bodyRaw = new Dictionary<string, string>
            {
                { "LoginForm[email]", "xxx" },
                { "LoginForm[password]", pwd },
                { "_csrf-app", crsfToken }
            };

            var body = new FormUrlEncodedContent(bodyRaw);
            await httpClient.PostAsync(fshareLogin, body);
        }

        public async Task<string> RetrieveLink(HttpClient httpClient, string crsfToken, string url)
        {
            Uri uri = new Uri(url);

            var bodyRaw = new Dictionary<string, string>
            {
                { "linkcode", uri.Segments.Last() },
                { "_csrf-app", crsfToken },
                { "withFcode5", "0" }
            };

            var body = new FormUrlEncodedContent(bodyRaw);
            var response = await httpClient.PostAsync(fshareGet, body);

            var responseString = response.Content.ReadAsStringAsync().Result;
            JObject responseJson = JObject.Parse(responseString);

            return responseJson["url"].ToString();
        }

        public async Task<string> GetToken(HttpClient httpClient, string fileUrl)
        {
            HttpResponseMessage getTokenResponse = new HttpResponseMessage();
            if (string.IsNullOrEmpty(fileUrl))
            {
                getTokenResponse = await httpClient.GetAsync(fshareLogin);
            }
            else
            {
                getTokenResponse = await httpClient.GetAsync(fileUrl);
            }
            var getTokenResponseString = await getTokenResponse.Content.ReadAsStringAsync();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(getTokenResponseString);

            var crsfToken = document.DocumentNode
                                    .SelectNodes("//meta[@name='csrf-token']")
                                    .First()
                                    .Attributes["content"]
                                    .Value;

            return crsfToken;
        }

        #region Encryption
        static String encodeWithKey(String textIn, String key)
        {
            var _crypt = new TripleDESCryptoServiceProvider();
            var _hashmd5 = new MD5CryptoServiceProvider();

            var _byteHash = _hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
            var _byteText = Encoding.UTF8.GetBytes(textIn);

            _crypt.Key = _byteHash;
            _crypt.Mode = CipherMode.ECB;

            var _encodeByte = _crypt.CreateEncryptor().TransformFinalBlock(_byteText, 0, _byteText.Length);

            return Convert.ToBase64String(_encodeByte);
        }

        static String decodeWithKey(String encode, String key)
        {
            var _crypt = new TripleDESCryptoServiceProvider();
            var _hashmd5 = new MD5CryptoServiceProvider();

            var _byteHash = _hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
            var _byteText = Convert.FromBase64String(encode);

            _crypt.Key = _byteHash;
            _crypt.Mode = CipherMode.ECB;

            var decodeByte = _crypt.CreateDecryptor().TransformFinalBlock(_byteText, 0, _byteText.Length);

            return Encoding.UTF8.GetString(decodeByte);
        }
        #endregion
    }
}
