using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GithubClient.Models
{

    public class Event
    {
        public int id { get; set; }
        public string url { get; set; }
        public User actor { get; set; }
        public User user { get; set; }        
        public string commit_id { get; set; }
        public string commit_url { get; set; }
        [JsonProperty("event")]
        public string EventType { get; set; }
        public DateTime created_at { get; set; }
        public Label label { get; set; }
        public User assignee { get; set; }
        public User assigner { get; set; }
        public Milestone milestone { get; set; }
        public RenameDetails rename { get; set; }
        public CommitUser author { get; set; }
        public CommitUser committer { get; set; }
        public string sha { get; set; }
        public string message { get; set; }

    }

    public enum events
    {
        /// <summary>
        /// The issue was closed by the actor. When the commit_id is present, it identifies the commit that closed the issue using "closes /fixes #NN" syntax.
        /// </summary>
        closed,
        /// <summary>
        /// The issue was reopened by the actor.
        /// </summary>
        reopened,
        /// <summary>
        /// The actor subscribed to receive notifications for an issue.
        /// </summary>
        subscribed,
        /// <summary>
        /// The issue was merged by the actor. The `commit_id` attribute is the SHA1 of the HEAD commit that was merged.
        /// </summary>
        merged,
        /// <summary>
        /// The issue was referenced from a commit message.The `commit_id` attribute is the commit SHA1 of where that happened.
        /// </summary>
        referenced,
        /// <summary>
        /// The actor was @mentioned in an issue body.
        /// </summary>
        mentioned,
        /// <summary>
        /// The issue was assigned to the actor.
        /// </summary>
        assigned,
        /// <summary>
        /// The actor was unassigned from the issue.
        /// </summary>
        unassigned,
        /// <summary>
        /// A label was added to the issue.
        /// </summary>
        labeled,
        /// <summary>
        /// A label was removed from the issue.
        /// </summary>
        unlabeled,
        /// <summary>
        /// The issue was added to a milestone.
        /// </summary>
        milestoned,
        /// <summary>
        /// The issue was removed from a milestone.
        /// </summary>
        demilestoned,
        /// <summary>
        /// The issue title was changed.
        /// </summary>
        renamed,
        /// <summary>
        /// The issue was locked by the actor.
        /// </summary>
        locked,
        /// <summary>
        /// The issue was unlocked by the actor.
        /// </summary>
        unlocked,
        /// <summary>
        /// The pull request's branch was deleted.
        /// </summary>
        head_ref_deleted,
        /// <summary>
        /// The pull request's branch was restored.
        /// </summary>
        head_ref_restored

    }

    public class RenameDetails
    {
        public string to { get; set; }
        public string from { get; set; }
    }
}
