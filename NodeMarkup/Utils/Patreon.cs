using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace NodeMarkup
{
    public static class Patreon
    {
        public static Action<string> OnData { get; set; }

        private static string PatrionURL { get; } = "https://www.patreon.com";
        public static string RedirectURL { get; } = "http://localhost:2712/";
        private static string PatreonOAuthURL => $"{PatrionURL}/oauth2/authorize";
        private static string PatreonTokenURL => $"{PatrionURL}/api/oauth2/token";
        private static string PatreonOauthURL => $"{PatrionURL}/api/oauth2/v2";
        private static string PatreonIdentityURL => $"{PatreonOauthURL}/identity";

        private static string ClientId { get; } = "xk0BpoVsoIGKYeHmK6MzkXGyZbJrpcOWHCoiY0O-mX12nJzmeNqWBRme5RmzhXHw";
        private static string ClientSecret { get; } = "VJfkCmJD_cSOObBQnZArKGqr4kXFkK8sL80Ck8YJWEzFxxVQpKVqpCRslf4IdyAj";

        public static string OAuthURL { get; } = GetURL(PatreonOAuthURL, new Dictionary<string, string> { { "response_type", "code" }, { "client_id", ClientId } });
        public static string GetOAuthURLWithState(string state) =>
GetURL(PatreonOAuthURL, new Dictionary<string, string> { { "response_type", "code" }, { "client_id", ClientId }, { "redirect_uri", RedirectURL }, { "state", state } });

        public static bool GetToken(string code, out string accessToken, out string refreshToken)
        {
            Logger.LogDebug(nameof(GetToken));
            var args = new Dictionary<string, string>
            {
                { "code", code },
                { "grant_type",  "authorization_code"},
                { "client_id",  ClientId},
                { "client_secret",  ClientSecret},
                { "redirect_uri",  RedirectURL},
            };
            var url = GetURL(PatreonTokenURL, args);

            var result = ProcessTokenRequest(url, out accessToken, out refreshToken);
            Logger.LogDebug($"{nameof(GetToken)} - {result}");
            return result;
        }
        public static bool RefreshToken(string token, out string accessToken, out string refreshToken)
        {
            Logger.LogDebug(nameof(RefreshToken));
            var args = new Dictionary<string, string>
            {
                { "grant_type",  "refresh_token"},
                { "refresh_token",  token},
                { "client_id",  ClientId},
                { "client_secret",  ClientSecret},
            };
            var url = GetURL(PatreonTokenURL, args);

            var result = ProcessTokenRequest(url, out accessToken, out refreshToken);
            Logger.LogDebug($"{nameof(RefreshToken)} - {result}");
            return result;
        }
        private static bool ProcessTokenRequest(string url, out string accessToken, out string refreshToken)
        {
            UnityWebRequest request = null;
            try
            {
                Logger.LogDebug(nameof(ProcessTokenRequest));

                request = UnityWebRequest.Post(url, string.Empty);
                request.timeout = 3;
                request.Send();

                while (!request.isDone) { }

                if (!request.isError)
                {
                    var jsonResult = JObject.Parse(request.downloadHandler.text);
                    if (jsonResult.TryGetValue("access_token", out JToken accessTokenValue) && jsonResult.TryGetValue("refresh_token", out JToken refreshTokenValue))
                    {
                        accessToken = accessTokenValue.ToString();
                        refreshToken = refreshTokenValue.ToString();
                        return true;
                    }
                }
            }
            catch (Exception error)
            {
                Logger.LogError(() => nameof(ProcessTokenRequest), error);
            }
            finally
            {
                request?.Dispose();
            }

            accessToken = null;
            refreshToken = null;
            return false;
        }

        public static bool IsMember(string token, out string id)
        {
            UnityWebRequest request = null;
            try
            {
                Logger.LogDebug(nameof(IsMember));

                var args = new Dictionary<string, string>
                {
                    {"include", "memberships" },
                };
                var url = GetURL(PatreonIdentityURL, args);

                request = UnityWebRequest.Get(url);
                request.timeout = 3;
                request.SetRequestHeader("Authorization", $"Bearer {token}");
                request.Send();

                while (!request.isDone) { }

                if (!request.isError)
                {
                    var jsonResult = JObject.Parse(request.downloadHandler.text);
                    var userData = jsonResult["data"];
                    id = userData.Value<string>("id");
                    var isMember = userData["relationships"]["memberships"]["data"] is JArray data && data.Count != 0;

                    return isMember;
                }
            }
            catch (Exception error)
            {
                Logger.LogError(() => nameof(IsMember), error);
            }
            finally
            {
                request?.Dispose();
            }
            id = null;
            return false;
        }
        private static string GetURL(string baseURL, Dictionary<string, string> args)
        {
            var builder = new UriBuilder(baseURL)
            {
                Query = string.Join("&", args.Select(a => $"{a.Key}={a.Value}").ToArray())
            };
            var url = builder.ToString();
            return url;
        }
    }
}
