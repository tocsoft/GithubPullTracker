using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GithubPullTracker
{
    public class AuthAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (filterContext.ActionDescriptor != null)
            {
                if (filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ||
                    filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
                {
                    return;
                }
            }

            var cb = (filterContext.Controller as Controllers.ControllerBase);
            if (cb == null)
            {
                throw new InvalidCastException("Controller must inherit from 'ControllerBase'");
            }
            if (cb != null)
            {
                if (cb.CurrentUser == null)
                {
                    filterContext.Result = new HttpUnauthorizedResult();
                    return;
                }
            }
        }
    }
}