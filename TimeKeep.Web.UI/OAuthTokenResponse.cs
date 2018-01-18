using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace TimeKeep.Web.UI
{
    public class OAuthTokenResponse
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_on")]
        public int IntExpiresOn { get; set; }

        [JsonProperty("not_before")]
        public int IntNotBefore { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("id_token")]
        public string IDToken { get; set; }

        [JsonIgnore]
        public JwtSecurityToken Token
        {
            get
            {
                if (!string.IsNullOrEmpty(IDToken))
                    return new JwtSecurityToken(IDToken);
                return null;
            }
        }
        [JsonIgnore]
        public ClaimsPrincipal User
        {
            get
            {
                if (Token == null)
                    return null;

                ClaimsPrincipal prin = new ClaimsPrincipal();
                ClaimsIdentity identity = new ClaimsIdentity(Token.Claims, "Bearer", "name", "scope");
                prin.AddIdentity(identity);
                return prin;
            }
        }

        [JsonIgnore]
        public DateTime? ExpiresOn
        {
            get
            {
                if (IntExpiresOn == 0)
                    return null;
                return new DateTime(IntExpiresOn);
            }
        }
        [JsonIgnore]
        public DateTime? NotBefore
        {
            get
            {
                if (IntExpiresOn == 0)
                    return null;
                return new DateTime(IntNotBefore);
            }
        }

    }
}