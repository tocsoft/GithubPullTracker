using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubClient.Models
{
    public class Issue
    {
        public int id { get; set; }
        public string url { get; set; }
        public string repository_url { get; set; }
        public string labels_url { get; set; }
        public string comments_url { get; set; }
        public string events_url { get; set; }
        public string html_url { get; set; }
        public int number { get; set; }
        public string state { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string body_html { get; set; }
        public User user { get; set; }
        public Label[] labels { get; set; }
        public User assignee { get; set; }
        public Milestone milestone { get; set; }
        public bool locked { get; set; }
        public int comments { get; set; }
        public PullRequestLinks pull_request { get; set; }
        public object closed_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public User closed_by { get; set; }
    }
    
    public class PullRequestLinks
    {
        public string url { get; set; }
        public string html_url { get; set; }
        public string diff_url { get; set; }
        public string patch_url { get; set; }
    }
    
    public class Label
    {
        public string url { get; set; }
        public string name { get; set; }
        public string color { get; set; }

        public bool AreSame(Label l)
        {
            return l.name == name && l.color == color;
        }
    }

}
