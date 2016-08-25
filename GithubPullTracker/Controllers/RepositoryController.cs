using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AttributeRouting.Web.Mvc;
using GithubClient;
using GithubClient.Models;
using GithubPullTracker.DataStore;
using GithubPullTracker.DataStore.Models;
using GithubPullTracker.Models;

namespace GithubPullTracker.Controllers
{
    public class RepositoryController : ControllerBase
    {
        static string WebHookProxyurl = SettingsManager.Settings.GetSetting("WebhookProxyUrl");
        string _webHookUrl;
        public string WebHookUrl
        {
            get
            {
                if (_webHookUrl == null)
                {
                    var server = WebHookProxyurl;
                    if (WebHookProxyurl == null)
                    {
                        server = Request.Url.GetLeftPart(UriPartial.Authority);
                    }
                    _webHookUrl = server + "/repositories/receive";
                }

                return _webHookUrl;
            }
        }

        private async Task<RepoSettingsViewModel> GetSettings(string owner, string repo)
        {
            var ghRepo = await Client.Repo(owner, repo);
            if (!ghRepo.permissions.admin)
            {
                throw new UnauthorizedAccessException();
            }
            var settingsTask = Store.GetOwnerSettings(owner);

            var hooksTask = Client.GetHooks(owner, repo);
            //todo retrive protected branches etc

            await Task.WhenAll(settingsTask, hooksTask, settingsTask);
            var hook = hooksTask.Result.Where(x => x.config.url.Equals(WebHookUrl, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var repoSettings = settingsTask.Result.Repositories.Where(x => x.Repo.Equals(repo, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            return new RepoSettingsViewModel(ghRepo,
                repoSettings ?? new DataStore.Models.RepoSettings(owner, repo),
                settingsTask.Result,
                hook,
                WebHookUrl);
        }

        [Auth]
        [GET("{owner}/{repo}/settings")]
        public async Task<ActionResult> Settings(string owner, string repo, string result)
        {
            var settings = await GetSettings(owner, repo);
            //we need to show the configure ate setting shere
            return View(settings);
        }



        [GET("{owner}/{repo}")]
        public async Task<ActionResult> SearchRepo(string query, string owner, string repo, int page = 1, RequestState? state = null, SortOrder? order = null, SortOrderDirection? dir = null, RequestConnection? type = null)
        {

            var realState = state ?? RequestState.Open;
            var realOrder = order ?? SortOrder.bestmatch;
            var realdir = dir ?? SortOrderDirection.Decending;
            var realType = type ?? RequestConnection.All;
            int pagesize = 25;

            var resultsTask = Client.SearchPullRequests(page, pagesize, realState, realOrder, realdir, realType, CurrentUser.UserName, query, owner, repo);
            var repoTask = Client.Repo(owner, repo);

            await Task.WhenAll(resultsTask, repoTask);

            var vm = new RepoSearchResults(repoTask.Result, page, pagesize, resultsTask.Result, realState, realOrder, realdir, realType, query);

            return View(vm);
        }


        [Auth]
        [POST("{owner}/{repo}/settings/configure")]
        public async Task<ActionResult> Configure(string owner, string repo, bool enabled)
        {
            var settings = await GetSettings(owner, repo);

            if (!settings.CanConfigure)
            {
                return RedirectToAction("Settings", new { result = "config_error" });
            }

            if (enabled)
            {
                await settings.UpdateAsEnabled(CurrentUser.AuthKey, Client);
            }
            else
            {
                await settings.UpdateAsDisabled(Client);
            }
            await settings.SaveChanges(Store);
            return RedirectToAction("Settings", new { result = "config_success" });
        }

        const string statuscontext = "codereview/pulltracker";

        [POST("repositories/receive")]
        public async System.Threading.Tasks.Task<ActionResult> WebhookRecieve()
        {
            if (!Request.Headers.AllKeys.Contains("X-GitHub-Event", StringComparer.OrdinalIgnoreCase))
            {
                return Content("unknow event");//lets just swallow it and ignore earlly
            }

            var req = Request.InputStream;
            var json = new StreamReader(req).ReadToEnd();
            var eventType = Request.Headers["X-GitHub-Event"].ToLower();

            Webhook hook;
            if (eventType == "pull_request")
            {
                //only care about sepcial casing pull requests
                hook = Newtonsoft.Json.JsonConvert.DeserializeObject<PullRequestWebhook>(json);
            }
            else
            {
                hook = Newtonsoft.Json.JsonConvert.DeserializeObject<Webhook>(json);
            }
            //now we have a webhook we can determin if they have used the correct secret to sign it


            var store = new RepoStore();
            var settings = await store.GetRepoSettings(hook.repository.owner.login, hook.repository.name);

            var privateenabled = settings.PrivateEnabled && hook.repository.IsPrivate;
            var publicenabled = settings.PublicEnabled && !hook.repository.IsPrivate;
            if (!publicenabled && !privateenabled)//no enabled eather way
            {
                //lets just swallow it and ignore earlly don't have the ability to verify this let alone do anything with the data
                return Content("OK");
            }

            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(settings.WebhookSecret));
            var hashmessage = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
            var hash = BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            if (Request.Headers["X-Hub-Signature"] != ("sha1=" + hash))
            {
                //
                return Content("Invalid signature");
            }

            //we now can access the API in the context of the authorizor
            Client.AccessToken = settings.GetAuthToken();

            if (hook is PullRequestWebhook)
            {
                await RecievePullRequestHook((PullRequestWebhook)hook, store);
            }

            return Content("OK");

        }


        private async Task RecievePullRequestHook(PullRequestWebhook hook, RepoStore store)
        {
            //pr state change recalculate state and update status

            var owner = hook.repository.owner.login;
            var repo = hook.repository.name;
            var number = hook.pull_request.number;
            var assignees = Client.Assignees(owner, repo, number);
            var approvalsTask = store.GetApprovals(owner, repo, number);
            var statusTask = Client.GetStatuses(owner, repo, hook.pull_request.Head.sha);
            var cachedRepoSettingsTask = Store.GetPullRequestSettings(owner, repo, hook.pull_request.number);
            await Task.WhenAll(assignees, approvalsTask, statusTask, cachedRepoSettingsTask);

            var details = new PullRequestView(new GithubUser { UserName = hook.sender.login }, hook.pull_request, assignees.Result, approvalsTask.Result);

            var currentstatus = statusTask.Result.statuses.Where(x => x.context == statuscontext).SingleOrDefault();
            var status = currentstatus?.state ?? CommitStatus.error;
            var description = currentstatus?.description;

            var setting = cachedRepoSettingsTask.Result;
            setting.Approved = details.ExpectedStatus == CommitStatus.success;
            Store.UpdatePullRequestSettings(setting);
            await Store.Flush();

            if (status != details.ExpectedStatus || description != details.StatusDescription)
            {

                var url = Request.Url.GetLeftPart(UriPartial.Authority) + $"/{owner}/{repo}/pull/{number}";
                //we could enque this update for a background task to complete???
                await Client.SetStatus(owner, repo, hook.pull_request.Head.sha, details.ExpectedStatus, url, details.StatusDescription, statuscontext);
            }
            //don't really care about other PR based events 
        }
    }


}