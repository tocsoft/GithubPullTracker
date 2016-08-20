using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;

namespace GithubPullTracker.Models
{
    public class PullRequestView
    {
        public PullRequestView(PullRequest pr)
        {
            HeadSha = pr.Head.sha;
            BaseSha = pr.Base.sha;

            var path = new Uri(pr.html_url).GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            var parts = path.Split('/');
            this.Title = pr.title;
            this.Number = int.Parse(parts[3]);
            this.RepositoryOwner = parts[0];
            this.RepositoryName = parts[1];
            this.Details = pr.body_html;
            this.CreatedBy = pr.user;
            if (pr.assignee != null)
            {
                this.Assignee = pr.assignee;
            }
            this.CreatedAt = pr.created_at;
            Status = pr.state;
            Commits = pr.commits;

            HeadName = pr.Head.Ref;
            BaseName = pr.Base.Ref;

            var splitter = ":";
            if (pr.Head.repo.name != pr.Base.repo.name)
            {

                HeadName = pr.Head.repo.name + splitter + HeadName;
                BaseName = pr.Base.repo.name + splitter + pr.Base.label;
                splitter = "/";
            }
            if (pr.Head.user.login != pr.Base.user.login)
            {
                HeadName = pr.Head.user.login + splitter + HeadName;
                BaseName = pr.Base.user.login + splitter + pr.Base.label;
            }

            HeadNameFull = $"{pr.Head.user.login}/{pr.Head.repo.name}:{pr.Head.label}";
            BaseNameFull = $"{pr.Base.user.login}/{pr.Base.repo.name}:{pr.Base.label}";
            Files = pr.changed_files;
            Comments = pr.comments;
            ClosedDate = pr.closed_at;
            ClosedBy = pr.closed_by;
            this.MergedBy = pr.merged_by;
            this.MergedAt = pr.merged_at;
        }
        

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public int Number { get; set; }

        public string BaseSha { get; set; }

        public string HeadSha { get; set; }

        public string Title { get; private set; }
        public string Details { get; private set; }
        public User CreatedBy { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Status { get; private set; }
        public int Commits { get; private set; }
        public int Files { get; private set; }
        public int Comments { get; private set; }
        public string HeadName { get; private set; }
        public string BaseName { get; private set; }
        public string HeadNameFull { get; private set; }
        public string BaseNameFull { get; private set; }
        public User Assignee { get; private set; }
        public bool IsMerged { get; private set; }
        public DateTime? ClosedDate { get; private set; }
        public DateTime? MergedAt { get; private set; }
        public User MergedBy { get; private set; }
        public User ClosedBy { get; private set; }
    }
}