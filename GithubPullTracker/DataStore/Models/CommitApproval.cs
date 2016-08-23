using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace GithubPullTracker.DataStore.Models
{
    public class CommitApproval : TableEntity
    {

        public static string GeneratePartitionKey(string owner, string repo, int number)
        {
            return $"{owner}@{repo}@{number}";

        }
        public CommitApproval(string owner, string repo, int number, string headsha, string login, bool approved)
        {
            this.PartitionKey = GeneratePartitionKey(owner, repo, number);
            this.RowKey = $"{login}";

            this.Repo = repo;
            this.Owner = owner;
            this.Number = number;
            this.HeadSha = headsha;
            this.Login = login;
            this.Approved = approved;
        }

        public CommitApproval() { }
        public string HeadSha { get; set; }
        public string Login { get; set; }
        public int Number { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }
        public bool Approved { get;  set; }
    }
}