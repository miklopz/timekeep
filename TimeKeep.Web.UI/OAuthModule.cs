using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace TimeKeep.Web.UI
{
    public class OAuthModule : IHttpModule
    {
        public class TokenCacheItem
        {
            public string IDToken { get; set; }
            public string AuthorizationCode { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }

            public TokenCacheItem(string idToken, string authorizationCode)
            {
                IDToken = idToken;
                AuthorizationCode = authorizationCode;
            }
        }
        public class TokenCache
        {
            private static Dictionary<Guid, TokenCacheItem> _tokens;
            private static object _opLock = new object();
            private static Dictionary<Guid, TokenCacheItem> Tokens
            {
                get
                {
                    if(_tokens == null)
                    {
                        lock(_opLock)
                        {
                            if (_tokens == null)
                                _tokens = new Dictionary<Guid, TokenCacheItem>();
                        }
                    }
                    return _tokens;
                }
            }

            public static Guid AddToken(string id_token, string authCode)
            {
                Guid guid = Guid.NewGuid();
                lock(_opLock)
                {
                    Tokens.Add(guid, new TokenCacheItem(id_token, authCode));
                }
                return guid;
            }

            public static void SetAccessAndRefreshToken(Guid userID, string accessToken, string refreshToken)
            {
                lock (_opLock)
                {
                    if (Tokens.ContainsKey(userID))
                    {
                        Tokens[userID].AccessToken = accessToken;
                        Tokens[userID].RefreshToken = refreshToken;
                    }
                }
            }

            public static TokenCacheItem GetToken(Guid userID)
            {
                if (Tokens.ContainsKey(userID))
                    return Tokens[userID];
                return null;
            }

            public static void DeleteToken(Guid userID)
            {
                if (Tokens.ContainsKey(userID))
                    Tokens.Remove(userID);
            }
        }

        public void Dispose()
        {
            
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += Context_AuthenticateRequest;
        }

        private void Context_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpApplication Application = (HttpApplication)sender;
            HttpContext Context = Application.Context;
            HttpRequest Request = Application.Context.Request;
            HttpResponse Response = Application.Context.Response;

            if(Request.Cookies["OAuthIDToken"] == null)
            {
                Uri redirectURI = new Uri(Configuration.OAuth.ReplyURL);
                Uri currentURI = Request.Url;

                if(Request.HttpMethod == "POST" &&
                    redirectURI.Scheme.Equals(currentURI.Scheme) &&
                    redirectURI.Host.Equals(currentURI.Host) &&
                    redirectURI.Port.Equals(currentURI.Port) &&
                    redirectURI.AbsolutePath.Equals(currentURI.AbsolutePath))
                {
                    string id_token = Request.Form["id_token"];
                    string code = Request.Form["code"];
                    if (id_token != null && code != null)
                    {
                        ClaimsPrincipal user = ConvertToPrincipal(id_token);
                        Guid userID = TokenCache.AddToken(id_token, code);
                        HttpCookie cookie = new HttpCookie("OAuthIDToken");
                        if(currentURI.IsDefaultPort)
                            cookie.Domain = currentURI.Host + (currentURI.IsDefaultPort ? string.Empty : ":" + currentURI.Port.ToString());
                        cookie.Path = "/";
                        cookie.HttpOnly = true;
                        cookie.Secure = true;
                        cookie.Value = userID.ToString();

                        Context.User = user;

                        // As a courtesy, we should get a bearer token for the WebAPI...

                        HttpWebRequest request = WebRequest.CreateHttp(Configuration.OAuth.TokenEndpoint);
                        byte[] body = Encoding.UTF8.GetBytes(string.Format(@"grant_type=authorization_code&client_id={0}&client_secret={1}&redirect_uri={2}&resource={3}&code={4}",
                            Configuration.OAuth.ClientID, Configuration.OAuth.Secret, Configuration.OAuth.ReplyURL, Configuration.OAuth.Resource, code));
                        request.UserAgent = Request.UserAgent;
                        request.Method = "POST";
                        using(Stream output = request.GetRequestStream())
                        {
                            output.Write(body, 0, body.Length);
                            output.Close();
                        }

                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            if(response.StatusCode == HttpStatusCode.OK)
                            {
                                using (Stream inbound = response.GetResponseStream())
                                using (StreamReader sr = new StreamReader(inbound))
                                {
                                    OAuthTokenResponse tokResp = JsonConvert.DeserializeObject<OAuthTokenResponse>(sr.ReadToEnd());
                                    TokenCache.SetAccessAndRefreshToken(userID, tokResp.AccessToken, tokResp.RefreshToken);
                                    sr.Close();
                                    inbound.Close();
                                }
                            }
                        }
                        catch(WebException)
                        {
                            // TODO: Something went wrong here. Handle
                        }

                        Response.Cookies.Set(cookie);
                        Response.Redirect(Configuration.OAuth.ReplyURL);
                        Application.CompleteRequest();
                    }
                }
                RedirectToSSO(Application, Response);
                return;
            }
            else
            {
                Guid userId = Guid.Empty;
                if(!Guid.TryParse(Request.Cookies["OAuthIDToken"].Value, out userId))
                {
                    HttpCookie cookie = new HttpCookie("OAuthIDToken");
                    if (Request.Url.IsDefaultPort)
                        cookie.Domain = Request.Url.Host + (Request.Url.IsDefaultPort ? string.Empty : ":" + Request.Url.Port.ToString());
                    cookie.Path = "/";
                    cookie.HttpOnly = true;
                    cookie.Secure = true;
                    cookie.Value = string.Empty;
                    cookie.Expires = DateTime.Now.AddYears(-10);
                    Response.SetCookie(cookie);
                }
                TokenCacheItem tokenCacheItem = TokenCache.GetToken(userId);
                if(tokenCacheItem == null)
                {
                    HttpCookie cookie = new HttpCookie("OAuthIDToken");
                    if(Request.Url.IsDefaultPort)
                        cookie.Domain = Request.Url.Host + (Request.Url.IsDefaultPort ? string.Empty : ":" + Request.Url.Port.ToString());
                    cookie.Path = "/";
                    cookie.HttpOnly = true;
                    cookie.Secure = true;
                    cookie.Value = string.Empty;
                    cookie.Expires = DateTime.Now.AddYears(-10);
                    Response.SetCookie(cookie);
                    RedirectToSSO(Application, Response);
                }
                ClaimsPrincipal user = ConvertToPrincipal(tokenCacheItem.IDToken);

                // TODO: Acquire access token or refresh Token implementation

                Context.User = user;
            }
        }

        private void RedirectToSSO(HttpApplication Application, HttpResponse Response)
        {
            string nonce = Guid.NewGuid().ToString();
            // 401
            Response.Redirect(string.Format(@"{0}?response_type=id_token+code&scope=openid&client_id={1}&redirect_uri={2}&response_mode=form_post&nonce={3}&resource={4}", Configuration.OAuth.AuthEndpoint, Configuration.OAuth.ClientID, Configuration.OAuth.ReplyURL, nonce, Configuration.OAuth.Resource));
            Application.CompleteRequest();
        }

        private ClaimsPrincipal ConvertToPrincipal(string id_token)
        {
            JwtSecurityToken token = new JwtSecurityToken(id_token);

            ClaimsPrincipal prin = new ClaimsPrincipal();
            ClaimsIdentity identity = new ClaimsIdentity(token.Claims, "Bearer", "name", "scope");
            prin.AddIdentity(identity);
            return prin;
        }
    }
}