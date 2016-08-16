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
            this.Details = pr.Body;
            this.Creater = new User(pr.User);
            this.CreatedAt = pr.CreatedAt;
            Status = pr.State;
            Commits = pr.Commits;

            HeadName = pr.Head.Ref;
            BaseName = pr.Base.Ref;

            var splitter = ":";
            if (pr.Head.Repository.Name != pr.Base.Repository.Name)
            {

                HeadName = pr.Head.Repository.Name + splitter + HeadName;
                BaseName = pr.Base.Repository.Name + splitter + pr.Base.Label;
                splitter = "/";
            }
            if (pr.Head.User.Login != pr.Base.User.Login)
            {
                HeadName = pr.Head.User.Login + splitter + HeadName;
                BaseName = pr.Base.User.Login + splitter + pr.Base.Label;
            }

            HeadNameFull = $"{pr.Head.User.Login}/{pr.Head.Repository.Name}:{pr.Head.Label}"; ;
            BaseNameFull = $"{pr.Base.User.Login}/{pr.Base.Repository.Name}:{pr.Base.Label}"; ;

        }
        

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public int Number { get; set; }

        public string BaseSha { get; set; }

        public string HeadSha { get; set; }

        public string Title { get; private set; }
        public string Details { get; private set; }
        public User Creater { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public ItemState Status { get; private set; }
        public int Commits { get; private set; }
        public string HeadName { get; private set; }
        public string BaseName { get; private set; }
        public string HeadNameFull { get; private set; }
        public string BaseNameFull { get; private set; }
    }
}