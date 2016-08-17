using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{

    public class Commit
    {
        public string url { get; set; }
        public string sha { get; set; }
        public string html_url { get; set; }
        public string comments_url { get; set; }
        public CommitDetails commit { get; set; }
        public User author { get; set; }
        public User committer { get; set; }
        public Tree[] parents { get; set; }
    }

    public class CommitDetails
    {
        public string url { get; set; }
        public CommitUser author { get; set; }
        public CommitUser committer { get; set; }
        public string message { get; set; }
        public Tree tree { get; set; }
        public int comment_count { get; set; }
        public Verification verification { get; set; }
    }

    public class CommitUser
    {
        public string name { get; set; }
        public string email { get; set; }
        public DateTime date { get; set; }
    }

    public class Committer
    {
        public string name { get; set; }
        public string email { get; set; }
        public DateTime date { get; set; }
    }

    public class Tree
    {
        public string url { get; set; }
        public string sha { get; set; }
    }

    public class Verification
    {
        public bool verified { get; set; }
        public string reason { get; set; }
        public string signature { get; set; }
        public string payload { get; set; }
    }
    
    
}
