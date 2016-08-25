using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using GithubClient.Models;
using GithubPullTracker.DataStore;
using GithubPullTracker.DataStore.Models;

namespace GithubPullTracker.Models
{
    public class RepoSettingsViewModel
    {
        static string[] hookEvents = new[] { "pull_request", "repository" };
        private readonly RepoSettings repoSettings;
        private readonly WebhokSettings _hook;
        private readonly Repo _repo;
        private readonly string targetHookUrl;

        public RepoSettingsViewModel(Repo repo, RepoSettings settings, OwnerConfig ownerSettings, WebhokSettings hook, string targetHookUrl)
        {
            this.targetHookUrl = targetHookUrl;
            _hook = hook;
            this.repoSettings = settings;
            _repo = repo;
            //lets calculate state here!!!
            Enabled = settings.PrivateEnabled || settings.PublicEnabled;

            WebhookMissConfigured = false;

            
            if(Enabled)
            {
                if (hook == null)
                {
                    WebhookMissConfigured = true;
                }else
                {
                    if(hook.config.url != targetHookUrl)
                    {
                        WebhookMissConfigured = true;
                    }

                    if(hookEvents.Any(x => !hook.events.Contains(x)))
                    {
                        //missing events needs fixing
                        WebhookMissConfigured = true;
                    }
                }
            }
            
            BranchProtected = true;//if repo.defualt branch !have protecton || has protection and dorsn't have our context as a required success!

            IsPrivate = repo.IsPrivate;

            //is this repo already allocated one of the private seats? or is it public
            CanConfigure = !IsPrivate;
        }

        public bool IsPrivate { get; set; }

        public bool Enabled { get; set; }

        public bool CanConfigure { get; set; }

        public bool WebhookMissing { get; set; }

        public bool BranchProtected { get; set; }
        public bool WebhookMissConfigured { get; private set; }

        internal async Task SaveChanges(RepoStore store)
        {
            store.SetRepoSettings(repoSettings);
            await store.Flush();
        }

        internal async Task UpdateAsEnabled(string authKey, GithubClient.Client client)
        {
            repoSettings.SetAuthToken(authKey);

            if (string.IsNullOrWhiteSpace(repoSettings.WebhookSecret))
            {
                repoSettings.WebhookSecret = Guid.NewGuid().ToString();
            }

            repoSettings.PrivateEnabled = IsPrivate;
            repoSettings.PublicEnabled = !IsPrivate;

            var hook = _hook ?? new WebhokSettings()
            {
                active = true,
                config = new Config
                {
                    url = targetHookUrl,
                    content_type = "json",
                    insecure_ssl = "0"
                },
                events = new string[0],
            };
            hook.events = hookEvents;
            hook.config.secret = repoSettings.WebhookSecret;
            await client.SetHook(_repo.owner.login, _repo.name, hook);//we need it enabled lest update the hook

            //todo try to update the protected branch settings
        }

        internal async Task UpdateAsDisabled(GithubClient.Client client)
        {
            repoSettings.PrivateEnabled = false;
            repoSettings.PublicEnabled = false;
            if (_hook != null)
            {
                await client.DeleteHook(_repo.owner.login, _repo.name, _hook);
            }
        }
    }
}