using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GithubPullTracker.Models;
using AttributeRouting.Web.Mvc;
using GithubClient;

namespace GithubPullTracker.Controllers
{
    public class AuthController : ControllerBase
    {

        [GET("logout", SitePrecedence = 2)]
        public ActionResult SignOut()
        {
            CurrentUser = null;

            return RedirectToAction("signin");
        }

        [GET("login", SitePrecedence = 1)]
        public ActionResult SignInStart()
        {

            string csrf = Membership.GeneratePassword(24, 1);
            TempData["CSRF:State"] = csrf;
            var clientId = SettingsManager.Settings.GetSetting("Github.ClientId");

            var request = new OAuthRedirectUrlBuilder(clientId)
            {
                Scopes = { "repo" },
                State = csrf,
            };
            
            return Redirect(request.Build());
        }

        [GET("login/github-callback", SitePrecedence = 2)]
        public async Task<ActionResult> SignInComplete(string code, string state)
        {
            if (String.IsNullOrEmpty(code))
                return RedirectToAction("signin");

            var expectedState = TempData["CSRF:State"] as string;
            if (state != expectedState) throw new InvalidOperationException("SECURITY FAIL!");

            var clientId = SettingsManager.Settings.GetSetting("Github.ClientId");

            var clientSecret = SettingsManager.Settings.GetSetting("Github.ClientSecret");


            var token = await Client.CreateAccessToken(clientId, clientSecret, code);

            Client.AccessToken = token.access_token;

            var currentUser = await Client.CurrentUser();

            CurrentUser = new GithubUser()
            {
                AuthKey = token.access_token,
                UserName = currentUser.login,
                AvartarUrl = currentUser.avatar_url,
                ProfileUrl = currentUser.html_url
            };
            
            return RedirectToAction("Home", "Home");
        }
    }
}