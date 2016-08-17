using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{

    public class Comment
    {
        public string url { get; set; }
        public int id { get; set; }
        public User user { get; set; }
        public string body { get; set; }
        public string body_html { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string html_url { get; set; }
    }
    
    public class CommitComment : Comment
    {
        public string diff_hunk { get; set; }
        public string path { get; set; }
        public int? position { get; set; }
        public int? original_position { get; set; }
        public string commit_id { get; set; }
        public string original_commit_id { get; set; }
        public string pull_request_url { get; set; }
        public LinksCollection _links { get; set; }
    }
}
