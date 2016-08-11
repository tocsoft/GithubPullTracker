using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GithubPullTracker.Models;
using Octokit;
using System.Net.Http;

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
            if (this.Request.IsAjaxRequest())
            {
                var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

                var files = await Client.PullRequest.Files(owner, repo, reference);
                var file = files.Where(x => x.FileName == path).Single();
                var comments = await Client.PullRequest.Comment.GetAll(owner, repo, reference);



                var fileComments = comments.Where(x => x.Path == path).Select(x=> new {
                    comment = x,
                    diff = x.DiffHunk.Split('\n').Last()[0]
                });

                var sourceComments = fileComments
                    .Where(x=>x.diff == '-')
                    .Select(x=>x.comment)
                    .GroupBy(x => x.Position)
                    
                    .ToDictionary(x => x.Key, x => x.Select(c => new
                    {
                        c.Body,
                        Commenter = new { c.User.AvatarUrl, c.User.Login },
                        c.CreatedAt
                    }).ToList());

                var targetComments = fileComments
                    .Where(x => x.diff == '+' || x.diff == ' ')
                    .Select(x => x.comment)
                    .GroupBy(x => x.Position)                    
                   .ToDictionary(x => x.Key, x => x.Select(c => new
                   {
                       c.Body,
                       Commenter = new { c.User.AvatarUrl, c.User.Login },
                       c.CreatedAt
                   }).ToList());

                //we are loading the json compare data here
                string targetText = "";
                if (file.Status != "removed")
                {
                    var target = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, pullRequest.Head.Sha);
                    targetText = target.SingleOrDefault()?.Content;
                }
                string sourceText = "";

                if (file.Status != "added")
                {
                    var source = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, pullRequest.Base.Sha);
                    sourceText = source.SingleOrDefault()?.Content;
                }
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    source = sourceText,
                    target = targetText,
                    sourceSha = pullRequest.Base.Sha,
                    targetSha = pullRequest.Head.Sha,
                    sourceComments = sourceComments,
                    targetComments = targetComments
                });
                return Content(json, "application/json");
            }
            else
            {
                
                var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

                //todo load file list via ajax
                var files = await Client.PullRequest.Files(owner, repo, reference);

                //var comments = await Client.PullRequest.Comment.GetAll(owner, repo, reference);

                if (path != null)
                {
                    path = path.TrimEnd('/');
                    var vm = new PullRequestView(pullRequest, files, path);
                    return View(vm);
                }

                var pr = new PullRequestView(pullRequest, files);
                return View(pr);
            }
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