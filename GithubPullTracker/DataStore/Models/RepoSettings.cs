using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace GithubPullTracker.DataStore.Models
{
    public class RepoSettings : TableEntity
    {

        public static string GeneratePartitionKey(string owner)
        {
            return owner;
        }

        public RepoSettings() { }

        public RepoSettings(string owner, string repo)
        {
            this.PartitionKey = GeneratePartitionKey(owner);
            this.RowKey = repo.ToLower();
            Repo = repo;
        }

        public string Owner
        {
            get { return PartitionKey; }
        }

        public string Repo
        {
            get;
            set;
        }

        public bool PrivateEnabled { get; set; } = false;
        public bool PublicEnabled { get; set; } = false;

        public string EncodedAuthorizationToken
        {
            get;
            set;
        }

        public string WebhookSecret { get; set; }

        public string GetAuthToken()
        {
            if (string.IsNullOrWhiteSpace(EncodedAuthorizationToken))
            {
                return null;
            }
            using (var enc = Encrypter.Create())
            {
                return enc.Decrypt(EncodedAuthorizationToken);
            }
        }

        public void SetAuthToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                EncodedAuthorizationToken =  null;
            }
            using (var enc = Encrypter.Create())
            {
                EncodedAuthorizationToken = enc.Encrypt(token);
            }
        }

    }
}