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
        public class PageMap
        {
            public IDictionary<int, int> SourceFile = new Dictionary<int, int>();
            public IDictionary<int, int> TargetFile = new Dictionary<int, int>();

            public int? PatchToTargetLineNumber(int? patchnumber)
            {
                if (patchnumber == null)
                { return null; }
                var parts = TargetFile.Where(x => x.Value == patchnumber).Select(x => x.Key).ToArray();
                if (parts.Length > 0)
                {
                    return parts[0];
                }
                return null;
            }

            public int? PatchToSourceLineNumber(int? patchnumber)
            {
                if (patchnumber == null)
                { return null; }
                var parts = SourceFile.Where(x => x.Value == patchnumber).Select(x => x.Key).ToArray();
                if (parts.Length > 0)
                {
                    return parts[0];
                }
                return null;
            }
        }

        [Auth]
        [GET("{owner}/{repo}/pull/{reference}/contents/{*path}")]
        public async Task<ActionResult> GetFile(string owner, string repo, int reference, string path, string expectedSha)
        {
           // var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            //if (pullRequest.Head.Sha != expectedSha)
            //{
            //        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
            //        {
            //            headSha = pullRequest.Head.Sha
            //        }), "application/json");
            //}
            
            //var files = await Client.PullRequest.Files(owner, repo, reference);
            //var file = files.Where(x => x.FileName == path).SingleOrDefault();

            //if (file == null)
            //{
            //    return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
            //    {
            //        headSha = pullRequest.Head.Sha,
            //        notfound = true,
            //    }), "application/json");
            //}

            //PageMap map = MapPatchToFiles(file.Patch);


            //we are loading the json compare data here
            string sourceText = null;
            bool isBinaryDataType = false;

            var source = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, expectedSha);

            var sourceFile = source.SingleOrDefault();
            if (sourceFile != null)
            {
                var data = Convert.FromBase64String(sourceFile.EncodedContent);
                isBinaryDataType = isBinary(data);
                if (!isBinaryDataType)
                {
                    sourceText = sourceFile.Content;
                }
            }

            //if (file.Status != "added" && file.Status != "renamed")
            //{
            //    var source = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, pullRequest.Base.Sha);

            //    var sourceFile = source.SingleOrDefault();
            //    if (sourceFile != null)
            //    {
            //        var data = Convert.FromBase64String(sourceFile.EncodedContent);
            //        isBinaryDataType = isBinary(data);
            //        if (!isBinaryDataType)
            //        {
            //            sourceText = sourceFile.Content;
            //        }
            //    }
            //}
            //else
            //{
            //    sourceText = "";
            //}

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                //headSha = pullRequest.Head.Sha,
                target = sourceText,
                //patch = file.Patch,
                //pageMap = map,
                //change = file.Status,
                isBinary = isBinaryDataType
            });
            return Content(json, "application/json");
        }


        [Auth]
        [GET("{owner}/{repo}/pull/{reference}/comments")]
        public async Task<ActionResult> GetFileComments(string owner, string repo, int reference, string expectedSha)
        {
            var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            if (pullRequest.Head.Sha != expectedSha)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    headSha = pullRequest.Head.Sha
                }), "application/json");
            }

            var files = await Client.PullRequest.Files(owner, repo, reference);

            var comments = await Client.PullRequest.Comment.GetAll(owner, repo, reference);
            
            var commentedFiles = comments.Select(x => x.Path).ToList();
            var issueComments = await Client.Issue.Comment.GetAllForIssue(owner, repo, reference, new ApiOptions { PageSize = 1000 });

            var maps = files
                        .Where(x=> commentedFiles.Contains(x.FileName))//only parse patch files we will need
                        .ToDictionary(x => x.FileName, x => MapPatchToFiles(x.Patch));

            var convertedComments = comments.Select(x => new Comment(x, maps[x.Path]));
            var allComments = convertedComments.Union(issueComments.Select(x => new Comment(x))).OrderBy(x=>x.createdAt);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                headSha = pullRequest.Head.Sha,
                comments = allComments
            });
            return Content(json, "application/json");

        }


        [Auth]
        [GET("{owner}/{repo}/pull/{reference}/files")]
        public async Task<ActionResult> GetFileList(string owner, string repo, int reference, string expectedSha)
        {
            var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            if (pullRequest.Head.Sha != expectedSha)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    headSha = pullRequest.Head.Sha
                }), "application/json");
            }

            //todo load file list via ajax
            var files = await Client.PullRequest.Files(owner, repo, reference);
            
            var pr = new PullFileList(pullRequest, files);
            var json = pr.TreeData.ToString();
            return Content(json, "application/json");

        }


        [Auth]
        [GET("{owner}/{repo}/pull/{reference}")]
        [GET("{owner}/{repo}/pull/{reference}/files/{*path}")]
        public async Task<ActionResult> ViewPullRequest(string owner, string repo, int reference, string path = null)
        {
            var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            var files = await Client.PullRequest.Files(owner, repo, reference);
            
            if (path != null)
            {
                path = path.TrimEnd('/');
                var vm = new PullFileList(pullRequest, files, path);
                return View(vm);
            }

            var pr = new PullFileList(pullRequest, files);
            return View(pr);
        }

        private static PageMap MapPatchToFiles(string patch)
        {
            var map = new PageMap();
            if (string.IsNullOrWhiteSpace(patch))
            {
                return map;
            }
            var lines = patch.Split('\n');

            int sourcePageIndex = 0;
            int targetPageIndex = 0;
            for (var line = 1; line <= lines.Length; line++)
            {
                try
                {
                    var text = lines[line - 1];
                    if (text.StartsWith("@@"))
                    {
                        var match = Regex.Match(text, @"^\@\@\s?(?:-(\d*),(\d*))?\s?(?:\+(\d*),(\d*))?\s?\@\@");
                        sourcePageIndex = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) - 1 : -1;
                        targetPageIndex = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) - 1 : -1;
                    }
                    else
                    {
                        var prefix = text[0];
                        if (prefix == '-')
                        {
                            map.SourceFile.Add(++sourcePageIndex, line);
                        }
                        else if (prefix == ' ')
                        {
                            map.TargetFile.Add(++targetPageIndex, line);
                            map.SourceFile.Add(++sourcePageIndex, -1);
                            //++sourcePageStart;//only increment source but allow commenting on right
                        }
                        if (prefix == '+')
                        {
                            map.TargetFile.Add(++targetPageIndex, line);
                        }
                    }
                }catch(Exception ex)
                {
                    throw;
                }

            }

            return map;
        }

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

