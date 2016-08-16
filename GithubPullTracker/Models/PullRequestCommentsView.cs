using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Octokit;

namespace GithubPullTracker.Models
{
    public class PullRequestCommentsView : PullRequestView
    {
        public PullRequestCommentsView(PullRequest pr, IEnumerable<Comment> comments):base(pr)
        {
            this.Comments = comments;
        }
        
        public IEnumerable<Comment> Comments { get; private set; }
    }
}