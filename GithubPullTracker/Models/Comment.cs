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
        public User(GithubClient.Models.User x)
        {
            Login = x.login;
            AvatarUrl = x.avatar_url;
            ProfileUrl = x.html_url;
        }
        public User(Octokit.User x)
        {
            AvatarUrl = x.AvatarUrl;
            Login = x.Login;
            ProfileUrl = x.HtmlUrl;
        }
        public string AvatarUrl { get; private set; }
        public string Login { get; private set; }
        public string ProfileUrl { get; private set; }
    }


    public class Comment
    {
        public string Body { get; internal set; }
        public User CreatedBy { get; internal set; }
        public DateTimeOffset CreatedAt { get; internal set; }

        public Comment(IssueComment comment)
        {
            Body = comment.Body;
            CreatedAt = comment.CreatedAt;
            CreatedBy = new User(comment.User);
        }
    }

    public class FileComment
    {
        public FileComment(PullRequestReviewComment comment)
        {
            LineNumber = comment.Position;
            Patch = comment.DiffHunk;
            Path = comment.Path;
            CreatedAt = comment.CreatedAt;
            CreatedBy = new User(comment.User);
            Body = comment.Body;
        }
        public string Body { get; internal set; }
        public User CreatedBy { get; internal set; }
        public DateTimeOffset CreatedAt { get; internal set; }
        public int? LineNumber { get; set; }

        PageMap _map;
        private PageMap map
        {
            get
            {
                return _map ?? (_map = new PageMap(Patch));
            }
        }
        public int? SourceLineNumber
        {
            get
            {
                return map.PatchToSourceLineNumber(LineNumber);
            }
        }
        public int? TargetLineNumber
        {
            get
            {
                return map.PatchToSourceLineNumber(LineNumber);
            }
        }

        public string Patch { get; private set; }
        public string Path { get; private set; }
    }
}