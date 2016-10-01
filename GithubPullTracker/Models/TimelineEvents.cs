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
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();



        public override bool Merge(TimelineEvent evnt)
        {
            return false;
        }
    }
    
    public abstract class TimelineEventGroup<T> : TimelineEvent<T>
    {
        public override bool Merge(TimelineEvent evnt)
        {
            if(evnt is TimelineEventGroup<T>)
            {
                if (CreatedBy.login == evnt.CreatedBy.login)
                {
                    if (UpdatedAt.AddMinutes(60) >= evnt.CreatedAt)
                    {
                        this.Items = this.Items.Union(((TimelineEventGroup<T>)evnt).Items);
                        UpdatedAt = evnt.UpdatedAt;//slide the window
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public class TimelineEventCommitComment : TimelineEvent<CommitComment>
    {
        public bool OutdatedFile { get; internal set; }

    }
    public class TimelineEventComment : TimelineEvent<Comment>
    {
    }
    public class TimelineEventCommit : TimelineEvent<Commit>
    {
        public override bool Merge(TimelineEvent evnt)
        {
            if (evnt is TimelineEventCommit)
            {
                if (CreatedBy.login == evnt.CreatedBy.login)
                {
                    if (UpdatedAt.DayOfYear == evnt.CreatedAt.DayOfYear)
                    {
                        this.Items = this.Items.Union(((TimelineEventCommit)evnt).Items);
                        UpdatedAt = evnt.UpdatedAt;//slide the window
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public class TimelineEventOther : TimelineEvent<Event>
    {
    }

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
                    UpdatedAt = x.Select(c => c.created_at).Max(),
                    CreatedBy = x.First().user
                });
        }

        public static IEnumerable<TimelineEvent> Merge(params IEnumerable<TimelineEvent>[] events)
        {
            var all = events.SelectMany(x => x).OrderBy(X => X.CreatedAt).ToList();

                for (var i = 1; i < all.Count; i++)
                {
                    var prev = all[i - 1];

                    if (prev.Merge(all[i]))
                    {
                        all.Remove(all[i]);
                        i--;
                    }

                }

            return all;
        }

        public static IEnumerable<TimelineEvent> Create(IEnumerable<Comment> comments)
        {
            return comments
                .Select(x =>
                {
                    return new TimelineEventComment()
                    {
                        Item = x,
                        CreatedAt = x.created_at,
                        UpdatedAt = x.created_at,
                        CreatedBy = x.user
                    };
                });
        }
        public static IEnumerable<TimelineEvent> Create(IEnumerable<Commit> commits)
        {
            return commits
                .Select(x =>
                {
                    return new TimelineEventCommit()
                    {
                        Item = x,
                        Items = new[]{ x },
                        CreatedAt = x.commit.committer.date,
                        UpdatedAt = x.commit.committer.date,
                        CreatedBy = x.author
                    };
                });
        }


        public static IEnumerable<TimelineEvent> Create(IEnumerable<Event> events)
        {
            Dictionary<string, string> _eventMap = new Dictionary<string, string> {
                { "labeled", "unlabeled" },
                { "assigned", "unassigned" },
                { "milestoned", "demilestoned" }
            };

            var mergable = new[] { "unlabeled", "unassigned", "demilestoned", "committed" };
            
            var all =  events
                .Where(x=>x!=null)
                    .Select(x=>new TimelineEventOther()
                         {
                             Item = x,
                             Items = new[] { x },
                             CreatedAt = x.committer?.date ?? x.created_at,
                             UpdatedAt = x.committer?.date ?? x.created_at,
                             CreatedBy = x.assigner ?? x.actor ?? x.user
                    }).ToList();
            

            for (var i = 1; i < all.Count; i++)
            {
                var prev = all[i - 1];
                var nxt = all[i];

                var prevType = _eventMap.ContainsKey(prev.Item.EventType) ? _eventMap[prev.Item.EventType] : prev.Item.EventType;
                if (mergable.Contains(prevType)) {
                    var nxtType = _eventMap.ContainsKey(nxt.Item.EventType) ? _eventMap[nxt.Item.EventType] : nxt.Item.EventType;

                    if (prevType == nxtType)
                    {
                        if (prev.CreatedBy?.login == nxt.CreatedBy?.login)
                        {
                            if(prev.UpdatedAt.DayOfYear == prev.UpdatedAt.DayOfYear)
                            {
                                prev.Items = prev.Items.Union(nxt.Items).ToArray();
                                prev.UpdatedAt = nxt.UpdatedAt;
                                all.Remove(nxt);
                                i--;
                            }
                        }
                    }
                }
            }
            var removedEvents = new string[] { };// "committed"};
            return all.Where(x=> !removedEvents.Contains( x.Item.EventType)).ToList();
        }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public User CreatedBy { get; set; }
        
        public abstract bool Merge(TimelineEvent evnt);
        

    }
    
}