using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GithubPullTracker.DataStore.Models;

namespace GithubPullTracker.DataStore
{
    public class OwnerConfig
    {

        public OwnerConfig(string owner, OwnerSettings ownerSettings, IEnumerable<RepoSettings> repos)
        {
            Settings = ownerSettings ?? new Models.OwnerSettings(owner);

            Repositories = repos;
        }

        public OwnerSettings Settings { get; set; }
        public IEnumerable<RepoSettings> Repositories { get; set; }

    }
}