﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GithubPullTracker.Models;
using Octokit;

namespace GithubPullTracker.Controllers
{
    [RoutePrefix("")]
    [Auth]
    public class HomeController : ControllerBase
    {

        [Route("{owner}/{repo}/pull/{reference}")]
        [Route("{owner}/{repo}/pull/{reference}/files/{*path}")]
        public async Task<ActionResult> ViewPullRequest(string owner , string repo, int reference, string path = null)
        {
            
            var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            var files = await Client.PullRequest.Files(owner, repo, reference);

            if (path != null)
            {
                path = path.TrimEnd('/');
                var vm = new PullRequestView(pullRequest, files, path);
                return View(vm);
            }

            var pr = new PullRequestView(pullRequest, files);
            return View(pr);
        }

        [Route("")]
        public async Task<ActionResult> Search(string query, int page =1, RequestState? state = null, RequestConnection? type = null)
        {
            var realType = type ?? RequestConnection.Assigned;
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
            return View(vm);
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
    }
}