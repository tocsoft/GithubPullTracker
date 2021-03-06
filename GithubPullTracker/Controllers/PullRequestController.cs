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
using GithubPullTracker.DataStore;
using System.Security.Cryptography;
using GithubClient.Models;

namespace GithubPullTracker.Controllers
{
    public class PullRequestController : ControllerBase
    {

        private async Task<PullRequestView> GetPullRequestDetails(string owner, string repo, int number)
        {

            var store = new RepoStore();
            var prTask = Client.PullRequest(owner, repo, number);
            
            var assignees = Client.Assignees(owner, repo, number);
            var approvalsTask = store.GetApprovals(owner, repo, number);
            var repoSettingsTask = store.GetOwnerConfig(owner, repo);
            await Task.WhenAll(prTask,  assignees, approvalsTask, repoSettingsTask);

            return new PullRequestView(CurrentUser, prTask.Result, assignees.Result, approvalsTask.Result, repoSettingsTask.Result);
        }
        [Auth]
        [POST("{owner}/{repo}/pull/{reference}/approve")]
        public async Task<ActionResult> Approve(string owner, string repo, int reference, string headSha, bool approved, string path = null)
        {
            Store.UpdateApproval(owner, repo, reference, headSha, CurrentUser.UserName, approved);
            await Store.Flush();

            var pullRequest= await GetPullRequestDetails(owner, repo, reference);

            var reposettings = await Store.GetRepoSettings(owner, repo);

            var privateenabled = reposettings.PrivateEnabled;
            var publicenabled = reposettings.PublicEnabled && !pullRequest.IsPrivate;
            if (publicenabled || privateenabled)//no enabled eather way
            {
                if (pullRequest.IsPrivate)
                {
                    //getorg settings
                    var ownerSettings = await Store.GetOwnerSettings(owner);
                    if (ownerSettings.SubscriptionExpires <= DateTime.UtcNow)
                    {
                        return RedirectToAction("ViewPullRequest", new { owner, repo, reference, error="subscriptionexpired" });
                    }
                }

                var ownerClient = new GithubClient.Client(Client.UserAgent) { AccessToken = reposettings.GetAuthToken() };

                var settings = await Store.GetPullRequestSettings(owner, repo, reference);

                var statuslist= await ownerClient.GetStatuses(owner, repo, headSha);

                var details = pullRequest;
                settings.Approved = details.ExpectedStatus == CommitStatus.success;

                var currentstatus = statuslist.statuses.Where(x => x.context == RepositoryController.statuscontext).SingleOrDefault();
                var status = currentstatus?.state ?? CommitStatus.error;
                var description = currentstatus?.description;

                if (status != details.ExpectedStatus || description != details.StatusDescription)
                {

                    var url = Request.Url.GetLeftPart(UriPartial.Authority) + $"/{owner}/{repo}/pull/{reference}";
                    //we could enque this update for a background task to complete???
                    await ownerClient.SetStatus(owner, repo, headSha, details.ExpectedStatus, url, details.StatusDescription, RepositoryController.statuscontext);
                }

                //all now approved

                Store.UpdatePullRequestSettings(settings);
                await Store.Flush();
            }

            if (path == null) {
                return RedirectToAction("ViewPullRequest", new { owner, repo, reference });
            }else
            {
                return RedirectToAction("ViewFiles", new { owner, repo, reference, path = path });
            }
        }

        [Auth]
        [GET("{owner}/{repo}/pull/{reference}")]
        public async Task<ActionResult> ViewPullRequest(string owner, string repo, int reference)
        {
            var pullRequestTask = GetPullRequestDetails(owner, repo, reference);

            var issueTask = Client.Issue(owner, repo, reference);
            
            var commentsTask =  Client.FileComments(owner, repo, reference);            
            
            var timelineTask =  Client.Timeline(owner, repo, reference);

            var issueCommentsTask = Client.Comments(owner, repo, reference);
            var commitsTask = Client.Commits(owner, repo, reference);
            await Task.WhenAll(pullRequestTask, issueTask, commentsTask, timelineTask, issueCommentsTask, commitsTask);

            var pr = new PullRequestCommentsView(pullRequestTask.Result, issueTask.Result, commentsTask.Result, timelineTask.Result, issueCommentsTask.Result, commitsTask.Result);
            return View(pr);
        }

        [GET("{owner}/{repo}/pull/{reference}/files/{*path}")]
        public async Task<ActionResult> ViewFiles(string owner, string repo, int reference, string path = null, string sha = null, string filesha = null)
        {
            var repoStore = new RepoStore();
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

                if (!string.IsNullOrEmpty(filesha))
                {
                    repoStore.AddFileView(owner, repo, reference, filesha, CurrentUser.UserName);
                }
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    contents = sourceText,
                    isBinary = isBinaryDataType,
                    missing = fileMissing,
                });

                await repoStore.Flush();

                return Content(json, "application/json");
            }

            var files = await Client.Files(owner, repo, reference);

            if (path == null)
            {
                path = files.First().filename;
                return RedirectToAction("ViewFiles", new { owner, repo, reference, path });
            }
            else
            {

                var pullRequest = await GetPullRequestDetails(owner, repo, reference);

                path = path.TrimEnd('/');

                var file = files.Where(x => x.filename == path).Single();

                repoStore.AddFileView(owner, repo, reference, file.sha, CurrentUser.UserName);

                string sourceText = "";
                bool isBinaryDataType = false;

                try
                {
                    if (file.status != "removed")
                    {
                        var sourceFile = await Client.FileContents(owner, repo, path, pullRequest.HeadSha);
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
                var visistedFiles = await repoStore.ListFileViews(owner, repo, reference, CurrentUser.UserName);

                var comments = await Client.FileComments(owner, repo, reference);
                var vm = new PullRequestFileView(pullRequest, files, path, sourceText, isBinaryDataType, comments, visistedFiles);


                return View(vm);
            }
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

