﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GithubClient.Models
{
    public class PullRequest
    {
        public int id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string diff_url { get; set; }
        public string patch_url { get; set; }
        public string issue_url { get; set; }
        public string commits_url { get; set; }
        public string review_comments_url { get; set; }
        public string review_comment_url { get; set; }
        public string comments_url { get; set; }
        public string statuses_url { get; set; }
        public int number { get; set; }
        public string state { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string body_html { get; set; }
        public User assignee { get; set; }
        public User[] assignees { get; set; }
        public Milestone milestone { get; set; }
        public bool locked { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public DateTime? merged_at { get; set; }
        [JsonProperty("head")]
        public Branch Head { get; set; }
        [JsonProperty("base")]
        public Branch Base { get; set; }
        public LinksCollection _links { get; set; }
        public User user { get; set; }
        public string merge_commit_sha { get; set; }
        public bool merged { get; set; }
        public bool? mergeable { get; set; }
        public User merged_by { get; set; }
        public User closed_by { get; set; }
        
        public int comments { get; set; }
        public int commits { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int changed_files { get; set; }
    }

    public class Milestone
    {
        public string url { get; set; }
        public string html_url { get; set; }
        public string labels_url { get; set; }
        public int id { get; set; }
        public int number { get; set; }
        public string state { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public User creator { get; set; }
        public int open_issues { get; set; }
        public int closed_issues { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public DateTime? due_on { get; set; }
    }
    

    public class Branch
    {
        public string label { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }
        public string sha { get; set; }
        public User user { get; set; }
        public Repo repo { get; set; }
    }

    public class User
    {
        public string login { get; set; }
        public int id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public bool site_admin { get; set; }
    }

    public class Repo
    {
        public int id { get; set; }
        public User owner { get; set; }
        public string name { get; set; }
        public string full_name { get; set; }
        public string description { get; set; }
        public bool _private { get; set; }
        public bool fork { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string archive_url { get; set; }
        public string assignees_url { get; set; }
        public string blobs_url { get; set; }
        public string branches_url { get; set; }
        public string clone_url { get; set; }
        public string collaborators_url { get; set; }
        public string comments_url { get; set; }
        public string commits_url { get; set; }
        public string compare_url { get; set; }
        public string contents_url { get; set; }
        public string contributors_url { get; set; }
        public string deployments_url { get; set; }
        public string downloads_url { get; set; }
        public string events_url { get; set; }
        public string forks_url { get; set; }
        public string git_commits_url { get; set; }
        public string git_refs_url { get; set; }
        public string git_tags_url { get; set; }
        public string git_url { get; set; }
        public string hooks_url { get; set; }
        public string issue_comment_url { get; set; }
        public string issue_events_url { get; set; }
        public string issues_url { get; set; }
        public string keys_url { get; set; }
        public string labels_url { get; set; }
        public string languages_url { get; set; }
        public string merges_url { get; set; }
        public string milestones_url { get; set; }
        public string mirror_url { get; set; }
        public string notifications_url { get; set; }
        public string pulls_url { get; set; }
        public string releases_url { get; set; }
        public string ssh_url { get; set; }
        public string stargazers_url { get; set; }
        public string statuses_url { get; set; }
        public string subscribers_url { get; set; }
        public string subscription_url { get; set; }
        public string svn_url { get; set; }
        public string tags_url { get; set; }
        public string teams_url { get; set; }
        public string trees_url { get; set; }
        public string homepage { get; set; }
        public object language { get; set; }
        public int forks_count { get; set; }
        public int stargazers_count { get; set; }
        public int watchers_count { get; set; }
        public int size { get; set; }
        public string default_branch { get; set; }
        public int open_issues_count { get; set; }
        public bool has_issues { get; set; }
        public bool has_wiki { get; set; }
        public bool has_pages { get; set; }
        public bool has_downloads { get; set; }
        public DateTime pushed_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public Permissions permissions { get; set; }
    }
    
    public class Permissions
    {
        public bool admin { get; set; }
        public bool push { get; set; }
        public bool pull { get; set; }
    }

   
    public class LinksCollection
    {
        public Link self { get; set; }
        public Link html { get; set; }
        public Link issue { get; set; }
        public Link comments { get; set; }
        public Link review_comments { get; set; }
        public Link review_comment { get; set; }
        public Link commits { get; set; }
        public Link statuses { get; set; }
        public Link pull_request { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
    }
}
