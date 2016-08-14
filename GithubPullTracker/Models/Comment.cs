using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static GithubPullTracker.Controllers.HomeController;

namespace GithubPullTracker.Models
{
    public class User
    {
        public User(Octokit.User x)
        {
            avatarUrl = x.AvatarUrl;
            login = x.Login;
        }

        public string avatarUrl { get; private set; }
        public string login { get; private set; }
    }

    public class Comment
    {
        public string body { get; private set; }
        public User user { get; private set; }
        public DateTimeOffset createdAt { get; private set; }
        public string path { get; private set; }
        public int? sourceLine { get; private set; }
        public int? targetLine { get; private set; }

        public Comment(PullRequestReviewComment x, PageMap map)
        {
            body = x.Body;
            user = new User(x.User);
            createdAt = x.CreatedAt;
            path = x.Path;
            sourceLine = map.PatchToSourceLineNumber(x.Position);
            targetLine = map.PatchToTargetLineNumber(x.Position);
        }
        public Comment(IssueComment x)
        {
            body = x.Body;
            user = new User(x.User);
            createdAt = x.CreatedAt;
            path = null;
            sourceLine = null;
            targetLine = null;
        }
    }
}