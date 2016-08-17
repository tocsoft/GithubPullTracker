using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GithubPullTracker.Models
{
    public class TimelineEvent
    {
        public static IEnumerable<TimelineEvent> Create(IEnumerable<FileComment> comments)
        {
            return comments
                .OrderBy(x => x.CreatedAt)
                .GroupBy(x => new { x.Path, x.LineNumber })
                .Select(x => {
                    var src = x.First();
                    return new TimelineEvent()
                    {
                        Item = src,
                        Items = x.ToList(),
                        CreatedAt = src.CreatedAt,
                        CreatedBy = src.CreatedBy
                    };
                });
        }

        public static IEnumerable<TimelineEvent> Create(IEnumerable<Comment> comments)
        {
            return comments
                .OrderBy(x => x.CreatedAt)
                .Select(x => {
                    return new TimelineEvent()
                    {
                        Item = x,
                        Items = new[] { x },
                        CreatedAt = x.CreatedAt,
                        CreatedBy = x.CreatedBy
                    };
                });
        }
        public static IEnumerable<TimelineEvent> Create(IEnumerable<Commit> commits)
        {
            return commits
                .OrderBy(x => x.CreatedAt)
                .Select(x => {
                    return new TimelineEvent()
                    {
                        Item = x,
                        Items = new[] { x },
                        CreatedAt = x.CreatedAt,
                        CreatedBy = x.Commiter
                    };
                });
        }

        protected TimelineEvent() { }

        public DateTimeOffset CreatedAt { get; set; }

        public User CreatedBy { get; set; }

        public object Item { get; set; }

        public IEnumerable<object> Items { get; set; }

        public IEnumerable<TValue> ItemsOf<TValue>()
        {
            return Items.OfType<TValue>();
        }
    }
    
}