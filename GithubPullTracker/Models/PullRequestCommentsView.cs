using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;

namespace GithubPullTracker.Models
{
    public class PullRequestCommentsView
    {
        private readonly IEnumerable<Event> events;

        public PullRequestCommentsView(PullRequestView details,
            Issue issue,
            IEnumerable<CommitComment> prComment,
            IEnumerable<Event> events,
            IEnumerable<Comment> comments,
            IEnumerable<Commit> commits
            )
        {
            this.events = events;
            this.Details = details;
            this.FileCommentsList = prComment;
            this.CommentsList = comments;
            this.Events =
                TimelineEvent.Merge(
                    TimelineEvent.Create(CommentsList),
                    TimelineEvent.Create(FileCommentsList),
                    TimelineEvent.Create(events),
                    TimelineEvent.Create(commits)
                    )
                    .ToList();

            List<User> users = new List<User>();

            
                users.AddRange(details.Assignees);

            users.Add(details.CreatedBy);
            users.AddRange(commits.Select(x=>x.committer));
            users.AddRange(commits.Select(x=>x.author));
            users.AddRange(comments.Select(x=>x.user));
            users.AddRange(FileCommentsList.Select(x=>x.user));
            
            Participents = users.GroupBy(x => x.login).Select(x => x.First());

            Labels = issue.labels ?? Enumerable.Empty<Label>();
            
        }
        public IEnumerable<Label> Labels { get; private set; }

        public IEnumerable<CommitComment> FileCommentsList { get; private set; }
        public IEnumerable<TimelineEvent> Events { get; private set; }
        public IEnumerable<User> Participents { get; private set; }
        public PullRequestView Details { get; private set; }
        public IEnumerable<Comment> CommentsList { get; private set; }
    }
}