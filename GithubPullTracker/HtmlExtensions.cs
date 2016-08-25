using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace GithubPullTracker.Models
{
    
    public static class HtmlExtensions
    {
        public static IHtmlString JavascriptString(this HtmlHelper html, object str)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(str, Newtonsoft.Json.Formatting.Indented);
            json = new Regex(@"(\<\/?scr)(ipt.*?>)", RegexOptions.IgnoreCase).Replace(json, "$1\"+\"$2");
            return html.Raw(json);
        }
    }
}