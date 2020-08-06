//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Security.Authentication;
//using System.Text;
//using UnityEngine.Networking;

//namespace EarlyAccess
//{
//    public static class Patreon
//    {
//        public const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
//        public const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

//        public static event Action<string> OnData;
//        public static event Action OnAbort;

//        private static string PatrionURL { get; } = "https://www.patreon.com";

//        private static string PatreonOAuthURL => $"{PatrionURL}/oauth2/authorize";
//        private static string PatreonTokenURL => $"{PatrionURL}/api/oauth2/token";
//        private static string PatreonOauthURL => $"{PatrionURL}/api/oauth2/v2";
//        private static string PatreonIdentityURL => $"{PatreonOauthURL}/identity";

//        private static string ClientId { get; } = "xk0BpoVsoIGKYeHmK6MzkXGyZbJrpcOWHCoiY0O-mX12nJzmeNqWBRme5RmzhXHw";
//        private static string ClientSecret { get; } = "VJfkCmJD_cSOObBQnZArKGqr4kXFkK8sL80Ck8YJWEzFxxVQpKVqpCRslf4IdyAj";

//        public static string OAuthURL { get; } = GetURL(PatreonOAuthURL, new Dictionary<string, string> { { "response_type", "code" }, { "client_id", ClientId } });
//        private static string RedirectURL { get; } = "http://localhost:2712/";

//        private static HttpListener Listener { get; set; }

//        public static void Listen()
//        {
//            try
//            {
//                if (Listener != null)
//                    Abort();

//                Logger.LogDebug();
//                Listener = new HttpListener();
//                Listener.Prefixes.Add(RedirectURL);
//                Listener.Start();
//                var context = Listener.GetContext();
//                var request = context.Request;

//                var code = request.QueryString.GetValues("code").FirstOrDefault();
//                OnData?.Invoke(code);

//                //var response = context.Response;
//                //string responseString = "<HTML><BODY>Authorization success</BODY></HTML>";
//                //byte[] buffer = Encoding.UTF8.GetBytes(responseString);
//                //response.ContentLength64 = buffer.Length;
//                //Stream output = response.OutputStream;
//                //output.Write(buffer, 0, buffer.Length);
//                //output.Close();
//            }
//            catch 
//            {
//                try { Listener?.Close(); }
//                catch { }
//            }
//            finally
//            {
//                Listener?.Stop();
//                Listener = null;
//            }
//        }
//        public static void Abort()
//        {
//            Listener?.Abort();
//            OnAbort?.Invoke();
//        }


//        public static bool GetToken(string code, out string accessToken, out string refreshToken)
//        {
//            var args = new Dictionary<string, string>
//            {
//                { "code", code },
//                { "grant_type",  "authorization_code"},
//                { "client_id",  ClientId},
//                { "client_secret",  ClientSecret},
//                { "redirect_uri",  RedirectURL},
//            };
//            var url = GetURL(PatreonTokenURL, args);

//            return ProcessTokenRequest(url, out accessToken, out refreshToken);
//        }
//        public static bool RefreshToken(string token, out string accessToken, out string refreshToken)
//        {
//            var args = new Dictionary<string, string>
//            {
//                { "grant_type",  "refresh_token"},
//                { "refresh_token",  token},
//                { "client_id",  ClientId},
//                { "client_secret",  ClientSecret},
//            };
//            var url = GetURL(PatreonTokenURL, args);

//            return ProcessTokenRequest(url, out accessToken, out refreshToken);
//        }
//        private static bool ProcessTokenRequest(string url, out string accessToken, out string refreshToken)
//        {
//            UnityWebRequest request = null;
//            try
//            {
//                request = UnityWebRequest.Post(url, string.Empty);
//                request.Send();

//                if (!request.isError)
//                {
//                    var jsonResult = JObject.Parse(request.downloadHandler.text);
//                    if (jsonResult.TryGetValue("access_token", out JToken accessTokenValue) && jsonResult.TryGetValue("refresh_token", out JToken refreshTokenValue))
//                    {
//                        accessToken = accessTokenValue.ToString();
//                        refreshToken = refreshTokenValue.ToString();
//                        return true;
//                    }
//                }
//            }
//            catch { }
//            finally
//            {
//                request?.Dispose();
//            }

//            accessToken = null;
//            refreshToken = null;
//            return false;
//        }

//        public static bool IsMember(string token, out string id)
//        {
//            UnityWebRequest request = null;
//            try
//            {
//                var args = new Dictionary<string, string>
//                {
//                    {"include", "memberships" },
//                };
//                var url = GetURL(PatreonIdentityURL, args);

//                request = UnityWebRequest.Get(url);
//                request.SetRequestHeader("Authorization", $"Bearer {token}");
//                request.Send();

//                if (!request.isError)
//                {
//                    var jsonResult = JObject.Parse(request.downloadHandler.text);
//                    var userData = jsonResult["data"];
//                    id = userData.Value<string>("id");
//                    var isMember = userData["relationships"]["memberships"]["data"] is JArray data && data.Count != 0;

//                    return isMember;
//                }
//            }
//            catch { }
//            finally
//            {
//                request?.Dispose();
//            }
//            id = null;
//            return false;
//        }
//        private static string GetURL(string baseURL, Dictionary<string, string> args)
//        {
//            var builder = new UriBuilder(baseURL)
//            {
//                Query = string.Join("&", args.Select(a => $"{a.Key}={a.Value}").ToArray())
//            };
//            var url = builder.ToString();
//            return url;
//        }
//        private static JObject Send(WebRequest request)
//        {
//            WebResponse response = request.GetResponse();
//            using (var stream = response.GetResponseStream())
//            using (var memory = new MemoryStream())
//            {
//                byte[] buffer = new byte[10000];
//                int readed;
//                while ((readed = stream.Read(buffer, 0, buffer.Length)) > 0)
//                {
//                    memory.Write(buffer, 0, readed);
//                }
//                var result = Encoding.UTF8.GetString(memory.ToArray());
//                var jsonResult = JObject.Parse(result);
//                return jsonResult;
//            }
//        }
//    }
//}
