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
        public class CommentDetails
        {
            public string body { get; internal set; }
            public User user { get; internal set; }
            public DateTimeOffset createdAt { get; internal set; }
        }

        public static IEnumerable<Comment> Create(IEnumerable<PullRequestReviewComment> comments)
        {
            return comments
                .GroupBy(x => new { x.Path, x.Position, x.DiffHunk })
                .Select(x => new Comment()
                {
                    isFileComment = true,
                    lineNumber = x.Key.Position,
                    patch = x.Key.DiffHunk,
                    path = x.Key.Path,
                    createdAt = x.Min(c => c.CreatedAt),
                    details = x.Select(c => new CommentDetails
                    {
                        body = c.Body,
                        createdAt = c.CreatedAt,
                        user = new User(c.User)
                    }).ToList()
                }).ToList();
        }

        public static IEnumerable<Comment> Create(IEnumerable<IssueComment> comments)
        {
            return comments
                .Select(c => new Comment()
                {
                    isFileComment = false,
                    lineNumber = null,
                    patch = null,
                    path = null,
                    createdAt = c.CreatedAt,
                    details = new[] { new CommentDetails
                    {
                        body = c.Body,
                        createdAt = c.CreatedAt,
                        user = new User(c.User)
                    } }
                }).ToList();
        }
        public bool isFileComment { get; set; } = false;
        public int? lineNumber { get; set; }

        PageMap _map;
        private PageMap map
        {get
            {
                return _map ?? (_map = new PageMap(patch));
            }
        }
        public int? sourceLineNumber
        {
            get
            {
                return map.PatchToSourceLineNumber(lineNumber);
            }
        }
        public int? targetLineNumber
        {
            get
            {
                return map.PatchToSourceLineNumber(lineNumber);
            }
        }

        public string patch { get; private set; }
        public string path { get; private set; }
        public DateTimeOffset createdAt { get; internal set; }

        public IEnumerable<CommentDetails> details { get; set; }
        private Comment()
        {
        }
     
    }
}