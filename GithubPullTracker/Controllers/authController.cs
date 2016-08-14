using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GithubPullTracker.Models;
using Octokit;
using AttributeRouting.Web.Mvc;

namespace GithubPullTracker.Controllers
{
    public class AuthController : ControllerBase
    {
        [GET("login", SitePrecedence = 1)]
        public ActionResult SignIn()
        {
            return View();
        }

        [GET("logout", SitePrecedence = 2)]
        public ActionResult SignOut()
        {
            CurrentUser = null;

            return RedirectToAction("signin");
        }

        [GET("login/github", SitePrecedence = 1)]
        public ActionResult SignInStart()
        {

            string csrf = Membership.GeneratePassword(24, 1);
            TempData["CSRF:State"] = csrf;
            var clientId = SettingsManager.Settings.GetSetting("Github.ClientId");
            var request = new OauthLoginRequest(clientId)
            {
                Scopes = { "repo" },
                State = csrf,
            };

            // NOTE: user must be navigated to this URL
            var oauthLoginUrl = Client.Oauth.GetGitHubLoginUrl(request);


            return Redirect(oauthLoginUrl.ToString());
        }

        [GET("login/github-callback", SitePrecedence = 1)]
        public async Task<ActionResult> SignInComplete(string code, string state)
        {
            if (String.IsNullOrEmpty(code))
                return RedirectToAction("signin");

            var expectedState = TempData["CSRF:State"] as string;
            if (state != expectedState) throw new InvalidOperationException("SECURITY FAIL!");

            var clientId = SettingsManager.Settings.GetSetting("Github.ClientId");

            var clientSecret = SettingsManager.Settings.GetSetting("Github.ClientSecret");
            
            var request = new OauthTokenRequest(clientId, clientSecret, code);
            var token = await Client.Oauth.CreateAccessToken(request);

            Client.Credentials = new Credentials(token.AccessToken);

            var currentUser = await Client.User.Current();

            CurrentUser = new GithubUser()
            {
                AuthKey = token.AccessToken,
                UserName = currentUser.Login,
                AvartarUrl = currentUser.AvatarUrl,
                ProfileUrl = currentUser.HtmlUrl
            };
            
            return RedirectToAction("Search", "Home");
        }
    }
}