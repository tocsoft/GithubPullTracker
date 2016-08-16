using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GithubPullTracker.Models
{
    
    public static class HtmlExtensions
    {
        public static IHtmlString JavascriptString(this HtmlHelper html, object str)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(str);
            return html.Raw(json);
        }
    }
}