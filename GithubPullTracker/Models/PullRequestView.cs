using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Octokit;

namespace GithubPullTracker.Models
{
    public class PullRequestView
    {
        public PullRequestView(PullRequest pr) 
        {
            HeadSha = pr.Head.Sha;
            BaseSha = pr.Base.Sha;

            var path = pr.HtmlUrl.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            var parts = path.Split('/');
            this.Title = pr.Title;
            this.Number = int.Parse(parts[3]);
            this.RepositoryOwner = parts[0];
            this.RepositoryName = parts[1];
        }
        public PullRequestView(PullRequest pr, string selectedPath) :this(pr)
        {
            CurrentFile = selectedPath;
        }
        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public int Number { get; set; }

        public string BaseSha { get; set; }

        public string HeadSha { get; set; }
        
        public string CurrentFile { get; set; }
        public string Title { get; private set; }
    }
}