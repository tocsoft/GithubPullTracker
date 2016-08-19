using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GithubClient.Models;

namespace GithubPullTracker.Models
{
    public abstract class TimelineEvent<T> : TimelineEvent
    {
        public T Item { get; set; }
    }

    public abstract class TimelineEventGroup<T> : TimelineEvent
    {
        public IEnumerable<T> Items { get; set; }
    }
    public class TimelineEventCommitComment : TimelineEventGroup<CommitComment> { }
    public class TimelineEventComment : TimelineEvent<Comment> { }
    public class TimelineEventCommit : TimelineEventGroup<Commit> { }
    public class TimelineEventOther : TimelineEvent<Event> { }
    public abstract class TimelineEvent
    {
        public static IEnumerable<TimelineEvent> Create(IEnumerable<CommitComment> comments)
        {
            return comments
                .GroupBy(x => new { x.path, x.position })
                .Select(x => new TimelineEventCommitComment()
                {
                    Items = x.ToList(),
                    CreatedAt = x.Select(c => c.created_at).Min(),
                    CreatedBy = null
                });
        }

        public static IEnumerable<TimelineEvent> Create(IEnumerable<Comment> comments)
        {
            return comments
                .Select(x => {
                    return new TimelineEventComment()
                    {
                        Item = x,
                        CreatedAt = x.created_at,
                        CreatedBy = x.user
                    };
                });
        }
        public static IEnumerable<TimelineEvent> Create(IEnumerable<Commit> commits)
        {
            return commits
                .GroupBy(x=>new { x.commit.committer.date, x.commit.committer.email })
                .Select(x => {
                    return new TimelineEventCommit()
                    {
                        Items = x,
                        CreatedAt = x.Min(c=>c.commit.committer.date),
                        CreatedBy = x.First().author
                    };
                });
        }


        public static IEnumerable<TimelineEvent> Create(IEnumerable<Event> events)
        {
            return events
                    .Select(x =>
                    {
                        return new TimelineEventOther()
                        {
                            Item = x,
                            CreatedAt = x.created_at,
                            CreatedBy = x.assigner ?? x.actor
                        };
                    });
        }
        
        public DateTimeOffset CreatedAt { get; set; }

        public User CreatedBy { get; set; }

    }
    
}