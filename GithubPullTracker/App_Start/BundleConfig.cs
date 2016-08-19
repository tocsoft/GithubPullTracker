using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace GithubPullTracker
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {

            bundles.Add(new ScriptBundle("~/Site.js")
                .Include("~/Scripts/jquery-{version}.js")
                .Include("~/Scripts/lscache.js")
                .Include("~/Scripts/marked.js")
                .Include("~/Scripts/timeago.js")
                .Include("~/Scripts/diff.js")
                .Include("~/Scripts/diff_match_patch.js")
                .Include("~/Scripts/splitter.js")
                .Include("~/Scripts/codemirror.js")
                .Include("~/Scripts/mode/meta.js")
                .Include("~/Scripts/addon/mode/loadmode.js")
                .Include("~/Scripts/addon/mode/overlay.js")
                .Include("~/Scripts/jquery.hotkeys.js")
                .Include("~/Scripts/addon/scroll/annotatescrollbar.js")
                .Include("~/Scripts/addon/scroll/scrollpastend.js")
                .Include("~/Scripts/addon/merge/merge.js")
                .Include("~/Scripts/site.js"));

            bundles.Add(new StyleBundle("~/Site.css")
                .Include("~/Content/codemirror.css")
                .Include("~/Content/addon/merge/merge.css")
                .Include("~/Content/addon/merge/merge.css")
                //.Include("~/Content/bootstrap.css")
                //.Include("~/Content/bootstrap-theme.css")
                //.Include("~/Content/bootstrap-treeview.css")
                .Include("~/Content/icomoon.css")
                .Include("~/Content/Site.css"));
        }
    }
}
