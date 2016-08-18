using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient
{
    public class OAuthRedirectUrlBuilder
    {
        public OAuthRedirectUrlBuilder(string clientId)
        {
            ClientId = clientId;
        }
        public string AuthorizationUrl { get; } = "https://github.com/login/oauth/authorize";

        public string ClientId { get; } 

        public string RedirectUrl { get; set; }

        public ICollection<string> Scopes { get; set; } = new HashSet<string>();

        public string State { get; set; }

        public bool? AllowSignup { get; set; }

        public string Build()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(AuthorizationUrl);
            sb.AppendFormat($"?client_id={Uri.EscapeDataString(ClientId)}");
            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                sb.AppendFormat($"&redirect_uri={Uri.EscapeDataString(RedirectUrl)}");
            }
            if (Scopes.Any())
            {
                sb.AppendFormat($"&scope={Uri.EscapeDataString(string.Join(" ", Scopes))}");
            }
            if (!string.IsNullOrEmpty(State))
            {
                sb.AppendFormat($"&state={Uri.EscapeDataString(State)}");
            }
            if (AllowSignup.HasValue)
            {
                sb.Append("&allow_signup=");
                if (AllowSignup.Value)
                {
                    sb.Append("true");
                }
                else
                {
                    sb.Append("false");
                }
            }
            return sb.ToString();
        }
    }
}
