using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace GithubPullTracker.DataStore.Models
{
    public class OwnerSettings : TableEntity
    {

        public static string GeneratePartitionKey(string owner)
        {
            return owner;
        }

        public OwnerSettings() { }

        public OwnerSettings(string owner)
        {
            this.PartitionKey = GeneratePartitionKey(owner);
            this.RowKey = "@";
        }

        public string Owner
        {
            get { return PartitionKey; }
        }

        public DateTime SubscriptionExpires { get; set; }

        public int PrivateRepoCount { get; set; }

        List<string> _excludedFallbackApprovers = new List<string>();
        public string ExcludedFallbackApprovers { get { return string.Join(",", _excludedFallbackApprovers); } set
            {
                _excludedFallbackApprovers.Clear();
                if (value != null)
                {
                    _excludedFallbackApprovers.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        public IEnumerable<string> ExcludedFallbackApproversList()
        {
            return _excludedFallbackApprovers;
        }
        public void AddExcludedFallbackApprover(string approver)
        {
            _excludedFallbackApprovers.Add(approver);
        }
        public void RemoveExcludedFallbackApprover(string approver)
        {
            _excludedFallbackApprovers.Remove(approver);
        }
    }
}