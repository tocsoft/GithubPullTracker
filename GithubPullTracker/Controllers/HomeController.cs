using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GithubPullTracker.Models;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Net;
using AttributeRouting.Web.Mvc;
using GithubClient;

namespace GithubPullTracker.Controllers
{
    public class HomeController : ControllerBase
    {
        [GET("")]
        public async Task<ActionResult> Search(string query, int page = 1, RequestState? state = null, RequestConnection? type = null, SortOrder? order = null, SortOrderDirection? dir = null)
        {
            if(CurrentUser == null)
            {
                return View("SignIn");
            }
            

            var realState = state ?? RequestState.Open;
            var realType = type ?? RequestConnection.Assigned;
            var realOrder = order ?? SortOrder.bestmatch;
            var realdir = dir ?? SortOrderDirection.Decending;
            int pagesize = 30;

            var resultsTask = Client.SearchPullRequests(page, pagesize, realState, realOrder, realdir, realType, CurrentUser.UserName, query, null, null);

            var userRepoTask = Client.CurrentUsersRepos();

            await Task.WhenAll(resultsTask, userRepoTask);
            //var results = await this.Client.Search.SearchIssues(request);
            //var results = new SearchIssuesResult(); 
            var vm = new UserSearchResults(userRepoTask.Result,  page, pagesize, resultsTask.Result, realState, realOrder, realdir, realType, query);

            return View(vm);
        }


        [GET("{owner}")]
        public async Task<ActionResult> SearchOwner(string query, string owner, int page = 1, RequestState? state = null, SortOrder? order = null, SortOrderDirection? dir = null, RequestConnection? type = null)
        {

            var realState = state ?? RequestState.Open;
            var realOrder = order ?? SortOrder.bestmatch;
            var realdir = dir ?? SortOrderDirection.Decending;
            var realType = type ?? RequestConnection.All;
            int pagesize = 25;

            var resultsTask = Client.SearchPullRequests(page, pagesize, realState, realOrder, realdir, realType, CurrentUser.UserName, query, owner, null);
            var repoTask = Client.AllRepos(owner);
            var userRepoTask = Client.CurrentUsersRepos();
            var ownerTask =  Client.User(owner);

            await Task.WhenAll(resultsTask, repoTask, ownerTask, userRepoTask);

            var repoList = userRepoTask.Result.Where(x => x.owner.login.Equals(owner, StringComparison.InvariantCultureIgnoreCase))
                .Union(repoTask.Result)
                .GroupBy(x => x.name)
                .Select(x => x.First());

            var vm = new OwnerSearchResults(ownerTask.Result, repoList, page, pagesize, resultsTask.Result, realState, realOrder, realdir, realType, query);

            return View(vm);
        }

        //[Auth]
        //[GET("{owner}/{repo}")]
        //[GET("{owner}")]
        //public async Task<ActionResult> Search(string query, string owner, string repo = null, int page = 1, RequestState? state = null, RequestConnection? type = null)
        //{
        //    string viewPage = "Search";

        //    var realState = state ?? RequestState.Open;

        //    List<IssueIsQualifier> isQual = new List<IssueIsQualifier>();
        //    isQual.Add(IssueIsQualifier.PullRequest);

        //    switch (realState)
        //    {
        //        case RequestState.Open:
        //            isQual.Add(IssueIsQualifier.Open);
        //            break;
        //        case RequestState.Closed:
        //            isQual.Add(IssueIsQualifier.Closed);
        //            break;

        //    }

        //    var request = new Octokit.SearchIssuesRequest
        //    {
        //        Is = isQual,
        //        Involves = CurrentUser.UserName,
        //    };


        //    if (repo == null)
        //    {
        //        request.Involves = owner;
        //        viewPage = "SearchOwner";
        //    }
        //    else
        //    {
        //        request.Repos.Add($"{owner}/{repo}");
        //        viewPage = "SearchRepo";
        //    }

        //    request.Page = page;
        //    request.PerPage = 25;

        //    //var results = await this.Client.Search.SearchIssues(request);
        //    var results = new SearchIssuesResult();
        //    var vm = new PullRequestResult(1, 25, results);
        //    vm.RepoName = repo;
        //    vm.Owner = owner;
        //    return View(viewPage, vm);
        //}


    }
}

