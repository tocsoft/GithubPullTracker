using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Octokit;

namespace GithubPullTracker.Models
{
    public class PullRequestResult
    {
        public PullRequestResult(int page, int pagesize, SearchIssuesResult results)
        {
            Page = page;
            PageSize = pagesize;

            PageCount = (int)Math.Ceiling(results.TotalCount / (float)pagesize);

            Items = results.Items.Select(x=>new PullRequestItem(x)).ToList();
        }

        public int PageSize = int.MaxValue;
        public int PageCount = 1;
        public int Page = 1;

        public IEnumerable<PullRequestItem> Items { get; set; }
    }

    public class PullRequestItem
    {
        public PullRequestItem(Octokit.Issue issue) 
        {
            var path = issue.HtmlUrl.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            var parts = path.Split('/');
            this.Title = issue.Title;
            this.Number = int.Parse( parts[3]);
            this.RepositoryOwner = parts[0];
            this.RepositoryName = parts[1];

            if (issue.PullRequest.Merged)
            {
                LastActionedBy = issue.PullRequest.MergedBy;
                LastActioned = issue.PullRequest.MergedAt.Value;
                LastAction = Status.Merged;
            }
            else if (issue.ClosedAt.HasValue)
            {
                LastActionedBy = issue.ClosedBy;
                LastActioned = issue.ClosedAt.Value;
                LastAction = Status.Closed;
            }
            else
            {
                LastActionedBy = issue.User;
                LastActioned = issue.CreatedAt;
                LastAction = Status.Open;
            }

            Comments = issue.Comments;
        }
        

        public string Title { get; private set; }
        public int Number { get; private set; }

        public string RepositoryOwner { get; private set; }

        public string RepositoryName { get; private set; }

        public Octokit.User LastActionedBy { get; private set; }
        public DateTimeOffset LastActioned { get; private set; }
        
        public Status LastAction { get; private set; }
        public int Comments { get; private set; }
    }
    public enum Status
    {
        Open,
        Closed,
        Merged
    }
}