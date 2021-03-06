﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using GithubClient;
using GithubPullTracker.DataStore;
using GithubPullTracker.Models;
using Newtonsoft.Json;

namespace GithubPullTracker.Controllers
{
    public abstract class ControllerBase : Controller
    {
        const string cookieName = "pull-tracker-auth";
        static ControllerBase()
        {

        }

        public GithubUser CurrentUser { get { return ViewData["__CurrentUser__"] as GithubUser; } protected set { ViewData["__CurrentUser__"] = value; } }

        public GithubClient.Client Client { get; private set; } = new GithubClient.Client("pull-tracker");

        public RepoStore Store { get; private set; } = new RepoStore();

        protected override IAsyncResult BeginExecute(RequestContext requestContext, AsyncCallback callback, object state)
        {
            //load up the auth from cookie
            var cookie = requestContext.HttpContext.Request.Cookies.Get(cookieName);

            if(cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                try
                {
                    var data = Convert.FromBase64String(cookie.Value);
                    data = MachineKey.Unprotect(data, "authentication");
                    var json = Encoding.UTF8.GetString(data);
                    CurrentUser = JsonConvert.DeserializeObject<GithubUser>(json);
                }
                catch {
                    CurrentUser = null;
                }
            }

            if (CurrentUser != null)
            {
                Client.AccessToken = CurrentUser.AuthKey;
            }

            return base.BeginExecute(requestContext, callback, state);
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if(filterContext.Exception is UnauthorizedAccessException)
            {
                filterContext.ExceptionHandled = true;
                filterContext.Result = new HttpUnauthorizedResult();
                return;
            }

            var ex = filterContext.Exception as RestException;
            if (ex != null)
            {
                if (ex.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    CurrentUser = null;
                    filterContext.ExceptionHandled = true;
                    filterContext.Result = new HttpUnauthorizedResult();
                }
                return;
            }
            base.OnException(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            if (CurrentUser == null)
            {
                filterContext.HttpContext.Response.SetCookie(new HttpCookie(cookieName) { Expires = DateTime.Now.AddDays(-1) });//expire the auth cookie
            }
            else
            {
                var json = JsonConvert.SerializeObject(CurrentUser);
                var data = Encoding.UTF8.GetBytes(json);
                data = MachineKey.Protect(data, "authentication");

                filterContext.HttpContext.Response.SetCookie(new HttpCookie(cookieName)
                {
                    Value = Convert.ToBase64String(data),
                    Expires = DateTime.Now.AddDays(30) //resign in after 30 days of inactivity
                });
            }
        }
    }
}