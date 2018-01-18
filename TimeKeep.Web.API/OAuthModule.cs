using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Web;
using TimeKeep.Web.API.Models;

namespace TimeKeep.Web.API
{
    public class OAuthModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += Context_AuthenticateRequest;
            context.PostRequestHandlerExecute += Context_PostRequestHandlerExecute;
        }

        private void Context_PostRequestHandlerExecute(object sender, EventArgs e)
        {
            HttpApplication Application = (HttpApplication)sender;
            HttpContext Context = Application.Context;
            HttpRequest Request = Application.Context.Request;
            HttpResponse Response = Application.Context.Response;

            if(Response.StatusCode == 401)
            {
                Response.ClearContent();
                Response.ContentType = "application/json";
                Response.Write("{\"Message\":\"Please request an access token from your STS\"}");
                return;
            }
        }

        private void Context_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpApplication Application = (HttpApplication)sender;
            HttpContext Context = Application.Context;
            HttpRequest Request = Application.Context.Request;
            HttpResponse Response = Application.Context.Response;

            string authHeader = Request.Headers["Authorization"];
            if (authHeader != null && authHeader.Trim().Length > 0)
            {
                string bearerToken = Request.Headers["Authorization"].Substring(7);
                JwtSecurityToken token = new JwtSecurityToken(bearerToken);
                Context.User = ConvertToPrincipal(token);
            }
        }


        private ClaimsPrincipal ConvertToPrincipal(JwtSecurityToken token)
        {

            ClaimsPrincipal prin = new ClaimsPrincipal();
            ClaimsIdentity identity = new ClaimsIdentity(token.Claims, "Bearer", "name", "scope");
            prin.AddIdentity(identity);
            return prin;
        }
    }
}