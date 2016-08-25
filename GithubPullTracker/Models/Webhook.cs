using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GithubClient.Models;

namespace GithubPullTracker.Models
{
    public class PullRequestWebhook : Webhook
    {
        public string action { get; set; }
        public PullRequest pull_request { get; set; }
        public string before { get; set; }
        public string after { get; set; }
    }
    public class Webhook
    {
        public Repo repository { get; set; }
        public User sender { get; set; }
    }
}