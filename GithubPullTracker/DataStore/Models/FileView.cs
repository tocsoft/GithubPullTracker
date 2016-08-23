using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace GithubPullTracker.DataStore.Models
{
    public class FileView : TableEntity
    {
        public static string GeneratePartitionKey(string owner, string repo, int number, string login)
        {
            return $"{owner}@{repo}@{number}@{login}";

        }
        public FileView(string owner, string repo, int number, string login, string filesha)
        {
            this.PartitionKey = GeneratePartitionKey(owner, repo, number,login);
            this.RowKey = $"{filesha}";

            this.Repo = repo;
            this.Owner = owner;
            this.Number = number;
            this.FileSha = filesha;
            this.Login = login;
        }
        public FileView() { }
        public string FileSha { get; set; }
        public string Login { get; set; }
        public int Number { get; set; }
        public string Owner { get; set; }
        public string Repo { get; set; }
    }
}