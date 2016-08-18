using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{
    public class SearchResult
    {
        public int total_count { get; set; }
        public bool incomplete_results { get; set; }
        public SearchResultItem[] items { get; set; }
    }

    public class SearchResultItem
    {
        public string url { get; set; }
        public string repository_url { get; set; }
        public string labels_url { get; set; }
        public string comments_url { get; set; }
        public string events_url { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public int number { get; set; }
        public string title { get; set; }
        public User user { get; set; }
        public Label[] labels { get; set; }
        public string state { get; set; }
        public User assignee { get; set; }
        public Milestone milestone { get; set; }
        public int comments { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public DateTime? merged { get; set; }
        public LinksCollection pull_request { get; set; }
        public string body { get; set; }
        public float score { get; set; }
    }

    
    public class Pull_Request
    {
        public string url { get; set; }
        public string html_url { get; set; }
        public string diff_url { get; set; }
        public string patch_url { get; set; }
    }
}
