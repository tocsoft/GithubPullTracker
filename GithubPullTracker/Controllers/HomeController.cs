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

namespace GithubPullTracker.Controllers
{
    [RoutePrefix("")]
    [Auth]
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
        
        [Route("{owner}/{repo}/pull/{reference}/contents/{*path}")]
        public async Task<ActionResult> GetFile(string owner, string repo, int reference, string path)
        {
            var fileExtension = Path.GetExtension(path).ToLower();

            var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

            var files = await Client.PullRequest.Files(owner, repo, reference);
            var file = files.Where(x => x.FileName == path).Single();

            //Client.PullRequest.Comment.Create(owner, repo, reference, new PullRequestReviewCommentCreate("", pullRequest.Head.Sha, path, ) );

            PageMap map = MapPatchToFiles(file.Patch);

            var comments = await Client.PullRequest.Comment.GetAll(owner, repo, reference);
            var fileComments = comments.Where(x => x.Path == path).Select(x => new
            {
                body = x.Body,
                commenter = new { avatarUrl = x.User.AvatarUrl, login = x.User.Login },
                createdAt = x.CreatedAt,
                x.Position

            })
            .Select(x => new
            {

                x.body,
                x.commenter,
                x.createdAt,
                sourceLine = map.PatchToSourceLineNumber(x.Position) ?? -1,
                targetLine = map.PatchToTargetLineNumber(x.Position) ?? -1

            }).ToList();


            //we are loading the json compare data here
            string sourceText = null;
            bool isBinaryDataType = false;

            if (file.Status != "added")
            {
                var source = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, pullRequest.Base.Sha);
                
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
            }else
            {
                sourceText = "";
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                source = sourceText,
                status = file.Status,
                patch = file.Patch,
                pageMap = map,
                isBinary = isBinaryDataType,
                comments = fileComments
            });
            return Content(json, "application/json");

        }

        [Route("{owner}/{repo}/pull/{reference}")]
        [Route("{owner}/{repo}/pull/{reference}/files/{*path}")]
        public async Task<ActionResult> ViewPullRequest(string owner, string repo, int reference, string path = null)
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
                var text = lines[line - 1];
                if (text.StartsWith("@@"))
                {
                    var match = Regex.Match(text, @"^\@\@\s?(?:-(\d*),(\d*))?\s?(?:\+(\d*),(\d*))?\s?\@\@");
                    sourcePageIndex = int.Parse(match.Groups[1].Value) -1;
                    targetPageIndex = int.Parse(match.Groups[3].Value) -1 ;
                }else
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

            }

            return map;
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

