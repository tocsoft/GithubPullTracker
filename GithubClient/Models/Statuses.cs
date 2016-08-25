using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{
    public class Statuses
    {
        public string state { get; set; }
        public string sha { get; set; }
        public int total_count { get; set; }
        public Status[] statuses { get; set; }
        public Repo repository { get; set; }
        public string commit_url { get; set; }
        public string url { get; set; }
    }

    public enum CommitStatus
    {
        pending,
        success,
        error,
        failure
    }

    public class Status
    {
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public CommitStatus? state { get; set; }
        public string target_url { get; set; }
        public string description { get; set; }
        public int id { get; set; }
        public string url { get; set; }
        public string context { get; set; }
    }

}
