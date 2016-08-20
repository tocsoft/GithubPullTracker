using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;

namespace GithubPullTracker.Models
{
    public class PullRequestCommentsView : PullRequestView
    {
        private readonly IEnumerable<Event> events;

        public PullRequestCommentsView(PullRequest pr,
            Issue issue,
            IEnumerable<Commit> commits,
            IEnumerable<Comment> issueComments,
            IEnumerable<CommitComment> prComment,
            IEnumerable<Event> events
            ) :base(pr)
        {
            this.events = events;
            this.CommitsList = commits;
            this.FileCommentsList = prComment;
            this.CommentsList = issueComments;
            this.Events =
                TimelineEvent.Merge(
                    TimelineEvent.Create(CommitsList),
                    TimelineEvent.Create(FileCommentsList),
                    TimelineEvent.Create(events),
                    TimelineEvent.Create(CommentsList)
                    )
                    .ToList();

            List<User> users = new List<User>();

            if (Assignee != null)
            {
                users.Add(Assignee);
            }

            users.Add(CreatedBy);
            users.AddRange(CommitsList.Select(x=>x.committer));
            users.AddRange(CommitsList.Select(x=>x.author));
            users.AddRange(CommentsList.Select(x=>x.user));
            users.AddRange(FileCommentsList.Select(x=>x.user));
            
            Participents = users.GroupBy(x => x.login).Select(x => x.First());

            Labels = issue.labels ?? Enumerable.Empty<Label>();
            
        }
        public IEnumerable<Label> Labels { get; private set; }

        public IEnumerable<CommitComment> FileCommentsList { get; private set; }
        public IEnumerable<Comment> CommentsList { get; private set; }
        public IEnumerable<Commit> CommitsList { get; private set; }
        public IEnumerable<TimelineEvent> Events { get; private set; }
        public IEnumerable<User> Participents { get; private set; }
    }
}