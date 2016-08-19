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
        public T Item { get; set; }
        public IEnumerable<T> Items { get; set; }
    }
    public class TimelineEventCommitComment : TimelineEventGroup<CommitComment>
    {
        public bool OutdatedFile { get; internal set; }
    }
    public class TimelineEventComment : TimelineEvent<Comment> { }
    public class TimelineEventCommit : TimelineEventGroup<Commit>
    {
    }
    public class TimelineEventOther : TimelineEvent<Event> { }
    
        public abstract class TimelineEvent
    {
        public static IEnumerable<TimelineEvent> Create(IEnumerable<CommitComment> comments)
        {
            return comments
                .GroupBy(x => new { x.path, x.original_position, x.diff_hunk })
                .Select(x => new TimelineEventCommitComment()
                {
                    Item = x.First(),
                    Items = x.ToList(),
                    OutdatedFile = !x.First().position.HasValue,
                    CreatedAt = x.Select(c => c.created_at).Min(),
                    CreatedBy = null
                });
        }

        public static IEnumerable<TimelineEvent> Merge(params IEnumerable<TimelineEvent>[] events)
        {
            var all = events.SelectMany(x => x).OrderBy(X => X.CreatedAt).ToList();

            for(var i = 1; i<all.Count; i++)
            {
                var prev = all[i - 1] as TimelineEventCommit;
                var crnt = all[i] as TimelineEventCommit;
                if(prev != null && crnt != null)
                {
                    if(prev.CreatedAt.DayOfYear == crnt.CreatedAt.DayOfYear)
                    {
                        //happend on the same day
                        if(prev.CreatedBy.login == crnt.CreatedBy.login)
                        {
                            prev.Items = prev.Items.Union(crnt.Items);
                            all.Remove(crnt);
                            i--;
                        }
                    }
                }
            }

            return all;
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
                .Select(x => {
                    return new TimelineEventCommit()
                    {
                        Item = x,
                        Items = new[] { x },
                        CreatedAt = x.commit.committer.date,
                        CreatedBy = x.author
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