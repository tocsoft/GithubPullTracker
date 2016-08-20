﻿using System;
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

namespace GithubPullTracker.Controllers
{
    public class PullRequestController : ControllerBase
    {

        //[Auth]
        //[GET("{owner}/{repo}/pull/{reference}/contents/{*path}")]
        //public async Task<ActionResult> GetFile(string owner, string repo, int reference, string path, string expectedSha)
        //{
        //    string sourceText = null;
        //    bool isBinaryDataType = false;

        //    var source = await Client.Repository.Content.GetAllContentsByRef(owner, repo, path, expectedSha);

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
            
        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
        //    {
        //        target = sourceText,
        //        isBinary = isBinaryDataType
        //    });
        //    return Content(json, "application/json");
        //}

        //[Auth]
        //[GET("{owner}/{repo}/pull/{reference}/comments")]
        //public async Task<ActionResult> GetFileComments(string owner, string repo, int reference, string expectedSha)
        //{
        //    var pullRequest = await Client.PullRequest.Get(owner, repo, reference);

        //    if (pullRequest.Head.Sha != expectedSha)
        //    {
        //        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
        //        {
        //            headSha = pullRequest.Head.Sha
        //        }), "application/json");
        //    }

        //    var files = await Client.PullRequest.Files(owner, repo, reference);

        //    var comments = await Client.PullRequest.Comment.GetAll(owner, repo, reference);
            
        //    var commentedFiles = comments.Select(x => x.Path).ToList();
        //    var issueComments = await Client.Issue.Comment.GetAllForIssue(owner, repo, reference, new ApiOptions { PageSize = 1000 });

        //    var maps = files
        //                .Where(x=> commentedFiles.Contains(x.FileName))//only parse patch files we will need
        //                .ToDictionary(x => x.FileName, x => new PageMap(x.Patch));

        //    var convertedComments = comments.Select(x => new Comment(x, maps[x.Path]));
        //    var allComments = convertedComments.Union(issueComments.Select(x => new Comment(x))).OrderBy(x=>x.createdAt);

        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
        //    {
        //        headSha = pullRequest.Head.Sha,
        //        comments = allComments
        //    });
        //    return Content(json, "application/json");

        //}

        [Auth]
        [GET("{owner}/{repo}/pull/{reference}")]
        public async Task<ActionResult> ViewPullRequest(string owner, string repo, int reference)
        {
            var pullRequestTask = Client.PullRequest(owner, repo, reference);

            var issueTask = Client.Issue(owner, repo, reference);

            var commitsTask =  Client.Commits(owner, repo, reference);

            var commentsTask =  Client.FileComments(owner, repo, reference);
            
            var issueCommentsTask =  Client.Comments(owner, repo, reference);

           // var events = await Client.Events(owner, repo, reference);

            var timelineTask =  Client.Timeline(owner, repo, reference);

            await Task.WhenAll(pullRequestTask, issueTask, commitsTask, commentsTask, issueCommentsTask, timelineTask);

            var pr = new PullRequestCommentsView(pullRequestTask.Result, issueTask.Result, commitsTask.Result, issueCommentsTask.Result, commentsTask.Result, timelineTask.Result);
            return View(pr);
        }

        [GET("{owner}/{repo}/pull/{reference}/files/{*path}")]
        public async Task<ActionResult> ViewFiles(string owner, string repo, int reference, string path = null, string sha = null)
        {
            if (Request.IsAjaxRequest())
            {
                string sourceText = "";
                bool fileMissing = false;
                bool isBinaryDataType = false;
                try
                {
                    var sourceFile = await Client.FileContents(owner, repo, path, sha);
                    if (sourceFile != null)
                    {
                        if (sourceFile.encoding == "base64")
                        {
                            var data = Convert.FromBase64String(sourceFile.content);
                            
                            sourceText = readData(data, out isBinaryDataType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    fileMissing = true;
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    contents = sourceText,
                    isBinary = isBinaryDataType,
                    missing = fileMissing,
                });

                return Content(json, "application/json");
            }

            var files = await Client.Files(owner, repo, reference);
            if(path == null)
            {
                path = files.First().filename;
                return RedirectToAction("ViewFiles", new { owner, repo, reference, path });
            }

            var pullRequest = await Client.PullRequest(owner, repo, reference);

            if (path != null)
            {
                path = path.TrimEnd('/');

                var file = files.Where(x => x.filename == path).Single();
                
                string sourceText = "";
                bool isBinaryDataType = false;
                
                try
                {
                    if (file.status != "removed")
                    {
                        var sourceFile = await Client.FileContents(owner, repo, path, pullRequest.Head.sha);
                        if (sourceFile != null)
                        {
                            if (sourceFile.encoding == "base64")
                            {
                                var data = Convert.FromBase64String(sourceFile.content);

                                sourceText = readData(data, out isBinaryDataType);
                            }
                        }                        
                    }
                }
                catch (Exception ex)
                {
                }

                var comments = await Client.FileComments(owner, repo, reference);
                var vm = new PullRequestFileView(pullRequest, files, path, sourceText, isBinaryDataType, comments);


                return View(vm);
            }

            var pr = new PullRequestFileView(pullRequest, files);
            return View(pr);
        }

        public static string readData(byte[] data, out bool isBinary)
        {
            isBinary = false;
            long length = data.Length;
            if (length == 0) return "";

            StringBuilder sb = new StringBuilder();
            using (StreamReader stream = new StreamReader(new MemoryStream(data)))
            {
                int ch;
                while ((ch = stream.Read()) != -1)
                {
                    if (isControlChar(ch))
                    {
                        isBinary = true;
                        return "";
                    }
                    sb.Append((char)ch);
                }
            }
            return sb.ToString();
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

