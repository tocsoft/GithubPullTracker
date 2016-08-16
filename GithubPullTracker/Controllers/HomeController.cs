using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GithubPullTracker.Models;
using Octokit;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Net;
using AttributeRouting.Web.Mvc;

namespace GithubPullTracker.Controllers
{
    public class HomeController : ControllerBase
    {
        [GET("")]
        public async Task<ActionResult> Home(string query, int page = 1, RequestState? state = null, RequestConnection? type = null)
        {
            if(CurrentUser == null)
            {
                return View("SignIn");
            }

            string viewPage = "Search";

            var realState = state ?? RequestState.Open;

            List<IssueIsQualifier> isQual = new List<IssueIsQualifier>();
            isQual.Add(IssueIsQualifier.PullRequest);

            switch (realState)
            {
                case RequestState.Open:
                    isQual.Add(IssueIsQualifier.Open);
                    break;
                case RequestState.Closed:
                    isQual.Add(IssueIsQualifier.Closed);
                    break;

            }

            var request = new Octokit.SearchIssuesRequest
            {
                Is = isQual,
                Involves = CurrentUser.UserName,
            };
                var realType = type ?? RequestConnection.Assigned;
                switch (realType)
                {
                    case RequestConnection.Involved:
                        request.Assignee = CurrentUser.UserName;
                        break;
                    case RequestConnection.Created:
                        request.Author = CurrentUser.UserName;
                        break;
                    case RequestConnection.Assigned:
                        request.Involves = CurrentUser.UserName;
                        break;
                }
         

            request.Page = page;
            request.PerPage = 25;

            var results = await this.Client.Search.SearchIssues(request);
            var vm = new PullRequestResult(1, 25, results);

            return View(viewPage, vm);
        }

        [Auth]
        [GET("{owner}/{repo}")]
        [GET("{owner}")]
        public async Task<ActionResult> Search(string query, string owner, string repo = null, int page = 1, RequestState? state = null, RequestConnection? type = null)
        {
            string viewPage = "Search";

            var realState = state ?? RequestState.Open;

            List<IssueIsQualifier> isQual = new List<IssueIsQualifier>();
            isQual.Add(IssueIsQualifier.PullRequest);

            switch (realState)
            {
                case RequestState.Open:
                    isQual.Add(IssueIsQualifier.Open);
                    break;
                case RequestState.Closed:
                    isQual.Add(IssueIsQualifier.Closed);
                    break;

            }

            var request = new Octokit.SearchIssuesRequest
            {
                Is = isQual,
                Involves = CurrentUser.UserName,
            };


            if (repo == null)
            {
                request.Involves = owner;
                viewPage = "SearchOwner";
            }
            else
            {
                request.Repos.Add($"{owner}/{repo}");
                viewPage = "SearchRepo";
            }

            request.Page = page;
            request.PerPage = 25;

            var results = await this.Client.Search.SearchIssues(request);
            var vm = new PullRequestResult(1, 25, results);
            vm.RepoName = repo;
            vm.Owner = owner;
            return View(viewPage, vm);
        }
        
        public enum RequestConnection
        {
            Involved,
            Created,
            Assigned
        }

        public enum RequestState
        {
            Open,
            Closed,
        }

        public static bool isBinary(byte[] data)
        {
            long length = data.Length;
            if (length == 0) return false;

            using (StreamReader stream = new StreamReader(new MemoryStream(data)))
            {
                int ch;
                while ((ch = stream.Read()) != -1)
                {
                    if (isControlChar(ch))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool isControlChar(int ch)
        {
            return (ch > Chars.NUL && ch < Chars.BS)
                || (ch > Chars.CR && ch < Chars.SUB);
        }

        public static class Chars
        {
            public static char NUL = (char)0; // Null char
            public static char BS = (char)8; // Back Space
            public static char CR = (char)13; // Carriage Return
            public static char SUB = (char)26; // Substitute
        }
    }
}

