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
        public PullRequestCommentsView(PullRequest pr,
            IEnumerable<PullRequestCommit> commits,
            IEnumerable<IssueComment> issueComments, 
            IEnumerable<PullRequestReviewComment> prComment
            ):base(pr)
        {
            this.CommitsList = commits.Select(x => new Commit(x));
            this.FileCommentsList= prComment.Select(x => new FileComment(x));
            this.CommentsList = issueComments.Select(x => new Comment(x));
            this.Events = TimelineEvent.Create(CommitsList)
                .Union(TimelineEvent.Create(FileCommentsList))
                .Union(TimelineEvent.Create(CommentsList)).OrderBy(x => x.CreatedAt).ToList();

            List<User> users = new List<User>();

            if (Assignee != null)
            {
                users.Add(Assignee);
            }

            users.Add(CreatedBy);
            users.AddRange(CommitsList.Select(x=>x.Commiter));
            users.AddRange(CommentsList.Select(x=>x.CreatedBy));
            users.AddRange(FileCommentsList.Select(x=>x.CreatedBy));
            
            Participents = users.GroupBy(x => x.Login).Select(x => x.First());
        }

        public IEnumerable<FileComment> FileCommentsList { get; private set; }
        public IEnumerable<Comment> CommentsList { get; private set; }
        public IEnumerable<Commit> CommitsList { get; private set; }
        public IEnumerable<TimelineEvent> Events { get; private set; }
        public IEnumerable<User> Participents { get; private set; }
    }
}