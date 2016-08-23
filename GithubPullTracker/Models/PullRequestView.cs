﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using GithubPullTracker.DataStore.Models;

namespace GithubPullTracker.Models
{
    public class PullRequestView
    {
        public PullRequestView(GithubUser user, PullRequest pr, IEnumerable<User> assignees, IEnumerable<CommitApproval> approvals)
        {
            var applicableApprovals = approvals.Where(x => x.HeadSha == pr.Head.sha);

            //all assignees must aprove the pr
            var requiredPeople = pr.assignees.Where(x => x.login != pr.user.login).ToList();
            var fallbackPeople = assignees.Where(x => x.login != pr.user.login).ToList();

            if (requiredPeople.Count + fallbackPeople.Count == 0)
            {
                //we don't have anyone that could aprove it, probably a repo with no collaberators where the PR was created by the owner
                fallbackPeople.Add(pr.user);
            }
            
            //lets calculate the list of people that have approved the PR
            var approvedPeople = applicableApprovals.Select(x => x.Login).ToList();
            

            if (requiredPeople.Any())
            {
                RequiredAprovers = requiredPeople.ToList();

                OutstandingAprovers = requiredPeople.Where(x => !approvedPeople.Contains(x.login)).ToList();
                AllRequired = true;

                //no one outstanding
                this.IsApproved = !OutstandingAprovers.Any();
            }
            else
            {
                RequiredAprovers = fallbackPeople.ToList();
                if (fallbackPeople.Any(x => !approvedPeople.Contains(x.login)))
                {
                    //any of the fallbacks have agreed then we are saying its approved
                    this.IsApproved = true;
                }
                else
                {
                    //none hav aagreed s all must be outstanding
                    OutstandingAprovers = fallbackPeople.ToList();

                    this.IsApproved = false;
                }
                AllRequired = false;
                //
            }

            HasUserApproved = approvedPeople.Contains(user.UserName);

            HeadSha = pr.Head.sha;
            BaseSha = pr.Base.sha;

            var path = new Uri(pr.html_url).GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            var parts = path.Split('/');
            this.Title = pr.title;
            this.Number = int.Parse(parts[3]);
            this.RepositoryOwner = parts[0];
            this.RepositoryName = parts[1];
            this.Body = pr.body_html;
            this.CreatedBy = pr.user;
            
            this.Assignees = (pr.assignees ?? Enumerable.Empty<User>()).ToList();

            this.CreatedAt = pr.created_at;
            Status = pr.state;
            Comments = pr.comments;
            Commits = pr.commits;

            HeadName = pr.Head.Ref;
            BaseName = pr.Base.Ref;

            var splitter = ":";
            if (pr.Head.repo.name != pr.Base.repo.name)
            {

                HeadName = pr.Head.repo.name + splitter + HeadName;
                BaseName = pr.Base.repo.name + splitter + pr.Base.label;
                splitter = "/";
            }
            if (pr.Head.user.login != pr.Base.user.login)
            {
                HeadName = pr.Head.user.login + splitter + HeadName;
                BaseName = pr.Base.user.login + splitter + pr.Base.label;
            }

            HeadNameFull = $"{pr.Head.user.login}/{pr.Head.repo.name}:{pr.Head.label}";
            BaseNameFull = $"{pr.Base.user.login}/{pr.Base.repo.name}:{pr.Base.label}";
            Files = pr.changed_files;
            ClosedDate = pr.closed_at;
            ClosedBy = pr.closed_by;
            this.MergedBy = pr.merged_by;
            this.MergedAt = pr.merged_at;
        }

        public int Commits { get; set; }

        public IList<User> OutstandingAprovers { get; set; }

        public IList<User> RequiredAprovers { get; set; }

        public bool AllRequired { get; set; }

        //public IList<User> ApprovedBy { get; set; }

        public bool HasUserApproved { get; set; }

        public bool IsApproved { get; set; }

        public string RepositoryOwner { get; set; }

        public string RepositoryName { get; set; }

        public int Number { get; set; }

        public string BaseSha { get; set; }

        public string HeadSha { get; set; }

        public string Title { get; private set; }
        public string Body { get; private set; }
        public User CreatedBy { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Status { get; private set; }
        public int Files { get; private set; }
        public int Comments { get; private set; }
        public string HeadName { get; private set; }
        public string BaseName { get; private set; }
        public string HeadNameFull { get; private set; }
        public string BaseNameFull { get; private set; }
        public IList<User> Assignees { get; private set; }
        public bool IsMerged { get; private set; }
        public DateTime? ClosedDate { get; private set; }
        public DateTime? MergedAt { get; private set; }
        public User MergedBy { get; private set; }
        public User ClosedBy { get; private set; }
    }
}