using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using GithubClient.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using GithubPullTracker.DataStore.Models;
using System.Text;
using GithubPullTracker.DataStore;

namespace GithubPullTracker.Models
{
    public class PullRequestView
    {
        public PullRequestView(GithubUser user, PullRequest pr, IEnumerable<User> assignees, IEnumerable<CommitApproval> approvals, OwnerConfig ownerSerttings)
        {
            var settings = ownerSerttings.Repositories.Single();
            var applicableApprovals = approvals.Where(x => x.HeadSha == pr.Head.sha && x.Approved);

            var excluded = ownerSerttings.Settings.ExcludedFallbackApproversList()//excluded at the org level (build server accounts etc)
            //all assignees must aprove the pr
            var requiredPeople = pr.assignees.Where(x => x.login != pr.user.login).ToList();
            var fallbackPeople = assignees.Where(x => x.login != pr.user.login)
                .Where(x=> !excluded.Contains(x.login))// remove people who have been excluded
                .ToList();

            if (requiredPeople.Count + fallbackPeople.Count == 0)
            {
                //we don't have anyone that could aprove it, probably a repo with no collaberators where the PR was created by the owner
                fallbackPeople.Add(pr.user);
            }
            
            //lets calculate the list of people that have approved the PR
            var approvedPeople = applicableApprovals.Select(x => x.Login).ToList();

            ApprovedBy = Enumerable.Empty<User>();
            OutstandingAprovers = Enumerable.Empty<User>();
            if (requiredPeople.Any())
            {
                RequiredAprovers = requiredPeople.ToList();

                OutstandingAprovers = requiredPeople.Where(x => !approvedPeople.Contains(x.login)).ToList();
                AllRequired = true;

                //no one outstanding
                this.IsApproved = !OutstandingAprovers.Any();
                if (IsApproved)
                {
                    ApprovedBy = RequiredAprovers;
                }else
                {
                    ApprovedBy = Enumerable.Empty<User>();
                }
            }
            else
            {
                RequiredAprovers = fallbackPeople.ToList();
                this.ApprovedBy = fallbackPeople.Where(x => approvedPeople.Contains(x.login)).ToList();
                if (ApprovedBy.Any())
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
            this.IsMerged = pr.merged;
            ExpectedStatus = IsApproved ? CommitStatus.success : CommitStatus.pending;
            StringBuilder sb = new StringBuilder();
            if (!IsApproved)
            {
                sb.Append("awaiting approval from ");
                if (!AllRequired && OutstandingAprovers.Count() > 1)
                {
                    sb.Append("any of ");
                }

                var first = OutstandingAprovers.First();
                sb.AppendFormat($"{first.login}");
                foreach (var app in OutstandingAprovers.Skip(1))
                {
                    sb.AppendFormat($", {app.login}");
                }
            }
            else
            {

                sb.Append("approved by ");

                var first = ApprovedBy.First();
                sb.AppendFormat($"{first.login}");
                foreach (var app in ApprovedBy.Skip(1))
                {
                    sb.AppendFormat($", {app.login}");
                }
                //list all approving people who mattered
            }
            StatusDescription = sb.ToString();

            IsPrivate = pr.Base.repo.IsPrivate;
            
            

            ApprovalsEnabled = (settings.PrivateEnabled || (settings.PublicEnabled && !IsPrivate));

            // the user can approve if
                // repo is setup
                // not merged
                // and user is on the required approvers list
            CanApprove = ApprovalsEnabled && !IsMerged && RequiredAprovers.Any(x => x.login == user.UserName);
        }


        public string StatusDescription { get; set; }
        public CommitStatus ExpectedStatus { get; set; }
        public int Commits { get; set; }

        public IEnumerable<User> OutstandingAprovers { get; set; }

        public IEnumerable<User> RequiredAprovers { get; set; }

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
        public IEnumerable<User> Assignees { get; private set; }
        public bool IsMerged { get; private set; }
        public DateTime? ClosedDate { get; private set; }
        public DateTime? MergedAt { get; private set; }
        public User MergedBy { get; private set; }
        public User ClosedBy { get; private set; }
        public bool IsPrivate { get; internal set; }
        public IEnumerable<User> ApprovedBy { get; private set; }
        public bool ApprovalsEnabled { get; private set; }
        public bool CanApprove { get; private set; }
    }
}